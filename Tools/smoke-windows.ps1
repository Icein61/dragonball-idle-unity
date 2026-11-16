param([string]$Executable, [string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'Builds/Windows/DragonIdle.exe'}
if(!$OutputDirectory){$OutputDirectory=Join-Path $root ('.validation/headless-smoke-'+(Get-Date -Format 'yyyyMMddHHmmssfff'))}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$argsList=@('-batchmode','-nographics','-logicSmoke','-smokeOut',('"'+$OutputDirectory+'"'),'-savePath',('"'+$OutputDirectory+'/test-save.json"'),'-logFile',('"'+$OutputDirectory+'/player.log"'))
$process=Start-Process -FilePath $Executable -ArgumentList $argsList -WindowStyle Hidden -PassThru
if(!$process.WaitForExit(60000)){Stop-Process -Id $process.Id;throw 'Headless Player smoke timed out'}
if($process.ExitCode -ne 0){throw ('Player smoke failed; see '+$OutputDirectory)}
$result=Get-Content -LiteralPath (Join-Path $OutputDirectory 'smoke-result.json') -Raw | ConvertFrom-Json
if(!$result.battleWon -or !$result.saveRoundTrip -or !$result.legacyMigration -or !$result.characterPersistence -or !$result.artResourceLoaded -or !$result.chapterFlow -or !$result.v020Migration -or !$result.galleryFlow -or !$result.audioSettings){throw 'Incomplete smoke verification'}
if(Select-String -LiteralPath (Join-Path $OutputDirectory 'player.log') -Pattern 'Exception|NullReferenceException' -Quiet){throw 'Exception found in Player smoke log'}
Write-Output ('HEADLESS_SMOKE_PASS '+$OutputDirectory)
