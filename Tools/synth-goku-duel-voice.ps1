param([Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$ReferenceTiming)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Speech
[IO.Directory]::CreateDirectory($OutputDirectory)|Out-Null
$voice=[System.Speech.Synthesis.SpeechSynthesizer]::new()
try {
 $locale=[globalization.cultureinfo]::GetCultureInfo('zh-CN')
 $matching=@($voice.GetInstalledVoices() | Where-Object {$_.Enabled -and $_.VoiceInfo.Culture.Name -eq $locale.Name})
 if(!$matching.Count){throw 'No Mandarin Windows voice available'}
 $selected=$matching | Where-Object {$_.VoiceInfo.Gender -eq [System.Speech.Synthesis.VoiceGender]::Male} | Select-Object -First 1
 if(!$selected){$selected=$matching[0]}
 $voice.SelectVoice($selected.VoiceInfo.Name)
 $format=[System.Speech.AudioFormat.SpeechAudioFormatInfo]::new(22050,
  [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,[System.Speech.AudioFormat.AudioChannel]::Mono)
 $lines=[ordered]@{
  'voice-entry'=@{Text=@('来吧！');Rate=1}
  'voice-punch'=@{Text=@('喝！');Rate=3}
  'voice-gather'=@{Text=@('龟','派','气');Rate=0}
  'voice-fire'=@{Text=@('功！');Rate=1}
  'voice-kick'=@{Text=@('呀！');Rate=4}
  'voice-opening'=@{Text=@('准备好了！');Rate=2}
 }
 if($ReferenceTiming){
  $lines['voice-punch']=@{Text=@('喝！');Rate=5}
  $lines['voice-gather']=@{Text=@('龟派');Rate=4}
  $lines['voice-fire']=@{Text=@('气功！');Rate=2}
  $lines['voice-kick']=@{Text=@('哈！');Rate=6}
 }
 foreach($cue in $lines.GetEnumerator()){
  $builder=[System.Speech.Synthesis.PromptBuilder]::new($locale)
  for($i=0;$i -lt $cue.Value.Text.Count;$i++){
   if($i -gt 0){$builder.AppendBreak([TimeSpan]::FromMilliseconds(105))}
   $builder.AppendText($cue.Value.Text[$i])
  }
  $voice.Rate=$cue.Value.Rate
  $voice.SetOutputToWaveFile((Join-Path $OutputDirectory ($cue.Key+'.wav')),$format)
  $voice.Speak($builder)
  $voice.SetOutputToNull()
 }
 "Synthesized $($lines.Count) Mandarin cues with $($selected.VoiceInfo.Name)"
} finally { $voice.Dispose() }
