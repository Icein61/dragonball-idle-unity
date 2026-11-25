param(
 [string]$BaselineTag='v0.18.2',
 [string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$OutputDirectory){
 $OutputDirectory=Join-Path $root ('.validation/round-trace-'+(Get-Date -Format 'yyyyMMddHHmmssfff'))
}
$output=(New-Item -ItemType Directory -Path $OutputDirectory).FullName
$archive=Join-Path $output 'baseline.zip'
$baseline=Join-Path $output 'baseline'
$corePath='Assets/DragonIdle/Core'
$configPath='Assets/DragonIdle/Resources/Config'
$commit=& git -C $root rev-parse --verify "${BaselineTag}^{commit}"
if($LASTEXITCODE -ne 0){throw "Unable to resolve baseline tag $BaselineTag"}
& git -C $root archive --format=zip "--output=$archive" $commit $corePath "$configPath/Characters/RoundKits.json" "$configPath/Battle/Scenes.json"
if($LASTEXITCODE -ne 0){throw "Unable to export the baseline source"}
Expand-Archive -LiteralPath $archive -DestinationPath $baseline

$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compiler=Join-Path $framework 'csc.exe'
$harness=Join-Path $PSScriptRoot 'RoundTrace.cs'
function Invoke-Trace([string]$Source,[string]$Name,[string[]]$CompilerOptions){
 $exe=Join-Path $output ("$Name.exe")
 $trace=Join-Path $output ("$Name.jsonl")
 $sources=@(Get-ChildItem -LiteralPath (Join-Path $Source $corePath) -Filter '*.cs' -File | ForEach-Object FullName)
 & $compiler /nologo /target:exe /reference:System.Web.Extensions.dll "/out:$exe" @CompilerOptions @sources $harness
 if($LASTEXITCODE -ne 0){throw "Unable to compile $Name"}
 & $exe (Join-Path $Source $configPath) $trace
 if($LASTEXITCODE -ne 0){throw "Unable to record $Name"}
}
Invoke-Trace $baseline 'published' @('/define:PUBLISHED_BASELINE')
Invoke-Trace $root 'current' @()
$publishedTrace=Join-Path $output 'published.jsonl'
$currentTrace=Join-Path $output 'current.jsonl'
$publishedHash=(Get-FileHash -LiteralPath $publishedTrace).Hash
if((Get-FileHash -LiteralPath $currentTrace).Hash -ne $publishedHash){
 $before=[IO.File]::ReadAllLines($publishedTrace)
 $after=[IO.File]::ReadAllLines($currentTrace)
 for($line=0;$line -lt [Math]::Min($before.Length,$after.Length);$line++){
  if($before[$line] -cne $after[$line]){throw "Battle trace differs at line $($line+1). See $output"}
 }
 throw "Battle traces differ in length or encoding. See $output"
}
Write-Output "ROUND_TRACE_PARITY_PASS tag=$BaselineTag commit=$commit sha256=$publishedHash output=$output"
