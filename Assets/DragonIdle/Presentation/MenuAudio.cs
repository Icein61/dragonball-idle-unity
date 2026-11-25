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
  const string CharacterPageBgm="Audio/V0152/BGM/character-selection-loop";
  AudioPreferences audioSettings;AudioSource uiSounds,pageMusic;AudioClip[] uiClips;
  string activeMusicPath;
  string audioPath;bool silentAudio,audioFocused=true,audioSavePending;float audioSaveAt;int uiSoundEvents;
  void InitializeMenuAudio(bool silent){
   silentAudio=silent;audioPath=Path.Combine(Path.GetDirectoryName(savePath),"audio-settings.json");
   audioSettings=new AudioPreferences();
   try{if(File.Exists(audioPath)){audioSettings=JsonUtility.FromJson<AudioPreferences>(File.ReadAllText(audioPath))??new AudioPreferences();audioSettings.Normalize();}}catch(Exception ex){Debug.LogWarning("Audio settings fallback: "+ex.Message);}
   uiSounds=gameObject.AddComponent<AudioSource>();uiSounds.playOnAwake=false;uiSounds.spatialBlend=0;
   pageMusic=gameObject.AddComponent<AudioSource>();pageMusic.playOnAwake=false;pageMusic.loop=true;pageMusic.spatialBlend=0;
   if(FindObjectOfType<AudioListener>()==null)new GameObject("MenuAudioListener").AddComponent<AudioListener>();
   uiClips=new[]{MakeUITone("select",.055f,new[]{520f}),MakeUITone("confirm",.25f,new[]{523.25f,659.25f,783.99f}),MakeUITone("unavailable",.16f,new[]{220f,196f})};
   InitializeBattleAudio();ApplyAudioLevels();
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
  void ApplyAudioLevels(){
   if(audioSettings==null||uiSounds==null)return;
   float gate=silentAudio||(!audioFocused&&audioSettings.MuteUnfocused)?0:audioSettings.Master;
   uiSounds.volume=gate*audioSettings.Effects;
   pageMusic.volume=gate*audioSettings.Music;
   if(battleSound!=null)battleSound.volume=gate*audioSettings.Effects;
   if(roundCueAudio!=null)roundCueAudio.volume=gate*audioSettings.Effects;
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
  void SyncPageMusic(){
   if(pageMusic==null)return;
   bool menu=page==PageRoute.Home||page==PageRoute.Characters||page==PageRoute.Formation;
   string path=menu&&!shellLoad.Active?CharacterPageBgm:
    page==PageRoute.RoundBattle&&roundDemo?demoScene?.bgm:null;
   if(activeMusicPath==path)return;
   activeMusicPath=path;
   pageMusic.Stop();
   pageMusic.clip=path==null?null:Resources.Load<AudioClip>(path);
   if(pageMusic.clip!=null&&!silentAudio)pageMusic.Play();
  }
  void UpdateMenuAudio(){ApplyAudioLevels();SyncPageMusic();if(audioSavePending&&Time.unscaledTime>=audioSaveAt)FlushAudioSettings();}
  void OnApplicationFocus(bool focused){audioFocused=focused;ApplyAudioLevels();}
 }
}
