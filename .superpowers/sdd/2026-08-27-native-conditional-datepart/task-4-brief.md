### Task 4: Translation — date-part member chains (`MongoExpressionTranslator`)

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs` (add `TryTranslateDateTimeMember` + `DatePartsByMemberName`; call it from `TranslateOperand`)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoConditionalAndDateTimeTranslatorTests.cs` (same file as Task 3 — one cohesive test file for both new capabilities)

**Interfaces:**
- Consumes: `MongoDatePartExpression`, `MongoDateTimeOffsetLocalExpression`, `MongoDatePart`, `MongoElementRefExpression` (existing) from Task 1; `TryResolveMember` (existing, private instance method).
- Produces: `TryTranslateDateTimeMember(Expression node, out MongoExpression? result)` — a new private instance method on `MongoExpressionTranslator`, called from `TranslateOperand`.

- [ ] **Step 1: Write the failing test**

Append to `MongoConditionalAndDateTimeTranslatorTests.cs` from Task 3:

```csharp
    [Fact]
    public void Plain_DateTime_Year_translates_directly_with_no_offset_reconstruction()
    {
        var (translator, body) = BuildValueBody(r => r.Occurred.Year);

        Assert.True(translator.TryTranslateValue(body, out var result));

        var datePart = Assert.IsType<MongoDatePartExpression>(result);
        Assert.Equal(MongoDatePart.Year, datePart.Part);
        Assert.IsType<MongoFieldExpression>(datePart.Operand);
    }

    [Fact]
    public void DateTimeOffset_Year_wraps_local_time_reconstruction()
    {
        var (translator, body) = BuildValueBody(r => r.OccurredOffset!.Value.Year);

        Assert.True(translator.TryTranslateValue(body, out var result));

        var datePart = Assert.IsType<MongoDatePartExpression>(result);
        Assert.Equal(MongoDatePart.Year, datePart.Part);
        var local = Assert.IsType<MongoDateTimeOffsetLocalExpression>(datePart.Operand);
        Assert.Equal("OccurredOffset", local.Operand.ElementName);
    }

    [Fact]
    public void DateTimeOffset_Value_DateTime_Date_three_hop_chain_composes()
    {
        // The motivating shape: BuiltInDataTypesMongoTest.Optional_datetime_reading_null_from_database.
        var (translator, body) = BuildValueBody(r => r.OccurredOffset!.Value.DateTime.Date);

        Assert.True(translator.TryTranslateValue(body, out var result));

        var datePart = Assert.IsType<MongoDatePartExpression>(result);
        Assert.Equal(MongoDatePart.Date, datePart.Part);
        var local = Assert.IsType<MongoDateTimeOffsetLocalExpression>(datePart.Operand);
        Assert.Equal("OccurredOffset", local.Operand.ElementName);
    }

    [Fact]
    public void DateTimeOffset_UtcDateTime_reads_the_raw_UTC_subfield_with_no_offset_addition()
    {
        var (translator, body) = BuildValueBody(r => r.OccurredOffset!.Value.UtcDateTime);

        Assert.True(translator.TryTranslateValue(body, out var result));

        var elementRef = Assert.IsType<MongoElementRefExpression>(result);
        Assert.Equal("OccurredOffset.DateTime", elementRef.Path);
    }

    [Fact]
    public void DateTimeOffset_DateTime_alone_returns_the_local_reconstruction_as_a_value()
    {
        var (translator, body) = BuildValueBody(r => r.OccurredOffset!.Value.DateTime);

        Assert.True(translator.TryTranslateValue(body, out var result));

        Assert.IsType<MongoDateTimeOffsetLocalExpression>(result);
    }

    [Fact]
    public void TimeOfDay_declines()
    {
        var (translator, body) = BuildValueBody(r => (object)r.Occurred.TimeOfDay);

        Assert.False(translator.TryTranslateValue(body, out _));
    }

    [Fact]
    public void Conditional_branch_with_a_date_part_translates()
    {
        var (translator, body) = BuildValueBody(
            r => r.OccurredOffset == null ? (DateTime?)null : r.OccurredOffset.Value.DateTime.Date);

        Assert.True(translator.TryTranslateValue(body, out var result));

        var conditional = Assert.IsType<MongoConditionalExpression>(result);
        Assert.IsType<MongoDatePartExpression>(conditional.IfFalse);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoConditionalAndDateTimeTranslatorTests"`
Expected: FAIL — the 7 new tests fail (`TryTranslateValue` returns `false` for every date-member shape today). `TimeOfDay_declines` passes already (everything declines today) — it becomes a real regression guard once Step 3 lands.

- [ ] **Step 3: Write the implementation**

In `MongoExpressionTranslator.cs`, add the member-name-to-part lookup as a `private static readonly` field near `WideningNumericConversions` (around line 184, same section of static lookup tables):

