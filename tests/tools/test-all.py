#!/usr/bin/env python3
"""Build and test the provider on EF8, EF9, EF10 and EF11, in both query modes, all runs in parallel.

By default this is EIGHT runs: {EF8, EF9, EF10, EF11} x {Default, NativeOnly}. Each run executes UnitTests, FunctionalTests
and SpecificationTests one after the other, and every test process starts its own MongoDB Atlas Local Docker container
(TestContainers, random port), so no two runs ever share a database. MONGODB_URI / ATLAS_URI are cleared for the children
for that reason (use --shared-db to keep them; the runs are then serialised).

Run it from a command prompt through the wrappers next to the solution file:

    test-all.cmd                                  (Windows)
    ./test-all.sh                                 (bash)
    test-all.cmd --versions 10,11 --mode native --projects functional --filter QueryableEncryption

Results come from TRX files, not from scraping console output, so the counts cannot be misread: every run reports
Passed/Failed/Skipped/Total per test assembly, and a run whose assembly produced no TRX (crash, abort, timeout) is reported
as INCOMPLETE rather than as a smaller green number. Output (logs, TRX, summary.md, summary.json) goes to
<repo>/bin/test-all/<timestamp>/ (override with --out). Exit code: 0 all green, 1 failures or incomplete runs, 2 usage or
pre-flight error.

Needs: .NET SDK + runtime for each selected version (net8.0, net10.0, net11.0), Docker (unless --shared-db), Python 3.9+.
"""
from __future__ import annotations

import argparse
import concurrent.futures as cf
import datetime as dt
import json
import os
import re
import shutil
import signal
import subprocess
import sys
import threading
import time
import traceback
import xml.etree.ElementTree as ET
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple

REPO = Path(__file__).resolve().parents[2]
SLN = REPO / 'MongoDB.EFCoreProvider.sln'

VERSIONS = ['EF8', 'EF9', 'EF10', 'EF11']
TFM = {'EF8': 'net8.0', 'EF9': 'net8.0', 'EF10': 'net10.0', 'EF11': 'net11.0'}
PROJECTS = {
    'unit': 'MongoDB.EntityFrameworkCore.UnitTests',
    'functional': 'MongoDB.EntityFrameworkCore.FunctionalTests',
    'spec': 'MongoDB.EntityFrameworkCore.SpecificationTests',
}
PROJECT_ORDER = ['unit', 'functional', 'spec']
MODE_LABEL = {'default': 'Default', 'native': 'NativeOnly'}
MODE_ORDER = ['default', 'native']
ENCRYPTION_SKIP_TEXT = 'tests are being skipped'  # the TestServer warning when CRYPT_SHARED_LIB_PATH is unset


# ----------------------------------------------------------------------------------------------------------------------
# Small utilities
# ----------------------------------------------------------------------------------------------------------------------

_print_lock = threading.Lock()


def say(message: str) -> None:
    with _print_lock:
        print(f'[{time.strftime("%H:%M:%S")}] {message}', flush=True)


def fmt_secs(seconds: float) -> str:
    seconds = int(round(seconds))
    h, rem = divmod(seconds, 3600)
    m, s = divmod(rem, 60)
    if h:
        return f'{h}h{m:02d}m{s:02d}s'
    if m:
        return f'{m}m{s:02d}s'
    return f'{s}s'


def md_table(headers: List[str], rows: List[List[object]], right: Tuple[int, ...] = ()) -> str:
    cells = [[str(c) for c in row] for row in rows]
    widths = [max(len(h), *(len(r[i]) for r in cells)) if cells else len(h) for i, h in enumerate(headers)]

    def line(values: List[str]) -> str:
        out = []
        for i, v in enumerate(values):
            out.append(v.rjust(widths[i]) if i in right else v.ljust(widths[i]))
        return '| ' + ' | '.join(out) + ' |'

    sep = '|' + '|'.join(('-' * (w + 1) + ':') if i in right else ('-' * (w + 2)) for i, w in enumerate(widths)) + '|'
    return '\n'.join([line(headers), sep] + [line(r) for r in cells])


def fail(message: str, code: int = 2) -> 'None':
    print(f'error: {message}', file=sys.stderr)
    sys.exit(code)


# ----------------------------------------------------------------------------------------------------------------------
# Child processes: logged to a file, killed as a tree on timeout / Ctrl-C
# ----------------------------------------------------------------------------------------------------------------------

_active: Dict[int, subprocess.Popen] = {}
_active_lock = threading.Lock()
_cancelled = threading.Event()


