$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$record = Get-Content -LiteralPath (Join-Path $root 'Art/Production/V052/branding.json') -Raw | ConvertFrom-Json
$asset = Join-Path $root $record.asset
if ((Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant() -ne $record.sha256) { throw 'Brand icon provenance hash mismatch' }
Add-Type -AssemblyName System.Drawing
$image = [Drawing.Image]::FromFile($asset)
try {
 if ($image.Width -ne $image.Height -or $image.Width -lt 1024) { throw 'Expected square high-resolution brand icon' }
 Write-Output ('BRAND_ICON_PASS ' + $image.Width + 'x' + $image.Height + ' / source hash matched')
} finally { $image.Dispose() }
