### Task 23: Assert the event payload, not just the rendered message

Every diagnostics test string-matches `spyLogger.GetLogMessageByEventId(...)`. Nothing subscribes to a `DiagnosticSource` or interceptor, so `MongoBulkWriteEventData` — including `TargetCount`, `MatchedCount` and `Command`, the new public surface — is never verified as event data.

**Files:**
- Create: `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsEventDataTests.cs`

**Interfaces:**
- Consumes: the event data shape from Task 4.
- Produces: nothing.

- [ ] **Step 1: Write the tests**

Create the file with the standard licence header, then:

```csharp
// The rendered log message is a convenience; MongoBulkWriteEventData is the contract external
// DiagnosticSource subscribers consume. These tests assert the payload directly.

#if !EF8

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using Xunit;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

[XUnitCollection("QueryTests")]
public class BulkOperationsEventDataTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    private class Order
    {
        public ObjectId _id { get; set; }
        public string Status { get; set; } = null!;
        public int Quantity { get; set; }
    }

    private sealed class BulkEventCollector : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        public List<(string Name, MongoBulkWriteEventData Data)> Events { get; } = [];

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == DbLoggerCategory.Name)
            {
                listener.Subscribe(this);
            }
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Value is MongoBulkWriteEventData data)
            {
                Events.Add((value.Key, data));
            }
        }

        public void OnCompleted() { }
        public void OnError(System.Exception error) { }
    }

    [Fact]
    public void Two_phase_update_event_data_carries_target_matched_and_modified_counts()
    {
        var collector = new BulkEventCollector();
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);

        var collection = database.CreateCollection<Order>(
            nameof(Two_phase_update_event_data_carries_target_matched_and_modified_counts));
        collection.InsertMany(
        [
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 },
            new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 2 }
        ]);

        using var db = SingleEntityDbContext.Create(collection);

        var updated = db.Entities.OrderBy(o => o.Quantity).Take(2)
            .ExecuteUpdate(s => s.SetProperty(o => o.Status, "shipped"));

        Assert.Equal(2, updated);

        var executing = collector.Events
            .Where(e => e.Name.Contains(nameof(MongoEventId.ExecutingBulkUpdate)))
            .Select(e => e.Data).ToList();
        Assert.NotEmpty(executing);
        Assert.Equal(2, Assert.Single(executing).TargetCount);

        var executed = collector.Events
            .Where(e => e.Name.Contains(nameof(MongoEventId.ExecutedBulkUpdate)))
            .Select(e => e.Data).ToList();
        Assert.Equal(2, Assert.Single(executed).MatchedCount);
        Assert.Equal(2, Assert.Single(executed).ModifyCount);
    }

    [Fact]
    public void Single_command_delete_event_data_has_no_target_count_and_a_redacted_command()
    {
        var collector = new BulkEventCollector();
        using var subscription = DiagnosticListener.AllListeners.Subscribe(collector);

        var collection = database.CreateCollection<Order>(
            nameof(Single_command_delete_event_data_has_no_target_count_and_a_redacted_command));
        collection.InsertOne(new Order { _id = ObjectId.GenerateNewId(), Status = "open", Quantity = 1 });

        using var db = SingleEntityDbContext.Create(collection);

        db.Entities.Where(o => o.Status == "open").ExecuteDelete();

        var executing = collector.Events
            .Where(e => e.Name.Contains(nameof(MongoEventId.ExecutingBulkDelete)))
            .Select(e => e.Data).ToList();

        var data = Assert.Single(executing);
        Assert.Null(data.TargetCount);
        Assert.False(data.LogSensitiveData);
        Assert.Equal("?", data.Command);
    }
}
```

The diagnostic listener name and event-key format need checking against how EF dispatches — `DiagnosticSource` keys are the event-id names. Verify by running the test and printing `collector.Events.Select(e => e.Name)` once; adjust the `Contains` predicates to whatever EF actually emits. Do not guess twice: print, then fix.

- [ ] **Step 2: Run them**

```bash
dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" \
  --filter "FullyQualifiedName~BulkOperationsEventDataTests"
```

Expected: PASS after the key-format adjustment. Note that `DiagnosticListener.AllListeners` is process-wide, so these tests must not run in parallel with other bulk tests — the `[XUnitCollection("QueryTests")]` attribute already serialises them within that collection; confirm that collection is not marked parallelisable.

- [ ] **Step 3: Commit**

```bash
git add tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/BulkOperationsEventDataTests.cs
git commit -m "test: assert MongoBulkWriteEventData payloads via DiagnosticSource, not only log messages"
```

---

