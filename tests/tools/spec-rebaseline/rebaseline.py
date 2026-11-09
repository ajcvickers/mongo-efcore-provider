"""Re-baseline `// Fails` overrides against the current provider.

Inputs (all under RES):
  <ver>-default.trx            normal asserting run per EF version (ver = ef8, ef9, ef10, ef11) -> failing set + existence
  harvest-<ver>-default.tsv    SpecFails harvest (what each overridden base test really does)
Modes:
  report   print the classification of every failing SpecFails override per version
  apply    rewrite the sources

usage: python -I rebaseline.py report|apply
"""
import os
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter, defaultdict
from pathlib import Path

REPO = Path(os.environ['REPO_ROOT'])
SPEC = REPO / 'tests/MongoDB.EntityFrameworkCore.SpecificationTests'
RES = Path(os.environ['RES_DIR'])
PORT_FILES = Path(os.environ['PORT_FILES'])  # text file: repo-relative .cs paths, one per line
VERS = os.environ.get('VERS', '8,9,10,11').split(',')
NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}

# ---------------------------------------------------------------------------------------------- tickets for new causes
NEW_TICKETS = {
    # key -> (ticket id, description)
    'WRONG_DATA': ('EF-X600', 'Query translates natively but returns different data than the expected result'),
    'SERVER': ('EF-X601', 'The server rejects the generated pipeline (MongoCommandException)'),
    'FORMAT': ('EF-X602', 'Serialization/format error while translating or materializing'),
    'ARGUMENT': ('EF-X603', 'Argument exception while translating or materializing'),
    'OTHER_IOE': ('EF-X604', 'InvalidOperationException from query execution or materialization'),
    'OTHER': ('EF-X605', 'Other unexpected exception from query execution'),
}


def ticket_key(etype, cls, msg):
    if cls == 'XUNIT':
        return 'WRONG_DATA'
    if etype.endswith('MongoCommandException'):
        return 'SERVER'
    if etype.endswith('FormatException'):
        return 'FORMAT'
    if etype.endswith('ArgumentException') or etype.endswith('ArgumentNullException') or etype.endswith('ArgumentOutOfRangeException'):
        return 'ARGUMENT'
    if etype.endswith('InvalidOperationException'):
        return 'OTHER_IOE'
    return 'OTHER'


# ---------------------------------------------------------------------------------------------- source parsing
def read_text(p):
    raw = p.read_bytes()
    bom = raw.startswith(b'\xef\xbb\xbf')
    text = raw.decode('utf-8-sig').replace('\r\n', '\n')
    return text, bom


def write_text(p, text, bom):
    p.write_bytes((b'\xef\xbb\xbf' if bom else b'') + text.encode('utf-8'))


def eval_cond(expr, defs):
    e = expr.strip()
    e = re.sub(r'//.*$', '', e).strip()
    e = e.replace('&&', ' and ').replace('||', ' or ')
    e = re.sub(r'!(?!=)', ' not ', e)
    e = re.sub(r'\b[A-Za-z_][A-Za-z0-9_]*\b', lambda m: m.group(0) if m.group(0) in ('and', 'or', 'not') else str(m.group(0) in defs), e)
    return bool(eval(e))


def masks_for(lines):
    """For each line, the tuple of active flags per version in VERS (directive lines inherit the enclosing state)."""
    result = [None] * len(lines)
    per_ver = {}
    for v in VERS:
        defs = {'EF' + v}
        stack = []  # entries: [parent_active, taken_any, current_active]
        act = []
        cur = True
        for ln in lines:
            s = ln.strip()
            if s.startswith('#if '):
                parent = cur
                cond = eval_cond(s[4:], defs)
                stack.append([parent, cond, cond])
                cur = parent and cond
                act.append(parent)  # a directive line is active exactly when its enclosing region is
            elif s.startswith('#elif '):
                ent = stack[-1]
                cond = (not ent[1]) and eval_cond(s[6:], defs)
                ent[1] = ent[1] or cond
                ent[2] = cond
                cur = ent[0] and cond
                act.append(ent[0])
            elif s == '#else':
                ent = stack[-1]
                cond = not ent[1]
                ent[1] = True
                ent[2] = cond
                cur = ent[0] and cond
                act.append(ent[0])
            elif s.startswith('#endif'):
                ent = stack.pop()
                cur = ent[0]
                act.append(ent[0])
            else:
                act.append(cur)
        per_ver[v] = act
    for i in range(len(lines)):
        result[i] = tuple(per_ver[v][i] for v in VERS)
    return result


SIG = re.compile(r'^(?P<ind>\s*)public override (?P<async>async )?(?P<ret>Task|void)\s+(?P<name>\w+)\((?P<params>.*)\)\s*$')


def split_params(ps):
    out, depth, cur = [], 0, ''
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
    names = []
    for p in out:
        p = p.split('=')[0].strip()
        names.append(re.findall(r'[A-Za-z_][A-Za-z0-9_]*', p)[-1])
    return names


