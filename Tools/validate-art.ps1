$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$assets=Join-Path $root 'Assets/DragonIdle/Resources/Art/V050'
if(@(Get-ChildItem -LiteralPath $assets -Filter '*-atlas.png').Count -ne 15){throw 'Expected 15 character atlases'}
New-Item -ItemType Directory -Force (Join-Path $root '.validation') | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output=Join-Path $root '.validation/InspectAtlas.exe'
& $compiler /nologo /r:System.Drawing.dll /out:$output (Join-Path $PSScriptRoot 'InspectAtlas.cs')
if($LASTEXITCODE -ne 0){throw 'Art inspector compile failed'}
& $output $assets --check
if($LASTEXITCODE -ne 0){throw 'Art alpha or import validation failed'}
Write-Output 'ART_CHECK_PASS 15 portraits / 180 poses / transparent PNGs / bounded regions'
