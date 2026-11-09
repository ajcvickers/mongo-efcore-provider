#!/usr/bin/env python3
"""Counts the `// Fails:` tags in the SpecificationTests project, per ticket and per EF version.

The counting rule is the one docs/failing-spec-tests.md states: the number of DISTINCT OVERRIDDEN TEST METHODS
carrying a `// Fails: ... <ticket>` tag. A method split across `#if` arms counts once per ticket, and a method
that cites two tickets counts once for each. A tag belongs to the method whose body encloses it; a tag between
members belongs to the next method declaration.

Usage:
    python tests/tools/count-spec-fails.py                 # summary + per-ticket table (all EF versions)
    python tests/tools/count-spec-fails.py --json          # machine readable
    python tests/tools/count-spec-fails.py --by-file EF-X526   # which files carry a ticket

`#if` conditions over EF8 / EF9 / EF10 / EF11 are evaluated, so the per-version figures only count tags active in that
version's build. `--all-arms` ignores the conditions (every arm counts for every version).
"""
import argparse
import collections
import json
import os
import re
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'MongoDB.EntityFrameworkCore.SpecificationTests'))
VERSIONS = ['EF8', 'EF9', 'EF10', 'EF11']
TICKET = re.compile(r'(EF-X?\d+|CSHARP-\d+|dotnet/efcore#\d+)')
FAILS = re.compile(r'//\s*Fails:(.*)')
METHOD = re.compile(r'\b(?:override|new|virtual)\b[^=;{}]*?\b([A-Za-z_]\w*)\s*(?:<[^>()]*>)?\s*\(')
PLAIN_METHOD = re.compile(r'^\s*(?:public|protected|internal|private)\s+(?:(?:async|static|virtual|abstract|sealed|unsafe)\s+)*[\w<>\[\],.?\s]+?\b([A-Za-z_]\w*)\s*(?:<[^>()]*>)?\s*\(')


def lex(text):
    """Returns (code_lines, fails) where code_lines has strings/comments blanked and fails is [(line_no, text)]."""
    out, fails = [], []
    i, n, line = 0, len(text), 1
    buf = []
    while i < n:
        c = text[i]
        two = text[i:i + 2]
        if c == '\n':
            out.append(''.join(buf)); buf = []; line += 1; i += 1
        elif two == '//':
            j = text.find('\n', i)
            j = n if j < 0 else j
            m = FAILS.match(text[i:j])
            if m:
                fails.append((line, m.group(1)))
            i = j
        elif two == '/*':
            j = text.find('*/', i + 2)
            j = n if j < 0 else j + 2
            seg = text[i:j]
            line += seg.count('\n')
            for _ in range(seg.count('\n')):
                out.append(''.join(buf)); buf = []
            i = j
        elif text.startswith('"""', i):
            q = 3
            while text[i + q:i + q + 1] == '"':
                q += 1
            j = text.find('"' * q, i + q)
            j = n if j < 0 else j + q
            seg = text[i:j]
            line += seg.count('\n')
            buf.append('""')
            for _ in range(seg.count('\n')):
                out.append(''.join(buf)); buf = []
            i = j
        elif c == '@' and text[i + 1:i + 2] == '"' or (two == '$@') or (two == '@$'):
            j = i + 2 if two in ('$@', '@$') else i + 2
            if two in ('$@', '@$'):
                j = i + 3
            while j < n:
                if text[j] == '"':
                    if text[j + 1:j + 2] == '"':
                        j += 2; continue
                    break
                j += 1
            seg = text[i:j + 1]
            line += seg.count('\n')
            buf.append('""')
            for _ in range(seg.count('\n')):
                out.append(''.join(buf)); buf = []
            i = j + 1
        elif c == '"' or (c == '$' and text[i + 1:i + 2] == '"'):
            j = i + (2 if c == '$' else 1)
            while j < n and text[j] != '"' and text[j] != '\n':
                if text[j] == '\\':
                    j += 1
                j += 1
            buf.append('""')
            i = j + 1
        elif c == "'":
            j = i + 1
            while j < n and text[j] != "'" and text[j] != '\n':
                if text[j] == '\\':
                    j += 1
                j += 1
            buf.append("''")
            i = j + 1
        else:
            buf.append(c); i += 1
    out.append(''.join(buf))
    return out, fails


def eval_cond(expr, version):
    expr = expr.strip()
    expr = re.sub(r'//.*', '', expr)
    toks = re.findall(r'EF\d+|\(|\)|!|&&|\|\||true|false', expr)
    def sym(t):
        return 'True' if t in ('true',) or t == version else 'False'
    py = []
    for t in toks:
        if t == '&&': py.append(' and ')
        elif t == '||': py.append(' or ')
        elif t == '!': py.append(' not ')
        elif t in ('(', ')'): py.append(t)
        else: py.append(sym(t))
    try:
        return bool(eval(''.join(py) or 'True'))
    except Exception:
        return True