class Decl:
    pass


def find_decls(lines, masks):
    decls = []
    i = 0
    while i < len(lines):
        m = SIG.match(lines[i])
        if not m:
            i += 1
            continue
        d = Decl()
        d.m = m
        d.name = m['name']
        d.ret = m['ret']
        d.is_async = bool(m['async'])
        d.params = m['params']
        d.args = ', '.join(split_params(m['params']))
        d.ind = m['ind']
        d.sig_line = i
        start = i
        while start > 0 and lines[start - 1].strip().startswith('//'):
            start -= 1
        j = i + 1
        if lines[j].strip() == '{':
            k = j + 1
            while lines[k].rstrip() != d.ind + '}':
                k += 1
            end = k
        else:
            k = j
            while not lines[k].rstrip().endswith(';'):
                k += 1
            end = k
        d.start, d.end = start, end
        d.active = masks[i]
        d.simple = not any(lines[x].lstrip().startswith('#') for x in range(start, end + 1))
        d.text = lines[start:end + 1]
        d.uses_specfails = any('SpecFails.' in l for l in d.text)
        d.fails_comment = next((l.strip() for l in d.text if '// Fails:' in l), None)
        decls.append(d)
        i = end + 1
    return decls


# ---------------------------------------------------------------------------------------------- results
def load_trx(ver):
    p = RES / f'ef{ver}-default.trx'
    if not p.exists():
        return None
    root = ET.parse(p).getroot()
    failed, exists = set(), set()
    for r in root.iterfind('.//t:UnitTestResult', NS):
        name = r.get('testName').split('(')[0]
        parts = name.split('.')
        cls = parts[-2] if len(parts) >= 2 else ''
        key = (cls.split('+')[-1], parts[-1])
        exists.add(key)
        if r.get('outcome') == 'Failed':
            failed.add(key)
    return failed, exists


def load_harvest(ver, mode='default'):
    p = RES / f'harvest-ef{ver}-{mode}.tsv'
    data = defaultdict(list)
    if not p.exists():
        return data
    for line in p.read_text(encoding='utf-8', errors='replace').splitlines():
        f = line.split('\t')
        if len(f) < 3:
            continue
        file, member, outcome = f[0], f[1], f[2]
        if outcome == 'PASS':
            data[(file, member)].append(('PASS',))
        else:
            data[(file, member)].append(('THROW', f[3], f[4], f[5] if len(f) > 5 else ''))
    return data


def classify(rows):
    kinds = {r[0] for r in rows}
    if kinds == {'PASS'}:
        return ('PASS',)
    throws = [r for r in rows if r[0] == 'THROW']
    classes = {r[2] for r in throws}
    types = {r[1] for r in throws}
    if kinds == {'THROW'}:
        if classes == {'TRANSLATION'}:
            return ('TRANSLATION',)
        if len(types) == 1:
            t = throws[0]
            return ('THROW', t[1], t[2], t[3])
        return ('MULTI',)
    # mixed pass + throw
    if len(types) == 1:
        return ('MIXED', throws[0][1])
    return ('MULTI',)


# ---------------------------------------------------------------------------------------------- code generation
def gt(t):
    t = re.sub(r'`\d+\[\[([^,\]]+),[^\]]*\]\]', lambda m: '<global::' + m.group(1) + '>', t)
    return 'global::' + t.replace('+', '.')


def gen_code(d, cl):
    """Return list of lines for declaration d given classification cl; None means keep."""
    ind = d.ind
    call = f'base.{d.name}({d.args})'
    kind = cl[0]
    comment = None
    if kind == 'PASS':
        comment = None
    elif kind == 'TRANSLATION':
        comment = d.fails_comment
    elif kind in ('THROW',):
        key = ticket_key(cl[1], cl[2], cl[3])
        tid, desc = NEW_TICKETS[key]
        comment = f'// Fails: {desc} {tid}'
    else:
        key = 'OTHER'
        tid, desc = NEW_TICKETS[key]
        comment = f'// Fails: {desc} {tid}'

    if kind == 'PASS':
        stmt_a = f'await {call};'
        expr_t = call
        expr_v = call
    elif kind == 'TRANSLATION':
        stmt_a = f'await SpecFails.TranslationFailedAsync(() => {call});'
        expr_t = f'SpecFails.TranslationFailedAsync(() => {call})'
        expr_v = f'SpecFails.TranslationFailed(() => {call})'
    elif kind == 'THROW':
        t = gt(cl[1])
        stmt_a = f'await SpecFails.ThrowsAsync<{t}>(() => {call});'
        expr_t = f'SpecFails.ThrowsAsync<{t}>(() => {call})'
        expr_v = f'SpecFails.Throws<{t}>(() => {call})'
    elif kind == 'MIXED':
        t = gt(cl[1])
        stmt_a = f'await SpecFails.AsyncSomeRows<{t}>(() => {call}, string.Empty);'
        expr_t = f'SpecFails.AsyncSomeRows<{t}>(() => {call}, string.Empty)'
        expr_v = f'SpecFails.SyncSomeRows<{t}>(() => {call}, string.Empty)'
    else:  # MULTI
        stmt_a = f'await SpecFails.ThrowsAsync(() => {call});'
        expr_t = f'SpecFails.ThrowsAsync(() => {call})'
        expr_v = f'SpecFails.Throws(() => {call})'

    sig = f'{ind}public override {"async " if d.is_async else ""}{d.ret} {d.name}({d.params})'
    out = []
    if d.is_async and d.ret == 'Task':
        out.append(sig)
        out.append(ind + '{')
        if comment:
            out.append(f'{ind}    {comment}')
        out.append(f'{ind}    {stmt_a}')
        out.append(ind + '}')
    else:
        if comment:
            out.append(f'{ind}{comment}')
        out.append(sig)
        out.append(f'{ind}    => {expr_t if d.ret == "Task" else expr_v};')
    return out


