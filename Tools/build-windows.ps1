param([string]$UnityPath,[ValidatePattern('^Windows(?:-[A-Za-z0-9._]+)?$')][string]$BuildFolder='Windows',[string]$CharacterSheetPython)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$UnityPath = & (Join-Path $PSScriptRoot 'resolve-unity.ps1') -UnityPath $UnityPath
& (Join-Path $PSScriptRoot 'sync-character-sheet.ps1') -PythonPath $CharacterSheetPython
& (Join-Path $PSScriptRoot 'validate-core.ps1')
& (Join-Path $PSScriptRoot 'validate-art.ps1')
& (Join-Path $PSScriptRoot 'validate-branding.ps1')
& (Join-Path $PSScriptRoot 'validate-ui-shell.ps1')
$validationPath = Join-Path $projectRoot '.validation'
$logPath = Join-Path $validationPath 'build.log'
$referenceDir = Join-Path $projectRoot 'Assets/DragonIdle/Resources/Videos'
$runtimeArt = Join-Path $projectRoot 'Assets/DragonIdle/Resources/Art'
$kit = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets/DragonIdle/Resources/Config/Characters/RoundKits.json') -Raw | ConvertFrom-Json
$activeCards = $kit.cards.cardId
$archivedAssets = @(Get-ChildItem -LiteralPath (Join-Path $runtimeArt 'V050'), (Join-Path $runtimeArt 'V051') -File |
 Where-Object { $_.Name -match '^(.+)-(atlas[.]png|frames[.]json|portrait[.]png)([.]meta)?$' -and $Matches[1] -notin $activeCards } |
 ForEach-Object FullName)
$unityProject = $projectRoot
$stagedProject = $null
$stageArgs = @()
try {
 if ($archivedAssets.Count -gt 0 -or ((Test-Path -LiteralPath $referenceDir) -and
     (Get-ChildItem -LiteralPath $referenceDir -Filter '*.mp4' -File -Recurse | Select-Object -First 1))) {
  $validationDir = Get-Item -LiteralPath $validationPath
  if ($validationDir.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Unexpected validation directory' }
  $stagedProject = Join-Path $validationDir.FullName ('unity-player-' + [Guid]::NewGuid().ToString('N'))
  $unityProject = $stagedProject
  $stageArgs = @('-dragonStagedReferences')
  function Copy-UnityFolder($relative, [string[]]$exclusions = @()) {
   $from = Join-Path $projectRoot $relative
   $to = Join-Path $unityProject $relative
   if (!(Test-Path -LiteralPath $from -PathType Container)) { return }
   & robocopy $from $to /E /XJ /R:1 /W:1 /NFL /NDL /NJH /NJS /NP @exclusions | Out-Host
   if ($LASTEXITCODE -ge 8) { throw "Build staging failed for $relative" }
  }

  Copy-UnityFolder 'Assets' (@('/XD', $referenceDir, '/XF', (Join-Path (Split-Path $referenceDir -Parent) 'Videos.meta')) + $archivedAssets)
  Copy-UnityFolder 'ProjectSettings'
  Copy-UnityFolder 'Packages'
  Copy-UnityFolder 'Library/PackageCache'
  if (Test-Path -LiteralPath (Join-Path $unityProject 'Assets/DragonIdle/Resources/Videos')) {
   throw 'Authoring references appeared in the staged project'
  }
 }
 $arguments = @('-batchmode','-nographics','-quit','-projectPath',('"' + $unityProject + '"'),'-executeMethod','DragonIdle.Editor.BuildDemo.Build','-dragonBuildFolder',$BuildFolder,'-logFile',('"' + $logPath + '"')) + $stageArgs
 $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
 if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $logPath -Pattern 'DRAGON_IDLE_BUILD_SUCCESS' -Quiet)) { throw 'Unity build failed; see .validation/build.log' }
 if ($stagedProject) {
  $stagedAssets = Join-Path $unityProject 'Assets'
  $mainAssets = Join-Path $projectRoot 'Assets'
  foreach ($meta in Get-ChildItem -LiteralPath $stagedAssets -Recurse -File -Filter '*.meta') {
   $relative = $meta.FullName.Substring($stagedAssets.Length + 1)
   $target = Join-Path $mainAssets $relative
   $asset = $target.Substring(0,$target.Length - 5)
   if ((Test-Path -LiteralPath $asset) -and !(Test-Path -LiteralPath $target)) {
    Copy-Item -LiteralPath $meta.FullName -Destination $target
   }
  }
 }
} finally {
 if ($stagedProject -and (Test-Path -LiteralPath $stagedProject -PathType Container)) {
  $createdDir = Get-Item -LiteralPath $stagedProject
  if ($createdDir.FullName -ne $stagedProject -or
      $createdDir.Parent.FullName -ne $validationDir.FullName -or
      $createdDir.Name -notmatch '^unity-player-[a-f0-9]{32}$' -or
      ($createdDir.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
   throw 'Unexpected staged project directory'
  }
  Remove-Item -LiteralPath $createdDir.FullName -Recurse -Force
 }
}
Write-Output (Join-Path $projectRoot ("Builds/$BuildFolder/龙珠爆裂激斗.exe"))