def kill_tree(proc: subprocess.Popen) -> None:
    try:
        if os.name == 'nt':
            subprocess.run(['taskkill', '/PID', str(proc.pid), '/T', '/F'], stdout=subprocess.DEVNULL,
                           stderr=subprocess.DEVNULL, check=False)
        else:
            os.killpg(proc.pid, signal.SIGKILL)
    except Exception:
        try:
            proc.kill()
        except Exception:
            pass


def kill_all_active() -> None:
    with _active_lock:
        procs = list(_active.values())
    for p in procs:
        kill_tree(p)


def run_logged(cmd: List[str], log: Path, env: Dict[str, str], timeout_s: Optional[float]) -> Tuple[int, bool]:
    """Runs cmd with stdout+stderr going to `log`. Returns (exit code, timed out)."""
    if _cancelled.is_set():
        return -1, False
    log.parent.mkdir(parents=True, exist_ok=True)
    with open(log, 'w', encoding='utf-8', errors='replace', newline='\n') as f:
        f.write('$ ' + subprocess.list2cmdline(cmd) + '\n')
        for name in ('MONGODB_EF_QUERY_MODE', 'MONGODB_URI', 'ATLAS_URI', 'CRYPT_SHARED_LIB_PATH', 'MONGODB_BINARIES',
                     'DRIVER_VERSION'):
            f.write(f'# {name}={env[name] if name in env else "<unset>"}\n')
        f.flush()
        kwargs = {}
        if os.name == 'nt':
            kwargs['creationflags'] = subprocess.CREATE_NEW_PROCESS_GROUP
        else:
            kwargs['start_new_session'] = True
        proc = subprocess.Popen(cmd, stdout=f, stderr=subprocess.STDOUT, stdin=subprocess.DEVNULL, env=env,
                                cwd=str(REPO), **kwargs)
        with _active_lock:
            _active[proc.pid] = proc
        timed_out = False
        try:
            rc = proc.wait(timeout=timeout_s)
        except subprocess.TimeoutExpired:
            kill_tree(proc)
            proc.wait()
            rc, timed_out = -1, True
            f.write('\n# test-all: killed after exceeding the per-project timeout\n')
        finally:
            with _active_lock:
                _active.pop(proc.pid, None)
    return rc, timed_out


# ----------------------------------------------------------------------------------------------------------------------
# Pre-flight
# ----------------------------------------------------------------------------------------------------------------------

def capture(cmd: List[str], timeout: float = 60) -> Tuple[int, str]:
    try:
        p = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout, check=False)
        return p.returncode, p.stdout
    except (OSError, subprocess.TimeoutExpired) as e:
        return -1, str(e)


def real_external_db(name: str) -> bool:
    """Mirrors TestServer.GetOrInitialize: set, not blank and not the 'Disabled' sentinel."""
    value = os.environ.get(name, '')
    return bool(value.strip()) and value.strip().lower() != 'disabled'


def preflight(versions: List[str], shared_db: bool, dotnet: str) -> None:
    problems: List[str] = []
    rc, out = capture([dotnet, '--list-sdks'])
    sdk_majors = {int(m.group(1)) for m in re.finditer(r'^(\d+)\.', out, re.M)} if rc == 0 else set()
    rc, out = capture([dotnet, '--list-runtimes'])
    runtime_majors = {int(m.group(1)) for m in re.finditer(r'^Microsoft\.NETCore\.App (\d+)\.', out, re.M)} \
        if rc == 0 else set()
    for v in versions:
        major = int(re.match(r'net(\d+)', TFM[v]).group(1))
        if not sdk_majors or max(sdk_majors) < major:
            problems.append(f'{v} builds {TFM[v]}: a .NET {major}.x SDK is required but not installed.')
        if major not in runtime_majors:
            problems.append(f'{v} runs on {TFM[v]}: the .NET {major}.x runtime (Microsoft.NETCore.App) is not installed.')
    if shared_db:
        if not real_external_db('MONGODB_URI'):
            problems.append('--shared-db needs MONGODB_URI to be set to a real server.')
    else:
        docker = shutil.which('docker')
        if not docker:
            problems.append('Docker is required (each run starts its own MongoDB container) but `docker` was not found. '
                            'Start Docker, or use --shared-db with MONGODB_URI.')
        else:
            rc, _ = capture([docker, 'info'], timeout=60)
            if rc != 0:
                problems.append('`docker info` failed: start Docker (each run starts its own MongoDB container), '
                                'or use --shared-db with MONGODB_URI.')
    if problems:
        fail('pre-flight failed:\n  - ' + '\n  - '.join(problems))


def find_bash() -> Optional[str]:
    if os.name != 'nt':
        return shutil.which('bash')
    # Not System32\bash.exe (WSL): use Git for Windows' bash, found relative to git.
    candidates: List[Path] = []
    git = shutil.which('git')
    if git:
        g = Path(git).resolve()
        candidates += [g.parent.parent / 'bin' / 'bash.exe', g.parent.parent.parent / 'bin' / 'bash.exe']
    candidates.append(Path(os.environ.get('ProgramFiles', r'C:\Program Files')) / 'Git' / 'bin' / 'bash.exe')
    for c in candidates:
        if c.exists():
            return str(c)
    return None


