param([string]$UnityPath = 'C:/Unity2022/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'validate-core.ps1')
& (Join-Path $PSScriptRoot 'validate-art.ps1')
$logPath = Join-Path $projectRoot '.validation/build.log'
$arguments = @('-batchmode','-nographics','-quit','-projectPath',('"' + $projectRoot + '"'),'-executeMethod','DragonIdle.Editor.BuildDemo.Build','-logFile',('"' + $logPath + '"'))
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $logPath -Pattern 'DRAGON_IDLE_BUILD_SUCCESS' -Quiet)) { throw 'Unity build failed; see .validation/build.log' }
Write-Output (Join-Path $projectRoot 'Builds/Windows/DragonIdle.exe')
