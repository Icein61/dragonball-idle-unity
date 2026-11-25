param([string]$Executable,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'Builds/Windows/龙珠爆裂激斗.exe'}
if(!$OutputDirectory){$OutputDirectory=Join-Path $root ('.validation/profile-smoke-'+(Get-Date -Format 'yyyyMMddHHmmssfff'))}
$output=(New-Item -ItemType Directory -Path $OutputDirectory).FullName

function Invoke-ProfileSmoke([string]$Mode,[string]$Path){
 $log=Join-Path $output ($Mode+'.log')
 $arguments=@('-batchmode','-nographics','-profileSmoke',$Mode,'-savePath',('"'+$Path+'"'),'-logFile',('"'+$log+'"'))
 $process=Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
 if(!$process.WaitForExit(60000)){
  Stop-Process -Id $process.Id
  throw "Profile smoke timed out: $Mode"
 }
 if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch "PROFILE_SMOKE_PASS $Mode" -Quiet)){
  throw "Profile smoke failed: $Mode. See $log"
 }
 Write-Output "PROFILE_SMOKE_PASS $Mode"
}

$save=Join-Path $output 'profile.json'
Invoke-ProfileSmoke 'create' $save
$profileHash=(Get-FileHash -LiteralPath $save).Hash
$backupHash=(Get-FileHash -LiteralPath ($save+'.bak')).Hash
New-Item -ItemType Directory -Path ($save+'.tmp') | Out-Null
Invoke-ProfileSmoke 'reload' $save
if((Get-FileHash -LiteralPath $save).Hash -ne $profileHash -or (Get-FileHash -LiteralPath ($save+'.bak')).Hash -ne $backupHash){
 throw 'Relaunch or failed write modified the saved profile or backup'
}

$invalid=Join-Path $output 'corrupt.json'
[IO.File]::WriteAllText($invalid,'{ invalid JSON')
Copy-Item -LiteralPath ($save+'.bak') -Destination ($invalid+'.bak')
$invalidHash=(Get-FileHash -LiteralPath $invalid).Hash
Invoke-ProfileSmoke 'corrupt' $invalid
if((Get-FileHash -LiteralPath $invalid).Hash -ne $invalidHash -or (Get-FileHash -LiteralPath ($invalid+'.bak')).Hash -ne $backupHash){
 throw 'Corrupt profile or its backup was modified'
}

$blocked=Join-Path $output 'blocked.json'
New-Item -ItemType Directory -Path ($blocked+'.tmp') | Out-Null
Invoke-ProfileSmoke 'blocked-create' $blocked
Write-Output "PROFILE_PERSISTENCE_PASS $output"