def cached_encryption() -> Optional[Dict[str, str]]:
    """Reuses what fetch-crypt-shared.sh already downloaded. Re-running that script just to confirm the cache is slow, needs
    the network, and can fail with a PermissionError when another process (a sibling checkout's test run) has the
    crypt_shared library loaded and the downloader tries to overwrite it."""
    cache = Path(os.environ.get('CRYPT_SHARED_CACHE') or Path.home() / '.cache' / 'mongo-efcore-provider' / 'crypt_shared')
    lib_dir, bin_dir = cache / 'latest-release', cache / 'mongocryptd-latest-release'
    if not lib_dir.is_dir() or not bin_dir.is_dir():
        return None
    libs = [p for n in ('mongo_crypt_v1.dll', 'mongo_crypt_v1.so', 'mongo_crypt_v1.dylib') for p in lib_dir.rglob(n)]
    has_bin = any((bin_dir / n).exists() for n in ('mongocryptd', 'mongocryptd.exe'))
    if not libs or not has_bin:
        return None
    return {'CRYPT_SHARED_LIB_PATH': str(libs[0]), 'MONGODB_BINARIES': str(bin_dir)}


def resolve_encryption(skip: bool, refresh: bool = False) -> Tuple[Dict[str, str], str]:
    """Returns (env overrides for the children, the 'Encryption: ...' status text).

    CSFLE / Queryable Encryption tests skip unless BOTH CRYPT_SHARED_LIB_PATH and MONGODB_BINARIES are set; with only the
    first the Mongocryptd cases fail instead of skipping. So the children always get both set, or both explicitly empty.
    """
    empty = {'CRYPT_SHARED_LIB_PATH': '', 'MONGODB_BINARIES': ''}
    if skip:
        return empty, 'SKIPPED - --no-encryption was given'
    lib = os.environ.get('CRYPT_SHARED_LIB_PATH', '').strip()
    bins = os.environ.get('MONGODB_BINARIES', '').strip()
    if lib and bins:
        return {}, 'ran (crypt_shared + mongocryptd, from the environment)'
    if not refresh:
        cached = cached_encryption()
        if cached:
            return cached, 'ran (crypt_shared + mongocryptd, from the fetch-crypt-shared.sh cache)'
    bash = find_bash()
    script = REPO / 'tests' / 'tools' / 'fetch-crypt-shared.sh'
    if not bash:
        return empty, 'SKIPPED - no bash found to run tests/tools/fetch-crypt-shared.sh'
    say('Fetching crypt_shared + mongocryptd (cached after the first run; the first run can take a minute or two)...')
    env = os.environ.copy()
    env.setdefault('PYTHON', sys.executable)
    try:
        p = subprocess.run([bash, script.as_posix()], capture_output=True, text=True, env=env, timeout=1200, check=False)
    except (OSError, subprocess.TimeoutExpired) as e:
        return empty, f'SKIPPED - fetch-crypt-shared.sh could not run ({e})'
    if p.returncode != 0:
        last = (p.stderr.strip().splitlines() or ['no output'])[-1]
        return empty, f'SKIPPED - fetch-crypt-shared.sh failed ({last})'
    values = dict(re.findall(r"^export (\w+)='(.*)'$", p.stdout, re.M))
    result: Dict[str, str] = {}
    for name in ('CRYPT_SHARED_LIB_PATH', 'MONGODB_BINARIES'):
        value = values.get(name, '')
        if os.name == 'nt':
            m = re.match(r'^/([A-Za-z])/(.*)$', value)  # Git Bash path -> Windows path
            if m:
                value = f'{m.group(1).upper()}:\\' + m.group(2).replace('/', '\\')
        if not value or not os.path.exists(value):
            return empty, f'SKIPPED - fetch-crypt-shared.sh did not produce a usable {name}'
        result[name] = value
    return result, 'ran (crypt_shared + mongocryptd, fetched by tests/tools/fetch-crypt-shared.sh)'


# ----------------------------------------------------------------------------------------------------------------------
# Results
# ----------------------------------------------------------------------------------------------------------------------

@dataclass
class BuildResult:
    version: str
    ok: Optional[bool] = None  # None: not built (--no-build)
    seconds: float = 0.0
    log: str = ''
    errors: List[str] = field(default_factory=list)


