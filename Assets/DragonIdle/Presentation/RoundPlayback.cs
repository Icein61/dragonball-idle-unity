using UnityEngine;

namespace DragonIdle {
 public partial class DemoGame {
  const float RoundActionGap=.78f,RoundOpeningGap=.18f,RoundPursuitGap=.14f,RoundReactionGap=.075f;
  void StartRoundPresentation(){
   roundLast=null;roundLog.Clear();roundPaused=false;roundClock=RoundOpeningGap;
   PrepareRoundMotion();
   NavigateGallery(PageRoute.RoundBattle);
  }
  void UpdateRoundPreview(float dt){
   if(page!=PageRoute.RoundBattle||roundBattle==null)return;
   bool presenting=roundMotion!=null;
   UpdateRoundMotion(dt);
   if(presenting||roundPaused||roundBattle.Result!=0)return;
   roundClock-=dt;
   if(roundClock>0)return;
   if(roundDemo&&!ReducedRoundMotion){
    var reviving=roundBattle.NextRevivingFighter;
    if(reviving?.CardId==RoundMotionArt.YamchaCardId&&demoReactions.TryGetValue(reviving,out var fall)&&
       fall.Defeated&&fall.Active)return;
   }
   AdvanceRoundPreview(false);
  }
  float NormalRoundActionGap=>roundDemo&&DemoScenes().demo.actionGap>0?DemoScenes().demo.actionGap:RoundActionGap;
  float NextRoundActionGap(){
   if(roundBattle.ReactionSkillNext)return RoundReactionGap;
   if(!roundBattle.PursuitNext)return NormalRoundActionGap;
   float remaining=DemoChaseRemaining();
   return remaining>=0?Mathf.Max(0,remaining-RoundMotionPlayback.PursuitContact)/1000:RoundPursuitGap;
  }
  void AdvanceRoundPreview(bool manual){
   if(roundBattle==null)return;
   if(roundMotion!=null){FinishRoundMotion(true);return;}
   if(StartNextRoundOpening(manual)){DrawRoundPreview();return;}
   if(roundBattle.Result!=0)return;
   var step=roundBattle.ResolveStep(!ReducedRoundMotion);
   roundLast=step.Action;
   roundClock=NextRoundActionGap();
   if(roundLast!=null)PresentRoundStep(step,manual);
   DrawRoundPreview();
  }

  void PresentRoundStep(RoundStep step,bool manual){
   var action=step.Action;
   if(roundDemo)BeginDemoComboAction(action);
   if(StartRoundActionMotion(action,manual)){
    roundBefore=step.Before;
    return;
   }
   PlayPursuitCue(action);
   PlayUnanimatedRoundCue(action);
   if(action.Detonates&&roundDemo)CompleteDemoDetonation(action.Actor);
   if(action.Revives){
    if(roundDemo)ApplyDemoHit(action);
    PlayRoundCue("yamcha-rise");
   }
   if(!action.Charging)foreach(var hit in action.Hits){
    if(roundDemo){RecordDemoHit(hit);RevealDemoComboHit(hit,true);}
    PlayRoundHitVoice(hit);
   }
   if(roundDemo)demoStats.RecordCost(action);
   AddRoundLog(action);
  }
 }
}