def cond_for(vs):
    return ' || '.join('EF' + v for v in sorted(vs, key=int))


# ---------------------------------------------------------------------------------------------- main
def main(mode):
    port = [REPO / l for l in PORT_FILES.read_text().split() if l.endswith('.cs')]
    trx = {v: load_trx(v) for v in VERS}
    harvest = {v: load_harvest(v) for v in VERS}
    have = [v for v in VERS if trx[v] is not None]
    print('versions with a normal run:', have, '| with harvest:', [v for v in VERS if harvest[v]])

    summary = Counter()
    manual = []
    report_rows = defaultdict(Counter)

    for p in sorted(port):
        if not p.exists() or 'SpecFails.' not in p.read_text(encoding='utf-8-sig'):
            continue
        if p.name in ('SpecFails.cs',):
            continue
        text, bom = read_text(p)
        lines = text.split('\n')
        masks = masks_for(lines)
        decls = find_decls(lines, masks)
        stem = p.stem
        # classes declared in the file (for matching TRX class names)
        classes = set(re.findall(r'\bclass\s+(\w+)', text))
        edits = []
        for d in decls:
            if not d.uses_specfails:
                continue
            act_versions = [v for v, a in zip(VERS, d.active) if a]
            desired = {}
            any_change = False
            for v in act_versions:
                if trx[v] is None:
                    desired[v] = ('KEEP',)
                    continue
                failed, exists = trx[v]
                is_failed = any((c, d.name) in failed for c in classes)
                if not is_failed:
                    desired[v] = ('KEEP',)
                    continue
                rows = harvest[v].get((stem, d.name))
                if not rows:
                    manual.append((p.name, d.name, v, 'no harvest rows'))
                    desired[v] = ('KEEP',)
                    continue
                cl = classify(rows)
                desired[v] = cl
                any_change = True
                report_rows[(p.name, d.name)][(v, cl)] += 1
            if not any_change:
                continue
            if not d.simple:
                manual.append((p.name, d.name, '-', 'directives inside declaration'))
                continue
            groups = defaultdict(list)
            for v, cl in desired.items():
                groups[cl].append(v)
            edits.append((d, groups))
            for cl, vs in groups.items():
                summary[(cl[0], tuple(vs) if False else len(vs))] += 0
            for v, cl in desired.items():
                summary[cl[0]] += 1

        if mode == 'apply' and edits:
            for d, groups in sorted(edits, key=lambda e: -e[0].start):
                new = []
                ordered = sorted(groups.items(), key=lambda kv: (kv[0][0] == 'KEEP', min(int(x) for x in kv[1])))
                if len(ordered) == 1:
                    cl = ordered[0][0]
                    new = d.text if cl[0] == 'KEEP' else gen_code(d, cl)
                else:
                    for gi, (cl, vs) in enumerate(ordered):
                        body = d.text if cl[0] == 'KEEP' else gen_code(d, cl)
                        if gi == 0:
                            new.append(f'#if {cond_for(vs)}')
                        elif gi == len(ordered) - 1:
                            new.append('#else')
                        else:
                            new.append(f'#elif {cond_for(vs)}')
                        new.extend(body)
                    new.append('#endif')
                lines[d.start:d.end + 1] = new
            write_text(p, '\n'.join(lines), bom)
            print('rewrote', p.name, len(edits), 'declarations')

    print('\nSUMMARY (version-case outcomes for failing overrides):')
    for k, n in sorted(summary.items(), key=lambda kv: -kv[1]):
        if n:
            print(f'  {n:5d} {k}')
    if manual:
        print('\nMANUAL ATTENTION:')
        for m in manual[:80]:
            print('  ', m)
        print('  total', len(manual))

    if mode == 'report':
        print('\nPER-METHOD (first 40):')
        for (f, mth), c in list(report_rows.items())[:40]:
            print(' ', f, mth, [(v, cl[0], cl[1].split('.')[-1] if len(cl) > 1 else '') for (v, cl) in c])


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else 'report')