def active_lines(raw_lines, version):
    """Per-line flag: is the line active under `version` (None = all arms)."""
    active = []
    stack = []  # (parent_active, taken, this_active)
    cur = True
    for l in raw_lines:
        s = l.strip()
        if s.startswith('#if'):
            c = True if version is None else eval_cond(s[3:], version)
            stack.append((cur, c, c))
            cur = cur and (c if version is not None else True)
            active.append(False)
        elif s.startswith('#elif'):
            parent, taken, _ = stack[-1]
            c = True if version is None else eval_cond(s[5:], version)
            this = (not taken) and c if version is not None else True
            stack[-1] = (parent, taken or c, this)
            cur = parent and this
            active.append(False)
        elif s.startswith('#else'):
            parent, taken, _ = stack[-1]
            this = (not taken) if version is not None else True
            stack[-1] = (parent, True, this)
            cur = parent and this
            active.append(False)
        elif s.startswith('#endif'):
            parent, _, _ = stack.pop()
            cur = parent
            active.append(False)
        else:
            active.append(cur)
    return active


def scan_file(path, version):
    text = open(path, encoding='utf-8-sig').read().replace('\r\n', '\n')
    raw_lines = text.split('\n')
    act = active_lines(raw_lines, version)
    code, fails = lex(text)
    # fails use line numbers of the original text; code[] is index = line-1
    depth = 0
    stack = []  # [name, decl_depth, opened]
    types = []  # [name, decl_depth, opened] of enclosing types
    method_at_line = {}   # line -> enclosing method name
    next_methods = []     # (line, name, depth) of method declarations
    paren = 0
    for idx, l in enumerate(code):
        ln = idx + 1
        if idx < len(act) and not act[idx]:
            # inactive or directive line: ignore for structure, but still need depth tracking of braces? skip entirely
            continue
        tm = re.search(r'\b(?:class|struct|record)\s+([A-Za-z_]\w*)', l)
        if tm and not stack:
            types.append([tm.group(1), depth, False])
        m = None if tm and not stack else (METHOD.search(l) or PLAIN_METHOD.match(l))
        if m and '=>' not in l.split(m.group(1))[0] and not re.search(r'\b(class|struct|interface|enum|record)\b', l):
            # a method declaration only when not already inside another method body at deeper depth
            if not stack or depth <= stack[-1][1]:
                qual = (types[-1][0] if types else '') + '.' + m.group(1)
                stack.append([qual, depth, False])
                next_methods.append((ln, qual, depth))
        if stack:
            method_at_line[ln] = stack[-1][0] if (stack[-1][2] or depth > stack[-1][1]) else None
        for ch in l:
            if ch == '{':
                depth += 1
                if stack and depth > stack[-1][1]:
                    stack[-1][2] = True
                if types and not types[-1][2] and depth > types[-1][1] and not stack:
                    types[-1][2] = True
            elif ch == '}':
                depth -= 1
                if stack and stack[-1][2] and depth <= stack[-1][1]:
                    stack.pop()
                elif types and types[-1][2] and depth <= types[-1][1] and not stack:
                    types.pop()
            elif ch == ';' and stack and not stack[-1][2] and depth == stack[-1][1]:
                stack.pop()
    res = []
    for ln, txt in fails:
        if ln - 1 < len(act) and not act[ln - 1]:
            continue
        # A tag may continue on the following comment-only lines; the ticket id is often on the last one.
        # Continuation lines are read only until the first one that cites a ticket; later mentions are prose.
        def clean(s):
            s = re.sub(r'github\.com/dotnet/efcore/issues/(\d+)', r'dotnet/efcore#\1', s)
            return re.sub(r'post-EF-\d+', '', s)  # a mention, not the ticket the tag is filed under
        txt = clean(txt)
        tickets = set(TICKET.findall(txt))
        k = ln
        while not tickets and k < len(raw_lines) and raw_lines[k].strip().startswith('//') and not FAILS.search(raw_lines[k]):
            more = clean(raw_lines[k].strip()[2:])
            txt += ' ' + more
            tickets = set(TICKET.findall(more))
            k += 1
        if not tickets:
            continue
        owner = method_at_line.get(ln)
        if owner is None:
            for dl, name, _ in next_methods:
                if dl > ln:
                    owner = name
                    break
        res.append((ln, owner or '?', txt, tickets))
    return res


