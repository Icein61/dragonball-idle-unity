$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
New-Item -ItemType Directory -Force (Join-Path $projectRoot '.validation') | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output = Join-Path $projectRoot '.validation/CoreChecks.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/DragonIdle/Core') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler /nologo /target:exe /out:$output $sources (Join-Path $PSScriptRoot 'CoreChecks.cs') (Join-Path $PSScriptRoot 'CharacterChecks.cs') (Join-Path $PSScriptRoot 'ChapterChecks.cs') (Join-Path $PSScriptRoot 'CatalogChecks.cs') (Join-Path $PSScriptRoot 'ScopeChecks.cs') (Join-Path $PSScriptRoot 'PresentationChecks.cs') (Join-Path $PSScriptRoot 'MenuChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Core compile failed' }
& $output
if ($LASTEXITCODE -ne 0) { throw 'Core checks failed' }
