param([string]$UnityPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$versionLine = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') | Where-Object { $_ -match '^m_EditorVersion: ' }
$version = ($versionLine -replace '^m_EditorVersion: ', '').Trim()
if (!$UnityPath) { $UnityPath = $env:DRAGON_IDLE_UNITY_PATH }
if ($UnityPath) {
 if (!(Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw "Unity editor not found: $UnityPath" }
 $actualVersion = (Get-Item -LiteralPath $UnityPath).VersionInfo.ProductVersion
 if ($actualVersion -notmatch ('^' + [regex]::Escape($version) + '(_|$)')) { throw "Expected Unity $version, found $actualVersion. Project version was not changed." }
 return (Resolve-Path -LiteralPath $UnityPath).Path
}
$candidates = @(
 "D:/Unity/$version/Editor/Unity.exe",
 "C:/Unity/$version/Editor/Unity.exe",
 "C:/Unity2022/Editor/Unity.exe",
 "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe",
 "D:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe",
 "D:/Program Files/UnityEditer/$version/Editor/Unity.exe"
)
foreach ($candidate in $candidates) {
 if ((Test-Path -LiteralPath $candidate -PathType Leaf) -and (Get-Item -LiteralPath $candidate).VersionInfo.ProductVersion -match ('^' + [regex]::Escape($version) + '(_|$)')) { return (Resolve-Path -LiteralPath $candidate).Path }
}
throw "Unity $version not found. Install the locked editor, pass -UnityPath, or set DRAGON_IDLE_UNITY_PATH."