RAW_THROWS = re.compile(r'Assert\.Throws(?:Any)?(?:Async)?')
STRING_LITERAL = re.compile(r'"(?:[^"\\]|\\.)*"')
MEMBER_START =re.compile(r'^\s*(?:public|protected|private|internal|#if|#endif|#else|#elif|\[)')


def check_raw_throws(files):
    """Reports raw Assert.Throws* inside a method tagged `// Fails:`: those must use Utilities/SpecFails (issue #46)."""
    bad = []
    for f in files:
        lines = open(f, encoding='utf-8-sig').read().split(chr(10))
        rel = os.path.relpath(f, ROOT).replace(chr(92), '/')
        for i, l in enumerate(lines):
            if not FAILS.search(l) or not l.lstrip().startswith('//'):
                continue
            indent = len(l) - len(l.lstrip())
            j = i + 1
            while j < len(lines) and lines[j].lstrip().startswith('//'):
                j += 1
            if j >= len(lines):
                continue
            nxt = lines[j]
            before_sig = (len(nxt) - len(nxt.lstrip())) <= indent
            k = j + 1
            while k < len(lines):
                t = lines[k]
                ti = len(t) - len(t.lstrip())
                if t.strip() and ((before_sig and ti <= indent and MEMBER_START.match(t)) or (not before_sig and ti < indent)):
                    break
                if RAW_THROWS.search(STRING_LITERAL.sub('""', t)) and not t.lstrip().startswith('//') and not rel.startswith(('Utilities/', 'Query/MongoSpecTestHelpers')):
                    bad.append(f'{rel}:{k + 1}: {t.strip()[:110]}')
                k += 1
    return bad


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='fail if a `// Fails:` method uses raw Assert.Throws*')
    ap.add_argument('--json', action='store_true')
    ap.add_argument('--by-file', metavar='TICKET')
    ap.add_argument('--all-arms', action='store_true')
    args = ap.parse_args()

    files = []
    for dp, dn, fn in os.walk(ROOT):
        dn[:] = [d for d in dn if d not in ('obj', 'bin')]
        files += [os.path.join(dp, f) for f in fn if f.endswith('.cs')]
    files.sort()

    if args.check:
        bad = check_raw_throws(files)
        for b in bad:
            print(b)
        print(f'{len(bad)} raw Assert.Throws* in `// Fails:` methods')
        return 1 if bad else 0

    per_ticket = collections.defaultdict(lambda: collections.defaultdict(set))  # version -> ticket -> {(file,method)}
    raw_tag_lines = 0
    methods_any = collections.defaultdict(set)
    bulk = 'EF-X016'
    for f in files:
        rel = os.path.relpath(f, ROOT).replace('\\', '/')
        for v in (VERSIONS if not args.all_arms else [None]):
            for ln, owner, txt, tickets in scan_file(f, v):
                for t in tickets:
                    key = t
                    if t == bulk and re.search(r'ExecuteUpdate|ExecuteDelete', txt):
                        key = 'EF-X016/bulk'
                    per_ticket[v or 'ALL'][key].add((rel, owner))
                methods_any[v or 'ALL'].add((rel, owner))
        # raw tag lines (every arm)
        raw_tag_lines += sum(1 for l in open(f, encoding='utf-8-sig') if re.search(r'//\s*Fails:', l))

    versions = VERSIONS if not args.all_arms else ['ALL']
    union = collections.defaultdict(set)
    for v in versions:
        for t, s in per_ticket[v].items():
            union[t] |= s
    all_methods = set()
    for v in versions:
        all_methods |= methods_any[v]

    if args.by_file:
        c = collections.Counter(f for f, _ in union.get(args.by_file, set()))
        for f, n in c.most_common():
            print(f'{n:5d}  {f}')
        return

    data = {
        'raw_tag_lines': raw_tag_lines,
        'distinct_methods_any_version': len(all_methods),
        'distinct_methods_per_version': {v: len(methods_any[v]) for v in versions},
        'per_ticket': {t: len(s) for t, s in union.items()},
        'per_ticket_per_version': {v: {t: len(s) for t, s in per_ticket[v].items()} for v in versions},
    }
    if args.json:
        print(json.dumps(data, indent=1, sort_keys=True))
        return
    print(f'raw `// Fails:` tag lines: {raw_tag_lines}')
    print(f'distinct tagged methods (any version): {len(all_methods)}')
    for v in versions:
        print(f'  {v}: {len(methods_any[v])}')
    print()
    for t, n in sorted(data['per_ticket'].items(), key=lambda kv: -kv[1]):
        print(f'{n:6d}  {t}')


if __name__ == '__main__':
    sys.exit(main())
