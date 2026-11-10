### Task 0: Capture the baseline

**Files:** none (records numbers only).

- [ ] **Step 1: Build**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"` → expect `0 Error(s)`.

- [ ] **Step 2: Record the native-only failure list for the target class and for the whole Query spec sweep**

```bash
S=$(mktemp -d)
MONGODB_EF_NATIVE_ONLY=1 dotnet test $SPEC -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~NorthwindAggregateOperatorsQueryMongoTest" --logger "trx;LogFileName=$S/agg.trx" | tail -2
MONGODB_EF_NATIVE_ONLY=1 dotnet test $SPEC -c "Debug EF10" --no-build \
  --filter "FullyQualifiedName~Query" --logger "trx;LogFileName=$S/query.trx" | tail -2
python3 - "$S/query.trx" <<'EOF' > $S/baseline-native-failures.txt
import sys, xml.etree.ElementTree as ET
ns='{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
for u in ET.parse(sys.argv[1]).getroot().iter(ns+'UnitTestResult'):
    if u.get('outcome')=='Failed': print(u.get('testName'))
EOF
sort -o $S/baseline-native-failures.txt $S/baseline-native-failures.txt; wc -l $S/baseline-native-failures.txt; echo $S
```

Expected: the target class shows `Failed: 48, Passed: 375`. Keep `$S` for later diffs, re-running the same script with a new trx after each task.

---

