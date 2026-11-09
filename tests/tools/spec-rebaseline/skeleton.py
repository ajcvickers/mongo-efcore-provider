"""Insert pass-through overrides for every test a base class has that the port class does not override yet.

usage: python -I skeleton.py <trx> <ClassName> <file.cs>

Run the project once with the class present (it must contain Check_all_tests_overridden). That test fails and lists the missing
overrides in its message; this script reads the message for <ClassName> from the TRX and inserts `=> base.X(args)` overrides after the
`TestHelpers.AssertAllMethodsOverridden(GetType());` line. Re-run the project afterwards (the check then passes).

CAUTION: the suggested signatures in the message come from a relational template and may carry a `bool async` parameter that the
base test does not have. If the build says "no suitable method found to override", fix the signature by hand.
"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

trx, cls, path = sys.argv[1], sys.argv[2], Path(sys.argv[3])
NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
body = None
for r in ET.parse(trx).getroot().iter('{%s}UnitTestResult' % NS['t']):
    n = r.get('testName').split('(')[0].split('.')
    if n[-1] == 'Check_all_tests_overridden' and n[-2].split('+')[-1] == cls and r.get('outcome') == 'Failed':
        m = r.find('.//t:Message', NS)
        txt = (m.text or '') if m is not None else ''
        if 'Missing test overrides' in txt:
            body = txt.split('-- Missing test overrides --', 1)[1]
if body is None:
    sys.exit('no missing-overrides message for ' + cls)

SIG = re.compile(r'public override (?:async )?(?P<ret>Task|void) (?P<name>\w+)\((?P<params>[^)]*)\)')


def args(ps):
    out = []
    for p in [x for x in ps.split(',') if x.strip()]:
        out.append(re.findall(r'[A-Za-z_]\w*', p.split('=')[0].strip())[-1])
    return ', '.join(out)


lines = []
for m in SIG.finditer(body):
    lines += [f"    public override {m['ret']} {m['name']}({m['params']})", f"        => base.{m['name']}({args(m['params'])});", '']

raw = path.read_bytes()
bom = raw.startswith(b'\xef\xbb\xbf')
t = raw.decode('utf-8-sig')
nl = '\r\n' if '\r\n' in t else '\n'
t = t.replace('\r\n', '\n')
key = 'TestHelpers.AssertAllMethodsOverridden(GetType());'
assert t.count(key) == 1, 'need exactly one Check_all_tests_overridden in ' + str(path)
i = t.index(key) + len(key)
t = t[:i] + '\n\n' + '\n'.join(lines).rstrip('\n') + t[i:]
path.write_bytes((b'\xef\xbb\xbf' if bom else b'') + t.replace('\n', nl).encode('utf-8'))
print('inserted', len(lines) // 3, 'overrides into', path.name)
