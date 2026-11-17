param([string]$UnityPath,[switch]$VisibleSmoke)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
 if($VisibleSmoke){throw 'Visible smoke automation is disabled while the user works. Use the default headless validation.'}
 $gitExe=if(Test-Path 'C:/Program Files/Git/cmd/git.exe'){'C:/Program Files/Git/cmd/git.exe'}else{'git'}
 if(& $gitExe status --porcelain){throw 'Commit source changes before packaging.'}
 $commit=(& $gitExe rev-parse HEAD).Trim()
 $version=(Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
 & (Join-Path $PSScriptRoot 'build-windows.ps1') -UnityPath $UnityPath
 $dir=Join-Path $root ('.validation/release-smoke-'+(Get-Date -Format 'yyyyMMddHHmmss'))
 New-Item -ItemType Directory -Force $dir | Out-Null
 & (Join-Path $PSScriptRoot 'smoke-windows.ps1') -OutputDirectory $dir
 $result=Get-Content (Join-Path $dir 'smoke-result.json') -Raw | ConvertFrom-Json
 if(!$result.battleWon -or !$result.saveRoundTrip){throw 'Smoke verification failed'}
 if(Select-String (Join-Path $dir 'player.log') -Pattern 'Exception|NullReferenceException' -Quiet){throw 'Runtime exception in smoke log'}
 if(& $gitExe status --porcelain){throw 'Build modified tracked source; review and commit before retry.'}
 $release=Join-Path $root ('Builds/Releases/v'+$version)
 New-Item -ItemType Directory -Force $release | Out-Null
 $zip=Join-Path $release ('龙珠爆裂激斗-v'+$version+'-Windows-x64.zip')
 Compress-Archive -Path (Join-Path $root 'Builds/Windows/*') -DestinationPath $zip -Force
 $hash=(Get-FileHash -Algorithm SHA256 $zip).Hash.ToLowerInvariant()
 ($hash+'  '+[IO.Path]::GetFileName($zip)) | Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding utf8
 $battleArt=Get-Content -LiteralPath (Join-Path $dir 'battle-art-smoke.json') -Raw | ConvertFrom-Json
 $manifest=[ordered]@{gameTitle='龙珠爆裂激斗';executable='龙珠爆裂激斗.exe';version=$version;commit=$commit;unity='2022.3.62f3';platform='Windows-x64';builtAtUtc=[DateTime]::UtcNow.ToString('o');sha256=$hash;logicSmoke=$result;battleArtSmoke=$battleArt;visualVerification='pending-user-window-permission';smokeDirectory=[IO.Path]::GetFileName($dir);sourceClean=$true}

 $manifest.galleryRefreshSmoke=Get-Content -LiteralPath (Join-Path $dir 'gallery-refresh-smoke.json') -Raw | ConvertFrom-Json
 $manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $release 'build-manifest.json') -Encoding utf8
 Write-Output ('PACKAGE_READY '+$release)
 Write-Output ('HEADLESS_CHECK '+$dir)
} finally {Pop-Location}
