using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  sealed class RoundMotionPlayback {
   internal RoundFighter Actor;
   internal RoundAction Action;
   internal RoundAbility Opening;
   internal RoundMotionArt Library;
   internal RoundAction[] VolleyHits;
   internal int RevealedCount;
   internal readonly Dictionary<RoundFighter,(RoundAction Hit,float At)> SeenHits=new Dictionary<RoundFighter,(RoundAction,float)>();
   internal RoundMotionArt.Clip Clip;
   internal float Time;
   internal Vector2 Travel, BeamTarget, SourceOffset;
   internal bool Revealed, Manual, DemoLocomotion;
   const float PiccoloRunSpeed=.44f, PiccoloBasicTimeScale=.80f/BasicRate;
   internal const float PiccoloBasicStride=88*PiccoloBasicTimeScale;
   Vector2 approachEnd;
   internal float ApproachDuration {get;private set;}
   internal float ApproachStart => IsPursuit?PursuitLead:0;
   float AfterApproachRate => IsPursuit?Clip.duration/(PursuitLength-PursuitLead):PlaybackRate;
   float ReturnPoseEnd => Clip.root[Clip.root.Length-1].at;
   float ReturnStart => ApproachStart+ApproachDuration+(Clip.returnAt-Clip.runUntil)/AfterApproachRate;
   internal float PiccoloReturnDuration {get;private set;}

   internal void PacePiccoloApproach(){
    approachEnd=Clip.PlanarShift(Clip.runUntil,Travel);
    float contactDistance=Clip.PlanarShift(Clip.returnAt,Travel).magnitude;
    if(contactDistance<=2)return;
    float basicTimeRatio=IsPursuit?1:PiccoloBasicTimeScale;
    ApproachDuration=IsPursuit?Mathf.Clamp(approachEnd.magnitude/.98f,265,390):
     Mathf.Max(196,approachEnd.magnitude/PiccoloRunSpeed)*basicTimeRatio;
    PiccoloReturnDuration=Mathf.Clamp(contactDistance/1.03f,250,375)*basicTimeRatio;
   }

   internal float ClockAtPose(float pose){
    if(ApproachDuration<=0)return pose;
    if(pose<=Clip.runUntil)return ApproachStart+ApproachDuration*pose/Clip.runUntil;
    if(pose<=Clip.returnAt)return ReturnStart+(pose-Clip.returnAt)/AfterApproachRate;
    if(pose<ReturnPoseEnd)return ReturnStart+PiccoloReturnDuration*Mathf.InverseLerp(Clip.returnAt,ReturnPoseEnd,pose);
    return ReturnStart+PiccoloReturnDuration+(pose-ReturnPoseEnd)/AfterApproachRate;
   }

   internal const float PursuitLead=104, PursuitLength=505, OpeningFlash=320;
   internal const float ReleaseSlowSpan=120, ReleaseRate=.62f, DemoRunRate=.65f;
   const float CounterGuardHold=480;
   const float BasicRate=.68f, UltimateRate=.86f, HeavyUltimateRate=.73f, ChargeRate=.94f;
   internal bool IsCharge => Action?.Charging==true;
   internal bool IsUltimate => Action?.Move==RoundMove.Ultimate&&!IsCharge;
   internal bool IsCounter => Action?.Ability?.trigger=="onSingleTarget";
   internal float PlaybackRate => IsCounter ? .70f :
    Action?.Move==RoundMove.Basic ? BasicRate : IsPursuit&&IsScatter ? .86f : IsCharge ? ChargeRate :
    IsUltimate ? (Clip?.id=="gohanHeavy" ? HeavyUltimateRate : UltimateRate) : 1f;
   internal float ReleaseAt => Clip!=null&&Clip.launchAt>0?Clip.launchAt:(Clip?.beam?.fire>0?Clip.beam.fire:145);
   internal bool IsScatter=>Clip?.id=="krillinScatter";
   internal bool IsRaditzEnergy=>Clip?.fx=="raditzKi";
   internal bool IsRaditzTwin=>Clip?.id=="raditzTwin";
   internal float VolleyLaunch(int index)=>ReleaseAt+(IsScatter?PursuitLead+76*index:IsRaditzEnergy?65*index:145*index);
   internal float VolleyImpact(int index)=>VolleyLaunch(index)+(IsScatter?160:IsRaditzEnergy?Clip.impactAt-Clip.launchAt:260);
   internal RoundAction LastSeenHit=>IsCharge?null:VolleyHits==null||RevealedCount==0?Action:VolleyHits[RevealedCount-1];
   internal float CounterGuardWeight {
    get {
     if(!Revealed||Action?.Countered!=true)return 0;
     float age=Time-ImpactAt;
     return Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,85,age))*
      (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(210,410,age)));
    }
   }
   internal float CameraWeight {
    get {
     if(IsCounter)
      return Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,170,Time))*
       (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(ImpactAt+60,Duration,Time)));
     if(!IsUltimate)return 0;
     float releaseLength=Duration-ReleaseAt;
     float inEnd=ReleaseAt+Mathf.Min(160,releaseLength*.32f);
     float outStart=Duration-Mathf.Min(380,releaseLength*.46f);
     float push=Mathf.SmoothStep(0,1,Mathf.InverseLerp(ReleaseAt,inEnd,Time));
     float pull=Mathf.SmoothStep(0,1,Mathf.InverseLerp(outStart,Duration,Time));
     return push*(1-pull);
    }
   }
   internal float ReleaseShade {
    get {
     if(IsCounter)return CameraWeight*.56f;
     if(!IsUltimate)return 0;
     float sinceRelease=Time-ReleaseAt;
     float appear=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,90,sinceRelease));
     float clear=Mathf.SmoothStep(0,1,Mathf.InverseLerp(115,270,sinceRelease));
     return appear*(1-clear);
    }
   }
   internal string SkillName => Opening?.name??Action?.Ability?.name??(IsUltimate||IsCharge||Library!=null&&(Clip?.id=="gohanRally"||Clip?.id=="gohanFlash"||Clip?.id=="krillinBasic"||IsScatter||IsRaditzEnergy||Clip?.id=="raditzKick"||Clip?.id=="yamchaBasic"||Clip?.id=="yamchaSweep"||Clip?.id=="tienVolley"||Clip?.id=="tienSet")?Action.Name:null);

   internal void Advance(float milliseconds){
    if(ApproachDuration>0){Time+=milliseconds;return;}
    milliseconds*=PlaybackRate;
    if(!IsUltimate){
     if(DemoLocomotion&&!IsPursuit&&Clip?.runUntil>0&&Time<Clip.runUntil){
      float running=Mathf.Min(milliseconds,(Clip.runUntil-Time)/DemoRunRate);
      Time+=running*DemoRunRate;
      milliseconds-=running;
     }
     Time+=milliseconds;
     return;
    }
    float beforeRelease=Mathf.Clamp(ReleaseAt-Time,0,milliseconds);
    Time+=beforeRelease;
    milliseconds-=beforeRelease;
    float slowed=Mathf.Clamp((ReleaseAt+ReleaseSlowSpan-Time)/ReleaseRate,0,milliseconds);
    Time+=slowed*ReleaseRate+(milliseconds-slowed);
   }
   internal bool IsPursuit => Action?.Move==RoundMove.Pursuit;
   float AuthoredDuration => IsScatter?PursuitLead+Clip.duration:ApproachDuration>0?ClockAtPose(Clip.duration):IsPursuit?PursuitLength:(Clip?.duration??(Opening!=null?520:690));
   internal float Duration => Action?.Countered==true?
    Mathf.Max(AuthoredDuration,ImpactAt+CounterGuardHold*(ApproachDuration>0?1:PlaybackRate)):AuthoredDuration;
   internal float ImpactAt => VolleyHits!=null?VolleyImpact(0):ApproachDuration>0?ClockAtPose(Clip.impactAt):IsPursuit?
    PursuitLead+(Clip==null?115:Clip.impactAt*(PursuitLength-PursuitLead)/Clip.duration):
    (Clip?.impactAt??280);
   internal float FlashTime => IsPursuit&&Time<PursuitLead ? Time/PursuitLead : -1;
   internal float PoseTime => PoseTimeAt(Time);
   bool HasTravel=>ApproachDuration>0||(Clip?.fullStride==true?
    Mathf.Abs(Clip.Travel(Clip.runUntil)*Travel.x)>4:Travel.x>4);
   internal RoundMotionArt.Pose StagePose {
    get {
     float at=PoseTime;
     if(ApproachDuration>0&&Time>=ApproachStart&&Time<ApproachStart+ApproachDuration){
      if(IsPursuit)return Library.Atlas[Clip.approachDrawing];
      return Library.Atlas[Clip.runFrames[(int)((Time-ApproachStart)/PiccoloBasicStride)%Clip.runFrames.Length]];
     }
     if(DemoLocomotion&&HasTravel){
      if(ApproachDuration<=0&&at<Clip.runUntil&&(!IsPursuit||Time>=PursuitLead)){
       float stride=Clip.fullStride?Clip.runUntil/(float)Clip.runFrames.Length:
        Mathf.Min(IsPursuit?80:92,Clip.runUntil*.47f);
       return Library.Atlas[Clip.runFrames[(int)(at/stride)%Clip.runFrames.Length]];
      }
      float returning=Clip.ReturnPhase(at);
      if(Clip.returnHop>0&&returning>0&&returning<1)return Library.Atlas[Clip.hopDrawing];
     }
     return Library.At(Clip,at);
    }
   }

   internal float PoseTimeAt(float at){
    if(ApproachDuration>0){
     float elapsed=at-ApproachStart;
     if(elapsed<=ApproachDuration)return Clip.runUntil*Mathf.Clamp01(elapsed/ApproachDuration);
     if(at<=ReturnStart)return Clip.runUntil+(elapsed-ApproachDuration)*AfterApproachRate;
     float returning=at-ReturnStart;
     if(returning<PiccoloReturnDuration)
      return Mathf.Lerp(Clip.returnAt,ReturnPoseEnd,returning/PiccoloReturnDuration);
     return ReturnPoseEnd+(returning-PiccoloReturnDuration)*AfterApproachRate;
    }
    if(IsScatter)return Mathf.Max(0,at-PursuitLead);
    if(!IsPursuit)return at;
    float motion=Mathf.Clamp01((at-PursuitLead)/(PursuitLength-PursuitLead));
    return motion*(Clip?.duration??(PursuitLength-PursuitLead));
   }
   internal Vector2 ShiftAt(float at) {
    if(ApproachDuration>0&&at<ApproachStart+ApproachDuration){
     float progress=Mathf.Clamp01((at-ApproachStart)/ApproachDuration);
     if(IsPursuit)return approachEnd*Mathf.SmoothStep(0,1,progress)-
      Vector2.up*(22*Mathf.Sin(Mathf.PI*progress));
     return approachEnd*progress;
    }
    if(Clip!=null){
     float poseAt=PoseTimeAt(at);
     var shift=Clip.PlanarShift(poseAt,Travel,HasTravel);
     if(IsScatter)shift.y-=Mathf.Sin(Mathf.PI*Mathf.InverseLerp(140,505,poseAt))*22;
     return shift;
    }
    if(Action==null||IsUltimate||Action.Healing)return Vector2.zero;
    float start=IsPursuit?PursuitLead:0;
    float settle=IsPursuit?43:85;
    float phase=Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,ImpactAt,at))*
     (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(ImpactAt+settle,Duration,at)));
    return Travel*phase;
   }
  }

  struct RoundVitals {
   internal readonly double HP, Shield;
   internal readonly int Rage;
   internal readonly IReadOnlyList<RoundFighter.StatChange> DefenseChanges,DodgeChanges,AttackChanges;
   internal RoundVitals(RoundFighter fighter,bool freezeDefense=false) {
    HP = fighter.HP;
    Shield = fighter.Shield;
    Rage = fighter.Rage;
    AttackChanges=freezeDefense?fighter.AttackChanges.ToArray():fighter.AttackChanges;
    DodgeChanges=freezeDefense?fighter.DodgeChanges.ToArray():fighter.DodgeChanges;
    DefenseChanges=freezeDefense?fighter.DefenseChanges.ToArray():fighter.DefenseChanges;
   }
   internal RoundVitals(RoundAction hit,RoundVitals previous){
    HP=hit.TargetHPAfter;
    Shield=hit.TargetShieldAfter;
    Rage=hit.TargetRageAfter;
    AttackChanges=previous.AttackChanges;
    DefenseChanges=previous.DefenseChanges;
    DodgeChanges=previous.DodgeChanges;
   }
  }

  readonly Queue<(RoundFighter Actor,RoundAbility Skill)> roundOpenings = new Queue<(RoundFighter,RoundAbility)>();
  readonly Dictionary<RoundFighter, RawImage> roundStanding = new Dictionary<RoundFighter, RawImage>();
  RoundMotionArt roundArt,vegetaArt,piccoloArt,gohanArt,krillinArt,raditzArt,yamchaArt,tienArt;
  RoundMotionPlayback roundMotion;
  Dictionary<RoundFighter, RoundVitals> roundBefore;
  RawImage roundFocusPose;
  RectTransform roundFocusTarget;
  RoundMotionFx roundFrontFx, roundRearFx;
  Text roundMotionOutcome;
  float roundIdleTime;

  bool ReducedRoundMotion => audioSettings != null && audioSettings.ReducedMotion;
  RoundMotionArt GokuArt => roundArt ?? (roundArt = RoundMotionArt.Load());
  RoundMotionArt VegetaArt => vegetaArt??(vegetaArt=RoundMotionArt.Load("Art/V0154/Vegeta/"));
  RoundMotionArt PiccoloArt => piccoloArt??(piccoloArt=RoundMotionArt.Load("Art/V0144/Piccolo/"));
  RoundMotionArt GohanArt => gohanArt??(gohanArt=RoundMotionArt.Load("Art/V0153/Gohan/"));
  RoundMotionArt RaditzArt => raditzArt??(raditzArt=RoundMotionArt.Load("Art/V0161/Raditz/"));
  RoundMotionArt TienArt => tienArt??(tienArt=RoundMotionArt.Load("Art/V0173/Tien/"));
  RoundMotionArt YamchaArt => yamchaArt??(yamchaArt=RoundMotionArt.Load("Art/V0172/Yamcha/"));
  RoundMotionArt KrillinArt => krillinArt??(krillinArt=RoundMotionArt.Load("Art/V0154/Krillin/"));
  bool WaitingForRoundImpact => roundMotion != null && roundMotion.Action != null && !roundMotion.IsCharge && !roundMotion.Revealed;
  int VisibleRoundResult => WaitingForRoundImpact ? 0 : roundBattle.Result;

  void PrepareRoundMotion() {
   ResetRoundMotion();
   LoadRoundCueAudio();
   if (roundDemo) PrepareDemoPresentation();
   if (ReducedRoundMotion) return;
   foreach (var fighter in roundBattle.Order)
    foreach(var skill in RoundRules().OpeningSkills(fighter.CardId)
     .Concat(RoundRules().For(fighter.CardId).skills.Where(s=>s.trigger=="whileAlive"||s.trigger=="roundStart")))
     roundOpenings.Enqueue((fighter,skill));
   if (roundMotion==null) StartNextRoundOpening(false);
  }

  void ResetRoundMotion() {
   ClearPursuitSheen();
   roundOpenings.Clear();
   roundStanding.Clear();
   roundMotion = null;
   roundBefore = null;
   roundFocusPose = null;
   roundFocusTarget = roundFocusWorld = roundSkillBadge = null;
   roundMotionOutcome = null;
   roundFrontFx = roundRearFx = null;
   roundIdleTime = 0;
   ResetDemoPresentation();
  }

  bool StartNextRoundOpening(bool manual) {
   if (roundOpenings.Count == 0) return false;
   var opening = roundOpenings.Dequeue();
   RoundMotionArt library=null;
   if(opening.Actor.CardId==RoundMotionArt.CardId&&opening.Skill.name==RoundMotionArt.OpeningSkill)library=GokuArt;
   else if(opening.Actor.CardId==RoundMotionArt.VegetaCardId)library=VegetaArt;
   else if(opening.Actor.CardId==RoundMotionArt.KrillinCardId)library=KrillinArt;
   else if(opening.Actor.CardId==RoundMotionArt.RaditzCardId)library=RaditzArt;
   roundMotion = new RoundMotionPlayback { Actor=opening.Actor, Opening=opening.Skill,
    Library=library, Clip=library==null?null:library["opening"], Manual=manual };
   PlayRoundCue("opening-passive");
   return true;
  }

  bool StartRoundActionMotion(RoundAction action, bool manual) {
   if (ReducedRoundMotion) return false;
   string track=null;
   RoundMotionArt library=null;
   if(action.Actor.CardId==RoundMotionArt.CardId){
    library=GokuArt;
    switch(action.Move){
     case RoundMove.Basic: track=action.TargetState==RoundHitState.Knockdown?"sweep":"basic";break;
     case RoundMove.Ultimate:track="ultimate";break;
     case RoundMove.Pursuit:track="pursuit";break;
    }
   }else if(action.Actor.CardId==RoundMotionArt.VegetaCardId){
    library=VegetaArt;
    switch(action.Move){
     case RoundMove.Basic:track=action.TargetState==RoundHitState.Knockback?"kick":"basic";break;
     case RoundMove.Ultimate:track="volley";break;
     case RoundMove.Pursuit:track="pursuit";break;
    }
   }
   else if(action.Actor.CardId==RoundMotionArt.PiccoloCardId){
    library=PiccoloArt;
    switch(action.Move){
     case RoundMove.Basic:track=action.TargetState==RoundHitState.Knockdown?"slam":"basic";break;
     case RoundMove.Ultimate:track=action.Charging?"piccoloCharge":"specialBeam";break;
     case RoundMove.Pursuit:track="pursuit";break;
     case RoundMove.Skill:if(action.Ability?.trigger=="onSingleTarget")track="piccoloCounter";break;
    }
   }
   else if(action.Actor.CardId==RoundMotionArt.GohanCardId){
    library=GohanArt;
    switch(action.Move){
     case RoundMove.Basic:track="gohanRally";break;
     case RoundMove.Ultimate:track="gohanHeavy";break;
     case RoundMove.Pursuit:track="gohanFlash";break;
    }
   }
   else if(action.Actor.CardId==RoundMotionArt.KrillinCardId){
    library=KrillinArt;
    switch(action.Move){
     case RoundMove.Basic:track="krillinBasic";break;
     case RoundMove.Ultimate:track="krillinDisc";break;
     case RoundMove.Pursuit:track="krillinScatter";break;
    }
   }
   else if(action.Actor.CardId==RoundMotionArt.RaditzCardId){
    library=RaditzArt;
    switch(action.Move){
     case RoundMove.Basic:track=action.EnhancedBasic?"raditzSurge":"raditzShot";break;
     case RoundMove.Ultimate:track="raditzTwin";break;
     case RoundMove.Pursuit:track="raditzKick";break;
    }
   }
   else if(action.Actor.CardId==RoundMotionArt.YamchaCardId){
    library=YamchaArt;
    switch(action.Move){
     case RoundMove.Basic:track="yamchaBasic";break;
     case RoundMove.Ultimate:track="yamchaWolf";break;
     case RoundMove.Pursuit:track="yamchaSweep";break;
     case RoundMove.Skill:if(action.Revives)track="yamchaRise";break;
    }
   }
   else if(action.Actor.CardId==RoundMotionArt.TienCardId){
    library=TienArt;
    switch(action.Move){
     case RoundMove.Basic:track="tienVolley";break;
     case RoundMove.Ultimate:track="tienCannon";break;
     case RoundMove.Pursuit:track="tienSet";break;
     case RoundMove.Skill:if(action.Ability?.trigger=="onRoundStart")track="tienFinger";break;
    }
   }
   if(track==null&&!roundDemo&&action.Move!=RoundMove.Ultimate&&action.Ability==null)return false;
   roundMotion=new RoundMotionPlayback {
    Actor=action.Actor, Action=action, Library=library, Clip=track==null?null:library[track],
    VolleyHits=action.SequentialHits||(library==raditzArt&&track!="raditzKick"&&track!=null)?action.Hits.ToArray():null, Manual=manual
   };
   if(roundDemo)SetDemoMotionPath(roundMotion);
   else if(roundMotion.IsScatter)roundMotion.Travel=new Vector2(-82,0);
   else if((action.Actor.CardId==RoundMotionArt.GohanCardId||track=="krillinBasic"||track=="raditzKick"||action.Actor.CardId==RoundMotionArt.YamchaCardId||track=="tienVolley"||track=="tienSet")&&track!=null&&!action.Healing){
    var hit=library.Atlas[roundMotion.Clip.impacts[0].drawing];
    float targetX=action.Move==RoundMove.Ultimate?750:588;
    float reach=(hit.hit[0]-hit.pivot[0])*hit.zoom*RoundMotionFx.Scale;
    roundMotion.Travel=Vector2.right*(targetX-RoundMotionFx.Origin-reach-35);
    roundMotion.DemoLocomotion=true;
   }
   PlayPursuitCue(action);
   return true;
  }

  RoundVitals VisibleRoundVitals(RoundFighter fighter) =>
   WaitingForRoundImpact ? roundBefore[fighter] : new RoundVitals(fighter);

  void UpdateRoundMotion(float seconds) {
   if (!roundPaused) roundIdleTime += seconds * 1000;
   foreach (var item in roundStanding) SetStandingPose(item.Key, item.Value);
   if (roundPaused && (roundMotion==null || !roundMotion.Manual))return;
   if(roundDemo)demoFeedbackClock+=seconds*1000;
   if (roundMotion == null) { TickDemoReactions(seconds*1000); PaintDemo(); return; }
   float before = roundMotion.Time;
   roundMotion.Advance(seconds*1000);
   TickDemoReactions(roundMotion.Time-before);
   UpdateRoundCueAudio(roundMotion,before);
   if (roundMotion.Time >= roundMotion.Duration) {
    FinishRoundMotion();
    return;
   }
   if (roundMotion.Time>=roundMotion.ImpactAt&&RevealRoundAction())DrawRoundPreview();
   if(roundDemo)PaintDemo();else PaintRoundFocus();
  }

  bool RevealRoundAction(bool playAudio=true,bool finish=false) {
   if(!WaitingForRoundImpact)return false;
   var run=roundMotion;
   var hits=run.VolleyHits??run.Action.Hits.ToArray();
   int previous=run.RevealedCount;
   while(run.RevealedCount<hits.Length){
    int index=run.RevealedCount;
    float at=run.VolleyHits==null?run.ImpactAt:run.VolleyImpact(index);
    if(!finish&&run.Time<at)break;
    var hit=hits[index];
    run.RevealedCount++;
    run.SeenHits[hit.Target]=(hit,at);
    if(run.VolleyHits!=null)roundBefore[hit.Target]=new RoundVitals(hit,roundBefore[hit.Target]);
    if(roundDemo){ApplyDemoHit(hit);RecordDemoHit(hit);}
    if(playAudio){
     if(run.VolleyHits!=null)PlayRoundCue(hit.Dodged?"dodge":run.IsScatter?"krillin-impact":run.IsRaditzEnergy?"raditz-impact":"vegeta-impact");
     if(hit.Countered)PlayRoundCue("charge");
     PlayRoundHitVoice(hit);
    }
   }
   if(run.RevealedCount==hits.Length){
    run.Revealed=true;
    if(roundDemo)demoStats.RecordCost(run.Action);
    AddRoundLog(run.Action);
    roundBefore=null;
   }
   return run.RevealedCount>previous;
  }

  void FinishRoundMotion(bool skip=false) {
   RevealRoundAction(playAudio:!skip,finish:true);
   if(skip){
    if(roundDemo)CompleteDemoReaction();
    if(roundCueAudio!=null)roundCueAudio.Stop();
   }
   if(roundMotion.IsCharge)AddRoundLog(roundMotion.Action);
   roundClock=roundMotion.Action==null?RoundOpeningGap:NextRoundActionGap();
   roundBefore=null;
   roundMotion = null;
   DrawRoundPreview();
  }

  RawImage RoundPoseImage(Transform parent, string name) {
   var image = UNode(parent, name, 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
   image.raycastTarget = false;
   return image;
  }

  void DrawRoundStanding(Transform tile, RoundFighter fighter) {
   var image = RoundPoseImage(tile, "StandingPose");
   roundStanding.Add(fighter, image);
   SetStandingPose(fighter, image);
  }

  void SetStandingPose(RoundFighter fighter, RawImage image) {
   if(fighter.CardId==RoundMotionArt.TienCardId){
    RoundMotionArt.Draw(image,TienIdlePose(fighter),new Vector2(101,99),.205f,fighter.Side==1);
    return;
   }
   if(fighter.CardId==RoundMotionArt.YamchaCardId){
    RoundMotionArt.Draw(image,YamchaIdlePose(fighter),new Vector2(101,99),.205f,fighter.Side==1);
    return;
   }
   if(fighter.CardId==RoundMotionArt.RaditzCardId){
    RoundMotionArt.Draw(image,RaditzIdlePose(fighter),new Vector2(101,99),.205f,fighter.Side==1);
    return;
   }
   if(fighter.CardId==RoundMotionArt.KrillinCardId){
    RoundMotionArt.Draw(image,KrillinIdlePose(fighter),new Vector2(101,99),.205f,fighter.Side==1);
    return;
   }
   if(fighter.CardId==RoundMotionArt.GohanCardId){
    RoundMotionArt.Draw(image,GohanIdlePose(fighter),new Vector2(101,95),.195f,fighter.Side==1);
    return;
   }
   const float accentPeriod = 12000, accentStart = 10200;
   float time = ReducedRoundMotion ? 0 : roundIdleTime;
   float cadence = Mathf.Repeat(time + (fighter.Side == 1 ? 1800 : 0), accentPeriod);
   bool accent = !ReducedRoundMotion && cadence >= accentStart;
   var idle = GokuArt[accent ? "accent" : "idle"];
   var pose = GokuArt.At(idle, accent ? cadence - accentStart : Mathf.Repeat(time, idle.duration));
   RoundMotionArt.Draw(image, pose, new Vector2(101, 99), .205f, fighter.Side == 1);
  }

  RoundMotionArt.Pose RaditzIdlePose(RoundFighter fighter){
   var idle=RaditzArt["idle"];
   return RaditzArt.At(idle,Mathf.Repeat(roundIdleTime+fighter.Hand*107,idle.duration));
  }

  RoundMotionArt.Pose KrillinIdlePose(RoundFighter fighter){
   var clip=KrillinArt["idle"];
   float time=ReducedRoundMotion?0:Mathf.Repeat(roundIdleTime+fighter.Position*83,clip.duration);
   return KrillinArt.At(clip,time);
  }

  void DrawRoundFocus() {
   roundFocusPose = null;
   if (roundMotion == null) return;
   const float stageX = 183, stageY = 104;
   const float width = RoundMotionFx.Width, height = RoundMotionFx.Height;
   var panel = UPanel(roundPreviewRoot, "GokuMotionStage", stageX, stageY, width, height, InkPaper);
   panel.gameObject.AddComponent<RectMask2D>();
   roundFocusWorld=UNode(panel,"FocusWorld",0,0,width,height);
   UPanel(roundFocusWorld,"FocusSurface",0,0,width,height,InkPaper);
   UPanel(roundFocusWorld, "Footing", 30, RoundMotionFx.Floor + 7, width - 60, 2, InkBorder);
   roundCutShade=CreateRoundCutShade(roundFocusWorld);
   roundRearFx = UNode(roundFocusWorld, "RoundRearEffects", 0, 0, width, height).gameObject.AddComponent<RoundMotionFx>();
   roundRearFx.raycastTarget = false;
   roundFocusPose = RoundPoseImage(roundFocusWorld, "GokuActionPose");
   roundFocusTarget = null;
   var action = roundMotion.LastSeenHit;
   if (action != null) {
    bool mirror = roundMotion.Actor.Side == 1;
    bool beam = action.Move == RoundMove.Ultimate;
    float x = beam||roundMotion.IsScatter ? 750 : 588;
    if(action.Healing)x=RoundMotionFx.Origin;
    if(mirror)x=width-x;
    if(!action.Healing){
     roundFocusTarget=UNode(roundFocusWorld,"FocusTarget",x-58,108,116,236);
     UArt(roundFocusTarget,Data.Index(action.Target.CardId),0,0,116,215);
     UText(roundFocusTarget,"TargetName",action.Target.Name,-17,212,150,24,16,InkText,TextAnchor.MiddleCenter);
    }
    roundMotionOutcome = UText(panel, "MotionOutcome", "", x - 115, 73, 230, 39, 23, InkWarmText, TextAnchor.MiddleCenter);
   }
   roundFrontFx = UNode(roundFocusWorld, "RoundFrontEffects", 0, 0, width, height).gameObject.AddComponent<RoundMotionFx>();
   roundFrontFx.raycastTarget = false;
   UText(panel, "MotionTitle", roundMotion.Actor.Name + " · " + (roundMotion.SkillName??roundMotion.Clip.title),
    36, 18, width - 72, 40, 27, InkText, TextAnchor.MiddleCenter);
   roundSkillBadge=CreateRoundSkillBadge(panel,roundMotion);
   PaintRoundFocus();
  }

  void PaintRoundFocus() {
   if (roundFocusPose == null || roundMotion == null) return;
   var run = roundMotion;
   bool flip = run.Actor.Side == 1;
   Vector2 offset=run.ShiftAt(run.Time);
   float x = RoundMotionFx.Origin + offset.x;
   if (flip) x = RoundMotionFx.Width - x;
   var feet=new Vector2(x,RoundMotionFx.Floor+offset.y);
   if(run.Clip!=null)
    RoundMotionArt.Draw(roundFocusPose,run.StagePose,feet,RoundMotionFx.Scale,flip);
   else
    DrawRoundAtlasActor(roundFocusPose,run.Actor,feet,run.IsUltimate?(run.Time<run.ReleaseAt?5:3):1,.33f);
   var observed=run.LastSeenHit;
   bool missed=observed?.Dodged??false;
   Vector2? travel=run.DemoLocomotion||run.IsScatter?(Vector2?)run.Travel:null;
   roundRearFx.Present(run.Library,run.Clip,run.PoseTime,flip,missed,true,travel:travel);
   roundFrontFx.Present(run.Library,run.Clip,run.PoseTime,flip,missed,end:750,travel:travel,
    target:run.Clip?.id=="tienCannon"?(Vector2?)new Vector2(850,RoundMotionFx.Floor):
     run.IsCounter||run.Clip?.id=="specialBeam"||run.Clip?.id=="krillinDisc"||run.Clip?.id=="tienFinger"?(Vector2?)new Vector2(run.IsCounter?588:750,198):null);
   if(run.VolleyHits!=null)roundFrontFx.PresentVolley(BuildVolleyVisuals(run,new Vector2(RoundMotionFx.Origin,RoundMotionFx.Floor),
    RoundMotionFx.Scale,hit=>new Vector2(750,198),hit=>new Vector2(750,RoundMotionFx.Floor)));
   var anchor=new Vector2(flip?RoundMotionFx.Width-RoundMotionFx.Origin:RoundMotionFx.Origin,RoundMotionFx.Floor);
   float targetX=run.Action?.Move==RoundMove.Ultimate||run.IsScatter?750:588;
   Vector2? guardAnchor=observed?.Countered==true?
    (Vector2?)new Vector2(flip?RoundMotionFx.Width-targetX:targetX,RoundMotionFx.Floor):null;
   PaintRoundSkillPresentation(roundFocusWorld,roundFocusPose,run,anchor,guardAnchor);
   if(run.Action==null)return;
   float age=run.Time-(run.VolleyHits!=null&&run.RevealedCount>0?run.VolleyImpact(run.RevealedCount-1):run.ImpactAt);
   bool hitVisible=run.RevealedCount>0;
   float recoil = hitVisible ? Mathf.Max(0, 1 - Mathf.Abs(age - 95) / 160) : 0;
   roundMotionOutcome.text=!hitVisible?"":missed?observed.AvoidanceText:
    (observed.GuardAbility!=null?observed.GuardAbility.name+" · ":"")+(observed.Healing?"+":"−")+observed.Amount.ToString("0");
   if(roundFocusTarget==null)return;
   if(hitVisible&&observed.GuardAbility!=null)
    roundFrontFx.PresentGuard(new[]{new RoundMotionFx.GuardPulse{Center=new Vector2(targetX,198),Age=age,Barrier=true}});
   float push = recoil * (missed ? 36 : 14);
   if (flip) targetX = RoundMotionFx.Width - targetX - push;
   else targetX += push;
   roundFocusTarget.anchoredPosition = new Vector2(targetX - 58, -108);
   roundFocusTarget.gameObject.SetActive(!hitVisible||VisibleRoundVitals(observed.Target).HP>0||age<260);
  }
 }
}
