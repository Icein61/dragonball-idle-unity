param([string]$Executable, [string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'Builds/Windows/龙珠爆裂激斗.exe'}
if(!$OutputDirectory){$OutputDirectory=Join-Path $root ('.validation/headless-smoke-'+(Get-Date -Format 'yyyyMMddHHmmssfff'))}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$argsList=@('-batchmode','-nographics','-logicSmoke','-smokeOut',('"'+$OutputDirectory+'"'),'-savePath',('"'+$OutputDirectory+'/test-save.json"'),'-logFile',('"'+$OutputDirectory+'/player.log"'))
$process=Start-Process -FilePath $Executable -ArgumentList $argsList -WindowStyle Hidden -PassThru
if(!$process.WaitForExit(60000)){Stop-Process -Id $process.Id;throw 'Headless Player smoke timed out'}
if($process.ExitCode -ne 0){throw ('Player smoke failed; see '+$OutputDirectory)}
$result=Get-Content -LiteralPath (Join-Path $OutputDirectory 'smoke-result.json') -Raw | ConvertFrom-Json
if(!$result.battleWon -or !$result.saveRoundTrip -or !$result.legacyMigration -or !$result.characterPersistence -or !$result.artResourceLoaded -or !$result.chapterFlow -or !$result.v020Migration -or !$result.galleryFlow -or !$result.v040Migration -or !$result.leaderKeywords -or !$result.audioSettings){throw 'Incomplete smoke verification'}
if(Select-String -LiteralPath (Join-Path $OutputDirectory 'player.log') -Pattern 'Exception|NullReferenceException' -Quiet){throw 'Exception found in Player smoke log'}
$art=Get-Content -LiteralPath (Join-Path $OutputDirectory 'battle-art-smoke.json') -Raw | ConvertFrom-Json
if($art.atlases -ne 9 -or $art.signaturePortraits -ne 9 -or $art.poses -ne 108 -or !$art.inkCharge -or !$art.frameUpdate -or !$art.rewardAfterAnimation -or !$art.replayCleanup -or !$art.soundSilent){throw 'Battle art/presentation smoke incomplete'}

$refresh=Get-Content -LiteralPath (Join-Path $OutputDirectory 'gallery-refresh-smoke.json') -Raw | ConvertFrom-Json
if(!$refresh.dedicatedDetail -or !$refresh.cardCache -or !$refresh.focusReturn -or !$refresh.roundedSurfaces -or !$refresh.individualFilterClear -or !$refresh.filterDropdowns -or !$refresh.simplifiedCards -or !$refresh.fullPortraitAspect -or !$refresh.enabledTextContrast){throw 'Gallery refresh verification incomplete'}


$workshop=Get-Content -LiteralPath (Join-Path $OutputDirectory 'workshop-smoke.json') -Raw | ConvertFrom-Json
if(!$workshop.training -or !$workshop.formation -or !$workshop.equipment -or !$workshop.skills -or !$workshop.contextReturn -or !$workshop.saveRoundTrip -or !$workshop.modalIsolation){throw 'Workshop verification incomplete'}
$shell=Get-Content -LiteralPath (Join-Path $OutputDirectory 'shell-smoke.json') -Raw | ConvertFrom-Json
if(!$result.shellFlow -or !$shell.startupAsync -or !$shell.radialStartup -or !$shell.completedRequests -or $shell.assets -lt 4 -or !$shell.inkHome -or !$shell.chapterRing -or !$shell.summonPage -or !$shell.firstWinMoved -or !$shell.helpRemoved -or !$shell.homeIsolation -or !$shell.navigation -or !$shell.offlineClaim -or !$shell.saveRoundTrip -or !$shell.modalIsolation -or !$shell.focusReturn -or !$shell.contextReturn -or !$shell.loadingCancel -or !$shell.missingArtFallback -or !$shell.textLayout -or !$shell.soundSilent){throw 'Shell verification incomplete'}
Write-Output ('HEADLESS_SMOKE_PASS '+$OutputDirectory)