@dataclass
class ProjectResult:
    version: str
    mode: str
    project: str
    status: str = 'PENDING'  # PASSED FAILED NO RESULTS TIMEOUT ERROR NONE NOT BUILT BUILD FAILED SCRIPT ERROR
    exit_code: Optional[int] = None
    passed: int = 0
    failed: int = 0
    skipped: int = 0
    seconds: float = 0.0
    log: str = ''
    trx: str = ''
    failed_tests: List[str] = field(default_factory=list)
    note: str = ''

    @property
    def total(self) -> int:
        return self.passed + self.failed + self.skipped

    @property
    def good(self) -> bool:
        return self.status in ('PASSED', 'NONE')


def parse_trx(path: Path) -> Tuple[int, int, int, List[str], Optional[int]]:
    """Returns (passed, failed, skipped, failed test names, the TRX's own Counters total)."""
    passed = failed = skipped = 0
    names: List[str] = []
    counters_total: Optional[int] = None
    for _, el in ET.iterparse(str(path), events=('end',)):
        tag = el.tag.rsplit('}', 1)[-1]
        if tag == 'UnitTestResult':
            outcome = el.get('outcome', '')
            if outcome == 'Passed':
                passed += 1
            elif outcome == 'NotExecuted':
                skipped += 1
            else:  # Failed, Error, Timeout, Aborted, Inconclusive, Pending...: anything not green counts against the run
                failed += 1
                names.append(el.get('testName', '?'))
            el.clear()
        elif tag == 'Counters':
            try:
                counters_total = int(el.get('total', ''))
            except ValueError:
                pass
    return passed, failed, skipped, names, counters_total


