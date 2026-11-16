using System;
using System.IO;
using UnityEngine;
namespace DragonIdle {
 [Serializable] public sealed class AudioPreferences {
  public float Master=.65f,Music=.18f,Effects=.45f;public bool MuteUnfocused=true,ReducedMotion;
  public void Normalize(){Master=Safe(Master,.65f);Music=Safe(Music,.18f);Effects=Safe(Effects,.45f);}
  static float Safe(float v,float fallback){return float.IsNaN(v)||float.IsInfinity(v)?fallback:Mathf.Clamp01(v);}
 }
 public partial class DemoGame {
  AudioPreferences audioSettings;AudioSource menuMusic,uiSounds;AudioClip[] uiClips;
  string audioPath;bool silentAudio,audioFocused=true,audioSavePending;float audioSaveAt;int uiSoundEvents;
  void InitializeMenuAudio(bool silent){
   silentAudio=silent;audioPath=Path.Combine(Path.GetDirectoryName(savePath),"audio-settings.json");
   audioSettings=new AudioPreferences();
   try{if(File.Exists(audioPath)){audioSettings=JsonUtility.FromJson<AudioPreferences>(File.ReadAllText(audioPath))??new AudioPreferences();audioSettings.Normalize();}}catch(Exception ex){Debug.LogWarning("Audio settings fallback: "+ex.Message);}
   menuMusic=gameObject.AddComponent<AudioSource>();menuMusic.playOnAwake=false;menuMusic.loop=true;menuMusic.spatialBlend=0;
   uiSounds=gameObject.AddComponent<AudioSource>();uiSounds.playOnAwake=false;uiSounds.spatialBlend=0;
   if(FindObjectOfType<AudioListener>()==null)new GameObject("MenuAudioListener").AddComponent<AudioListener>();
   uiClips=new[]{MakeUITone("select",.055f,new[]{520f}),MakeUITone("confirm",.25f,new[]{523.25f,659.25f,783.99f}),MakeUITone("unavailable",.16f,new[]{220f,196f})};
   menuMusic.clip=MakeMenuLoop();ApplyAudioLevels();if(!silentAudio)menuMusic.Play();
  }
  AudioClip MakeUITone(string name,float seconds,float[] frequencies){
   int rate=22050;float[] data=new float[(int)(rate*seconds)];
   for(int n=0;n<data.Length;n++){
    double t=(double)n/rate;int part=Math.Min(frequencies.Length-1,(int)(t/seconds*frequencies.Length));
    double local=t-part*seconds/frequencies.Length,envelope=Math.Min(1,local/.006)*Math.Exp(-local*25);
    double remaining=seconds-t;envelope*=Math.Min(1,remaining/.018);
    data[n]=(float)(Math.Sin(t*frequencies[part]*Math.PI*2)*envelope*.18);
   }
   var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
  }
  AudioClip MakeMenuLoop(){
   // Original 32-second C-major pentatonic miniature, generated without external recordings.
   int rate=22050;float[] data=new float[rate*32];int[] roots={48,45,53,55};
   int[] melody={72,76,79,74,76,72,69,67,72,74,79,76,74,72,67,69};
   for(int beat=0;beat<64;beat++){
    int bar=beat/16;double start=beat*.5;
    if(beat%4==0){AddNote(data,rate,start,roots[bar],1.9,.07);AddNote(data,rate,start,roots[bar]+7,1.9,.04);}
    if(beat%2==0)AddNote(data,rate,start+.025,melody[(beat/2)%16],.8,.052);
   }
   for(int i=0;i<data.Length;i++){double fade=Math.Min(1,Math.Min(i/(rate*.15),(data.Length-1-i)/(rate*.7)));data[i]*=(float)fade;}
   var clip=AudioClip.Create("Gallery original menu sketch",data.Length,1,rate,false);clip.SetData(data,0);return clip;
  }
  static void AddNote(float[] data,int rate,double start,int midi,double duration,double gain){
   double frequency=440*Math.Pow(2,(midi-69)/12.0);int from=(int)(start*rate),length=(int)(duration*rate);
   for(int i=0;i<length&&from+i<data.Length;i++){
    double t=(double)i/rate,envelope=Math.Min(1,t/.012)*Math.Exp(-t*3.6)*Math.Min(1,(duration-t)/.08);
    double wave=Math.Sin(2*Math.PI*frequency*t)+.22*Math.Sin(4*Math.PI*frequency*t)+.06*Math.Sin(6*Math.PI*frequency*t);
    data[from+i]+=(float)(wave*envelope*gain);
   }
  }
  void ApplyAudioLevels(){
   if(audioSettings==null||menuMusic==null)return;
   float gate=silentAudio||(!audioFocused&&audioSettings.MuteUnfocused)?0:audioSettings.Master;
   menuMusic.volume=gate*audioSettings.Music*(running?.4f:1);uiSounds.volume=gate*audioSettings.Effects;
  }
  void PlayUISound(int kind){if(uiSounds==null||uiClips==null)return;uiSoundEvents++;ApplyAudioLevels();if(!silentAudio)uiSounds.PlayOneShot(uiClips[Mathf.Clamp(kind,0,2)]);}
  void ScheduleAudioSave(){audioSavePending=true;audioSaveAt=Time.unscaledTime+.35f;ApplyAudioLevels();}
  void FlushAudioSettings(){
   if(!audioSavePending||audioSettings==null)return;
   try{
    audioSettings.Normalize();Directory.CreateDirectory(Path.GetDirectoryName(audioPath));string temp=audioPath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(audioSettings,true));
    if(File.Exists(audioPath))File.Replace(temp,audioPath,audioPath+".bak");else File.Move(temp,audioPath);
    audioSavePending=false;
   }catch(Exception ex){audioSavePending=false;GalleryNotice("声音设置未保存："+ex.Message,false);}
  }
  void UpdateMenuAudio(){ApplyAudioLevels();if(audioSavePending&&Time.unscaledTime>=audioSaveAt)FlushAudioSettings();}
  void OnApplicationFocus(bool focused){audioFocused=focused;ApplyAudioLevels();}
 }
}
