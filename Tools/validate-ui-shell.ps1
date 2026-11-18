$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$manifest=Get-Content -LiteralPath (Join-Path $root 'Art/Production/V070/ui-assets.json') -Raw | ConvertFrom-Json
if($manifest.assets.Count -ne 11){throw 'Incomplete UI asset manifest'}
Add-Type -AssemblyName System.Drawing
foreach($entry in $manifest.assets){
 $path=Join-Path $root $entry.target
 if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256){throw ('UI provenance mismatch: '+$entry.key)}
 $image=[Drawing.Image]::FromFile($path)
 try{if($image.Width -ne $entry.width -or $image.Height -ne $entry.height){throw ('UI dimensions changed: '+$entry.key)}}finally{$image.Dispose()}
}
Write-Output 'UI_SHELL_ASSETS_PASS 11 exact extracted assets / dimensions / SHA256'
