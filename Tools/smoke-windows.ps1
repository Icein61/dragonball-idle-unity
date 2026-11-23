param([string]$Executable, [string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'Builds/Windows/龙珠爆裂激斗.exe'}
$expectedRoster=(Get-Content -LiteralPath (Join-Path $root 'Assets/DragonIdle/Resources/Config/Characters/RoundKits.json') -Raw -Encoding UTF8 | ConvertFrom-Json).cards.Count
if(!$OutputDirectory){$OutputDirectory=Join-Path $root ('.validation/headless-smoke-'+(Get-Date -Format 'yyyyMMddHHmmssfff'))}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$argsList=@('-batchmode','-nographics','-logicSmoke','-smokeOut',('"'+$OutputDirectory+'"'),'-savePath',('"'+$OutputDirectory+'/test-save.json"'),'-logFile',('"'+$OutputDirectory+'/player.log"'))
$process=Start-Process -FilePath $Executable -ArgumentList $argsList -WindowStyle Hidden -PassThru
if(!$process.WaitForExit(60000)){Stop-Process -Id $process.Id;throw 'Headless Player smoke timed out'}
if($process.ExitCode -ne 0){throw ('Player smoke failed; see '+$OutputDirectory)}
$result=Get-Content -LiteralPath (Join-Path $OutputDirectory 'smoke-result.json') -Raw | ConvertFrom-Json
if(!$result.roundCombat -or !$result.gokuMotion -or !$result.openingIcon -or !$result.gridDemo -or !$result.gridMultiTarget -or !$result.fullGridReadability -or !$result.groundedMotion -or !$result.contrastTiming -or !$result.waveFeather -or !$result.thickerWave -or !$result.expandedBattlefield -or !$result.hiddenBattleSlotNumbers -or !$result.upwardPursuitSheen -or !$result.ultimateAtlasSize -or !$result.pursuitFlash -or !$result.quickPursuitHandoff -or !$result.roundedVitals -or !$result.slotVitals -or !$result.actionGap -or !$result.shortOpening -or $result.reactionTransitions -ne 25 -or $result.gokuAudioCues -ne 21 -or $result.vegetaAudioCues -ne 9 -or !$result.savedFiveEnemyDemo -or !$result.characterSheetConfiguration -or !$result.vegetaVolley -or !$result.vegetaArt -or !$result.piccoloPresentation -or !$result.piccoloHeldCharge -or !$result.piccoloApproachPacing -or !$result.piccoloPursuitHop -or !$result.galleryOwnershipGroups -or !$result.galleryNumberSort -or !$result.galleryElasticSkills -or ($result.activeRoster -ne $expectedRoster) -or !$result.krillinPresentation -or !$result.galleryFavorites -or !$result.avatarElasticDrag -or !$result.authoredBuffIcons -or !$result.gohanPresentation -or !$result.characterPageLoop -or !$result.characterPageSessionSort -or !$result.characterSkillFourLines -or !$result.sharedMenuLoop -or !$result.avatarSwipe -or !$result.roundStatusHud -or !$result.piccoloFingerGlow -or !$result.gohanHairArt -or !$result.gohanHeavyWindup -or !$result.guardCounterPriority -or !$result.brokenPursuitRecovery -or !$result.savedLevel100Demo -or !$result.homeAndBattleBgm -or !$result.compactBattleGrid -or !$result.actionRates -or !$result.vegetaScaleStable -or !$result.ultimateCutIn -or !$result.smoothCameraRestore -or !$result.stableSkillBadge -or !$result.authoredSkillIcons -or !$result.beamTailWidened -or !$result.sharedOpeningFx -or !$result.battleSkillBadges -or !$result.congruentGridTiles -or !$result.horizontalRowBeam -or !$result.referenceTiming -or !$result.referenceVideoExcluded -or !$result.formationSave -or !$result.twoRoutes -or !$result.galleryDetails -or !$result.galleryCurrentAvatar -or !$result.gallerySkillColors -or !$result.gokuGalleryIcons -or !$result.neighborRarityBadges -or !$result.startupArt -or !$result.noRewards -or !$result.soundSilent){throw 'Incomplete round-combat Player verification'}
if(!$result.combinedStatGuide -or !$result.galleryRarityBackdrops -or !$result.multiCardHeldSwipe -or !$result.raditzPresentation){
 throw 'Incomplete gallery v0.16.2 Player verification'
}
if(!$result.twinRaysAndSingleHit -or !$result.krillinReactionCrop -or !$result.formationDragAndSave -or !$result.greenBadgeAndRadar){
 throw 'Incomplete v0.17.0 Player verification'
}
if(!$result.formationExplicitSave -or !$result.yamchaPresentation){throw 'Incomplete v0.17.2 Player verification'}
if(!$result.tienPresentation -or !$result.roleArtwork){throw 'Incomplete v0.17.3 Player verification'}
if(!$result.actualHitFeedback -or !$result.battleStats -or !$result.groundedGokuKit -or !$result.approvedReactionArt -or !$result.floatLaunchArtwork){throw 'Incomplete v0.18.0 battle verification'}
if(!$result.battleSidebarBars -or ($expectedRoster -eq 9 -and !$result.saibamanPresentation)){throw 'Incomplete v0.18.1 battle sidebar or Saibaman verification'}
if(Select-String -LiteralPath (Join-Path $OutputDirectory 'player.log') -Pattern 'Exception|NullReferenceException' -Quiet){throw 'Exception found in Player smoke log'}
Write-Output ('HEADLESS_SMOKE_PASS '+$OutputDirectory)
