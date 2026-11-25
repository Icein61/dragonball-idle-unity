using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DragonIdle {
 public partial class DemoGame {
  [Serializable] sealed class RoundCue {
   public string id,resource;
   public float gain=1;
  }

  [Serializable] sealed class RoundCueManifest {
   public string cardId;
   public RoundCue[] cues;
  }

  AudioSource roundCueAudio;
  Dictionary<string,(AudioClip Clip,float Gain)> roundCueClips;
  int roundCueEvents,comboCueEvents;

  void LoadRoundCueAudio() {
   if(roundCueClips!=null)return;
   var clips=new Dictionary<string,(AudioClip Clip,float Gain)>();
   LoadCueBank("GokuAudio",RoundMotionArt.CardId,clips);
   LoadCueBank("VegetaAudio",RoundMotionArt.VegetaCardId,clips);
   LoadCueBank("PiccoloAudio",RoundMotionArt.PiccoloCardId,clips);
   LoadCueBank("GohanAudio",RoundMotionArt.GohanCardId,clips);
   LoadCueBank("KrillinAudio",RoundMotionArt.KrillinCardId,clips);
   LoadCueBank("RaditzAudio",RoundMotionArt.RaditzCardId,clips);
   LoadCueBank("YamchaAudio",RoundMotionArt.YamchaCardId,clips);
   LoadCueBank("TienAudio",RoundMotionArt.TienCardId,clips);
   if(Data.IsRoundCard(RoundMotionArt.SaibamanCardId))
    LoadCueBank("SaibamanAudio",RoundMotionArt.SaibamanCardId,clips);
   var comboImpact=Resources.Load<AudioClip>("Audio/V0181/Tien/finger-impact");
   if(comboImpact==null)throw new InvalidOperationException("缺少连击命中音效");
   clips.Add("combo-hit",(comboImpact,.28f));
   roundCueClips=clips;
   if(clips.Values.All(cue=>cue.Clip==null))return;
   roundCueAudio=gameObject.AddComponent<AudioSource>();
   roundCueAudio.playOnAwake=false;
   roundCueAudio.spatialBlend=0;
   ApplyAudioLevels();
  }

  static void LoadCueBank(string bank,string card,Dictionary<string,(AudioClip Clip,float Gain)> clips){
   var file=Resources.Load<TextAsset>("Config/Characters/"+bank);
   var manifest=file==null?null:JsonUtility.FromJson<RoundCueManifest>(file.text);
   if(manifest==null||manifest.cardId!=card||manifest.cues==null||manifest.cues.Length==0||
      manifest.cues.Any(cue=>cue==null||string.IsNullOrWhiteSpace(cue.id)))
    throw new InvalidOperationException("音频配置无效："+bank);
   foreach(var cue in manifest.cues){
    var clip=string.IsNullOrEmpty(cue.resource)?null:Resources.Load<AudioClip>(cue.resource);
    if(clip==null&&!string.IsNullOrEmpty(cue.resource))throw new InvalidOperationException("缺少角色音频："+cue.id);
    clips.Add(cue.id,(clip,Mathf.Clamp01(cue.gain)));
   }
  }

  void PlayRoundCue(string id) {
   if(roundCueAudio==null||!roundCueClips.TryGetValue(id,out var cue)||cue.Clip==null)return;
   if(id=="combo-hit")comboCueEvents++;
   else roundCueEvents++;
   if(!silentAudio)roundCueAudio.PlayOneShot(cue.Clip,cue.Gain);
  }

  void PlayPursuitCue(RoundAction action){
   if(action.Move==RoundMove.Pursuit)PlayRoundCue("pursuit-start");
  }

  void PlayTienFingerImpact(RoundAction hit){
   if(!hit.Countered)PlayRoundCue(hit.Dodged?"dodge":"tien-finger-impact");
  }
  void PlayUnanimatedRoundCue(RoundAction hit){
   if(hit.Actor.CardId==RoundMotionArt.TienCardId&&hit.Ability?.trigger=="onRoundStart"){
    PlayRoundCue("tien-finger");
    PlayTienFingerImpact(hit);
   }else if(hit.Detonates){
    PlayRoundCue("saiba-burst-voice");
    PlayRoundCue("saiba-burst");
   }
  }

  void UpdateRoundCueAudio(RoundMotionPlayback run,float before) {
   if(run.Clip==null)return;
   float previous=run.PoseTimeAt(before);
   bool Cross(float cue)=>previous<cue&&run.PoseTime>=cue;
   if(run.Clip.id=="entry"&&Cross(455)){
    if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.CardId))PlayRoundCue("land");
    if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.PiccoloCardId))PlayRoundCue("piccolo-land");
    if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.GohanCardId))PlayRoundCue("gohan-land");
    if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.KrillinCardId))PlayRoundCue("krillin-land");
    if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.RaditzCardId))PlayRoundCue("raditz-land");
    if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.YamchaCardId))PlayRoundCue("yamcha-land");
    if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.TienCardId))PlayRoundCue("tien-land");
   }
   if(run.Actor.CardId==RoundMotionArt.VegetaCardId){
    if(run.Opening!=null&&Cross(210))PlayRoundCue("vegeta-opening");
    if(run.Action?.Move==RoundMove.Basic&&Cross(105))PlayRoundCue("vegeta-basic-voice");
    if(run.IsPursuit&&Cross(128))PlayRoundCue("vegeta-hammer-voice");
    if(run.VolleyHits!=null){
     if(Cross(run.ReleaseAt-90))PlayRoundCue("vegeta-volley-voice");
     for(int i=0;i<run.VolleyHits.Length;i++)if(Cross(run.VolleyLaunch(i)))PlayRoundCue("vegeta-launch");
    }
    foreach(var cue in run.Clip.impacts)
     if(Cross(cue.at)&&!run.Action.Countered)PlayRoundCue(run.Action.Dodged?"dodge":cue.kind=="hammer"?"vegeta-hammer":cue.kind=="body"?"punch":"heavy");
    return;
   }
   if(run.Actor.CardId==RoundMotionArt.PiccoloCardId&&run.Action!=null){
    if(run.Action.Move==RoundMove.Basic){
     if(Cross(70))PlayRoundCue("piccolo-dash");
     if(Cross(220))PlayRoundCue("piccolo-basic");
    }
    if(run.IsPursuit&&Cross(140))PlayRoundCue("piccolo-pursuit");
    bool revisedBeam=run.Clip.fxStyle=="Piccolo";
    if(run.IsCharge&&Cross(80)){
     PlayRoundCue("piccolo-gather");
     PlayRoundCue(revisedBeam?"piccolo-beam-charge-sfx":"charge");
    }
    if(run.IsCounter&&Cross(run.ReleaseAt)){PlayRoundCue("piccolo-counter");PlayRoundCue("piccolo-blast");}
    if(run.Clip.id=="specialBeam"&&Cross(run.ReleaseAt)){
     PlayRoundCue("piccolo-fire");
     PlayRoundCue(revisedBeam?"piccolo-beam-fire-sfx":"piccolo-blast");
    }
    foreach(var hit in run.Clip.impacts)
     if(Cross(hit.at)&&!run.Action.Countered)PlayRoundCue(run.Action.Dodged?"dodge":hit.kind=="sweep"?"heavy":hit.kind=="lift"?"uppercut":"punch");
    return;
   }
   if(run.Actor.CardId==RoundMotionArt.GohanCardId&&run.Action!=null){
    if(run.Clip.id=="gohanRally"){
     if(Cross(145))PlayRoundCue("gohan-rally-voice");
     if(Cross(350))PlayRoundCue("gohan-rally");
    }else if(run.Clip.id=="gohanHeavy"){
     if(Cross(run.ReleaseAt-105))PlayRoundCue("gohan-heavy-voice");
     if(Cross(run.Clip.impactAt)&&!run.Action.Countered)PlayRoundCue(run.Action.Dodged?"dodge":"gohan-heavy");
    }else if(run.Clip.id=="gohanFlash"){
     if(Cross(125))PlayRoundCue("gohan-flash-voice");
     if(Cross(run.Clip.impactAt)&&!run.Action.Countered)PlayRoundCue(run.Action.Dodged?"dodge":"gohan-flash");
    }
    return;
   }
   if(run.Actor.CardId==RoundMotionArt.KrillinCardId){
    if(run.Opening!=null&&Cross(285))PlayRoundCue("krillin-opening");
    if(run.Clip.id=="krillinBasic"){
     if(Cross(205))PlayRoundCue("krillin-basic-voice");
     if(Cross(302)&&!run.Action.Dodged)PlayRoundCue("krillin-basic-impact");
     if(Cross(run.Clip.impactAt))PlayRoundCue(run.Action.Dodged?"dodge":"heavy");
    }else if(run.Clip.id=="krillinDisc"){
     if(Cross(310))PlayRoundCue("krillin-disc-charge");
     if(Cross(run.ReleaseAt-180))PlayRoundCue("krillin-disc-voice");
     if(Cross(run.ReleaseAt))PlayRoundCue("krillin-disc-throw");
     if(Cross(run.Clip.impactAt)&&run.Action.Hits.Any(hit=>!hit.Dodged))PlayRoundCue("krillin-disc-cut");
    }else if(run.IsScatter){
     if(Cross(130))PlayRoundCue("krillin-scatter-charge");
     int shotsPerCue=run.Clip.fxStyle=="Krillin"?1:2;
     for(int i=0;i<run.VolleyHits.Length;i+=shotsPerCue)
      if(before<run.VolleyLaunch(i)&&run.Time>=run.VolleyLaunch(i))PlayRoundCue("krillin-scatter-shot");
    }
    return;
   }
   if(run.Actor.CardId==RoundMotionArt.RaditzCardId){
    if(run.Opening!=null&&Cross(185))PlayRoundCue("raditz-opening");
    if(run.IsRaditzEnergy){
     if(run.IsUltimate){
      if(Cross(138))PlayRoundCue("raditz-sunday-charge");
      if(Cross(222))PlayRoundCue("raditz-sunday");
      if(Cross(run.ReleaseAt))PlayRoundCue("raditz-sunday-release");
     }else{
      if(Cross(110))PlayRoundCue("raditz-friday");
      if(Cross(run.ReleaseAt))PlayRoundCue("raditz-friday-release");
      if(run.Action.EnhancedBasic&&Cross(run.ReleaseAt+105))PlayRoundCue("raditz-friday-hold");
     }
    }else if(run.IsPursuit){
     if(Cross(122))PlayRoundCue("raditz-kick");
     if(Cross(run.Clip.impactAt))PlayRoundCue(run.Action.Dodged?"dodge":"raditz-impact");
    }
    return;
   }
   if(run.Actor.CardId==RoundMotionArt.TienCardId&&run.Action!=null){
    if(run.Clip.id=="tienVolley"&&Cross(211))PlayRoundCue("tien-volley");
    if(run.Clip.id=="tienSet"&&Cross(115))PlayRoundCue("tien-set");
    if(run.Clip.id=="tienCannon"){
     if(Cross(235))PlayRoundCue("tien-cannon");
     if(Cross(run.Clip.impactAt)&&run.Action.Hits.Any(hit=>!hit.Dodged))PlayRoundCue("tien-cannon-impact");
    }
    if(run.Clip.id=="tienFinger"){
     if(Cross(run.ReleaseAt))PlayRoundCue("tien-finger");
     if(Cross(run.Clip.impactAt))PlayTienFingerImpact(run.Action);
    }
    foreach(var cue in run.Clip.impacts){
     if(!Cross(cue.at)||run.Action.Countered)continue;
     if(run.Action.Dodged){
      if(cue.at==run.Clip.impactAt)PlayRoundCue("dodge");
     }else PlayRoundCue(cue.kind=="lift"?"uppercut":cue.kind=="hammer"?"heavy":"punch");
    }
    return;
   }
   if(run.Actor.CardId==RoundMotionArt.YamchaCardId&&run.Action!=null){
    if(run.Clip.id=="yamchaBasic"&&Cross(175))PlayRoundCue("yamcha-basic");
    if(run.Clip.id=="yamchaWolf"&&Cross(160))PlayRoundCue("yamcha-wolf");
    if(run.Clip.id=="yamchaSweep"&&Cross(115))PlayRoundCue("yamcha-sweep");
    if(run.IsYamchaFeign){
     if(Cross(812))PlayRoundCue("yamcha-rise");
     if(Cross(run.ImpactAt))PlayRoundCue("yamcha-land");
    }else if(run.Clip.id=="yamchaRise"&&Cross(300))PlayRoundCue("yamcha-rise");
    foreach(var cue in run.Clip.impacts){
     if(!Cross(cue.at)||run.Action.Countered)continue;
     if(run.Action.Dodged){
      if(cue.at==run.Clip.impactAt)PlayRoundCue("dodge");
     }else PlayRoundCue(cue.kind=="sweep"?"sweep":cue.at==run.Clip.impactAt?"heavy":"punch");
    }
    return;
   }
   if(run.Actor.CardId==RoundMotionArt.SaibamanCardId&&run.Action!=null){
    if(run.Clip.id=="saibaBasic"&&Cross(130))PlayRoundCue("saiba-basic");
    if(run.Clip.id=="saibaSmash"&&Cross(130))PlayRoundCue("saiba-smash");
    if(run.IsAcid&&Cross(run.ReleaseAt))PlayRoundCue("saiba-acid");
    if(run.IsDetonation){
     if(Cross(55))PlayRoundCue("saiba-burst-voice");
     if(Cross(RoundMotionArt.SaibamanPushOff))PlayRoundCue("piccolo-dash");
     if(Cross(RoundMotionArt.SaibamanClasp))PlayRoundCue("punch");
     if(Cross(run.Clip.impactAt))PlayRoundCue("saiba-burst");
    }
    foreach(var cue in run.Clip.impacts){
     if(Cross(cue.at)&&!run.Action.Countered&&!run.Action.Dodged)
      PlayRoundCue(cue.kind=="hammer"?"heavy":"punch");
    }
    return;
   }
   if(run.Actor.CardId!=RoundMotionArt.CardId)return;
   if(run.Clip.id=="opening"&&Cross(200)){
    PlayRoundCue("cloth");
    if(run.Actor.Side==0)PlayRoundCue("voice-opening");
   }
   if(run.Action==null)return;
   var beam=run.Clip.beam;
   if(beam!=null&&beam.gather>0){
    // Start the spoken wind-up early enough to leave room for the release call.
    if(Cross(Mathf.Max(1,beam.fire-760)))PlayRoundCue("voice-gather");
    if(Cross(beam.gather))PlayRoundCue("charge");
    if(Cross(beam.fire)){PlayRoundCue("voice-fire");PlayRoundCue("beam");}
    if(Cross(beam.cut))PlayRoundCue("beam-tail");
    if(run.Action.Dodged&&!run.Action.Countered&&Cross(beam.fire+95))PlayRoundCue("dodge");
   }
   if(run.Action.Move==RoundMove.Basic){
    if(Cross(72))PlayRoundCue("voice-punch");
    if(roundDemo&&Cross(80))PlayRoundCue("guard");
   }
   if(run.Action.Move==RoundMove.Pursuit&&Cross(138))PlayRoundCue("voice-kick");
   foreach(var hit in run.Clip.impacts){
    if(!Cross(hit.at)||run.Action.Countered)continue;
    string cue=run.Action.Dodged?"dodge":hit.kind=="sweep"?"sweep":hit.kind=="head"?"punch":hit.kind=="uppercut"?"uppercut":"heavy";
    PlayRoundCue(cue);
   }
  }

  void PlayRoundHitVoice(RoundAction hit) {
   if(hit.Dodged||hit.Healing)return;
   string card=hit.Target.CardId;
   if(card!=RoundMotionArt.CardId&&card!=RoundMotionArt.VegetaCardId&&card!=RoundMotionArt.PiccoloCardId&&card!=RoundMotionArt.GohanCardId&&card!=RoundMotionArt.KrillinCardId&&card!=RoundMotionArt.RaditzCardId&&card!=RoundMotionArt.YamchaCardId&&card!=RoundMotionArt.TienCardId&&card!=RoundMotionArt.SaibamanCardId)return;
   if(hit.GuardAbility!=null&&card==RoundMotionArt.GohanCardId){
    PlayRoundCue("gohan-guard");
    if(hit.TargetHPAfter>0)return;
   }
   bool heavy=hit.TargetHPAfter<=0||hit.Move==RoundMove.Ultimate||
    (hit.TargetState!=RoundHitState.None&&hit.TargetState!=RoundHitState.Standing);
   if(card==RoundMotionArt.PiccoloCardId)PlayRoundCue(heavy?"piccolo-hit-heavy":"piccolo-hit-light");
   else if(card==RoundMotionArt.VegetaCardId)PlayRoundCue(heavy?"vegeta-hit-heavy":"vegeta-hit-light");
   else if(card==RoundMotionArt.GohanCardId)PlayRoundCue(heavy?"gohan-hit-heavy":"gohan-hit-light");
   else if(card==RoundMotionArt.KrillinCardId)PlayRoundCue(heavy?"krillin-hit-heavy":"krillin-hit-light");
   else if(card==RoundMotionArt.RaditzCardId)PlayRoundCue(heavy?"raditz-hit-heavy":"raditz-hit-light");
   else if(card==RoundMotionArt.YamchaCardId)PlayRoundCue(heavy?"yamcha-hit-heavy":"yamcha-hit-light");
   else if(card==RoundMotionArt.TienCardId)PlayRoundCue(heavy?"tien-hit-heavy":"tien-hit-light");
   else if(card==RoundMotionArt.SaibamanCardId)PlayRoundCue(heavy?"saiba-hit-heavy":"saiba-hit-light");
   else PlayRoundCue(heavy?"voice-hit-heavy":"voice-hit-light");
  }

  void PauseRoundCueAudio(bool freeze) {
   if(roundCueAudio==null)return;
   if(freeze)roundCueAudio.Pause();
   else roundCueAudio.UnPause();
  }
 }
}
