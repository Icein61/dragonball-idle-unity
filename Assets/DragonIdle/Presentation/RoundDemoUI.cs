using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  const string DemoBattleRoute = "战斗 Demo";
  const string ReactionFolder = "Art/V0135/Goku/";
  bool roundDemo;
  RoundSceneDefinitions demoScenes;
  RoundScene demoScene;
  readonly System.Random demoSceneRandom=new System.Random();
  RoundMotionArt demoReactionArt;
  Texture2D demoBackground;
  RectTransform demoViewport, demoStage, demoProjectile;
  RoundMotionFx demoRearFx, demoFrontFx;
  Text demoMoveText,demoSelfCost;
  readonly Dictionary<RoundFighter, DuelReaction> demoReactions = new Dictionary<RoundFighter, DuelReaction>();
  readonly Dictionary<RoundFighter, RawImage> demoActors = new Dictionary<RoundFighter, RawImage>();
  readonly Dictionary<RoundFighter, Text> demoOutcomes = new Dictionary<RoundFighter, Text>();
  readonly List<RoundMotionFx.ChargePulse> demoCharges = new List<RoundMotionFx.ChargePulse>();

  void OpenFeature(string route) {
   if(route==DemoBattleRoute)BeginRoundDemo();
   else NavigateGallery(route);
  }

  string RoundBackPage() => "主页";

  RoundSceneDefinitions DemoScenes() {
   if(demoScenes!=null)return demoScenes;
   var file=Resources.Load<TextAsset>("Config/Battle/Scenes");
   if(file==null)throw new InvalidOperationException("缺少战斗场景配置");
   var loaded=JsonUtility.FromJson<RoundSceneDefinitions>(file.text);
   loaded.Validate();
   if(!loaded.demo.useSavedFormation&&loaded.demo.allies.Concat(loaded.demo.enemies).Any(id=>!string.IsNullOrEmpty(id)&&!Data.IsRoundCard(id)))
    throw new InvalidOperationException("样板对战包含未上线的角色");
   return demoScenes=loaded;
  }

  void BeginRoundDemo()=>BeginRoundDemo(null);

  void BeginRoundDemo(RoundDemoSetup fixture) {
   var scenes=DemoScenes();
   demoScene=scenes.PickDemoScene(demoSceneRandom,demoScene?.id);
   demoBackground=Resources.Load<Texture2D>(demoScene.background);
   if(demoBackground==null)throw new InvalidOperationException("缺少战斗场景背景："+demoScene.id);
   if(demoReactionArt==null)demoReactionArt=RoundMotionArt.Load(ReactionFolder,"Reaction");
   int battleSeed=fixture==null?demoSceneRandom.Next():++seed;
   roundBattle=(fixture??scenes.demo).CreateBattle(RoundRules(),battleSeed,profile);
   roundDemo=true;
   StartRoundPresentation();
  }

  void PrepareDemoPresentation() {
   foreach(var fighter in roundBattle.Order){
    var art=demoReactionArt;
    if(fighter.CardId==RoundMotionArt.VegetaCardId)art=VegetaArt;
    else if(fighter.CardId==RoundMotionArt.PiccoloCardId)art=PiccoloArt;
    else if(fighter.CardId==RoundMotionArt.GohanCardId)art=GohanArt;
    else if(fighter.CardId==RoundMotionArt.KrillinCardId)art=KrillinArt;
    else if(fighter.CardId==RoundMotionArt.RaditzCardId)art=RaditzArt;
    else if(fighter.CardId==RoundMotionArt.YamchaCardId)art=YamchaArt;
    else if(fighter.CardId==RoundMotionArt.TienCardId)art=TienArt;
    demoReactions.Add(fighter,new DuelReaction(art));
   }
   if(ReducedRoundMotion)return;
   roundMotion=new RoundMotionPlayback{Actor=roundBattle.Order[0],Clip=demoReactionArt["entry"]};
   if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.CardId))PlayRoundCue("voice-entry");
   if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.PiccoloCardId))PlayRoundCue("piccolo-entry");
   if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.GohanCardId))PlayRoundCue("gohan-entry");
   if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.KrillinCardId))PlayRoundCue("krillin-entry");
   if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.RaditzCardId))PlayRoundCue("raditz-entry");
   if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.YamchaCardId))PlayRoundCue("yamcha-entry");
   if(roundBattle.Order.Any(fighter=>fighter.CardId==RoundMotionArt.TienCardId))PlayRoundCue("tien-entry");
  }

  void ResetDemoPresentation() {
   demoReactions.Clear();
   demoActors.Clear();
   demoOutcomes.Clear();
   demoCharges.Clear();
   demoViewport=demoStage=demoProjectile=null;
   if(roundCueAudio!=null)roundCueAudio.Stop();
  }

  const float DemoLaneWidth=88,DemoLaneShear=27,DemoRowDepth=80,DemoGridLeft=143.5f,DemoGridTop=167;
  static readonly RoundSlotGrid[] DemoGrids={
   new RoundSlotGrid(new Vector2(DemoGridLeft,DemoGridTop),DemoLaneWidth,DemoLaneShear,DemoRowDepth),
   new RoundSlotGrid(new Vector2(RoundMotionFx.Width-DemoGridLeft-3*(DemoLaneWidth+DemoLaneShear),DemoGridTop),
    DemoLaneWidth,DemoLaneShear,DemoRowDepth,1)
  };

  static Vector2 DemoGround(int side,int position)=>DemoGrids[side].Center(position);

  static Vector2 DemoGround(RoundFighter fighter) => DemoGround(fighter.Side,fighter.Position);
  static float BattleModelSize(string id)=>id==RoundMotionArt.RaditzCardId?1.09f:1;
  static float DemoScale(RoundFighter fighter)=>(.170f+.010f*RoundBoard.Row(fighter.Position))*BattleModelSize(fighter.CardId);

  RoundMotionArt BattleMotionLibrary(string id){
   switch(id){
    case RoundMotionArt.CardId:return GokuArt;
    case RoundMotionArt.VegetaCardId:return VegetaArt;
    case RoundMotionArt.PiccoloCardId:return PiccoloArt;
    case RoundMotionArt.GohanCardId:return GohanArt;
    case RoundMotionArt.KrillinCardId:return KrillinArt;
    case RoundMotionArt.RaditzCardId:return RaditzArt;
    case RoundMotionArt.YamchaCardId:return YamchaArt;
    case RoundMotionArt.TienCardId:return TienArt;
    default:return null;
   }
  }
  static Vector2 FacingSpace(Vector2 point,bool mirror) =>
   mirror?new Vector2(RoundMotionFx.Width-point.x,point.y):point;

  Vector2 DemoFeet(RoundFighter fighter) => DemoGround(fighter)+demoReactions[fighter].Offset;
  Vector2 DemoChest(RoundFighter fighter) => DemoFeet(fighter)+new Vector2(0,-164*DemoScale(fighter));

  void DrawDemoBoard() {
   demoActors.Clear();
   demoOutcomes.Clear();demoSelfCost=null;
   const float width=RoundMotionFx.Width,height=RoundMotionFx.Height;
   const float boardWidth=1326,boardHeight=650,inset=10,battlefieldWidth=boardWidth-inset*2;
   var board=UPanel(roundPreviewRoot,"DemoBoard",0,58,boardWidth,boardHeight,InkPaper);
   UText(board,"DemoSceneTitle",demoScene.title,inset,10,battlefieldWidth,26,16,InkMuted,TextAnchor.MiddleCenter);
   demoMoveText=UText(board,"DemoMove","",inset,34,battlefieldWidth,36,23,InkText,TextAnchor.MiddleCenter);

   demoViewport=UNode(board,"DemoViewport",inset,77,width,height);
   float battlefieldScale=battlefieldWidth/width;
   demoViewport.localScale=new Vector3(battlefieldScale,battlefieldScale,1);
   demoViewport.gameObject.AddComponent<RectMask2D>();
   demoStage=UNode(demoViewport,"DemoStage",0,0,width,height);
   var background=RoundPoseImage(demoStage,"Arena");
   background.rectTransform.sizeDelta=new Vector2(width,height);
   background.texture=demoBackground;
   float aspect=(float)demoBackground.width/demoBackground.height;
   float uvWidth=Mathf.Min(1,(width/height)/aspect);
   float uvHeight=Mathf.Min(1,aspect/(width/height));
   float groundFraming=demoScene.groundFraming;
   if(groundFraming>0){uvWidth*=groundFraming;uvHeight*=groundFraming;}
   background.uvRect=new Rect((1-uvWidth)/2,groundFraming>0?0:(1-uvHeight)/2,uvWidth,uvHeight);
   roundCutShade=CreateRoundCutShade(demoStage);

   var fighters=roundBattle.Order;
   for(int side=0;side<2;side++){
    int team=side;
    var floor=UNode(demoStage,"DemoGroundGrid"+side,0,0,width,height).gameObject.AddComponent<FormationGridGraphic>();
    floor.UseBattleLayout(DemoGrids[side]);
    for(int position=1;position<=RoundBoard.Cells;position++){
     var feet=DemoGround(team,position);
     var fighter=fighters.FirstOrDefault(f=>f.Side==team&&f.Position==position);
     var vitals=fighter==null?default(RoundVitals):VisibleRoundVitals(fighter);
     bool occupied=fighter!=null&&vitals.HP>0;
     string slotName="DemoSlot"+team+"_"+position;
     if(occupied){
      DrawRoundMeterFrame(demoStage,slotName,feet.x-23,feet.y+3,46,3,fighter,vitals);
     }
     else UNode(demoStage,slotName,feet.x-23,feet.y+3,46,19);
    }
   }

   demoRearFx=UNode(demoStage,"DemoRearFx",0,0,width,height).gameObject.AddComponent<RoundMotionFx>();
   demoRearFx.raycastTarget=false;
   var actorsLayer=UNode(demoStage,"DemoActors",0,0,width,height);
   foreach(var fighter in fighters)
    demoActors.Add(fighter,RoundPoseImage(actorsLayer,"DemoActor"+fighter.Side+"_"+fighter.Position));
   demoFrontFx=UNode(demoStage,"DemoFrontFx",0,0,width,height).gameObject.AddComponent<RoundMotionFx>();
   demoFrontFx.raycastTarget=false;
   demoProjectile=UPanel(demoStage,"DemoProjectile",0,0,34,26,InkGold);
   if(roundMotion!=null)
    foreach(var entry in roundMotion.SeenHits.Values){
     var hit=entry.Hit;
     var label=UText(demoStage,"DemoHit"+hit.Target.Side+"_"+hit.Target.Position,"",0,0,196,28,18,InkIvory,TextAnchor.MiddleCenter);
     var shadow=label.gameObject.AddComponent<Shadow>();
     shadow.effectColor=new Color(0,0,0,.86f);
     shadow.effectDistance=new Vector2(1,-1);
     label.fontStyle=FontStyle.Bold;
     demoOutcomes.Add(hit.Target,label);
    }

   if(roundMotion?.Revealed==true&&roundMotion.Action?.ActorHPCost>0){
    demoSelfCost=UText(demoStage,"DemoSelfCost","自身生命 −"+roundMotion.Action.ActorHPCost.ToString("0"),
     0,0,196,28,16,InkIvory,TextAnchor.MiddleCenter);
    demoSelfCost.gameObject.AddComponent<Shadow>().effectColor=new Color(0,0,0,.86f);
   }
   roundSkillBadge=CreateRoundSkillBadge(demoViewport,roundMotion);
   PaintDemo();
  }

  static string ReactionName(RoundHitState state) {
   switch(state) {
    case RoundHitState.Knockback:return "击退";
    case RoundHitState.SmallFloat:return "小浮空";
    case RoundHitState.LargeFloat:return "大浮空";
    case RoundHitState.Knockdown:return "倒地";
    default:return "";
   }
  }

  static string ReactionTrack(RoundHitState state) {
   switch(state) {
    case RoundHitState.Knockback:return "knockback";
    case RoundHitState.SmallFloat:return "smallFloat";
    case RoundHitState.LargeFloat:return "largeFloat";
    case RoundHitState.Knockdown:return "knockdown";
    default:return "standing";
   }
  }

  void ApplyDemoHit(RoundAction hit) {
   var target=hit.Target;
   if(hit.Revives){demoReactions[target]=new DuelReaction(YamchaArt);return;}
   if(hit.Dodged&&hit.Move==RoundMove.Pursuit)demoReactions[target].ReleaseChain();
   if(hit.Countered){
    demoReactions[target].Hit("counterPrepare",target.Side,false);
    return;
   }
   if(hit.Dodged||hit.Healing)return;
   bool survives=hit.TargetHPAfter>0;
   string track=survives?ReactionTrack(hit.TargetState):"defeat";
   if(survives&&hit.GuardAbility!=null&&target.CardId==RoundMotionArt.GohanCardId&&
      (hit.TargetState==RoundHitState.None||hit.TargetState==RoundHitState.Standing))track="gohanGuard";
   var from=target.CardId==RoundMotionArt.CardId?IdleDemoPose(target):
    target.CardId==RoundMotionArt.VegetaCardId?VegetaIdlePose(target):
    target.CardId==RoundMotionArt.PiccoloCardId?PiccoloIdlePose(target):
    target.CardId==RoundMotionArt.GohanCardId?GohanIdlePose(target):target.CardId==RoundMotionArt.KrillinCardId?KrillinIdlePose(target):
    target.CardId==RoundMotionArt.RaditzCardId?RaditzIdlePose(target):target.CardId==RoundMotionArt.YamchaCardId?YamchaIdlePose(target):target.CardId==RoundMotionArt.TienCardId?TienIdlePose(target):null;
   demoReactions[target].Hit(track,target.Side,survives&&hit.TriggersPursuit&&roundBattle.NextPursuitTarget==target,from);
  }

  RoundMotionArt.Pose IdleDemoPose(RoundFighter fighter) {
   float time=ReducedRoundMotion?0:roundIdleTime;
   float cadence=Mathf.Repeat(time+fighter.Side*1800+(fighter.Position-2)*140,12000);
   bool accent=!ReducedRoundMotion&&cadence>=10200;
   var clip=GokuArt[accent?"accent":"idle"];
   return GokuArt.At(clip,accent?cadence-10200:Mathf.Repeat(time,clip.duration));
  }

  RoundMotionArt.Pose TienIdlePose(RoundFighter fighter){
   float beat=ReducedRoundMotion?0:Mathf.Repeat(roundIdleTime+fighter.Hand*137,8200);
   var clip=TienArt[beat>=7500?"accent":"idle"];
   return TienArt.At(clip,beat>=7500?beat-7500:beat);
  }

  RoundMotionArt.Pose YamchaIdlePose(RoundFighter fighter){
   float beat=ReducedRoundMotion?0:Mathf.Repeat(roundIdleTime+fighter.Hand*173,7800);
   var clip=YamchaArt[beat>=7200?"accent":"idle"];
   return YamchaArt.At(clip,beat>=7200?beat-7200:beat);
  }

  RoundMotionArt.Pose VegetaIdlePose(RoundFighter fighter){
   float beat=ReducedRoundMotion?0:Mathf.Repeat(roundIdleTime+fighter.Side*1100,6000);
   var clip=VegetaArt[beat>=5350?"accent":"idle"];
   return VegetaArt.At(clip,beat>=5350?beat-5350:Mathf.Repeat(roundIdleTime,clip.duration));
  }

  RoundMotionArt.Pose PiccoloIdlePose(RoundFighter fighter){
   if(fighter.IsCharging)return PiccoloArt.At(PiccoloArt["piccoloCharge"],680);
   float beat=ReducedRoundMotion?0:Mathf.Repeat(roundIdleTime+fighter.Side*1100,6400);
   var idle=PiccoloArt[beat>=5700?"accent":"idle"];
   return PiccoloArt.At(idle,beat>=5700?beat-5700:Mathf.Repeat(beat,idle.duration));
  }

  RoundMotionArt.Pose GohanIdlePose(RoundFighter fighter){
   float beat=ReducedRoundMotion?0:Mathf.Repeat(roundIdleTime+fighter.Side*930+fighter.Position*170,8400);
   return beat<7770?GohanArt.Atlas[0]:GohanArt.At(GohanArt["accent"],beat-7770);
  }

  void TickDemoReactions(float milliseconds) {
   if(!roundDemo)return;
   foreach(var item in demoReactions){
    var reaction=item.Value;
    bool incoming=roundMotion?.IsPursuit==true&&!roundMotion.Revealed&&roundMotion.Action.Target==item.Key;
    if(!incoming&&roundBattle.NextPursuitTarget!=item.Key)reaction.ReleaseChain();
    float before=reaction.Elapsed;
    reaction.Advance(reaction.Track=="counterPrepare"?milliseconds*.70f:milliseconds);
    if(reaction.Defeated&&before<720&&reaction.Elapsed>=720){
     if(item.Key.CardId==RoundMotionArt.CardId)PlayRoundCue("land");
     if(item.Key.CardId==RoundMotionArt.PiccoloCardId)PlayRoundCue("piccolo-land");
     if(item.Key.CardId==RoundMotionArt.GohanCardId)PlayRoundCue("gohan-land");
     if(item.Key.CardId==RoundMotionArt.KrillinCardId)PlayRoundCue("krillin-land");
     if(item.Key.CardId==RoundMotionArt.RaditzCardId)PlayRoundCue("raditz-land");
     if(item.Key.CardId==RoundMotionArt.YamchaCardId)PlayRoundCue("yamcha-land");
     if(item.Key.CardId==RoundMotionArt.TienCardId)PlayRoundCue("tien-land");
    }
   }
  }

  void CompleteDemoReaction() {
   var next=roundBattle.NextPursuitTarget;
   foreach(var item in demoReactions){
    if(item.Key!=next)item.Value.ReleaseChain();
    item.Value.Finish();
   }
  }

  void SetDemoMotionPath(RoundMotionPlayback run) {
   bool mirror=run.Actor.Side==1;
   run.SourceOffset=demoReactions[run.Actor].Offset;
   Vector2 origin=FacingSpace(DemoGround(run.Actor)+run.SourceOffset,mirror);
   run.DemoLocomotion=true;
   if(run.IsCharge||run.Action.Healing||run.IsRaditzEnergy)return;
   if(run.IsScatter){run.Travel=new Vector2(-47,0);return;}
   if((run.IsUltimate&&run.Clip?.id!="gohanHeavy"&&run.Clip?.id!="yamchaWolf")||run.IsCounter||run.Clip?.id=="tienFinger") {
    var target=run.Action.Target;
    var aim=FacingSpace(DemoChest(target),mirror);
    if(run.IsUltimate&&RoundRules().For(run.Actor.CardId).techniques?.ultimateFrontRow==true){
     // Cover the full configured row, including the last lane when it happens to be empty.
     int lastSlot=RoundBoard.Row(target.Position)+7;
     aim.x=Mathf.Min(RoundMotionFx.Width-18,FacingSpace(DemoGround(target.Side,lastSlot),mirror).x+96);
     if(run.Clip?.id=="tienCannon")aim.y=DemoGround(target).y;
     else run.Travel.y=DemoGround(target).y-DemoGround(run.Actor).y;
    }
    if(run.Clip?.id=="krillinDisc"){
     var targets=run.Action.Hits.Select(hit=>FacingSpace(DemoChest(hit.Target),mirror)).ToArray();
     aim=targets.Aggregate(Vector2.zero,(sum,point)=>sum+point)/targets.Length;
    }
    if(run.Clip?.id=="specialBeam")run.Travel.y=DemoGround(target).y-DemoGround(run.Actor).y;
    run.BeamTarget=aim;
    return;
   }
   var contact=FacingSpace(DemoFeet(run.Action.Target),mirror);
   float depth=DemoGround(run.Action.Target).y-DemoGround(run.Actor).y;
   if(run.Clip==null){
    run.Travel=new Vector2(Mathf.Max(0,contact.x-origin.x-37),depth);
    return;
   }
   var cue=run.Clip.impacts.First(hit=>hit.at==run.Clip.impactAt);
   var frame=run.Library.Atlas[cue.drawing];
   var hand=RoundMotionArt.Anchor(frame,frame.hit,origin,DemoScale(run.Actor));
   float span=Mathf.Max(1,run.Clip.Travel(cue.at));
   float contactInset=cue.kind=="hammer"?24:6;
   run.Travel=new Vector2(Mathf.Max(0,contact.x-hand.x-contactInset)/span,depth);
   if(run.Actor.CardId==RoundMotionArt.PiccoloCardId&&run.Clip.runUntil>0)
    run.PacePiccoloApproach();
  }

  void PaintDemo() {
   if(!roundDemo||demoStage==null)return;
   var run=roundMotion;
   bool entry=run?.Clip?.id=="entry";
   bool mirror=run?.Actor.Side==1;
   Vector2 origin=Vector2.zero,activeFeet=Vector2.zero;
   if(run!=null){
    var footing=DemoGround(run.Actor);
    if(run.Action!=null){
     float carry=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,140,run.Time));
     footing+=run.SourceOffset*carry;
    }else footing=DemoFeet(run.Actor);
    origin=FacingSpace(footing,mirror);
    activeFeet=FacingSpace(origin+run.ShiftAt(run.Time),mirror);
   }
   int drawIndex=0;
   demoCharges.Clear();
   foreach(var item in demoActors.OrderBy(pair=>!entry&&run?.Actor==pair.Key?activeFeet.y:DemoGround(pair.Key).y)){
    var fighter=item.Key;
    Vector2 at=DemoFeet(fighter);
    if(entry){
     float t=Mathf.Clamp01(run.Time/455);
     at.x+=(fighter.Side==0?-1:1)*75*(1-Mathf.SmoothStep(0,1,t));
     at.y-=68*(1-t)*(1-t);
    }else if(run?.Actor==fighter)at=activeFeet;
    if(item.Value.transform.GetSiblingIndex()!=drawIndex)item.Value.transform.SetSiblingIndex(drawIndex);
    drawIndex++;
    PaintDemoActor(fighter,item.Value,at,run,entry);
   }

   Vector2 skillAnchor=run==null?Vector2.zero:DemoGround(run.Actor);
   if(run?.IsUltimate==true)skillAnchor.y+=run.Travel.y;
   Vector2? guardAnchor=run?.Action?.Countered==true?DemoGround(run.Action.Target):(Vector2?)null;
   PaintRoundSkillPresentation(demoStage,run==null?null:demoActors[run.Actor],run,skillAnchor,guardAnchor);
   var clip=entry?null:run?.Clip;
   bool miss=run?.Action!=null&&run.Action.Hits.All(hit=>hit.Dodged);
   float scale=run==null?RoundMotionFx.Scale:DemoScale(run.Actor);
   Vector2? aim=run?.IsUltimate==true&&run.Clip?.id!="yamchaWolf"||run?.IsCounter==true||run?.Clip?.id=="tienFinger"?(Vector2?)run.BeamTarget:null;
   demoRearFx.Present(run?.Library,clip,run?.PoseTime??0,mirror,false,true,origin,travel:run?.Travel,scale:scale);
   demoFrontFx.Present(run?.Library,clip,run?.PoseTime??0,mirror,miss,origin:origin,
    travel:run?.Travel,scale:scale,target:aim);
   var guards=demoReactions.Where(pair=>pair.Value.Track=="counterPrepare"&&pair.Value.Active&&run?.IsCounter!=true)
    .Select(pair=>new RoundMotionFx.GuardPulse{Center=FacingSpace(DemoChest(pair.Key),mirror),Age=pair.Value.Elapsed});
   if(run!=null)guards=guards.Concat(run.SeenHits.Values.Where(seen=>seen.Hit.GuardAbility!=null&&run.Time-seen.At<475)
    .Select(seen=>new RoundMotionFx.GuardPulse{
     Center=FacingSpace(DemoChest(seen.Hit.Target),mirror),Age=run.Time-seen.At,Barrier=true
    }));
   demoFrontFx.PresentGuard(guards.ToArray());
   demoFrontFx.PresentHeldCharges(demoCharges);
   if(clip?.id=="krillinDisc")
    demoFrontFx.PresentDiscCuts(run.SeenHits.Values.Where(seen=>!seen.Hit.Dodged&&run.Time-seen.At<290)
     .Select(seen=>new RoundMotionFx.DiscCut{Center=FacingSpace(DemoChest(seen.Hit.Target),mirror),Age=run.Time-seen.At}).ToArray());
   if(run?.VolleyHits!=null)demoFrontFx.PresentVolley(BuildVolleyVisuals(run,origin,scale,
    hit=>FacingSpace(DemoChest(hit.Target),mirror),hit=>FacingSpace(DemoGround(hit.Target),mirror)));
   demoProjectile.gameObject.SetActive(clip==null&&run?.IsUltimate==true&&aim.HasValue&&run.Time>=run.ReleaseAt&&!run.Revealed);
   if(demoProjectile.gameObject.activeSelf){
    var start=origin+new Vector2(0,-135*scale);
    var at=FacingSpace(Vector2.Lerp(start,aim.Value,Mathf.InverseLerp(run.ReleaseAt,run.ImpactAt,run.Time)),mirror);
    demoProjectile.anchoredPosition=new Vector2(at.x-17,-at.y+13);
   }

   demoMoveText.text=entry?"进场":run==null?"":run.Actor.Name+" · "+(run.SkillName??run.Action?.Name??run.Clip.title);
   if(run==null)return;
   if(demoSelfCost!=null){
    float age=run.Time-run.ImpactAt;
    var point=activeFeet+new Vector2(-98,-82-Mathf.Max(0,age)*.025f);
    demoSelfCost.rectTransform.anchoredPosition=new Vector2(point.x,-Mathf.Max(4,point.y));
    var color=InkIvory;color.a=1-Mathf.InverseLerp(430,735,age);demoSelfCost.color=color;
   }
   foreach(var seen in run.SeenHits.Values){
    var hit=seen.Hit;
    float age=run.Time-seen.At;
    var label=demoOutcomes[hit.Target];
    var at=DemoChest(hit.Target)+new Vector2(0,-46-Mathf.Min(age,500)*.045f);
    label.rectTransform.anchoredPosition=new Vector2(at.x-98,-Mathf.Max(1,at.y));
    string result=hit.Dodged?hit.AvoidanceText:(hit.Healing?"+":"−")+hit.Amount.ToString("0")+" "+ReactionName(hit.TargetState);
    if(hit.GuardAbility!=null)result=hit.GuardAbility.name+" · "+result;
    if(hit.DefenseDelta<0)result+=" 防御↓";
    if(hit.DefenseDelta>0)result+=" · 自身防御↑";
    label.text=result;
    var color=hit.Healing?new Color(.73f,1f,.88f):InkIvory;color.a=1-Mathf.InverseLerp(465,750,age);
    label.color=color;
   }
  }

  void PaintDemoActor(RoundFighter fighter,RawImage image,Vector2 at,RoundMotionPlayback run,bool entry) {
   var reaction=demoReactions[fighter];
   bool visibleAlive=VisibleRoundVitals(fighter).HP>0;
   if(fighter.CardId==RoundMotionArt.VegetaCardId){
    var library=VegetaArt;
    var vegetaPose=reaction.Pose??VegetaIdlePose(fighter);
    if(!visibleAlive)vegetaPose=reaction.Pose??library.Atlas[11];
    else if(entry)vegetaPose=library.Atlas[run.Time<455?2:1];
    else if(run?.Actor==fighter&&run.Library==library&&run.Clip!=null)vegetaPose=run.StagePose;
    RoundMotionArt.Draw(image,vegetaPose,at,DemoScale(fighter),fighter.Side==1);
    return;
   }
   if(fighter.CardId==RoundMotionArt.PiccoloCardId){
    var piccoloPose=reaction.Pose??PiccoloIdlePose(fighter);
    if(!visibleAlive)piccoloPose=reaction.Pose??PiccoloArt.At(PiccoloArt["defeat"],PiccoloArt["defeat"].duration);
    else if(entry)piccoloPose=PiccoloArt.At(PiccoloArt["entry"],run.Time);
    else if(run?.Actor==fighter&&run.Library==PiccoloArt&&run.Clip!=null)piccoloPose=run.StagePose;
    bool flip=fighter.Side==1;
    float scale=DemoScale(fighter);
    RoundMotionArt.Draw(image,piccoloPose,at,scale,flip);
    if(visibleAlive&&fighter.IsCharging&&(run?.Actor!=fighter||!run.IsCharge)){
     var palm=RoundMotionArt.Anchor(piccoloPose,piccoloPose.beam,FacingSpace(at,flip),scale);
     var point=FacingSpace(FacingSpace(palm,flip),run?.Actor.Side==1);
     float phase=ReducedRoundMotion?0:roundIdleTime+fighter.Position*44;
     demoCharges.Add(new RoundMotionFx.ChargePulse{Center=point,Phase=phase});
    }
    return;
   }
   if(fighter.CardId==RoundMotionArt.GohanCardId){
    var art=GohanArt;
    var poseGohan=reaction.Pose??GohanIdlePose(fighter);
    if(!visibleAlive)poseGohan=reaction.Pose??art.At(art["defeat"],art["defeat"].duration);
    else if(entry)poseGohan=art.At(art["entry"],run.Time);
    else if(run?.Actor==fighter&&run.Library==art&&run.Clip!=null)poseGohan=run.StagePose;
    RoundMotionArt.Draw(image,poseGohan,at,DemoScale(fighter),fighter.Side==1);
    return;
   }
   if(fighter.CardId==RoundMotionArt.RaditzCardId){
    var art=RaditzArt;
    var raditzPose=reaction.Pose??RaditzIdlePose(fighter);
    if(!visibleAlive)raditzPose=reaction.Pose??art.At(art["defeat"],art["defeat"].duration);
    else if(entry)raditzPose=art.At(art["entry"],run.Time);
    else if(run?.Actor==fighter&&run.Library==art&&run.Clip!=null)raditzPose=run.StagePose;
    RoundMotionArt.Draw(image,raditzPose,at,DemoScale(fighter),fighter.Side==1);
    return;
   }
   if(fighter.CardId==RoundMotionArt.TienCardId){
    var art=TienArt;
    var poseTien=reaction.Pose??TienIdlePose(fighter);
    if(!visibleAlive)poseTien=reaction.Pose??art.At(art["defeat"],art["defeat"].duration);
    else if(entry)poseTien=art.At(art["entry"],run.Time);
    else if(run?.Actor==fighter&&run.Library==art&&run.Clip!=null)poseTien=run.StagePose;
    RoundMotionArt.Draw(image,poseTien,at,DemoScale(fighter),fighter.Side==1);
    return;
   }
   if(fighter.CardId==RoundMotionArt.YamchaCardId){
    var art=YamchaArt;
    var poseYamcha=reaction.Pose??YamchaIdlePose(fighter);
    if(run?.Actor==fighter&&run.Library==art&&run.Clip!=null)poseYamcha=run.StagePose;
    else if(!visibleAlive)poseYamcha=reaction.Pose??art.At(art["defeat"],art["defeat"].duration);
    else if(entry)poseYamcha=art.At(art["entry"],run.Time);
    RoundMotionArt.Draw(image,poseYamcha,at,DemoScale(fighter),fighter.Side==1);
    return;
   }
   if(fighter.CardId==RoundMotionArt.KrillinCardId){
    var art=KrillinArt;
    var poseKrillin=reaction.Pose??KrillinIdlePose(fighter);
    if(!visibleAlive)poseKrillin=reaction.Pose??art.At(art["defeat"],art["defeat"].duration);
    else if(entry)poseKrillin=art.At(art["entry"],run.Time);
    else if(run?.Actor==fighter&&run.Library==art&&run.Clip!=null)poseKrillin=run.StagePose;
    RoundMotionArt.Draw(image,poseKrillin,at,DemoScale(fighter),fighter.Side==1);
    return;
   }
   if(fighter.CardId!=RoundMotionArt.CardId) {
    int card=Data.Index(fighter.CardId);
    int frame=!visibleAlive?11:reaction.Active?8:entry&&run.Time<455?2:1;
    if(visibleAlive&&run?.Action!=null&&run.Actor==fighter&&(!run.IsPursuit||run.Time>=RoundMotionPlayback.PursuitLead))
     frame=run.Time<run.ImpactAt?(run.Action.Move==RoundMove.Ultimate?5:2):run.Time<run.ImpactAt+158?3:12;
    DrawRoundAtlasActor(image,fighter,at,frame,FighterScale(card)*DemoScale(fighter)*4.2f);
    return;
   }
   var pose=reaction.Pose??IdleDemoPose(fighter);
   if(reaction.Pose==null&&run?.Action?.Target==fighter&&run.Action.Move==RoundMove.Basic&&
      run.Time>=48&&run.Time<(run.Clip?.impacts[0].at??run.ImpactAt))
    pose=GokuArt.At(GokuArt["guard"],run.Time+90);
   if(!visibleAlive)
    pose=reaction.Pose??demoReactionArt.At(demoReactionArt["defeat"],demoReactionArt["defeat"].duration);
   else if(entry)
    pose=demoReactionArt.At(run.Clip,run.Time);
   else if(run?.Actor==fighter&&run.Clip!=null)
    pose=run.StagePose;
   RoundMotionArt.Draw(image,pose,at,DemoScale(fighter),fighter.Side==1);
  }
 }
}
