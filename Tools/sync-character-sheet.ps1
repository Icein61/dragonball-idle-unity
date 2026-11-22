param([string]$PythonPath, [switch]$Check)
$ErrorActionPreference = 'Stop'
if (!$PythonPath) { $PythonPath = $env:DRAGON_SHEET_PYTHON }
if (!$PythonPath) {
    $python = Get-Command python.exe -ErrorAction SilentlyContinue
    if ($python) { $PythonPath = $python.Source }
}
if (!$PythonPath) {
    $python = Get-Command py.exe -ErrorAction SilentlyContinue
    if ($python) { $PythonPath = $python.Source }
}
if (!$PythonPath) {
    throw '角色表同步需要 Python 3。请传入 -PythonPath，或设置 DRAGON_SHEET_PYTHON。'
}
$arguments = @(Join-Path $PSScriptRoot 'sync-character-sheet.py')
if ($Check) { $arguments += '--check' }
& $PythonPath @arguments
if ($LASTEXITCODE -ne 0) { throw '角色表同步未完成，已停止打包。' }