```csharp
    // Mirrors MongoEFToLinqTranslatingExpressionVisitor.DateTimeOffsetComponentMembers' member set, minus
    // TimeOfDay (see MongoDatePart's own remarks for why that one is excluded), and applies identically to a
    // plain DateTime receiver, which needs none of that visitor's offset-reconstruction dance.
    private static readonly Dictionary<string, MongoDatePart> DatePartsByMemberName = new()
    {
        [nameof(DateTime.Year)] = MongoDatePart.Year,
        [nameof(DateTime.Month)] = MongoDatePart.Month,
        [nameof(DateTime.Day)] = MongoDatePart.Day,
        [nameof(DateTime.Hour)] = MongoDatePart.Hour,
        [nameof(DateTime.Minute)] = MongoDatePart.Minute,
        [nameof(DateTime.Second)] = MongoDatePart.Second,
        [nameof(DateTime.Millisecond)] = MongoDatePart.Millisecond,
        [nameof(DateTime.DayOfWeek)] = MongoDatePart.DayOfWeek,
        [nameof(DateTime.DayOfYear)] = MongoDatePart.DayOfYear,
        [nameof(DateTime.Date)] = MongoDatePart.Date
    };
```

Add the new resolver method, right after `TryTranslateValue` (after its closing brace, around line 146):

```csharp
    /// <summary>
    /// Resolves a <c>DateTime</c>/<c>DateTimeOffset</c> member-access chain (<c>.Year</c>, <c>.Date</c>,
    /// <c>.DateTime</c>, <c>.UtcDateTime</c>, etc.) to a native date expression. Recurses into the receiver so
    /// a multi-hop chain composes correctly — e.g. <c>x.Dto.Value.DateTime.Date</c> resolves the field first
    /// (the <c>.Value</c> hop is peeled generically by <see cref="TryResolveMember"/>), wraps it in
    /// <see cref="MongoDateTimeOffsetLocalExpression"/> for <c>.DateTime</c>, then wraps THAT in
    /// <see cref="MongoDatePartExpression"/> for <c>.Date</c>.
    /// </summary>
    /// <remarks>
    /// The ULTIMATE receiver (the innermost hop) must resolve to a plain stored field via
    /// <see cref="TryResolveMember"/> — a receiver that is itself an arbitrary COMPUTED value (a conditional
    /// branch, arithmetic) has no MQL sub-field-access form for the <c>DateTimeOffset</c> reconstruction case
    /// and declines here rather than being mistranslated. This does not limit chains of date-member hops
    /// themselves (those recurse through this same method), only what the chain may ultimately be rooted on.
    /// </remarks>
    private bool TryTranslateDateTimeMember(Expression node, [NotNullWhen(true)] out MongoExpression? result)
    {
        result = null;
        if (node is not MemberExpression { Expression: { } receiver } member)
            return false;

        var receiverType = Nullable.GetUnderlyingType(receiver.Type) ?? receiver.Type;
        if (receiverType != typeof(DateTime) && receiverType != typeof(DateTimeOffset))
            return false;

        MongoExpression receiverExpr;
        if (TryResolveMember(receiver, out var property, out var fieldPath))
        {
            receiverExpr = new MongoFieldExpression(property, fieldPath!);
        }
        else if (TryTranslateDateTimeMember(receiver, out var nestedDateTimeExpr))
        {
            receiverExpr = nestedDateTimeExpr;
        }
        else
        {
            return false;
        }

        if (receiverType == typeof(DateTimeOffset))
        {
            switch (member.Member.Name)
            {
                case nameof(DateTimeOffset.UtcDateTime):
                    if (receiverExpr is not MongoFieldExpression utcField)
                        return false;
                    result = new MongoElementRefExpression(utcField.ElementName + ".DateTime", typeof(DateTime));
                    return true;

                case nameof(DateTimeOffset.DateTime):
                case nameof(DateTimeOffset.LocalDateTime):
                    if (receiverExpr is not MongoFieldExpression localField)
                        return false;
                    result = new MongoDateTimeOffsetLocalExpression(localField);
                    return true;

                default:
                    if (!DatePartsByMemberName.TryGetValue(member.Member.Name, out var offsetPart))
                        return false;
                    if (receiverExpr is not MongoFieldExpression partField)
                        return false;
                    result = new MongoDatePartExpression(new MongoDateTimeOffsetLocalExpression(partField), offsetPart);
                    return true;
            }
        }

        // Plain DateTime receiver: no offset reconstruction, extract directly.
        if (!DatePartsByMemberName.TryGetValue(member.Member.Name, out var plainPart))
            return false;

        result = new MongoDatePartExpression(receiverExpr, plainPart);
        return true;
    }
```

Then call it from `TranslateOperand`, right after the new `ConditionalExpression` block from Task 3 (before line 882's `if (TryResolveMember(node, ...))` — order relative to that check doesn't matter, since a date-member chain's own `MemberExpression` never matches `TryResolveMember`'s fast path directly, but placing it adjacent to the other new case keeps the two Task-3/4 additions visually grouped):

```csharp
        // A DateTime/DateTimeOffset member-access or date-part chain (.Year, .Date, .DateTime, etc.). Must run
        // regardless of whether TryResolveMember below would also be tried — it never matches this shape
        // anyway (the member's OWN receiver is never the query parameter directly for a date-member chain),
        // but this is placed here to keep it visually adjacent to the ConditionalExpression case above.
        if (TryTranslateDateTimeMember(node, out var dateTimeMember))
            return dateTimeMember;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10" && dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoConditionalAndDateTimeTranslatorTests"`
Expected: PASS, all 11 tests (4 from Task 3 + 7 from this task).

- [ ] **Step 5: Run the full unit test suite to check for regressions**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~MongoDB.EntityFrameworkCore.UnitTests"`
Expected: PASS, zero regressions.

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs \
        tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoConditionalAndDateTimeTranslatorTests.cs
git commit -m "Translate DateTime/DateTimeOffset member-access chains to MongoDatePartExpression"
```

---

