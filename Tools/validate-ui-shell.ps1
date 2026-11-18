$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$resources=Join-Path $root 'Assets/DragonIdle/Resources'
$legacy=Join-Path $resources 'Interface/V070'
if(Test-Path -LiteralPath $legacy){throw 'Extracted interface assets must remain outside Unity Resources'}
Add-Type -AssemblyName System.Drawing
foreach($rank in @('C','B','A','S')){
 $path=Join-Path $resources ('Interface/Rarity/'+$rank+'.png')
 $image=[Drawing.Image]::FromFile($path)
 try{if($image.Width -ne 256 -or $image.Height -ne 192){throw ('Rarity badge dimensions changed: '+$rank)}}finally{$image.Dispose()}
}
Write-Output 'UI_SHELL_ASSETS_PASS four original badges / no extracted interface bundle'
