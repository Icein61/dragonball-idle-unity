param([string]$UnityPath='C:/Unity2022/Editor/Unity.exe',[switch]$VisibleSmoke)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
 $gitExe=if(Test-Path 'C:/Program Files/Git/cmd/git.exe'){'C:/Program Files/Git/cmd/git.exe'}else{'git'}
 if(& $gitExe status --porcelain){throw 'Commit source changes before packaging.'}
 $commit=(& $gitExe rev-parse HEAD).Trim()
 $version=(Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
 & (Join-Path $PSScriptRoot 'build-windows.ps1') -UnityPath $UnityPath
 $dir=Join-Path $root ('.validation/release-smoke-'+(Get-Date -Format 'yyyyMMddHHmmss'))
 New-Item -ItemType Directory -Force $dir | Out-Null
 $arguments=@('-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-smoke','-smokeOut',('"'+$dir+'"'),'-savePath',('"'+$dir+'/test-save.json"'),'-logFile',('"'+$dir+'/player.log"'))
 $style=if($VisibleSmoke){'Normal'}else{'Hidden'}
 $p=Start-Process (Join-Path $root 'Builds/Windows/DragonIdle.exe') -ArgumentList $arguments -WindowStyle $style -PassThru
 if(!$p.WaitForExit(60000)){Stop-Process -Id $p.Id;throw 'Smoke timeout'}
 if($p.ExitCode -ne 0){throw 'Player failed'}
 $result=Get-Content (Join-Path $dir 'smoke-result.json') -Raw | ConvertFrom-Json
 if(!$result.battleWon -or !$result.saveRoundTrip){throw 'Smoke verification failed'}
 if(!$VisibleSmoke){throw 'Logic smoke passed, but hidden-window rendering cannot be certified. Rerun with -VisibleSmoke for visual verification.'}
 if(Select-String (Join-Path $dir 'player.log') -Pattern 'Exception|NullReferenceException' -Quiet){throw 'Runtime exception in smoke log'}
 if(& $gitExe status --porcelain){throw 'Build modified tracked source; review and commit before retry.'}
 $release=Join-Path $root ('Builds/Releases/v'+$version)
 New-Item -ItemType Directory -Force $release | Out-Null
 $zip=Join-Path $release ('DragonIdle-v'+$version+'-Windows-x64.zip')
 Compress-Archive -Path (Join-Path $root 'Builds/Windows/*') -DestinationPath $zip -Force
 $hash=(Get-FileHash -Algorithm SHA256 $zip).Hash.ToLowerInvariant()
 ($hash+'  '+[IO.Path]::GetFileName($zip)) | Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding utf8
 $manifest=[ordered]@{version=$version;commit=$commit;unity='2022.3.62f3';platform='Windows-x64';builtAtUtc=[DateTime]::UtcNow.ToString('o');sha256=$hash;logicSmoke=$result;visualSmokeDirectory=[IO.Path]::GetFileName($dir);sourceClean=$true}
 $manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $release 'build-manifest.json') -Encoding utf8
 Write-Output ('PACKAGE_READY '+$release)
 Write-Output ('VISUAL_CHECK '+$dir)
} finally {Pop-Location}
