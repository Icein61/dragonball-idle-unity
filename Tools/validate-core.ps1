$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force (Join-Path $projectRoot '.validation') | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output = Join-Path $projectRoot '.validation/CoreChecks.exe'
& $compiler /nologo /target:exe /out:$output (Join-Path $projectRoot 'Assets/DragonIdle/Core/Combat.cs') (Join-Path $projectRoot 'Assets/DragonIdle/Core/GameData.cs') (Join-Path $PSScriptRoot 'CoreChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Core compile failed' }
& $output
if ($LASTEXITCODE -ne 0) { throw 'Core checks failed' }
