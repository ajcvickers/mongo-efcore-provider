@echo off
rem Build and test EF8-EF11 in Default and NativeOnly query modes (8 parallel runs, each with its own Docker container).
rem Thin wrapper around tests\tools\test-all.py; run "test-all --help" for the options.
setlocal
set "SCRIPT=%~dp0tests\tools\test-all.py"
if defined PYTHON goto usepython
where py >nul 2>&1
if not errorlevel 1 goto usepy
where python >nul 2>&1
if not errorlevel 1 goto usepath
echo No Python 3.9+ interpreter found ^(tried py, python^); set PYTHON=^<command^>. 1>&2
exit /b 2
:usepython
"%PYTHON%" "%SCRIPT%" %*
exit /b %ERRORLEVEL%
:usepy
py -3 "%SCRIPT%" %*
exit /b %ERRORLEVEL%
:usepath
python "%SCRIPT%" %*
exit /b %ERRORLEVEL%
