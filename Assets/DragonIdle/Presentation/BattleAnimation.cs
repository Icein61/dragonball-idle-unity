using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame {
  ActionPresentation action;int playedImpacts;AudioClip[] battleClips;AudioSource battleSound;
  bool ReducedBattleMotion {get{return audioSettings!=null&&audioSettings.ReducedMotion;}}
  string PreparingAction(BattleEvent e){return e.Move==Move.Stunned?e.Text:e.Actor.Name+" · "+(e.Move==Move.Basic?"普通攻击":e.Move==Move.Special?e.Actor.SpecialName:e.Actor.UltimateName)+"  /  出手中";}
  void ClearBattlePresentation(){action=null;last=null;fx=0;playedImpacts=0;clock=0;if(battleSound!=null)battleSound.Stop();}
  void UpdateBattlePresentation(float seconds){
   if(!running||paused)return;
   if(action!=null&&!action.Complete){
    action.Tick(seconds*rate,false);fx=1-(float)action.Progress;
    if(log.Count>0&&(action.Event.Hits.Count==0||action.HitVisible(action.Event.Hits.Count-1)))log[0]=action.Event.Text;
    while(playedImpacts<action.Event.Hits.Count&&action.HitVisible(playedImpacts)){
     var hit=action.Event.Hits[playedImpacts++];PlayBattleSound(hit.Dodged?3:action.Melee?1:2);
    }
    if(action.Complete){SettleBattle();clock=0;}return;
   }
   clock+=seconds*rate;if(clock>=.20f){clock=0;Advance();}
  }
  Vector2 FighterFeet(Unit u){return new Vector2(u.Side==0?(u.Slot<3?560:360):(u.Slot<3?935:1135),u.Slot<3?407+u.Slot*120:390+(u.Slot-3)*96);}
  Vector2 AnimatedFeet(Unit u){
   var p=FighterFeet(u);if(action==null||action.Complete||ReducedBattleMotion)return p;
   float progress=(float)action.Progress,sign=u.Side==0?1:-1;
   if(u==action.Event.Actor&&action.Melee&&action.Event.Target!=null){
    var end=FighterFeet(action.Event.Target)+new Vector2(-sign*66,0);
    float blend=progress<.42f?Mathf.SmoothStep(0,1,progress/.42f):progress<.72f?1:1-Mathf.SmoothStep(0,1,(progress-.72f)/.28f);
    p=Vector2.Lerp(p,end,blend);p.y-=Mathf.Sin(blend*Mathf.PI)*18;
   }else{
    int hit=action.Event.Hits.FindIndex(h=>h.Target==u);
    if(hit>=0&&action.HitVisible(hit)&&progress<.85f)p.x-=sign*(action.Event.Hits[hit].Dodged?22:8)*Mathf.Sin((progress-.52f)/.33f*Mathf.PI);
   }
   return p;
  }
  VisualVitals ShownVitals(Unit u){return action==null?new VisualVitals(u):action.Vitals(u);}
  void DrawBattleActors(List<Unit> units){
   if(battle==null&&action!=null)ClearBattlePresentation();
   // Ground-aligned layers, with the attacking fighter in front while crossing the arena.
   foreach(var u in units.OrderBy(x=>FighterFeet(x).y))if(action==null||action.Complete||u!=action.Event.Actor)DrawFighter(u);
   if(action!=null&&!action.Complete)DrawFighter(action.Event.Actor);
   DrawBattleEffects();
   foreach(var u in units)DrawFighterInfo(u);
   if(paused){Box(680,292,130,31,new Color(.03f,.05f,.08f,.9f));Label(697,295,100,28,"演出已暂停",16,gold);}
  }
  void DrawFighter(Unit u){
   int index=Data.Index(u.CardId);if(index<0)return;var feet=AnimatedFeet(u);var v=ShownVitals(u);
   int pose=action==null?(v.HP<=0?10:v.HP/u.MaxHP<.25?9:0):action.Pose(u);
   GUI.color=new Color(0,0,0,.3f);GUI.DrawTexture(new Rect(feet.x-42,feet.y-6,84,13),disc);GUI.color=Color.white;
   if(v.Shield>0)Ring(feet+new Vector2(0,-34),45,54,new Color(.3f,.85f,1,.38f),2);
   float fade=v.HP<=0?.32f:1;PaintFighter(index,pose,feet,u.Side==1,new Color(1,1,1,fade),u.Slot<3?1:.78f);
   if(u.Stunned&&v.HP>0){Label(feet.x-15,feet.y-FighterHeight(index)*(u.Slot<3?1:.78f)-24,80,24,"眩晕",12,gold);}
   if(action!=null&&!action.Complete&&u==action.Event.Actor)Ring(feet+new Vector2(0,1),44,9,new Color(1,.7f,.3f,.7f),2);
  }
  void DrawFighterInfo(Unit u){
   var p=FighterFeet(u);var v=ShownVitals(u);
   Bar(p.x-43,p.y+5,86,4,v.HP/u.MaxHP,u.Side==0?new Color(.3f,.8f,.57f):new Color(.95f,.38f,.42f));
   Bar(p.x-43,p.y+11,86,2,v.Rage/100,gold);
   Label(p.x-57,p.y+15,118,21,(u.Slot+1)+" "+u.Name.Replace("试炼·",""),12,v.HP>0?ink:muted);
   if(!new Rect(p.x-54,p.y-78,108,114).Contains(Event.current.mousePosition))return;
   string buffs=string.Join(" / ",u.Effects.Select(e=>EffectText(e.Type)+" "+(e.Value*100).ToString("0.#")+"% · "+(e.Expires==int.MaxValue?"整场":(e.Expires-u.Opportunities)+"回合")).ToArray());
   Box(590,294,320,103,new Color(.025f,.045f,.075f,.96f));
   Label(603,301,294,90,u.Name+"  生命 "+v.HP.ToString("0")+" / "+u.MaxHP.ToString("0")+"\n怒气 "+v.Rage.ToString("0")+" · 绝招CD "+u.Cooldown+" · 护盾 "+v.Shield.ToString("0")+"\n"+buffs,13);
  }
  void InitializeBattleAudio(){
   battleSound=gameObject.AddComponent<AudioSource>();battleSound.playOnAwake=false;battleSound.spatialBlend=0;
   battleClips=new AudioClip[4];int hz=22050;
   for(int kind=0;kind<4;kind++){
    float seconds=kind==2?.36f:.16f;var samples=new float[(int)(seconds*hz)];var rng=new System.Random(50+kind);
    for(int i=0;i<samples.Length;i++){
     double t=i/(double)hz,p=t/seconds,noise=rng.NextDouble()*2-1,envelope=Math.Sin(Math.PI*p)*Math.Exp(-p*(kind==1?5:2));
     double wave=kind==1?noise*.6+Math.Sin(t*90*Math.PI*2):kind==2?Math.Sin((230*t+500*t*t)*Math.PI*2)*.6+noise*.25:noise*.5+Math.Sin((600*t-900*t*t)*Math.PI*2)*.2;
     samples[i]=(float)(wave*envelope*.24);
    }
    var clip=AudioClip.Create(new[]{"charge-swoosh","impact","energy-release","dodge"}[kind],samples.Length,1,hz,false);clip.SetData(samples,0);battleClips[kind]=clip;
   }
  }
  void PlayBattleSound(int kind){if(battleSound==null||silentAudio)return;ApplyAudioLevels();battleSound.PlayOneShot(battleClips[kind]);}
 }
}
