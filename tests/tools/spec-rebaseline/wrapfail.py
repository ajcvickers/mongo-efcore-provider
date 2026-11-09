"""Wrap every plain override that FAILS in a given run with `SpecFails.ThrowsAsync/Throws(() => base.X(...))` (any exception) and a
`// Fails:` tag. Tickets are grouped by failure message and numbered from --first within your assigned range.

usage: python -I wrapfail.py --repo <worktree root> --first 800 --trx r1-ef10.trx [--trx r1-ef9.trx ...] \
           --class XMongoTest=tests/MongoDB.EntityFrameworkCore.SpecificationTests/Query/X.cs [--class ...]

The union of failing tests over all given TRX files is wrapped. Afterwards run all versions again: a wrapped test that PASSES on some
version now fails there ("Expected ... but the test passed"); the rebaseline tool turns those into per-version arms.
Prints the ticket table (id, count, sample message). Put those rows in your final report.
"""
import argparse
import collections
import re
import xml.etree.ElementTree as ET
from pathlib import Path

ap = argparse.ArgumentParser()
ap.add_argument('--repo', required=True)
ap.add_argument('--first', type=int, required=True)
ap.add_argument('--trx', action='append', required=True)
ap.add_argument('--class', dest='classes', action='append', required=True)
a = ap.parse_args()
NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
files = dict(x.split('=', 1) for x in a.classes)


def key_of(msg):
    first = msg.replace('\r', '').strip().split('\n')[0].strip()
    first = re.sub(r"'[^']*'", "'X'", first)
    first = re.sub(r'\d+', 'N', first)
    return first[:100]


failing = {}
for trx in a.trx:
    for r in ET.parse(trx).getroot().iter('{%s}UnitTestResult' % NS['t']):
        if r.get('outcome') != 'Failed':
            continue
        n = r.get('testName').split('(')[0].split('.')
        cls, meth = n[-2].split('+')[-1], n[-1]
        if cls not in files or meth == 'Check_all_tests_overridden':
            continue
        m = r.find('.//t:Message', NS)
        failing.setdefault((cls, meth), key_of((m.text or '') if m is not None else ''))

tickets = collections.OrderedDict()


def ticket(k):
    if k not in tickets:
        tickets[k] = [f'EF-X{a.first + len(tickets)}', 0]
    tickets[k][1] += 1
    return tickets[k][0]


SIG = re.compile(r'^(?P<ind>\s*)public override (?P<async>async )?(?P<ret>Task|void) (?P<name>\w+)\((?P<params>.*)\)\s*$')


def args(ps):
    out = []
    depth = 0
    cur = ''
    for ch in ps:
        if ch in '<([':
            depth += 1
        elif ch in '>)]':
            depth -= 1
        if ch == ',' and depth == 0:
            out.append(cur)
            cur = ''
        else:
            cur += ch
    if cur.strip():
        out.append(cur)
    return ', '.join(re.findall(r'[A-Za-z_]\w*', p.split('=')[0].strip())[-1] for p in out)


done = 0
for cls, rel in files.items():
    p = Path(a.repo) / rel
    raw = p.read_bytes()
    bom = raw.startswith(b'\xef\xbb\xbf')
    t = raw.decode('utf-8-sig')
    nl = '\r\n' if '\r\n' in t else '\n'
    lines = t.replace('\r\n', '\n').split('\n')
    edits = []
    for i, l in enumerate(lines):
        m = SIG.match(l)
        if not m or (cls, m['name']) not in failing:
            continue
        j = i + 1
        ind = m['ind']
        s = lines[j].strip()
        if s.startswith('{') and s.endswith('}'):
            end = j
        elif s == '{':
            k = j + 1
            while lines[k].rstrip() != ind + '}':
                k += 1
            end = k
        else:
            k = j
            while not lines[k].rstrip().endswith(';'):
                k += 1
            end = k
        body = '\n'.join(lines[i:end + 1])
        if 'SpecFails.' in body or 'base.' + m['name'] + '(' not in body:
            continue
        start = i
        while start > 0 and lines[start - 1].strip().startswith('//'):
            start -= 1
        msg = failing[(cls, m['name'])]
        tk = ticket(msg)
        call = f"base.{m['name']}({args(m['params'])})"
        if m['ret'] == 'Task':
            new = [f"{ind}// Fails: {msg} {tk}", f"{ind}public override Task {m['name']}({m['params']})", f"{ind}    => SpecFails.ThrowsAsync(() => {call});"]
        else:
            new = [f"{ind}// Fails: {msg} {tk}", f"{ind}public override void {m['name']}({m['params']})", f"{ind}    => SpecFails.Throws(() => {call});"]
        edits.append((start, end, new))
    for start, end, new in sorted(edits, reverse=True):
        lines[start:end + 1] = new
        done += 1
    p.write_bytes((b'\xef\xbb\xbf' if bom else b'') + '\n'.join(lines).replace('\n', nl).encode('utf-8'))

print('wrapped', done, 'overrides;', len(failing), 'failing (class, method) pairs seen')
print('TICKETS (id | methods | sample message):')
for k, (tid, n) in tickets.items():
    print(f'  {tid} | {n} | {k}')