class Context:
    def __init__(self, args: argparse.Namespace, versions: List[str], modes: List[str], projects: List[str],
                 out: Path, dotnet: str, enc_env: Dict[str, str]):
        self.args = args
        self.versions = versions
        self.modes = modes
        self.projects = projects
        self.out = out
        self.dotnet = dotnet
        self.enc_env = enc_env
        self.filter = normalize_filter(args.filter)
        self.timeout_s = args.timeout_minutes * 60
        self.running: Dict[Tuple[str, str, str], float] = {}
        self.running_lock = threading.Lock()

    def child_env(self, mode: Optional[str]) -> Dict[str, str]:
        env = os.environ.copy()
        env['DOTNET_NOLOGO'] = '1'
        env['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
        env.pop('MONGODB_EF_QUERY_MODE', None)
        env.pop('MONGODB_EF_NATIVE_ONLY', None)  # setting both makes the resolver throw; neither is inherited
        if mode == 'native':
            env['MONGODB_EF_QUERY_MODE'] = 'NativeOnly'
        if not self.args.shared_db:
            env.pop('MONGODB_URI', None)  # unset => TestContainers boots a private container per test process
            env.pop('ATLAS_URI', None)
        env.update(self.enc_env)
        return env

    # Command lines ------------------------------------------------------------------------------------------------------

    def build_cmd(self, version: str) -> List[List[str]]:
        common = ['-c', f'Debug {version}', '-v', 'quiet', '-nologo', '-nodeReuse:false']
        if set(self.projects) == set(PROJECTS):
            return [[self.dotnet, 'build', str(SLN)] + common]
        return [[self.dotnet, 'build', str(project_file(k))] + common for k in self.projects]

    def test_cmd(self, version: str, mode: str, key: str) -> Tuple[List[str], Path, Optional[str]]:
        """Returns (command, trx path, error). The EF11 SpecificationTests are xUnit v3 4.x (Microsoft.Testing.Platform
        only), which `dotnet test` cannot run, so they run as an executable and write the TRX themselves."""
        name = f'{version}-{mode}-{key}'
        trx = self.out / f'{name}.trx'
        if version == 'EF11' and key == 'spec':
            dll = REPO / 'tests' / PROJECTS[key] / 'bin' / f'Debug {version}' / TFM[version] / f'{PROJECTS[key]}.dll'
            cmd = [self.dotnet, str(dll), '-noColor', '-noLogo', '-result-trx', str(trx)]
            if self.filter:
                cmd += ['-filterVSTest', self.filter]
            return cmd, trx, (None if dll.exists() else f'{dll} does not exist (not built?)')
        cmd = [self.dotnet, 'test', str(project_file(key)), '-c', f'Debug {version}', '--no-build', '--nologo',
               '--logger', f'trx;LogFileName={trx.name}', '--logger', 'console;verbosity=normal',
               '--results-directory', str(self.out)]
        if self.filter:
            cmd += ['--filter', self.filter]
        return cmd, trx, None


def project_file(key: str) -> Path:
    return REPO / 'tests' / PROJECTS[key] / f'{PROJECTS[key]}.csproj'


def normalize_filter(value: Optional[str]) -> Optional[str]:
    """A bare word means 'fully qualified name contains it'; anything with an operator is a VSTest filter expression."""
    if value is None or not value.strip():
        return None
    value = value.strip()
    return value if re.search(r'[=~!|&()]', value) else f'FullyQualifiedName~{value}'


# ----------------------------------------------------------------------------------------------------------------------
# Build and run
# ----------------------------------------------------------------------------------------------------------------------

def do_build(ctx: Context, version: str) -> BuildResult:
    result = BuildResult(version=version, log=str(ctx.out / f'build-{version}.log'))
    if ctx.args.no_build:
        return result
    start = time.time()
    say(f'{version}: building...')
    ok = True
    for i, cmd in enumerate(ctx.build_cmd(version)):
        log = ctx.out / (f'build-{version}.log' if i == 0 else f'build-{version}-{i}.log')
        rc, _ = run_logged(cmd, log, ctx.child_env(None), ctx.timeout_s)
        if rc != 0:
            ok = False
            text = log.read_text(encoding='utf-8', errors='replace') if log.exists() else ''
            errs = sorted({m.strip() for m in re.findall(r'^.*: error .*$', text, re.M)})
            result.errors += errs[:5] or [f'dotnet build exited with {rc}; see {log}']
            result.log = str(log)
            break
    result.ok = ok
    result.seconds = time.time() - start
    say(f'{version}: build {"OK" if ok else "FAILED"} ({fmt_secs(result.seconds)})')
    return result


def run_project(ctx: Context, version: str, mode: str, key: str) -> ProjectResult:
    res = ProjectResult(version=version, mode=mode, project=key)
    res.log = str(ctx.out / f'{version}-{mode}-{key}.log')
    cmd, trx, error = ctx.test_cmd(version, mode, key)
    res.trx = str(trx)
    if error:
        res.status, res.note = 'NOT BUILT', error
        return res
    tag = (version, mode, key)
    with ctx.running_lock:
        ctx.running[tag] = time.time()
    say(f'{version:<4} {MODE_LABEL[mode]:<10} {key:<10} started')
    start = time.time()
    try:
        rc, timed_out = run_logged(cmd, Path(res.log), ctx.child_env(mode), ctx.timeout_s)
    finally:
        with ctx.running_lock:
            ctx.running.pop(tag, None)
    res.seconds = time.time() - start
    res.exit_code = rc
    if _cancelled.is_set():
        res.status, res.note = 'ERROR', 'cancelled'
        return res

    parsed = None
    if trx.exists():
        try:
            parsed = parse_trx(trx)
        except ET.ParseError as e:
            res.note = f'TRX unreadable ({e})'
    if parsed:
        res.passed, res.failed, res.skipped, res.failed_tests, counters_total = parsed
        if counters_total is not None and counters_total != res.total:
            res.note = f'TRX counters say {counters_total} tests but {res.total} results were read'
    log_text = ''
    if timed_out:
        res.status = 'TIMEOUT'
        res.note = f'killed after {ctx.args.timeout_minutes} minutes' + (f'; {res.note}' if res.note else '')
    elif parsed is None:
        log_text = Path(res.log).read_text(encoding='utf-8', errors='replace')
        if ctx.filter and re.search(r'No test (matches|is available)|no tests? (matched|were found)', log_text, re.I):
            res.status = 'NONE'
            res.note = 'the filter matched no tests'
        else:
            res.status = 'NO RESULTS'
            res.note = res.note or f'no TRX written (exit code {rc}): the test process crashed, aborted or never started'
    elif res.failed > 0:
        res.status = 'FAILED'
    elif res.total == 0:
        res.status = 'NONE' if ctx.filter else 'ERROR'
        res.note = 'the filter matched no tests' if ctx.filter else 'the run reported zero tests'
    elif rc != 0:
        res.status = 'ERROR'
        res.note = f'{res.passed} passed but the process exited with {rc}' + (f'; {res.note}' if res.note else '')
    elif res.note:
        res.status = 'ERROR'  # counter mismatch: don't trust the numbers
    else:
        res.status = 'PASSED'
    say(f'{version:<4} {MODE_LABEL[mode]:<10} {key:<10} {res.status:<10} {res.passed} passed, {res.failed} failed, '
        f'{res.skipped} skipped ({fmt_secs(res.seconds)})')
    return res


def do_run(ctx: Context, version: str, mode: str, build: 'cf.Future[BuildResult]') -> List[ProjectResult]:
    b = build.result()
    results: List[ProjectResult] = []
    if b.ok is False:
        return [ProjectResult(version=version, mode=mode, project=k, status='BUILD FAILED', note='; '.join(b.errors))
                for k in ctx.projects]
    for key in ctx.projects:
        if _cancelled.is_set():
            break
        try:
            results.append(run_project(ctx, version, mode, key))
        except Exception:  # a bug in this script must not look like a green run
            results.append(ProjectResult(version=version, mode=mode, project=key, status='SCRIPT ERROR',
                                         note=traceback.format_exc(limit=3)))
    return results


def heartbeat(ctx: Context, stop: threading.Event, interval: float) -> None:
    while not stop.wait(interval):
        with ctx.running_lock:
            running = sorted(ctx.running.items())
        if running:
            now = time.time()
            say('still running: ' + ', '.join(f'{v}/{MODE_LABEL[m]}/{k} ({fmt_secs(now - t)})' for (v, m, k), t in running))


# ----------------------------------------------------------------------------------------------------------------------
# Report
# ----------------------------------------------------------------------------------------------------------------------

def run_status(rs: List[ProjectResult], expected: int) -> str:
    if any(r.status == 'BUILD FAILED' for r in rs):
        return 'BUILD FAILED'
    if any(r.failed for r in rs):
        return 'FAILED'
    if len(rs) == expected and all(r.status == 'NONE' for r in rs):
        return 'NO TESTS MATCHED THE FILTER'  # a typo'd filter must not look like a green run
    bad = [r for r in rs if not r.good]
    if bad or len(rs) < expected:
        parts = sorted({f'{r.project}: {r.status}' for r in bad}) or ['not all projects ran']
        return 'INCOMPLETE (' + '; '.join(parts) + ')'
    return 'OK'


def rerun_hint(version: str, mode: str, rs: List[ProjectResult]) -> str:
    script = 'test-all.cmd' if os.name == 'nt' else './test-all.sh'
    projects = ','.join(sorted({r.project for r in rs if not r.good}, key=PROJECT_ORDER.index))
    return f'{script} --versions {version[2:]} --mode {mode} --projects {projects} --no-build'


def build_report(ctx: Context, builds: Dict[str, BuildResult], results: Dict[Tuple[str, str], List[ProjectResult]],
                 enc_status: str, elapsed: float) -> Tuple[str, bool]:
    lines: List[str] = []
    all_ok = True
    n_runs = len(ctx.versions) * len(ctx.modes)
    lines.append(f'# test-all results ({n_runs} runs, {fmt_secs(elapsed)})')
    lines.append('')
    lines.append(f'Output: {ctx.out}')
    if ctx.filter:
        lines.append(f'Filter: {ctx.filter}')
    lines.append('')

    for mode in ctx.modes:
        rows = []
        for v in ctx.versions:
            rs = results.get((v, mode), [])
            b = builds[v]
            status = run_status(rs, len(ctx.projects))
            if status != 'OK':
                all_ok = False
            build = 'skipped' if b.ok is None else ('OK' if b.ok else 'FAILED')
            p, f_, s = (sum(getattr(r, a) for r in rs) for a in ('passed', 'failed', 'skipped'))
            rows.append([v, build, p, f_, s, p + f_ + s, status])
        lines.append(f'## {MODE_LABEL[mode]}')
        lines.append('')
        lines.append(md_table(['Version', 'Build', 'Passed', 'Failed', 'Skipped', 'Total', 'Status'], rows,
                              right=(2, 3, 4, 5)))
        lines.append('')

    lines.append('## Per project')
    lines.append('')
    rows = []
    for v in ctx.versions:
        for mode in ctx.modes:
            for r in results.get((v, mode), []):
                rows.append([v, MODE_LABEL[mode], r.project, r.passed, r.failed, r.skipped, r.total,
                             fmt_secs(r.seconds), r.status])
    lines.append(md_table(['Version', 'Mode', 'Project', 'Passed', 'Failed', 'Skipped', 'Total', 'Time', 'Status'], rows,
                          right=(3, 4, 5, 6, 7)))
    lines.append('')

    problems: List[str] = []
    for v in ctx.versions:
        b = builds[v]
        if b.ok is False:
            problems.append(f'### {v}: build FAILED ({b.log})')
            problems += [f'    {e}' for e in b.errors]
    for v in ctx.versions:
        for mode in ctx.modes:
            rs = results.get((v, mode), [])
            bad = [r for r in rs if not r.good and r.status != 'BUILD FAILED']
            if not bad:
                continue
            problems.append(f'### {v} {MODE_LABEL[mode]}  (re-run alone: {rerun_hint(v, mode, bad)})')
            for r in bad:
                problems.append(f'- {r.project}: {r.status}' + (f' - {r.note}' if r.note else '') + f'  [{r.log}]')
                shown = r.failed_tests[:25]
                problems += [f'    - {n}' for n in shown]
                if len(r.failed_tests) > len(shown):
                    problems.append(f'    - ... and {len(r.failed_tests) - len(shown)} more (all in summary.json)')
    if problems:
        lines.append('## Problems')
        lines.append('')
        lines.append('A failure that only shows up with all runs going at once may be load (timeouts, container start-up): '
                     'run it again alone with the command shown before treating it as real.')
        lines.append('')
        lines += problems
        lines.append('')

    if 'default' in ctx.modes and 'native' in ctx.modes:
        native_only: List[str] = []
        for v in ctx.versions:
            d = {(r.project, n) for r in results.get((v, 'default'), []) for n in r.failed_tests}
            nat = {(r.project, n) for r in results.get((v, 'native'), []) for n in r.failed_tests}
            only = sorted(nat - d)
            if only:
                native_only.append(f'- {v}: {len(only)} fail only under NativeOnly (a test needing an `IsNativeOnly` '
                                   f'branch, or a translator gap), {len(nat & d)} fail in both (plain failures)')
                native_only += [f'    - {p}: {n}' for p, n in only[:25]]
        if native_only:
            lines.append('## NativeOnly-only failures')
            lines.append('')
            lines += native_only
            lines.append('')

    skipped_warning = []
    for v in ctx.versions:
        for mode in ctx.modes:
            log = ctx.out / f'{v}-{mode}-functional.log'
            if log.exists() and ENCRYPTION_SKIP_TEXT in log.read_text(encoding='utf-8', errors='replace'):
                skipped_warning.append(f'{v}/{MODE_LABEL[mode]}')
    if skipped_warning and enc_status.startswith('ran'):
        enc_status = 'SKIPPED - ' + ', '.join(skipped_warning) + ' logged the CRYPT_SHARED_LIB_PATH warning'
    lines.append(f'Encryption: {enc_status}')
    lines.append('')
    lines.append(f'Overall: {"PASS" if all_ok else "FAIL"}')
    return '\n'.join(lines), all_ok


# ----------------------------------------------------------------------------------------------------------------------
# Main
# ----------------------------------------------------------------------------------------------------------------------

def parse_list(value: str, allowed: List[str], what: str, norm) -> List[str]:
    out: List[str] = []
    for part in re.split(r'[,\s]+', value.strip()):
        if not part:
            continue
        item = norm(part)
        if item not in allowed:
            fail(f'unknown {what} "{part}" (expected one of: {", ".join(allowed)})')
        if item not in out:
            out.append(item)
    if not out:
        fail(f'--{what}s is empty')
    return out


def main() -> int:
    if sys.version_info < (3, 9):
        fail('Python 3.9 or newer is required.')
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')

    ap = argparse.ArgumentParser(
        prog='test-all', formatter_class=argparse.RawDescriptionHelpFormatter,
        description='Build and test EF8-EF11 in Default and NativeOnly query modes, all runs in parallel, each test '
                    'process with its own Docker container.',
        epilog='Examples:\n'
               '  test-all                                  all 8 runs (4 EF versions x 2 modes)\n'
               '  test-all --mode native --versions 10,11   NativeOnly on EF10 and EF11\n'
               '  test-all --projects unit,functional       skip the (long) SpecificationTests\n'
               '  test-all --filter ConferencePlanner       tests whose fully qualified name contains the text\n'
               '  test-all --filter "FullyQualifiedName~A|FullyQualifiedName~B"   any VSTest filter expression')
    ap.add_argument('--versions', default='8,9,10,11', help='EF versions to run (default: 8,9,10,11)')
    ap.add_argument('--mode', choices=['both', 'default', 'native'], default='both',
                    help='query modes: default, native (MONGODB_EF_QUERY_MODE=NativeOnly) or both (default)')
    ap.add_argument('--projects', default='unit,functional,spec', help='unit, functional, spec (default: all three)')
    ap.add_argument('--filter', help='bare text = fully qualified name contains it; or a VSTest filter expression')
    ap.add_argument('--parallel', type=int, default=0, metavar='N',
                    help='max runs at once (default: all of them; lower it on a small machine)')
    ap.add_argument('--no-build', action='store_true', help='test the binaries already built for each version')
    ap.add_argument('--shared-db', action='store_true',
                    help='use MONGODB_URI/ATLAS_URI instead of per-process containers; runs are then serialised')
    ap.add_argument('--no-encryption', action='store_true', help='do not fetch crypt_shared; encryption tests skip')
    ap.add_argument('--refresh-encryption', action='store_true',
                    help='run tests/tools/fetch-crypt-shared.sh even if its cache is populated (picks up a newer server)')
    ap.add_argument('--timeout-minutes', type=int, default=90, metavar='N',
                    help='kill a test project (or build) that runs longer than this (default: 90)')
    ap.add_argument('--out', help='output directory (default: <repo>/bin/test-all/<timestamp>)')
    ap.add_argument('--heartbeat-minutes', type=float, default=5, metavar='N',
                    help='print what is still running this often (default: 5)')
    ap.add_argument('--dry-run', action='store_true', help='print the commands and exit')
    args = ap.parse_args()

    versions = parse_list(args.versions, VERSIONS, 'version', lambda s: 'EF' + re.sub(r'^(?i:ef)', '', s))
    projects = [p for p in PROJECT_ORDER if p in parse_list(args.projects, PROJECT_ORDER, 'project', str.lower)]
    modes = MODE_ORDER if args.mode == 'both' else [args.mode]
    dotnet = shutil.which('dotnet')
    if not dotnet:
        fail('`dotnet` was not found on PATH.')
    if args.parallel < 0 or args.timeout_minutes < 1:
        fail('--parallel and --timeout-minutes must be positive.')

    out = Path(args.out).resolve() if args.out else REPO / 'bin' / 'test-all' / dt.datetime.now().strftime('%Y%m%d-%H%M%S')
    n_runs = len(versions) * len(modes)
    parallel = 1 if args.shared_db else (args.parallel or n_runs)

    if not args.dry_run:
        preflight(versions, args.shared_db, dotnet)
        enc_env, enc_status = resolve_encryption(args.no_encryption, args.refresh_encryption)
    else:
        enc_env, enc_status = {}, 'not resolved (dry run)'
    if not args.dry_run:
        out.mkdir(parents=True, exist_ok=True)
    ctx = Context(args, versions, modes, projects, out, dotnet, enc_env)

    if args.dry_run:
        for v in versions:
            for cmd in ctx.build_cmd(v):
                print(subprocess.list2cmdline(cmd))
        for v in versions:
            for m in modes:
                for k in projects:
                    cmd, _, _ = ctx.test_cmd(v, m, k)
                    env = 'MONGODB_EF_QUERY_MODE=NativeOnly ' if m == 'native' else ''
                    print(env + subprocess.list2cmdline(cmd))
        return 0

    say(f'{n_runs} run(s): {", ".join(versions)} x {", ".join(MODE_LABEL[m] for m in modes)}; projects: {", ".join(projects)}')
    if args.shared_db:
        say('--shared-db: using MONGODB_URI/ATLAS_URI from the environment; runs execute one at a time.')
    else:
        ignored = [n for n in ('MONGODB_URI', 'ATLAS_URI') if real_external_db(n)]
        if ignored:
            say(f'Ignoring {" and ".join(ignored)}: every test process gets its own container (use --shared-db to override).')
        say(f'Up to {parallel} run(s) at once; every test process starts its own Docker container.')
    if os.environ.get('DRIVER_VERSION'):
        say(f'DRIVER_VERSION={os.environ["DRIVER_VERSION"]} is set and applies to the builds.')
    say(f'Output: {out}')

    started = time.time()
    stop = threading.Event()
    threading.Thread(target=heartbeat, args=(ctx, stop, max(args.heartbeat_minutes, 0.1) * 60), daemon=True).start()
    build_pool = cf.ThreadPoolExecutor(max_workers=len(versions))
    run_pool = cf.ThreadPoolExecutor(max_workers=parallel)
    builds: Dict[str, BuildResult] = {}
    results: Dict[Tuple[str, str], List[ProjectResult]] = {}
    try:
        build_futures = {v: build_pool.submit(do_build, ctx, v) for v in versions}
        run_futures = {(v, m): run_pool.submit(do_run, ctx, v, m, build_futures[v]) for v in versions for m in modes}
        for v, fut in build_futures.items():
            builds[v] = fut.result()
        for key, fut in run_futures.items():
            results[key] = fut.result()
    except KeyboardInterrupt:
        _cancelled.set()
        say('Interrupted: stopping all child processes...')
        kill_all_active()
        run_pool.shutdown(wait=False, cancel_futures=True)
        build_pool.shutdown(wait=False, cancel_futures=True)
        return 130
    finally:
        stop.set()
    build_pool.shutdown()
    run_pool.shutdown()

    report, ok = build_report(ctx, builds, results, enc_status, time.time() - started)
    (out / 'summary.md').write_text(report + '\n', encoding='utf-8')
    payload = {
        'ok': ok,
        'elapsedSeconds': round(time.time() - started, 1),
        'filter': ctx.filter,
        'encryption': enc_status,
        'builds': {v: asdict(b) for v, b in builds.items()},
        'runs': [{**asdict(r), 'total': r.total} for rs in results.values() for r in rs],
    }
    (out / 'summary.json').write_text(json.dumps(payload, indent=2), encoding='utf-8')
    print()
    print(report)
    print()
    print(f'Summary files: {out / "summary.md"} , {out / "summary.json"}')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
