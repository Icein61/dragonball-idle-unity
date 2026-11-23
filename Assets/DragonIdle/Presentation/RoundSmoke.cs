using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  void VerifyGokuOpenings(){
   var icon=RoundMotionArt.LoadOpeningIcon();
   AssertSmoke(icon!=null&&icon.width==1254&&icon.height==1254&&GokuArt.Atlas.Count(pose=>pose.Texture.name=="CombatPoses")>=11&&GokuArt["opening"].duration==1050,"Missing Goku art");
   var expected=roundBattle.Order.SelectMany(fighter=>RoundRules().For(fighter.CardId).skills
    .Where(skill=>skill.trigger=="opening"||skill.trigger=="whileAlive"||skill.trigger=="roundStart")
    .Select(skill=>(Fighter:fighter,Skill:skill))).ToArray();
   int openings=0,gokuOpenings=0,otherOpenings=0;
   while(roundMotion!=null||roundOpenings.Count>0){
    int cues=roundCueEvents;
    if(roundMotion==null){
     AdvanceRoundPreview(false);
     AssertSmoke(roundCueEvents==cues+1,"Opening did not dispatch its shared cue once");
    }
    var active=roundMotion.Actor;
    var skill=roundMotion.Opening;
    AssertSmoke(openings<expected.Length&&active==expected[openings].Fighter&&skill==expected[openings].Skill&&
     roundLast==null&&roundSkillBadge.gameObject.activeSelf&&roundSkillText.text==skill.name,
     "Opening skills lost their order or caster badge");
    var badgeAnchor=roundSkillBadge.anchoredPosition;
    AssertSmoke(roundSkillBadge.sizeDelta==new Vector2(110,25)&&roundSkillBadge.parent!=roundFocusWorld&&
     roundSkillBadge.GetComponent<UI.Image>().color==RoundMeterPaper,"Badge needs its compact meter-style surface");
    double attack=active.Attack,shield=active.Shield;
    bool goku=active.CardId==RoundMotionArt.CardId;
    if(goku){
     gokuOpenings++;
     AssertSmoke(roundSkillArt.texture==icon&&roundSkillArt.gameObject.activeSelf,"Goku opening icon was replaced");
    }else if(active.CardId==RoundMotionArt.VegetaCardId){
     otherOpenings++;
     AssertSmoke(roundSkillArt.texture!=null&&roundSkillArt.texture.name=="saiyan-elite"&&roundMotion.Clip.id=="opening",
      "Vegeta missing the authored opening and icon");
    }else if(active.CardId==RoundMotionArt.KrillinCardId){
     otherOpenings++;
     AssertSmoke(roundSkillArt.texture==Resources.Load<Texture2D>("Art/V0154/Krillin/skill-icons")&&
      roundMotion.Clip.fx=="krillinMuster","Krillin missing the authored opening and icon");
    }else if(active.CardId==RoundMotionArt.RaditzCardId){
     otherOpenings++;
     AssertSmoke(roundSkillArt.texture==Resources.Load<Texture2D>("Art/V0161/Raditz/skill-icons")&&
      roundMotion.Clip.fx=="raditzAura","Raditz missing the phase skill opening art");
    }else {
     otherOpenings++;
     AssertSmoke(!roundSkillArt.gameObject.activeSelf&&roundSkillArt.texture==null&&roundMotion.Clip==null&&
      roundSkillText.alignment==UnityEngine.TextAnchor.MiddleCenter,"Missing skill art should show a centered name");
    }
    AssertSmoke(roundFocusPose.texture!=null&&(roundFocusPose.uvRect.width<0)==(active.Side==1),"Opening art or mirroring failed");
    GalleryTestButton("PauseRound").onClick.Invoke();
    UpdateRoundPreview(.2f);
    AssertSmoke(roundMotion.Time==0,"Pause advanced opening");
    GalleryTestButton("PauseRound").onClick.Invoke();
    UpdateRoundPreview(.16f);
    Canvas.ForceUpdateCanvases();
    AssertSmoke(pursuitSheen.isActiveAndEnabled&&pursuitSheen.color.r>pursuitSheen.color.b&&
     pursuitSheen.color.a>.55f&&pursuitSheen.texture==roundFocusPose.texture,
     "Opening lost its slower amber silhouette sheen");
    if(goku){
     UpdateRoundPreview(.46f);
     Canvas.ForceUpdateCanvases();
     AssertSmoke(roundRearFx.GeometryCount>0&&roundFocusPose.texture==GokuArt.At(GokuArt["opening"],620).Texture,
      "Opening gesture or halo unavailable");
    }
    AssertSmoke(roundSkillBadge.anchoredPosition==badgeAnchor,"Opening name moved with the casting frame");
    GalleryTestButton("StepRound").onClick.Invoke();
    AssertSmoke(roundMotion==null&&active.Attack==attack&&active.Shield==shield&&roundLog.Count==0,
     "Opening presentation replayed the combat effect");
    roundPaused=false;
    openings++;
   }
   AssertSmoke(openings==expected.Length&&gokuOpenings==expected.Count(entry=>entry.Fighter.CardId==RoundMotionArt.CardId)&&otherOpenings>0,"The opening queue omitted a caster");
  }

  void AdvanceToMotionTime(float time){
   var run=roundMotion;
   if(run.ApproachDuration>0){
    UpdateRoundPreview((time-run.Time)/1000);
    return;
   }
   float slowed=run.IsUltimate?Mathf.Max(0,Mathf.Min(time,run.ReleaseAt+RoundMotionPlayback.ReleaseSlowSpan)
    -Mathf.Max(run.Time,run.ReleaseAt)):0;
   float running=run.DemoLocomotion&&!run.IsPursuit&&run.Clip!=null?
    Mathf.Max(0,Mathf.Min(time,run.Clip.runUntil)-run.Time):0;
   UpdateRoundPreview((time-run.Time+slowed*(1/RoundMotionPlayback.ReleaseRate-1)+
    running*(1/RoundMotionPlayback.DemoRunRate-1))/(1000*run.PlaybackRate));
  }

  RoundFighter SetupMotionProbe(int fixedSeed=1){
   var goku=new RoundFighter(RoundMotionArt.CardId,0,1,1200,100,40,0){Dodge=0};
   var ally=new RoundFighter("vegeta-test",0,2,1200,100,40,0){Dodge=0};
   var enemies=RoundBoards().boards[0].positions.Take(5).Select((id,i)=>
    new RoundFighter(id,1,i+1,1200,100,40,0){Dodge=0});
   roundBattle=new RoundCombat(new[]{goku,ally}.Concat(enemies),RoundRules(),fixedSeed);
   roundLast=null;roundLog.Clear();roundPaused=false;
   PrepareRoundMotion();
   NavigateGallery(RoundPreviewPage);
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   roundPaused=false;
   return goku;
  }

  void VerifyGokuMoves(){
   var goku=SetupMotionProbe();
   roundIdleTime=0;
   UpdateRoundMotion(0);
   var guard=roundStanding[goku].uvRect;
   UpdateRoundMotion(.36f);
   AssertSmoke(guard!=roundStanding[goku].uvRect&&roundStanding[goku].texture==GokuArt.At(GokuArt["idle"],360).Texture,"Idle sprite did not animate");
   UpdateRoundMotion(10.13f);
   AssertSmoke(roundStanding[goku].texture==GokuArt.At(GokuArt["accent"],289).Texture,"Occasional idle did not play");

   GalleryTestButton("StepRound").onClick.Invoke();
   var action=roundLast;
   AssertSmoke(roundPaused&&roundMotion.Manual&&roundMotion.Clip.id=="basic"&&
    action.Name=="格斗攻击"&&roundLog.Count==0&&VisibleRoundVitals(action.Target).HP==1200,"Basic starts with pre-impact vitals");
   UpdateRoundPreview(.145f/roundMotion.PlaybackRate);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundFrontFx.GeometryCount>0&&roundLog.Count==0&&roundFocusPose.texture==GokuArt.At(GokuArt["basic"],145).Texture,"Left fist cue or damage timing failed");
   UpdateRoundPreview(.183f/roundMotion.PlaybackRate);
   AssertSmoke(roundLog.Count==1&&VisibleRoundVitals(action.Target).HP==action.Target.HP&&
    roundFocusPose.texture==GokuArt.At(GokuArt["basic"],328).Texture&&roundMotionOutcome.text.StartsWith("−"),"Right fist did not reveal one hit");
   GalleryTestButton("StepRound").onClick.Invoke();
   AssertSmoke(roundMotion==null&&roundLast==action&&roundLog.Count==1,"Skip consumed the following hand or duplicated damage");

   goku=SetupMotionProbe(2);
   GalleryTestButton("StepRound").onClick.Invoke();
   action=roundLast;
   AssertSmoke(action.TargetState==RoundHitState.Knockdown&&action.TriggersPursuit&&roundMotion.Clip.id=="sweep","Knockdown did not select sweep");
   UpdateRoundPreview(.708f/roundMotion.PlaybackRate);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundFrontFx.GeometryCount>0&&roundFocusPose.texture==GokuArt.At(GokuArt["sweep"],708).Texture&&
    roundLog.Count==1&&roundMotion.Clip.impactAt==672,"Sweep extension, trail or impact cue failed");
   GalleryTestButton("StepRound").onClick.Invoke();
   GalleryTestButton("StepRound").onClick.Invoke();
   AssertSmoke(roundMotion.Clip.id=="pursuit"&&roundLast.Actor==goku&&roundLast.Name=="上勾拳"&&
    roundLast.TargetState==RoundHitState.SmallFloat,"Knockdown did not dispatch the uppercut");
   AdvanceToMotionTime(roundMotion.ImpactAt+28);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundFrontFx.GeometryCount>0&&roundLog.Count==2&&roundFocusPose.texture==roundMotion.StagePose.Texture,
    "Uppercut pose, hit or rising effect missing");

   goku=SetupMotionProbe();
   goku.Rage=100;
   var target=roundBattle.Order.First(f=>f.Side==1);
   target.HP=1;DrawRoundPreview();
   GalleryTestButton("StepRound").onClick.Invoke();
   AssertSmoke(roundMotion.Clip.id=="ultimate"&&roundLast.Name=="龟派气功"&&!target.Alive&&VisibleRoundVitals(target).HP==1,"Lethal ultimate hid the target too early");
   var row=roundLast.Hits.ToArray();
   AssertSmoke(row.Length==2&&row.Count(hit=>hit.TargetState==RoundHitState.Knockback)==1,"Beam did not select its front row and one knockback");
   UpdateRoundPreview(.77f/roundMotion.PlaybackRate);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundFrontFx.GeometryCount>0&&roundMotionOutcome.text==""&&roundLog.Count==0,"Charge rendered a premature impact");
   UpdateRoundPreview(.47f/roundMotion.PlaybackRate);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundFrontFx.GeometryCount>300&&WaitingForRoundImpact&&VisibleRoundVitals(target).HP==1,"Beam transit revealed before arrival");
   UpdateRoundPreview(.44f/roundMotion.PlaybackRate);
   AssertSmoke(roundLog.Count==1&&VisibleRoundVitals(target).HP==0&&row.All(hit=>VisibleRoundVitals(hit.Target).HP==hit.Target.HP),"Beam arrival did not reveal the full row once");
   AdvanceToMotionTime(2280);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundFrontFx.GeometryCount==0&&roundMotion!=null&&roundLog.Count==1,"Beam did not extinguish through recovery");
   UpdateRoundPreview(.32f/roundMotion.PlaybackRate);
   AssertSmoke(roundMotion==null,"Ultimate did not return to idle");

   audioSettings.ReducedMotion=true;
   goku=SetupMotionProbe();
   target=roundBattle.Order.First(f=>f.Side==1);
   target.Dodge=1;
   AssertSmoke(roundMotion==null&&roundStanding[goku].texture==GokuArt.At(GokuArt["idle"],0).Texture,"Reduced motion skipped standing fallback");
   GalleryTestButton("StepRound").onClick.Invoke();
   AssertSmoke(roundMotion==null&&roundLast.Dodged&&roundLog[0].EndsWith("闪避"),"Reduced motion did not publish dodge normally");
   audioSettings.ReducedMotion=false;
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
   AssertSmoke(roundMotion==null&&roundOpenings.Count==0&&roundStanding.Count==0,"Presentation leaked across trial exit");
  }

  RoundCombat MakeGokuMirrorFixture(int seed){
   var both=new[]{"","goku-test","","","","","","",""};
   return new RoundDemoSetup{scene=demoScene.id,allies=both,enemies=both}.CreateBattle(RoundRules(),seed);
  }

  RoundDemoSetup FiveCardDemoFixture(){
   return new RoundDemoSetup{
    allies=new[]{"","goku-test","","vegeta-test","gohan-test","","krillin-test","piccolo-test",""},
    enemies=new[]{"","goku-test","","","gohan-test","vegeta-test","","piccolo-test","krillin-test"}
   };
  }

  void VerifyGridDemo(){
   var formation=profile.RoundFormation();
   NavigateGallery("主页");
   GalleryTestButton("HomeNav"+DemoBattleRoute).onClick.Invoke();
   var allies=roundBattle.Order.Where(f=>f.Side==0).ToArray();
   var enemies=roundBattle.Order.Where(f=>f.Side==1).ToArray();
   AssertSmoke(roundDemo&&page==RoundPreviewPage&&roundMotion.Clip.id=="entry"&&roundBattle.Order.All(f=>f.Level==100)&&
    allies.Length==formation.Count(i=>i>=0)&&allies.All(f=>formation[f.Position-1]==Data.Index(f.CardId))&&
    enemies.Length==5&&enemies.Select(f=>f.CardId).Distinct().Count()==5&&enemies.Select(f=>f.Position).Distinct().Count()==5,
    "The actual Demo did not load the saved level-100 roster and five random enemies");
   roundBattle=MakeGokuMirrorFixture(2);
   StartRoundPresentation();
   AssertSmoke(roundBattle.Order.All(f=>f.CardId==RoundMotionArt.CardId&&f.Position==2)&&
    roundBattle.Order[0].Side==0&&roundBattle.Order[1].Side==1,"Demo did not use configured slot two on both sides");
   AssertSmoke(demoStage.Find("Arena").GetComponent<UI.RawImage>().texture!=null&&demoActors.Count==2&&
    demoActors[roundBattle.Order[1]].uvRect.width<0,"World and mirrored sprites did not load");
   AssertSmoke(DemoScenes().demo.scenePool.Contains(demoScene.id)&&
    demoStage.Cast<Transform>().Count(t=>t.name.StartsWith("DemoSlot"))==18&&
    demoStage.Cast<Transform>().All(t=>!t.name.StartsWith("DemoSlot")||t.Find("Position")==null),
    "Demo scene or eighteen formation anchors missing");
   var demoBoard=(RectTransform)demoViewport.parent;
   AssertSmoke(demoBoard.Find("DemoTeam0")==null&&demoBoard.Find("DemoTeam1")==null&&!roundPreviewRoot.Cast<Transform>().Any(child=>child.name=="RoundLog"&&child.gameObject.activeSelf)&&
    demoViewport.sizeDelta.x*demoViewport.localScale.x>=980&&demoViewport.sizeDelta.y*demoViewport.localScale.y>=410&&
    ((RectTransform)demoBoard.Find("DemoStats0")).anchoredPosition.x<demoViewport.anchoredPosition.x&&
    ((RectTransform)demoBoard.Find("DemoStats1")).anchoredPosition.x>demoViewport.anchoredPosition.x,
    "The arena must be centered between two independent roster panels");
   foreach(int position in Enumerable.Range(1,9)){
    var playerGround=DemoGround(0,position);var enemyGround=DemoGround(1,position);
    var playerSlot=(RectTransform)demoStage.Find("DemoSlot0_"+position);
    var enemySlot=(RectTransform)demoStage.Find("DemoSlot1_"+position);
    int opposite=((position-1)/3)*3+3-RoundBoard.Row(position);
    AssertSmoke(DemoGround(0,position)+DemoGround(1,opposite)==new Vector2(RoundMotionFx.Width,574)&&
     playerGround.y==enemyGround.y&&
     playerSlot.anchoredPosition==new Vector2(playerGround.x-23,-3-playerGround.y)&&
     enemySlot.anchoredPosition==new Vector2(enemyGround.x-23,-3-enemyGround.y),"Demo markers are not registered to mirrored ground");
   }
   AssertSmoke(demoFrontFx.GetComponent<CanvasRenderer>()!=null&&demoRearFx.GetComponent<CanvasRenderer>()!=null,
    "Masked effects need a renderer before their first canvas draw");
   var rowStep=DemoGround(0,2)-DemoGround(0,1);
   var laneStep=DemoGround(0,4)-DemoGround(0,1);
   AssertSmoke(rowStep.x==27&&rowStep.y==80&&laneStep.x==-88&&laneStep.y==0,
    "Battle ground did not use the new lane angle and spacing");
   for(int side=0;side<2;side++){
    var grid=demoStage.Find("DemoGroundGrid"+side).GetComponent<FormationGridGraphic>();
    AssertSmoke(!grid.raycastTarget&&grid.Layout.Across==new Vector2(88,0)&&grid.Layout.Depth==new Vector2(27,80)&&
     Enumerable.Range(1,RoundBoard.Cells).All(slot=>grid.Layout.Center(slot)==DemoGround(side,slot)&&
      grid.Layout.SlotAt(DemoGround(side,slot))==slot),"Battle tiles must be congruent and centered on roster anchors");
   }
   AssertSmoke(demoReactionArt["entry"].poses.Length>1&&roundCueClips.Count==97&&
    roundCueClips["opening-passive"].Clip!=roundCueClips["pursuit-start"].Clip&&
    roundCueClips.Values.All(cue=>cue.Clip!=null&&cue.Clip.samples>0&&cue.Gain>0)&&
    roundCueAudio!=null&&roundCueAudio.volume==0&&!roundCueAudio.isPlaying&&Resources.LoadAll<AudioClip>("Audio/V0132/Goku").Length==0&&Resources.LoadAll<AudioClip>("Audio/V0133/Goku").Length==0,
    "Packaged Goku audio is missing, unmapped or not muted during headless smoke");

   int cues=roundCueEvents;
   GalleryTestButton("PauseRound").onClick.Invoke();
   UpdateRoundPreview(.6f);
   AssertSmoke(roundPaused&&roundMotion.Time==0&&roundCueEvents==cues,"Duel pause advanced entry or audio");
   GalleryTestButton("PauseRound").onClick.Invoke();
   UpdateRoundPreview(.5f);
   AssertSmoke(roundMotion.Time>=500&&roundCueEvents==cues+1,"Landing should dispatch one cue after resuming");
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);

   var goku=roundBattle.Order[0];var opponent=roundBattle.Order[1];
   goku.Rage=100;opponent.Dodge=0;
   GalleryTestButton("StepRound").onClick.Invoke();
   var last=roundLast;
   AssertSmoke(last.Move==RoundMove.Ultimate&&last.TargetState==RoundHitState.Knockback&&!last.TriggersPursuit&&
    demoReactions[opponent].Track==null&&VisibleRoundVitals(opponent).HP==opponent.MaxHP,"Duel revealed hit before the beam");

   cues=roundCueEvents;
   UpdateRoundPreview(.67f/roundMotion.PlaybackRate);
   Canvas.ForceUpdateCanvases();
   float chargeRadius=demoFrontFx.BeamChargeRadius;
   AssertSmoke(roundCueEvents==cues+2&&demoFrontFx.GeometryCount>0&&chargeRadius>23,"Charge needs a voice and an effect before release");
   AdvanceToMotionTime(1000);
   var castLabel=roundSkillBadge.anchoredPosition;
   AssertSmoke(demoStage.localScale==Vector3.one&&demoStage.anchoredPosition==Vector2.zero&&
    roundSkillBadge.gameObject.activeSelf&&roundSkillBadge.parent==demoViewport&&roundSkillText.text==last.Name&&
    roundSkillArt.gameObject.activeSelf&&roundSkillArt.texture.name=="skill-icons"&&roundSkillArt.texture.width==1254&&
    Mathf.Abs(roundSkillArt.uvRect.x-649f/1254)<.001f&&Mathf.Abs(roundSkillArt.uvRect.width-538f/1254)<.001f,
    "Pre-release framing or original skill-art binding failed");
   UpdateRoundPreview(.07f/roundMotion.PlaybackRate);
   float earlyZoom=demoStage.localScale.x;
   AssertSmoke(Mathf.Abs(roundMotion.Time-(roundMotion.ReleaseAt+50*RoundMotionPlayback.ReleaseRate))<.1f&&
    roundCutShade.color.a>0&&earlyZoom>1&&earlyZoom<1.025f&&roundSkillBadge.anchoredPosition==castLabel,
    "Ultimate camera must ease in while the badge stays fixed");
   AdvanceToMotionTime(1230);
   AssertSmoke(Mathf.Abs(demoStage.localScale.x-1.075f)<.001f&&roundSkillBadge.anchoredPosition==castLabel,
    "Camera did not reach its restrained zoom or the badge moved");
   AssertSmoke(roundCueEvents==cues+4&&demoReactions[opponent].Track==null&&roundLog.Count==0,
    "Beam sound and flight started after or revealed too early");
   AdvanceToMotionTime(1350);
   AssertSmoke(roundCutShade.color.a==0&&Mathf.Abs(demoStage.localScale.x-1.075f)<.001f&&
    roundSkillBadge.anchoredPosition==castLabel,"The released beam must retain framing without the shade or badge drift");
   AssertSmoke(roundCueEvents==cues+5&&demoReactions[opponent].Track=="knockback"&&roundLog.Count==1,
    "Beam voice, release, knockback or single reveal failed");
   AdvanceToMotionTime(1670);
   AssertSmoke(roundCueEvents==cues+6,"Beam fade must dispatch its tail cue");
   Canvas.ForceUpdateCanvases();
   float opacityAtTip=demoFrontFx.BeamOpacity;
   float radiusAtTip=demoFrontFx.BeamRadius;
   float tipLight=demoFrontFx.BeamTipOpacity;
   AssertSmoke(demoFrontFx.BeamReach==1&&opacityAtTip>.9f&&tipLight>0&&tipLight<opacityAtTip*.6f&&
    radiusAtTip>=20&&radiusAtTip<chargeRadius&&demoFrontFx.GeometryCount>0&&
    demoFrontFx.BeamRootDepth>12&&demoFrontFx.BeamTailRadius>radiusAtTip*.42f&&
    demoFrontFx.BeamTailRadius<radiusAtTip*.45f&&demoFrontFx.BeamPalm.y==demoFrontFx.BeamTip.y&&
    demoFrontFx.BeamTip.x>DemoGround(1,8).x+70,
    "Full-range wave needs a thicker body, a soft end and a core narrower than the charge");
   UpdateRoundPreview(.21f/roundMotion.PlaybackRate);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundCueEvents==cues+6,"Beam fade must not repeat its tail cue");
   AssertSmoke(roundMotion.Clip.KeyAt(roundMotion.Time)>=roundMotion.Clip.KeyAt(1840)&&demoFrontFx.BeamReach==1&&
    demoFrontFx.BeamOpacity>0&&demoFrontFx.BeamOpacity<opacityAtTip&&demoFrontFx.BeamRadius<radiusAtTip&&
    demoFrontFx.BeamTipOpacity/tipLight<demoFrontFx.BeamOpacity/opacityAtTip&&demoFrontFx.GeometryCount>0,
    "Beam did not remain visible and fade after the actor started lowering his hands");
   UpdateRoundPreview(.16f);
   var pushed=demoReactions[opponent].Offset;
   GalleryTestButton("PauseRound").onClick.Invoke();
   float elapsed=roundMotion.Time,pausedZoom=demoStage.localScale.x;
   var pausedOffset=demoStage.anchoredPosition;
   UpdateRoundPreview(.5f);
   AssertSmoke(roundMotion.Time==elapsed&&demoReactions[opponent].Offset==pushed&&
    demoStage.localScale.x==pausedZoom&&demoStage.anchoredPosition==pausedOffset,"Pause advanced the camera or hit reaction");
   GalleryTestButton("PauseRound").onClick.Invoke();
   AdvanceToMotionTime(2390);
   AssertSmoke(demoStage.localScale.x>1.03f&&demoStage.localScale.x<1.05f,"Recovery must ease the camera outward");
   AdvanceToMotionTime(roundMotion.Duration-1);
   AssertSmoke(Mathf.Abs(demoStage.localScale.x-1)<.001f&&demoStage.anchoredPosition.sqrMagnitude<.01f,
    "Camera must converge on its exact starting transform");
   UpdateRoundPreview(.012f);
   AssertSmoke(roundMotion==null&&demoStage.localScale==Vector3.one&&demoStage.anchoredPosition==Vector2.zero&&
    !roundSkillBadge.gameObject.activeSelf,"Ultimate did not restore the original view at settlement");
   roundBattle=MakeGokuMirrorFixture(2);
   StartRoundPresentation();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   goku=roundBattle.Order[0];opponent=roundBattle.Order[1];opponent.Dodge=0;
   GalleryTestButton("StepRound").onClick.Invoke();
   AssertSmoke(roundMotion.Clip.id=="sweep","Demo did not select the knockdown sweep");
   cues=roundCueEvents;
   AdvanceToMotionTime(110);
   AssertSmoke(roundCueEvents==cues+2,"Basic should dispatch its attack voice and guard effect");
   AssertSmoke(demoActors[opponent].texture==GokuArt.At(GokuArt["guard"],200).Texture&&
     demoActors[opponent].uvRect.width<0,
    "Receiver guard pose missing before contact");
   AssertSmoke(Mathf.Abs(RenderedGokuGroundY(goku)-DemoGround(goku).y)<.05f&&roundMotion.ShiftAt(roundMotion.Time).y==0,
    "Same-row approach raised the active fighter's feet");
   AdvanceToMotionTime(roundMotion.ImpactAt+35);
   AssertSmoke(demoReactions[opponent].Track=="knockdown"&&roundLog.Count==1&&
    Mathf.Abs(RenderedGokuGroundY(goku)-DemoGround(goku).y)<.05f,
    "Sweep did not hit with a stable same-row foot anchor");
   GalleryTestButton("StepRound").onClick.Invoke();
   var offset=demoReactions[opponent].Offset;
   var lowPose=demoReactions[opponent].Pose;
   cues=roundCueEvents;
   GalleryTestButton("StepRound").onClick.Invoke();
   AssertSmoke(roundLast.Move==RoundMove.Pursuit&&roundLast.Actor==goku&&roundLast.TargetState==RoundHitState.SmallFloat&&
    roundCueEvents==cues+1,"Demo did not cue the uppercut after knockdown");
   UpdateRoundPreview(.024f);
   Canvas.ForceUpdateCanvases();
   var model=demoActors[goku];
   var sheen=model.transform.Find("PursuitSheen").GetComponent<UI.RawImage>();
   var material=sheen.material;
   float lowerBand=material.GetFloat(SheenBandY);
   AssertSmoke(roundMotion.Duration==505&&roundMotion.ImpactAt>104&&roundMotion.ShiftAt(roundMotion.Time)==Vector2.zero&&
    sheen.texture==model.texture&&sheen.uvRect==model.uvRect&&sheen.isActiveAndEnabled&&sheen.color.a>0&&sheen.color.a<.44f&&
    material.shader.name=="DragonIdle/UI/PursuitSheen"&&WaitingForRoundImpact,
    "The pursuit sheen must use the acting sprite silhouette before moving");
   UpdateRoundPreview(.034f);
   AssertSmoke(material.GetFloat(SheenBandY)>lowerBand&&WaitingForRoundImpact,"The sheen must move from the feet upward");
   UpdateRoundPreview(.052f);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(!sheen.gameObject.activeSelf&&WaitingForRoundImpact,"Pursuit sheen must clear before impact");
   UpdateRoundPreview((roundMotion.ImpactAt+14-roundMotion.Time)/1000);
   AssertSmoke(demoReactions[opponent].Track=="smallFloat"&&demoReactions[opponent].Offset==offset&&demoReactions[opponent].Pose==lowPose,"Pursuit snapped the receiver to origin");
   AssertSmoke(roundCueEvents==cues+4,"Uppercut needs one attack voice, one impact and one hit voice");
   UpdateRoundPreview(.15f);
   AssertSmoke(demoReactions[opponent].Offset.y<0,"Pursuit did not lift the receiver");

   string[] hitStates={"standing","knockback","smallFloat","largeFloat","knockdown"};
   foreach(string from in hitStates){
    foreach(string to in hitStates){
     var react=new DuelReaction(demoReactionArt);
     react.Hit(from,1,false);
     react.Advance(150);
     var before=react.Offset;
     var pose=react.Pose;
     react.Hit(to,1,false);
     AssertSmoke(react.Active&&react.Track==to&&react.Offset==before&&react.Pose==pose,"Reaction snapped "+from+" -> "+to);
     react.Advance(110);
     AssertSmoke(react.Pose!=null&&react.Offset.y>=-51&&Mathf.Abs(react.Offset.x)<=64,"Reaction lost a legal pose "+from+" -> "+to);
     react.Advance(1000);
     AssertSmoke(!react.Active&&react.Offset==Vector2.zero,"Reaction failed to return after "+to);
    }
   }

   GalleryTestButton("RestartDemo").onClick.Invoke();
   AssertSmoke(roundMotion.Clip.id=="entry"&&roundLast==null&&roundLog.Count==0&&
    roundBattle.Order.Count(f=>f.Side==1)==5&&roundBattle.Order.Count(f=>f.Side==0)==formation.Count(i=>i>=0)&&
    roundBattle.Order.Where(f=>f.Side==0).All(f=>formation[f.Position-1]==Data.Index(f.CardId)),
    "Demo restart did not clear the old action and reload the saved lineup");
   roundBattle=MakeGokuMirrorFixture(2);
   StartRoundPresentation();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   opponent=roundBattle.Order[1];opponent.HP=1;opponent.Dodge=0;
   GalleryTestButton("StepRound").onClick.Invoke();
   AdvanceToMotionTime(roundMotion.Clip.impactAt+16);
   AssertSmoke(demoReactions[opponent].Track=="defeat"&&roundLog.Count==1,"Lethal hit did not enter defeat");
   GalleryTestButton("StepRound").onClick.Invoke();
   AssertSmoke(roundMotion==null&&demoReactions[opponent].Pose!=null&&!opponent.Alive&&VisibleRoundResult==1,"Final defeated pose did not persist");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
   AssertSmoke(page=="主页"&&roundBattle==null&&!roundDemo&&demoReactions.Count==0&&profile.RoundFormation().SequenceEqual(formation),"Duel exited with retained battle or changed formation");
  }

  void VerifyPageMusic(){
   NavigateGallery("主页");
   SyncPageMusic();
   var home=Resources.Load<AudioClip>(CharacterPageBgm);
   AssertSmoke(home!=null&&home.loadType==AudioClipLoadType.Streaming&&pageMusic.clip==home&&
    pageMusic.loop&&!pageMusic.isPlaying&&pageMusic.volume==0,"Home BGM missing or audible during smoke");
   var scenes=DemoScenes();
   var random=new System.Random(141);
   var visited=new HashSet<string>();
   string previous=null;
   for(int i=0;i<30;i++){
    var scene=scenes.PickDemoScene(random,previous);
    AssertSmoke(scene.id!=previous,"Consecutive Demo scene repeated");
    visited.Add(scene.id);previous=scene.id;
   }
   AssertSmoke(visited.SetEquals(new[]{"landing-meadow","wind-carved-plateau","red-rock-canyon"}),
    "Demo scene rotation did not cover the three configured maps");
   BeginRoundDemo();
   var first=demoScene.id;
   AssertSmoke(demoBackground==Resources.Load<Texture2D>(demoScene.background),"Demo entry did not load its selected background");
   SyncPageMusic();
   AssertSmoke(pageMusic.clip!=null&&pageMusic.clip.name==first,"Demo did not switch from home to the selected map track");
   GalleryTestButton("RestartDemo").onClick.Invoke();
   AssertSmoke(demoScene.id!=first&&demoBackground==Resources.Load<Texture2D>(demoScene.background),"Restart did not select and load a different map");
   foreach(var sceneId in visited){
    var scene=scenes.Scene(sceneId);
    demoScene=scene;SyncPageMusic();
    var clip=Resources.Load<AudioClip>(scene.bgm);
    AssertSmoke(clip!=null&&clip.samples>0&&clip.name==scene.id&&clip.loadType==AudioClipLoadType.Streaming&&
     pageMusic.clip==clip&&pageMusic.volume==0&&!pageMusic.isPlaying&&
     Resources.Load<Texture2D>(scene.background)!=null,"Missing, mispaired or unmuted Demo soundtrack "+scene.id);
   }
   NavigateGallery("主页");SyncPageMusic();
   AssertSmoke(pageMusic.clip==home,"Leaving Demo did not restore home music");
   NavigateGallery("角色");SyncPageMusic();
   var character=Resources.Load<AudioClip>(CharacterPageBgm);
   AssertSmoke(character!=null&&character.loadType==AudioClipLoadType.Streaming&&character.samples>0&&
    character.channels==2&&pageMusic.clip==character&&pageMusic.loop&&!pageMusic.isPlaying&&pageMusic.volume==0,
    "Character page is missing the shared menu loop");
   OpenGalleryCard(0);SyncPageMusic();
   AssertSmoke(pageMusic.clip==character&&activeMusicPath==CharacterPageBgm,"Opening detail interrupted the page music");
   NavigateGallery("主页");SyncPageMusic();
  }

  void VerifyVegetaScale(){
   BeginRoundDemo();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   foreach(var fighter in roundBattle.Order.Where(f=>f.CardId==RoundMotionArt.VegetaCardId)){
    var image=demoActors[fighter];
    var ground=DemoGround(fighter);
    float sideOffset=fighter.Side*1100;
    roundIdleTime=0;
    PaintDemoActor(fighter,image,ground,null,false);
    float height=image.rectTransform.sizeDelta.y;
    var idle=image.uvRect;
    roundIdleTime=5480-sideOffset;
    PaintDemoActor(fighter,image,ground,null,false);
    var accent=VegetaArt.At(VegetaArt["accent"],130);
    AssertSmoke(image.texture==accent.Texture&&image.uvRect!=idle,"Vegeta idle accent did not select its pose");
    AssertSmoke(Mathf.Abs(image.rectTransform.sizeDelta.y-height)<.01f,"Vegeta idle accent resized the sprite");
    var cast=new RoundMotionPlayback{Actor=fighter,Library=VegetaArt,Clip=VegetaArt["opening"]};
    foreach(float time in new[]{0f,225f,455f,650f}){
     cast.Time=time;
     PaintDemoActor(fighter,image,ground,cast,false);
     var pose=VegetaArt.At(cast.Clip,time);
     float feet=-image.rectTransform.anchoredPosition.y+(pose.pivot[1]-pose.crop[1])*pose.zoom*DemoScale(fighter);
     AssertSmoke(image.texture==pose.Texture&&Mathf.Abs(image.rectTransform.sizeDelta.y-height)<.01f&&
      Mathf.Abs(feet-ground.y)<.01f,"Vegeta opening resized or unplanted the sprite");
    }
   }
   AssertSmoke(PortraitKey(Data.Index(RoundMotionArt.VegetaCardId))=="V0143/Vegeta/portrait"&&
    PortraitTexture(Data.Index(RoundMotionArt.VegetaCardId))!=null,"Updated Vegeta card art is missing");
   NavigateGallery("主页");
  }

  void VerifyPiccoloPresentation(){
   NavigateGallery("主页");
   BeginRoundDemo();
   var goku=new RoundFighter(RoundMotionArt.CardId,0,2,1200,100,40,0){Dodge=0};
   var piccolo=new RoundFighter(RoundMotionArt.PiccoloCardId,1,2,1200,100,40,0){Dodge=0};
   roundBattle=RoundCombat.Demo(new[]{goku,piccolo},RoundRules(),2);
   StartRoundPresentation();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   AssertSmoke(!RoundRules().OpeningSkills(piccolo.CardId).Any()&&PiccoloArt.Atlas.Length>=24&&
    roundCueClips.Count(c=>c.Key.StartsWith("piccolo-"))==11&&roundCueClips.Where(c=>c.Key.StartsWith("piccolo-"))
     .All(c=>c.Value.Clip!=null&&c.Value.Clip.samples>0),"Piccolo assets or trigger type did not load");
   AdvanceRoundPreview(true);
   AssertSmoke(roundLast.TriggersPursuit&&!roundLast.Countered&&roundLast.Target==piccolo,
    "Ordinary sweep must hit rather than trigger Piccolo");
   AdvanceRoundPreview(true);
   double preCounter=piccolo.HP;
   AdvanceRoundPreview(true);
   AssertSmoke(roundLast.Move==RoundMove.Pursuit&&roundLast.Countered&&roundLast.Target==piccolo&&roundBattle.CounterNext,
    "Single-target pursuit did not trigger Piccolo");
   goku.Rage=100;
   var guardStart=demoReactions[piccolo].Offset;
   AdvanceToMotionTime(roundMotion.ImpactAt+9);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoReactions[piccolo].Track=="counterPrepare"&&piccolo.HP==preCounter&&
    (demoReactions[piccolo].Offset-guardStart).sqrMagnitude<.01f&&demoFrontFx.GeometryCount>0,
    "Counter guard must shield on the spot with a distinct energy effect");
   AdvanceToMotionTime(roundMotion.ImpactAt+112);
   AssertSmoke(demoStage.localScale.x>1.06f&&roundMotion.CounterGuardWeight>.9f&&
    roundMotion.Duration-roundMotion.ImpactAt>=450,"Guard close-up does not hold for the player's reaction");
   AdvanceRoundPreview(true);
   AdvanceRoundPreview(true);
   AssertSmoke(roundMotion.IsCounter&&roundMotion.Clip.id=="piccoloCounter"&&roundSkillBadge.gameObject.activeSelf&&
    roundSkillArt.texture==Resources.Load<Texture2D>("Art/V0143/Piccolo/skill-icons")&&
    roundSkillText.text=="反魔爆裂光","Counter icon and name not shown during the triggered action");
   int cueCount=roundCueEvents;
   AdvanceToMotionTime(320);
   Canvas.ForceUpdateCanvases();
   float openingWidth=demoFrontFx.CounterWidth;
   AssertSmoke(roundMotion.PlaybackRate==.70f&&demoStage.localScale.x>1.06f,"Counter needs its own slower camera close-up");
   AdvanceToMotionTime(465);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(openingWidth>0&&demoFrontFx.CounterWidth>openingWidth+20&&VisibleRoundVitals(goku).HP==goku.MaxHP&&
    roundCueEvents>=cueCount+2,"Counter wave must spread before impact and play its attack cues");
   AdvanceToMotionTime(535);
   AssertSmoke(VisibleRoundVitals(goku).HP<goku.MaxHP&&demoReactions[goku].Track=="knockback",
    "Counter landed without visual knockback");
   AdvanceRoundPreview(true);AdvanceRoundPreview(true);
   AssertSmoke(roundLast.Actor==goku&&roundLast.Move==RoundMove.Ultimate,
    "Full rage must resume after the guarded counter");

   piccolo=new RoundFighter(RoundMotionArt.PiccoloCardId,0,2,1200,100,40,0){Dodge=0};
   goku=new RoundFighter(RoundMotionArt.CardId,1,3,10000,100,40,0){Dodge=0};
   roundBattle=RoundCombat.Demo(new[]{piccolo,goku},RoundRules(),12);
   StartRoundPresentation();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   AdvanceRoundPreview(true);
   AdvanceToMotionTime(70);
   var run=roundMotion;
   Rect running=demoActors[piccolo].uvRect;
   float travel=run.ShiftAt(run.Time).x;
   AdvanceToMotionTime(155);
   AssertSmoke(run.ShiftAt(run.Time).x>travel&&running!=demoActors[piccolo].uvRect&&
    demoActors[piccolo].texture.name=="combat-poses","Piccolo must alternate locomotion frames while advancing");
   AdvanceToMotionTime(run.ClockAtPose(run.Clip.returnAt+74));
   AssertSmoke(run.ShiftAt(run.Time).y<run.Clip.PlanarShift(run.PoseTime,run.Travel).y-8&&
    run.StagePose==PiccoloArt.Atlas[run.Clip.hopDrawing],"Piccolo must use an airborne return");
   AdvanceRoundPreview(true);
   piccolo.Rage=100;
   AdvanceRoundPreview(true);
   AssertSmoke(piccolo.IsCharging&&roundMotion.IsCharge&&!WaitingForRoundImpact&&roundSkillArt.texture!=null,
    "Charge action must show its skill and leave damage unrevealed");
   AdvanceToMotionTime(710);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.BeamChargeRadius>0&&demoStage.Find("DemoSlot0_2/Buffs/Charge")!=null,
    "Charge pose or persistent status is missing");
   AdvanceRoundPreview(true);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundMotion==null&&piccolo.IsCharging&&demoFrontFx.VisibleHeldCharges==1&&
    demoFrontFx.GeometryCount==52&&demoStage.Find("DemoSlot0_2/Buffs/Charge")!=null,
    "Charge FX disappeared between actions or after skipping the charge animation");
   AdvanceRoundPreview(true);
   AssertSmoke(roundMotion?.Actor==goku,"Expected opponent's action while Piccolo holds his charge");
   AdvanceToMotionTime(roundMotion.ImpactAt+8);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundMotion!=null&&piccolo.IsCharging&&demoFrontFx.VisibleHeldCharges==1,
    "Incoming combat must not clear Piccolo's charge aura");
   AdvanceRoundPreview(true);
   int guard=0;
   do{
    AdvanceRoundPreview(true);
    if(roundMotion?.Clip?.id=="specialBeam")break;
    if(roundMotion!=null)AdvanceRoundPreview(true);
   }while(++guard<10);
   AssertSmoke(roundMotion?.Clip?.id=="specialBeam"&&!piccolo.IsCharging,"Piccolo's next round did not release the charged special");
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.VisibleHeldCharges==0&&demoFrontFx.BeamChargeRadius>8&&
    demoStage.Find("DemoSlot0_2/Buffs/Charge")==null,
    "The held charge must flow into the beam wind-up without a blank frame or duplicate aura");
   AdvanceToMotionTime(585);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.BeamReach>0&&demoFrontFx.BeamReach<1&&demoFrontFx.GeometryCount>200&&
    Mathf.Abs(demoFrontFx.BeamTip.y-demoFrontFx.BeamPalm.y)<.01f&&roundMotion.Travel.y!=0&&
    VisibleRoundVitals(goku).HP>goku.HP,"Special beam geometry or delayed damage was lost");
   AdvanceToMotionTime(700);
   AssertSmoke(VisibleRoundVitals(goku).HP==goku.HP,"Special beam did not reveal on arrival");
   AdvanceRoundPreview(true);
   piccolo.ChargeReadyRound=roundBattle.Round+1;
   audioSettings.ReducedMotion=true;
   DrawRoundPreview();Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.VisibleHeldCharges==1,"Reduced motion should keep the charge indicator");
   piccolo.HP=0;
   DrawRoundPreview();Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.VisibleHeldCharges==0&&demoStage.Find("DemoSlot0_2/Buffs")==null,
    "A defeated charger left an aura or status icon on the field");
   audioSettings.ReducedMotion=false;
   NavigateGallery("主页");
  }

  void VerifyPiccoloApproach(){
   foreach(string id in new[]{"basic","slam"}){
    foreach(var span in new[]{new Vector2(270,80),new Vector2(440,160),new Vector2(0,160)}){
     var run=new RoundMotionPlayback{
      Action=new RoundAction{Move=RoundMove.Basic},
      Library=PiccoloArt,Clip=PiccoloArt[id],Travel=span,DemoLocomotion=true
     };
     run.PacePiccoloApproach();
     AssertSmoke(run.ApproachDuration>=196&&run.ImpactAt>run.ApproachStart+run.ApproachDuration&&
      run.ShiftAt(run.ApproachStart)==Vector2.zero,"Invalid Piccolo approach timing");
     run.Advance(run.ApproachStart+48);
     var first=run.StagePose;
     var start=run.ShiftAt(run.Time);
     run.Advance(RoundMotionPlayback.PiccoloBasicStride);
     var path=Vector2.Distance(start,run.ShiftAt(run.Time));
     AssertSmoke(first!=run.StagePose&&path>0&&path<=38.73f&&
      (span.x==0||Mathf.Abs(path-38.72f)<.01f),"Piccolo approach cadence or distance varied by attack");
     AssertSmoke(Mathf.Abs(run.PoseTimeAt(run.ImpactAt)-run.Clip.impactAt)<.01f&&
      run.ShiftAt(run.Duration).sqrMagnitude<.001f,"Paced attack lost its hit or return anchor");
    }
   }
   BeginRoundDemo();
   var vegeta=new RoundFighter(RoundMotionArt.VegetaCardId,0,1,1200,100,40,0){Dodge=0};
   var piccolo=new RoundFighter(RoundMotionArt.PiccoloCardId,0,8,1200,100,40,0){Dodge=0};
   var target=new RoundFighter(RoundMotionArt.CardId,1,9,1200,100,40,0){Dodge=0};
   roundBattle=RoundCombat.Demo(new[]{vegeta,piccolo,target},RoundRules(),2);
   StartRoundPresentation();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   AdvanceRoundPreview(true);
   AssertSmoke(roundLast.TriggersPursuit,"Expected a knockback for Piccolo's pursuit");
   AdvanceRoundPreview(true);
   AdvanceRoundPreview(true);
   var chase=roundMotion;
   AssertSmoke(chase.Actor==piccolo&&chase.IsPursuit&&chase.ApproachDuration>=265&&chase.ApproachDuration<=390&&
    chase.PiccoloReturnDuration>=250&&chase.ShiftAt(chase.ApproachStart)==Vector2.zero,
    "Pursuit must wait for its cue and use readable jump durations");
   AdvanceToMotionTime(chase.ApproachStart+50);
   var airborne=demoActors[piccolo].uvRect;
   AdvanceToMotionTime(chase.ApproachStart+chase.ApproachDuration*.5f);
   var shift=chase.ShiftAt(chase.Time);
   var landing=chase.ShiftAt(chase.ApproachStart+chase.ApproachDuration);
   AssertSmoke(airborne==demoActors[piccolo].uvRect&&WaitingForRoundImpact&&shift.x>0&&
    Mathf.Abs(shift.y-landing.y*.5f+22)<.1f,"Forward jump lost its arc or used running sprites");
   AdvanceToMotionTime(chase.ImpactAt+8);
   AssertSmoke(chase.Revealed&&roundLast.Target==target&&roundLast.TargetState==RoundHitState.LargeFloat,
    "The jumping pursuit did not resolve its configured hit");
   float returnAt=chase.ClockAtPose(chase.Clip.returnAt);
   AdvanceToMotionTime(returnAt+chase.PiccoloReturnDuration*.5f);
   AssertSmoke(chase.StagePose==PiccoloArt.Atlas[chase.Clip.hopDrawing]&&chase.ShiftAt(chase.Time).x>0&&
    chase.ShiftAt(chase.Duration).sqrMagnitude<.001f,"The backward hop must return to the original position");
   NavigateGallery("主页");
  }

  void VerifyGohanPresentation(){
   NavigateGallery("主页");
   BeginRoundDemo();
   var gohan=new RoundFighter(RoundMotionArt.GohanCardId,0,2,1200,100,40,0){Dodge=0,HP=1100};
   var enemy=new RoundFighter(RoundMotionArt.CardId,1,2,1200,120,45,0){Dodge=0};
   var elite=new RoundFighter(RoundMotionArt.VegetaCardId,1,9,1200,150,45,0){Dodge=0};
   var rule=RoundRules();
   var guard=rule.For(gohan.CardId).skills.Single();
   float probability=(float)guard.chance;
   AssertSmoke(Math.Abs(guard.chance-.30)<.001,"Gohan guard probability missing");
   try{
    guard.chance=1;
    roundBattle=RoundCombat.Demo(new[]{gohan,enemy,elite},rule,2);
    StartRoundPresentation();
    while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
    AssertSmoke(GohanArt.Atlas.Length==17&&demoActors[gohan].texture.name=="combat-poses"&&
     roundCueClips.Where(cue=>cue.Key.StartsWith("gohan-")).Count(cue=>cue.Value.Clip!=null&&cue.Value.Clip.samples>0)==11,
     "Gohan's combat sprites or local audio bank are missing");
    int cues=roundCueEvents;
    AdvanceRoundPreview(true);
    AssertSmoke(roundMotion.Clip.id=="gohanRally"&&roundLast.Healing&&VisibleRoundVitals(gohan).HP==1100&&roundMotion.Travel==Vector2.zero,
     "Rally must heal in place without leaking HP early");
    AdvanceToMotionTime(480);
    Canvas.ForceUpdateCanvases();
    AssertSmoke(demoRearFx.GeometryCount>0&&VisibleRoundVitals(gohan).HP==1130&&
     roundCueEvents==cues+2&&demoOutcomes[gohan].text==DemoNumber(roundLast.Amount),"Rally FX, sound or HP reveal missing");
    AdvanceRoundPreview(true);
    gohan.Rage=100;
    AdvanceRoundPreview(true);
    AssertSmoke(roundMotion.Clip.id=="gohanHeavy"&&roundLast.Target==elite&&roundMotion.Travel.x>0&&roundLast.DefenseDelta==-.20,
     "Heavy strike must travel to the highest attack target and reduce defense");
    string eliteStatus="DemoSlot"+elite.Side+"_"+elite.Position+"/Buffs";
    AssertSmoke(demoStage.Find(eliteStatus+"/Elite")!=null&&demoStage.Find(eliteStatus+"/DefenseDown")==null,
     "Permanent badge missing or defense debuff revealed before impact");
    var heavy=roundMotion;
    AssertSmoke(heavy.PlaybackRate==.73f&&heavy.Clip.duration==1250&&heavy.ImpactAt==824,
     "Heavy strike did not slow down independently");
    AdvanceToMotionTime(443);
    var plant=heavy.ShiftAt(heavy.Time);
    var firstWindup=heavy.StagePose;
    AdvanceToMotionTime(620);
    Canvas.ForceUpdateCanvases();
    AssertSmoke(firstWindup==GohanArt.Atlas[15]&&heavy.StagePose==GohanArt.Atlas[16]&&
     (plant-heavy.ShiftAt(heavy.Time)).sqrMagnitude<.002f&&demoRearFx.GeometryCount>0&&
     WaitingForRoundImpact&&VisibleRoundVitals(elite).HP==elite.MaxHP,
     "Heavy windup must keep both ground coordinates planted and conceal pre-impact damage");
    AdvanceToMotionTime(roundMotion.ImpactAt+32);
    Canvas.ForceUpdateCanvases();
    AssertSmoke(roundMotion.Revealed&&demoOutcomes[elite].text.All(char.IsDigit)&&demoFrontFx.GeometryCount>0&&
     demoStage.Find(eliteStatus+"/DefenseDown")!=null,
     "Heavy strike must reveal the contact and weakening together");
    AdvanceRoundPreview(true);
    enemy.Rage=0;
    AdvanceRoundPreview(true);
    AdvanceToMotionTime(roundMotion.ImpactAt+75);
    Canvas.ForceUpdateCanvases();
    AssertSmoke(roundLast.Target==gohan&&roundLast.GuardAbility==guard&&demoFrontFx.GeometryCount>0&&
     demoOutcomes[gohan].text.All(char.IsDigit),"Gohan guard has no reveal or energy barrier");

    var ally=new RoundFighter(RoundMotionArt.CardId,0,1,1200,100,40,0){Dodge=0};
    gohan=new RoundFighter(RoundMotionArt.GohanCardId,0,2,1200,100,40,0){Dodge=0};
    enemy=new RoundFighter(RoundMotionArt.CardId,1,1,1200,100,40,0){Dodge=0};
    roundBattle=RoundCombat.Demo(new[]{ally,gohan,enemy},rule,2);
    StartRoundPresentation();
    while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
    for(int step=0;step<5;step++)AdvanceRoundPreview(true);
    AssertSmoke(roundMotion.Clip.id=="gohanFlash"&&roundLast.Actor==gohan&&roundLast.TargetState==RoundHitState.LargeFloat&&
     roundLast.DefenseDelta==.20&&WaitingForRoundImpact,"Small float did not start Gohan's pursuit clip");
    cues=roundCueEvents;
    AdvanceToMotionTime(roundMotion.ImpactAt+20);
    Canvas.ForceUpdateCanvases();
    AssertSmoke(demoFrontFx.GeometryCount>0&&demoOutcomes[enemy].text.All(char.IsDigit)&&roundCueEvents>=cues+3&&
     demoStage.Find("DemoSlot0_2/Buffs/DefenseUp")!=null,
     "Flash arc, defense feedback or matched voice and impact cues are missing");
   }finally{guard.chance=probability;}
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyVegetaVolley(){
   NavigateGallery("主页");
   BeginRoundDemo(FiveCardDemoFixture());
   AssertSmoke(demoActors.Count==10&&roundBattle.Order[0].Side==0&&roundBattle.Order[1].Side==1,
    "5v5 presentation did not load the configured rosters");
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   var vegeta=roundBattle.Order.First(f=>f.Side==0&&f.CardId==RoundMotionArt.VegetaCardId);
   var defender=roundBattle.Order.First(f=>f.Side==1&&f.CardId==RoundMotionArt.VegetaCardId);
   AssertSmoke(VegetaArt.Atlas.Length==23&&VegetaArt.Atlas[14].Texture.name=="opening-hammer"&&
    VegetaArt["basic"].impacts.Length==2&&VegetaArt["kick"].impacts.Length==3&&
    VegetaArt["pursuit"].impacts[0].kind=="hammer"&&demoActors[defender].uvRect.width<0,
    "Vegeta pose, knockback branch, hammer or mirroring missing");
   foreach(var fighter in roundBattle.Order)fighter.Dodge=0;
   vegeta.Rage=100;
   int cues=roundCueEvents;
   AdvanceRoundPreview(true);
   var run=roundMotion;
   var volley=run.VolleyHits;
   AssertSmoke(run.Actor==vegeta&&run.ReleaseAt==350&&volley.Length==6&&volley.Take(5).Select(hit=>hit.Target).Distinct().Count()==5&&
    roundLog.Count==0&&volley.All(hit=>VisibleRoundVitals(hit.Target).HP==hit.Target.MaxHP),
    "Vegeta volley did not target the live 5v5 or leaked damage");
   AdvanceToMotionTime(run.VolleyLaunch(0)+60);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.VisibleVolleyOrbs==1&&demoFrontFx.GeometryCount>0&&run.RevealedCount==0&&roundCueEvents==cues+2&&
    roundSkillText.text=="连续能源弹"&&roundSkillArt.texture.name=="continuous-energy-volley",
    "First launch state. Orbs "+demoFrontFx.VisibleVolleyOrbs+", hits "+run.RevealedCount+", audio "+(roundCueEvents-cues));
   for(int i=0;i<volley.Length;i++){
    AdvanceToMotionTime(run.VolleyImpact(i)+8);
    var hit=volley[i];
    Canvas.ForceUpdateCanvases();
    AssertSmoke(demoFrontFx.VisibleVolleyBursts>0&&run.RevealedCount==i+1&&Math.Abs(VisibleRoundVitals(hit.Target).HP-hit.TargetHPAfter)<.001&&
     demoOutcomes.ContainsKey(hit.Target)&&demoOutcomes[hit.Target].text.All(char.IsDigit),
     "Energy shot did not update its actual target at index "+i);
   }
   AssertSmoke(run.Revealed&&roundLog.Count==1&&demoOutcomes.Count==5&&roundCueEvents==cues+13+volley.Length,
    "Six launches and impacts must play leads once and publish one action");
   AdvanceRoundPreview(true);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.VisibleVolleyOrbs==0&&roundMotion==null&&demoStage.localScale==Vector3.one,
    "Skipping volley did not reset visuals and view");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyDemoMultiTarget(){
   var setup=new RoundDemoSetup{scene=demoScene.id,
    allies=new[]{"","goku-test","","","piccolo-test","","","",""},
    enemies=new[]{"","","goku-test","","","goku-test","","vegeta-test",""}};
   roundDemo=true;
   roundBattle=setup.CreateBattle(RoundRules(),1);
   StartRoundPresentation();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   AssertSmoke(demoActors.Count==5&&demoActors.Values.All(image=>image.texture!=null)&&
    demoActors[roundBattle.Order[2]].texture==PiccoloIdlePose(roundBattle.Order[2]).Texture,"Occupied slots did not draw 2D actors from their own art");
   var attacker=roundBattle.Order[0];
   foreach(var fighter in roundBattle.Order)fighter.Dodge=0;
   var target=roundBattle.Order[1];
   var flank=roundBattle.Order[3];
   var otherLane=roundBattle.Order[4];
   AdvanceRoundPreview(true);
   AssertSmoke(roundLast.Move==RoundMove.Basic&&roundLast.Target==target&&target.Position==3&&
    Mathf.Abs(roundMotion.Travel.y)>.1f&&VisibleRoundVitals(target).HP==target.MaxHP,
    "Cross-row movement or live front target selection failed");
   var rooted=roundMotion;
   float atContact=rooted.Clip.impacts[0].at;
   float depth=DemoGround(target).y-DemoGround(attacker).y;
   AssertSmoke(rooted.ShiftAt(0)==Vector2.zero&&rooted.ShiftAt(atContact).y==depth&&
    rooted.ShiftAt(rooted.ImpactAt).y==depth&&rooted.ShiftAt(rooted.Clip.returnAt).y==depth&&
    rooted.ShiftAt(rooted.Duration)==Vector2.zero,"Melee route drifted off the target row while planted");
   AdvanceRoundPreview(true);

   attacker.Rage=100;
   var flankHealth=flank.HP;
   AdvanceRoundPreview(true);
   AssertSmoke(roundLast.Move==RoundMove.Ultimate&&roundLast.Hits.Count()==2&&
    roundLast.Hits.Any(hit=>hit.Target==flank)&&VisibleRoundVitals(flank).HP==flankHealth&&
    otherLane.HP==otherLane.MaxHP&&roundMotion.BeamTarget.x>DemoGround(1,9).x+70&&
    roundMotion.ShiftAt(roundMotion.ReleaseAt).y==depth&&roundMotion.ShiftAt(roundMotion.Duration)==Vector2.zero,
    "Row beam did not aim at all affected enemies or leaked health early");
   AdvanceToMotionTime(1220);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.GeometryCount>300&&WaitingForRoundImpact&&roundMotion.SeenHits.Count==0,"Traveling effects or current hit reveal timing failed");
   AdvanceToMotionTime(1470);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.BeamTip.y==demoFrontFx.BeamPalm.y&&demoFrontFx.BeamTip.x>DemoGround(1,9).x+70,
    "Cross-row beam did not sweep every slot horizontally");
   AssertSmoke(demoOutcomes.Count==2&&demoOutcomes.Values.All(label=>label.text.All(char.IsDigit))&&
    VisibleRoundVitals(flank).HP==flank.HP&&flank.HP<flankHealth&&
    roundLast.Hits.Count(hit=>hit.TargetState==RoundHitState.Knockback)==1&&
    demoReactions[otherLane].Track==null,"Demo did not display each affected target at impact");
   AssertSmoke(demoStage.localScale.x>1&&roundSkillBadge.gameObject.activeSelf,"Cross-row view lacks its active ultimate");
   AdvanceRoundPreview(true);
   AssertSmoke(demoStage.localScale==Vector3.one&&demoStage.anchoredPosition==Vector2.zero&&
    !roundSkillBadge.gameObject.activeSelf,"Skipping an ultimate did not restore its view");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  float RenderedGokuGroundY(RoundFighter fighter) {
   var run=roundMotion;
   var pose=run.StagePose;
   return -demoActors[fighter].rectTransform.anchoredPosition.y+(pose.pivot[1]-pose.crop[1])*pose.zoom*DemoScale(fighter);
  }

  void VerifyFullDemoGrid(){
   var fullSide=Data.RoundRoster.Take(RoundBoard.PlayerLimit).ToArray();
   var setup=new RoundDemoSetup{scene=demoScene.id,
    allies=fullSide.Concat(Enumerable.Repeat("",RoundBoard.Cells-fullSide.Length)).ToArray(),
    enemies=RoundBoards().boards.Last().positions};
   roundDemo=true;
   roundBattle=setup.CreateBattle(RoundRules(),3);
   StartRoundPresentation();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoActors.Count==fullSide.Length+9&&demoActors.Keys.Count(fighter=>fighter.Side==1)==9,"Full side omitted actors");
   var boxes=demoActors.Select(item=>{
    var image=item.Value;
    var position=image.rectTransform.anchoredPosition;
    var size=image.rectTransform.sizeDelta;
    AssertSmoke(image.texture!=null&&image.isActiveAndEnabled&&
     position.x>=0&&position.x+size.x<=RoundMotionFx.Width&&
     -position.y>=0&&-position.y+size.y<=RoundMotionFx.Height,"Full board sprite is clipped");
    return new Rect(position.x,-position.y,size.x,size.y);
   }).ToArray();
   for(int i=0;i<boxes.Length;i++)for(int j=i+1;j<boxes.Length;j++){
    var a=boxes[i];var b=boxes[j];
    var overlap=Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*
     Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
    AssertSmoke(overlap<.16f*Mathf.Min(a.width*a.height,b.width*b.height),"Full board crops cover another fighter: "+demoActors.ElementAt(i).Key.CardId+"/"+demoActors.ElementAt(j).Key.CardId);
   }
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyBeatTiming(){
   foreach(var item in new[]{(RoundMove.Basic,340f),(RoundMove.Ultimate,430f),(RoundMove.Pursuit,500f)}){
    var playback=new RoundMotionPlayback{Action=new RoundAction{Move=item.Item1},Clip=GokuArt["ultimate"]};
    playback.Advance(500);
    AssertSmoke(Mathf.Abs(playback.Time-item.Item2)<.001f,"Unexpected clock rate for "+item.Item1);
   }
   var newAtlas=GokuArt.Atlas.Where(pose=>pose.Texture.name=="BeamPoses").ToArray();
   AssertSmoke(newAtlas.Length==6&&newAtlas.All(pose=>pose.beam.Length==2)&&
    newAtlas.Select(pose=>(pose.crop[0],pose.crop[1])).Distinct().Count()==6,
    "Goku ultimate atlas was rescaled or its crop positions shifted");
   var basic=GokuArt["basic"];
   var sweep=GokuArt["sweep"];
   var uppercut=GokuArt["pursuit"];
   var ultimate=GokuArt["ultimate"];
   AssertSmoke(basic.Travel(113)==basic.Travel(150)&&basic.Travel(300)==basic.Travel(350)&&basic.Travel(110)>110&&
    sweep.impacts.Last().at-sweep.impacts[1].at>330&&
    uppercut.Travel(232)==uppercut.Travel(285)&&
    ultimate.beam.fire-ultimate.beam.gather>2*ultimate.beam.flight&&
    ultimate.impactAt==ultimate.beam.fire+ultimate.beam.flight&&ultimate.Travel(ultimate.impactAt)==0,
    "Motion tracks lost acceleration, planted impacts, or the charge-to-release contrast");
  }

  void AssertDemoMeter(Transform slot,string name,float ratio,Color color){
   var track=(RectTransform)slot.Find(name);
   var fill=(RectTransform)track.Find("Fill");
   AssertSmoke(track.sizeDelta.y==3&&fill.sizeDelta.y==3&&
    Mathf.Abs(fill.sizeDelta.x-track.sizeDelta.x*Mathf.Clamp01(ratio))<.05f&&
    fill.GetComponent<UI.Image>().color==color&&track.GetComponent<UI.Image>().type==UI.Image.Type.Sliced&&
    fill.GetComponent<UI.Image>().sprite==RoundedSurface(false),"Wrong in-slot color, width or corners for "+name);
  }

  void VerifyRoundTempoAndMeters(){
   NavigateGallery("主页");
   GalleryTestButton("HomeNav"+DemoBattleRoute).onClick.Invoke();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   AssertSmoke(Mathf.Abs(roundClock-RoundOpeningGap)<.001f,"Opening should use the short transition");
   roundBattle=MakeGokuMirrorFixture(1);
   StartRoundPresentation();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   var attacker=roundBattle.Order[0];
   var defender=roundBattle.Order[1];
   defender.Dodge=0;defender.HP=defender.MaxHP;defender.Shield=13;defender.Rage=21;
   attacker.Rage=0;
   DrawRoundPreview();
   var defenderSlot=demoStage.Find("DemoSlot1_"+defender.Position);
   AssertDemoMeter(defenderSlot,"Health",1,RoundHealthFill);
   AssertDemoMeter(defenderSlot,"Shield",13f/(float)defender.MaxHP,RoundShieldFill);
   AssertDemoMeter(defenderSlot,"Rage",.21f,RoundRageFill);
   var surface=defenderSlot.GetComponent<UI.Image>();
   var health=(RectTransform)defenderSlot.Find("Health");
   var rage=(RectTransform)defenderSlot.Find("Rage");
   var size=((RectTransform)defenderSlot).sizeDelta;
   var buffs=(RectTransform)defenderSlot.Find("Buffs");
   var training=(RectTransform)buffs.Find("Training");
   AssertSmoke(surface.type==UI.Image.Type.Sliced&&surface.sprite==RoundedSurface(false)&&surface.color.a<.6f&&
    health.anchoredPosition==new Vector2(3,-3)&&size.x-health.sizeDelta.x==6&&size.y+buffs.anchoredPosition.y-buffs.sizeDelta.y==3&&
    buffs.anchoredPosition.y<rage.anchoredPosition.y-rage.sizeDelta.y&&
    training.rect.width==training.rect.height&&training.GetComponent<UI.RawImage>().texture==RoundMotionArt.LoadOpeningIcon(),
    "Outer meter frame needs soft rounded corners and equal four-side padding");
   AssertSmoke(demoStage.Find("DemoSlot1_1").Find("Health")==null&&
    defenderSlot.Find("Health").localPosition.y>defenderSlot.Find("Shield").localPosition.y&&
    defenderSlot.Find("Shield").localPosition.y>defenderSlot.Find("Rage").localPosition.y,
    "Meters should be vertically ordered and only drawn for occupied positions");

   AdvanceRoundPreview(false);
   var first=roundLast;
   AssertSmoke(first.Actor==attacker&&first.Move==RoundMove.Basic&&roundMotion!=null,"Missing expected opening attack");
   AdvanceToMotionTime(roundMotion.ImpactAt-12);
   defenderSlot=demoStage.Find("DemoSlot1_"+defender.Position);
   AssertDemoMeter(defenderSlot,"Health",1,RoundHealthFill);
   AssertDemoMeter(defenderSlot,"Shield",13f/(float)defender.MaxHP,RoundShieldFill);
   AssertDemoMeter(defenderSlot,"Rage",.21f,RoundRageFill);
   AdvanceToMotionTime(roundMotion.ImpactAt+12);
   defenderSlot=demoStage.Find("DemoSlot1_"+defender.Position);
   AssertSmoke(roundLog.Count==1&&defender.Shield<13,"Impact did not consume the shield");
   var damage=demoOutcomes[defender];
   var shield=damage.transform.Find("ShieldNumber").GetComponent<UI.Text>();
   AssertSmoke(damage.text==DemoNumber(first.HealthDamage)&&shield.text=="13"&&shield.fontSize==13&&shield.color==DemoShieldColor&&
    damage.rectTransform.anchoredPosition.y>demoActors[defender].rectTransform.anchoredPosition.y+40,
    "Impact must show only the actual HP loss and smaller blue shield number");
   var shieldOnly=new RoundAction{ShieldDamage=30};
   var critical=new RoundAction{HealthDamage=12,Critical=true};
   AssertSmoke(DemoHitText(shieldOnly)=="30"&&DemoHitColor(shieldOnly)==DemoShieldColor&&
    DemoHitText(critical)=="12"&&DemoHitColor(critical)!=Color.white&&
    DemoHitText(new RoundAction{Dodged=true})==""&&DemoHitText(new RoundAction{Healing=true,Amount=15})=="15",
    "Numeric feedback colors or dodge suppression failed");
   string loss=DemoNumber(first.HealthDamage+first.ShieldDamage);
   AssertSmoke(demoViewport.parent.Find("DemoStats0/StatsSlot"+attacker.Position+"/Value").GetComponent<UI.Text>().text==loss&&
    demoViewport.parent.Find("DemoStats1/StatsSlot"+defender.Position+"/Value").GetComponent<UI.Text>().text=="0",
    "Live damage panel did not count the impact exactly once");
   GalleryTestButton("Metric1").onClick.Invoke();
   AssertSmoke(demoMetric==RoundBattleMetric.Taken&&
    demoViewport.parent.Find("DemoStats1/StatsSlot"+defender.Position+"/Value").GetComponent<UI.Text>().text==loss,
    "Taken tab did not count actual HP and shield loss");
   GalleryTestButton("Metric2").onClick.Invoke();
   AssertSmoke(demoMetric==RoundBattleMetric.Healing,"Both panels did not switch to healing");
   defenderSlot=demoStage.Find("DemoSlot1_"+defender.Position);
   AssertDemoMeter(defenderSlot,"Health",(float)(defender.HP/defender.MaxHP),RoundHealthFill);
   AssertDemoMeter(defenderSlot,"Shield",(float)(defender.Shield/defender.MaxHP),RoundShieldFill);
   AssertDemoMeter(defenderSlot,"Rage",(float)defender.Rage/RoundCombat.RageCap,RoundRageFill);

   AdvanceToMotionTime(roundMotion.Duration+1);
   AssertSmoke(roundMotion==null&&roundLast==first&&Mathf.Abs(roundClock-.68f)<.001f,"Action did not start the slower handoff");
   UpdateRoundPreview(.65f);
   AssertSmoke(roundMotion==null&&roundLast==first,"Combat stepped before the recovery time ended");
   UpdateRoundPreview(.045f);
   AssertSmoke(roundLast!=first&&roundMotion!=null,"Combat did not advance after the recovery time");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyFormationDrag(){
   var initial=profile.RoundFormation();
   var saved=File.ReadAllText(savePath);
   NavigateGallery(FormationRoute);
   AssertSmoke(page==FormationRoute&&galleryChrome.Find("Nav"+FormationRoute)!=null&&homeRoot.Find("HomeNav"+FormationRoute)!=null&&
    formationRoot.Find("FormationStart")==null&&formationRoot.Find("FormationTitle")==null&&
    formationRoot.Find("FormationBattlefield/FormationCountPanel")==null&&formationRoot.Find("FormationBattlefield/FormationSelectedPanel")==null,
    "Formation entry or clean layout failed");
   Vector2 ScreenPoint(RectTransform rect,Vector2 point)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(point));
   Vector2 Cell(int slot,Vector2 offset=default){
    var center=FormationGridGraphic.Center(slot)+offset;
    return ScreenPoint(formationGrid.rectTransform,new Vector2(center.x,-center.y));
   }
   AssertSmoke(formationGrid.Layout.Depth==new Vector2(40,64)&&formationGrid.Layout.Across==new Vector2(183,0),
    "Formation tiles must be congruent and use the new shear");
   foreach(int slot in Enumerable.Range(1,RoundBoard.Cells))
    AssertSmoke(FormationSlotAt(Cell(slot),null)==slot&&FormationSlotAt(Cell(slot,new Vector2(-72,20)),null)==slot,
     "Parallelogram did not map its center and lower-left corner to slot "+slot);
   var id=Data.Index(RoundMotionArt.CardId);
   var source=Array.IndexOf(initial,id)+1;
   var tile=GalleryTestButton("FormationCard"+RoundMotionArt.CardId);
   var drag=tile.GetComponent<FormationDragHandle>();
   AssertSmoke(tile.transform.Find("FormationCardPosition")==null,"Roster tile still has a slot label");
   tile.onClick.Invoke();
   Canvas.ForceUpdateCanvases();
   var modal=dialogRoot.Find("Modal");
   var scroll=modal.Find("FormationCardScroll").GetComponent<UI.ScrollRect>();
   AssertSmoke(page==FormationRoute&&modal.Find("Title").GetComponent<UI.Text>().text==Data.Cards[id].Name&&
    scroll.content.Find("StatTile0")!=null&&scroll.content.GetComponentsInChildren<UI.RawImage>().Length==4&&
    scroll.content.rect.height>scroll.viewport.rect.height&&!tile.interactable,
    "Formation preview is missing portrait, stats, scrollable skills or modal isolation");
   CloseGalleryDialog();
   var events=UnityEngine.EventSystems.EventSystem.current;
   var pointer=new UnityEngine.EventSystems.PointerEventData(events){
    button=UnityEngine.EventSystems.PointerEventData.InputButton.Left
   };
   var origin=ScreenPoint((RectTransform)drag.transform,((RectTransform)drag.transform).rect.center);
   pointer.pressPosition=origin;pointer.position=origin+Vector2.left*12;
   drag.OnBeginDrag(pointer);drag.OnDrag(pointer);
   AssertSmoke(formationGhost==null,"Drag sprite appeared before the pointer left the list");
   pointer.position=Cell(7,new Vector2(-72,20));drag.OnDrag(pointer);
   AssertSmoke(formationGhost!=null&&formationGhost.texture==GokuArt.At(GokuArt["idle"],0).Texture,
    "The actual battle sprite did not follow the list drag");
   drag.OnEndDrag(pointer);
   AssertSmoke(formationGhost==null&&formationDraft[6]==id&&profile.RoundFormation().SequenceEqual(initial)&&File.ReadAllText(savePath)==saved,
    "Drop must update only the draft");
   GalleryTestButton("Nav"+DemoBattleRoute).onClick.Invoke();
   AssertSmoke(roundBattle.Order.Any(f=>f.Side==0&&f.CardId==RoundMotionArt.CardId&&f.Position==source)&&
    roundBattle.Order.Count(f=>f.Side==1)==5&&formationDraft==null,"Leaving with an unpublished draft affected Demo");
   NavigateGallery(FormationRoute);
   AssertSmoke(formationDraft.SequenceEqual(initial),"Reentering formation did not restore the saved lineup");

   int occupied=Enumerable.Range(1,9).First(slot=>slot!=source&&formationDraft[slot-1]>=0);
   int other=formationDraft[occupied-1];
   var gridInput=formationGrid.GetComponent<FormationGridInput>();
   pointer.position=Cell(source,new Vector2(-72,20));pointer.pressPosition=pointer.position;
   var outside=ScreenPoint(formationGrid.rectTransform,new Vector2(9,-28));
   AssertSmoke(formationGrid.Raycast(pointer.position,null)&&!formationGrid.Raycast(outside,null),
    "Grid raycast must follow the polygon even at a cell corner");
   gridInput.OnPointerDown(pointer);
   pointer.position=Cell(occupied,new Vector2(-72,20));
   gridInput.OnBeginDrag(pointer);gridInput.OnDrag(pointer);
   gridInput.OnPointerUp(pointer);gridInput.OnEndDrag(pointer);
   AssertSmoke(formationDraft[occupied-1]==id&&formationDraft[source-1]==other&&profile.RoundFormation().SequenceEqual(initial)&&
    GalleryTestButton("FormationSave").interactable,"Grid drag did not swap occupied slots as a draft");

   int yamcha=Data.Index(RoundMotionArt.YamchaCardId);
   BeginFormationDrag(yamcha);EndFormationDrag(Cell(source,new Vector2(-72,20)),null);
   AssertSmoke(formationDraft[source-1]==yamcha&&!formationDraft.Contains(other)&&File.ReadAllText(savePath)==saved,
    "Benched character did not replace the occupied slot or draft touched the saved file");
   var expected=(int[])formationDraft.Clone();
   BeginFormationDrag(id);EndFormationDrag(Vector2.zero,null);
   AssertSmoke(formationDraft.SequenceEqual(expected)&&formationGhost==null,"Outside drop changed the draft");
   GalleryTestButton("FormationSave").onClick.Invoke();
   Load();
   AssertSmoke(profile.RoundFormation().SequenceEqual(expected)&&!GalleryTestButton("FormationSave").interactable&&saveError=="",
    "Save button did not persist the whole draft");
   GalleryTestButton("Nav"+DemoBattleRoute).onClick.Invoke();
   AssertSmoke(roundBattle.Order.Any(f=>f.Side==0&&f.CardId==RoundMotionArt.YamchaCardId&&f.Position==source)&&
    roundBattle.Order.Count(f=>f.Side==1)==5,"Demo did not load the explicitly saved lineup");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
   AssertSmoke(profile.TrySaveRoundFormation(initial),"Unable to restore the original lineup after formation tests");
   Save();
  }

  void VerifyTienPresentation(){
   var id=RoundMotionArt.TienCardId;
   string[] Slots(params string[] ids)=>ids.Concat(Enumerable.Repeat("",9-ids.Length)).ToArray();
   void Stage(string[] allies,string[] enemies){
    BeginRoundDemo(new RoundDemoSetup{allies=allies,enemies=enemies});roundPaused=true;
    while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
    foreach(var unit in roundBattle.Order)unit.Dodge=0;
   }
   Stage(Slots("",id),Slots("",RoundMotionArt.CardId,RoundMotionArt.YamchaCardId,"",RoundMotionArt.VegetaCardId));
   var tien=roundBattle.Order[0];
   AssertSmoke(TienArt.Atlas.Length==16&&roundCueClips.Count(c=>c.Key.StartsWith("tien-")&&c.Value.Clip!=null)==10&&
    PortraitTexture(Data.Index(id))==Resources.Load<Texture2D>("Art/V0173/Tien/portrait"),"Tien portrait, sprites or local audio failed");
   tien.Rage=100;
   AdvanceRoundPreview(true);
   var finger=roundMotion;
   AdvanceToMotionTime((finger.ReleaseAt+finger.ImpactAt)*.5f);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(finger.Clip.id=="tienFinger"&&finger.Action.Move==RoundMove.Skill&&roundSkillText.text=="一指功"&&
    finger.Travel==Vector2.zero&&demoFrontFx.GeometryCount>0&&!finger.Revealed,"Round opener did not launch as a projectile before damage");
   AdvanceToMotionTime(finger.ImpactAt+8);
   AssertSmoke(finger.Revealed&&tien.Rage==100,"The round opener consumed rage or did not reveal");
   AdvanceRoundPreview(true);AdvanceRoundPreview(true);
   var blast=roundMotion;
   var oldHP=VisibleRoundVitals(tien).HP;
   AdvanceToMotionTime(blast.ReleaseAt+30);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(blast.Clip.id=="tienCannon"&&blast.Action.Hits.Count()==2&&!blast.Revealed&&
    Math.Abs(VisibleRoundVitals(tien).HP-oldHP)<.001&&demoFrontFx.GeometryCount>0,"Cannon windup, facing row or concealed cost failed");
   AdvanceToMotionTime(blast.ImpactAt+9);
   AssertSmoke(blast.Revealed&&Math.Abs(VisibleRoundVitals(tien).HP-oldHP+tien.MaxHP*.05)<.001&&demoSelfCost!=null&&
    blast.Action.Hits.All(hit=>RoundBoard.Row(hit.Target.Position)==1),"HP cost, floating display or row at impact failed");
   AdvanceRoundPreview(true);AdvanceRoundPreview(true);
   var basic=roundMotion;
   var hiddenHP=VisibleRoundVitals(basic.Action.Target).HP;
   AdvanceToMotionTime(basic.Clip.impacts[1].at+8);
   AssertSmoke(basic.Clip.id=="tienVolley"&&!basic.Revealed&&VisibleRoundVitals(basic.Action.Target).HP==hiddenHP,
    "Volley attack settled before the final spike");
   AdvanceToMotionTime(basic.ImpactAt+14);
   AssertSmoke(basic.Revealed&&basic.StagePose==TienArt.Atlas[2],"Volley final spike did not bind the attack settlement");

   Stage(Slots(RoundMotionArt.CardId,id),Slots(RoundMotionArt.VegetaCardId));
   AdvanceRoundPreview(true);AdvanceRoundPreview(true);
   var goku=RoundRules().For(RoundMotionArt.CardId).techniques;
   var prior=goku.basicReaction;var chance=goku.basicReactionChance;
   try{goku.basicReaction=RoundHitState.LargeFloat;goku.basicReactionChance=1;AdvanceRoundPreview(true);}
   finally{goku.basicReaction=prior;goku.basicReactionChance=chance;}
   AdvanceRoundPreview(true);AdvanceRoundPreview(true);
   var set=roundMotion;
   AdvanceToMotionTime(set.ImpactAt+8);
   AssertSmoke(set.Clip.id=="tienSet"&&set.Revealed&&set.Action.TargetState==RoundHitState.SmallFloat&&roundSkillArt.texture!=null,
    "The authored Tien pursuit did not link large float to small float at contact");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyV174Roles(){
   NavigateGallery("角色");
   var roleCards=new[]{Data.Index(RoundMotionArt.TienCardId),Data.Index(RoundMotionArt.GohanCardId),Data.Index(RoundMotionArt.YamchaCardId)};
   var icons=roleCards.Select(i=>cardButtons[i].transform.Find("CharacterRoleIcon").GetComponent<UI.RawImage>()).ToArray();
   void CheckTitleAlignment(UI.RawImage icon,UI.Text description){
    var label=description.rectTransform;
    Rect ink=RoleTextInkBounds(description);
    var mark=icon.rectTransform;
    AssertSmoke(icon.texture==Resources.Load<Texture2D>("Interface/Role/V0174/roles")&&
     mark.anchoredPosition.x-label.anchoredPosition.x>=Mathf.Min(label.rect.width,ink.xMax)+9.9f&&
     Mathf.Abs(mark.anchoredPosition.y-label.anchoredPosition.y-ink.yMax)<.01f&&
     Mathf.Abs(mark.rect.height-ink.height)<.01f,"Gallery role icon must match printed title bounds");
   }
   for(int i=0;i<icons.Length;i++)
    CheckTitleAlignment(icons[i],cardButtons[roleCards[i]].transform.Find("Theme").GetComponent<UI.Text>());
   AssertSmoke(icons.Select(icon=>icon.uvRect).Distinct().Count()==3,"Role motifs do not use three separate atlas regions");
   OpenGalleryCard(roleCards[0]);
   CheckTitleAlignment(detailRoot.Find("CharacterRoleIcon").GetComponent<UI.RawImage>(),
    detailRoot.Find("CharacterTitle").GetComponent<UI.Text>());
   NavigateGallery(FormationRoute);
   var tile=GalleryTestButton("FormationCard"+RoundMotionArt.TienCardId).transform;
   var badges=(RectTransform)tile.Find("FormationCardBadges");
   AssertSmoke(tile.Find("FormationCardDescription")!=null&&tile.Find("FormationCardName")!=null&&
    badges.Find("CharacterRoleIcon")!=null&&badges.Find("Rarity_C")!=null&&
    badges.anchoredPosition.y<((RectTransform)tile.Find("FormationCardName")).anchoredPosition.y,
    "Formation card does not use two rows for identity and badges");
   var rarity=(RectTransform)badges.Find("Rarity_C");
   var role=(RectTransform)badges.Find("CharacterRoleIcon");
   AssertSmoke(Mathf.Abs(role.rect.height-rarity.rect.height)<.01f&&
    Mathf.Abs(role.anchoredPosition.y-rarity.anchoredPosition.y)<.01f&&
    Mathf.Abs(role.anchoredPosition.x-rarity.anchoredPosition.x-rarity.rect.width-3)<.01f,
    "Formation badges must share visible height and have a three-pixel gap");
   NavigateGallery("主页");
  }

  void VerifyYamchaPresentation(){
   var id=RoundMotionArt.YamchaCardId;
   string[] Slots(params string[] ids)=>ids.Concat(Enumerable.Repeat("",9-ids.Length)).ToArray();
   void Stage(string[] allies,string[] enemies){
    BeginRoundDemo(new RoundDemoSetup{allies=allies,enemies=enemies});roundPaused=true;
    while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
    foreach(var unit in roundBattle.Order)unit.Dodge=0;
   }
   Stage(Slots(id),Slots(RoundMotionArt.CardId,RoundMotionArt.VegetaCardId));
   var yamcha=roundBattle.Order[0];var near=roundBattle.Order[1];var far=roundBattle.Order[2];
   far.Defense=0;near.Defense=90;
   AssertSmoke(YamchaArt.Atlas.Length==16&&roundCueClips.Count(c=>c.Key.StartsWith("yamcha-")&&c.Value.Clip!=null)==8&&
    PortraitTexture(Data.Index(id))==Resources.Load<Texture2D>("Art/V0172/Yamcha/portrait"),"Yamcha art or local cues are missing");
   AdvanceRoundPreview(true);
   var basic=roundMotion;
   var landed=VisibleRoundVitals(near).HP;
   AdvanceToMotionTime(basic.Clip.impacts[1].at+8);
   AssertSmoke(basic.Clip.id=="yamchaBasic"&&basic.Clip.impacts.Length==3&&!basic.Revealed&&VisibleRoundVitals(near).HP==landed&&
    roundSkillArt.texture.name=="icons-b","Yamcha basic revealed damage before the double strike or lost the B icon");
   AdvanceToMotionTime(basic.ImpactAt+6);
   AssertSmoke(basic.Revealed&&basic.StagePose==YamchaArt.Atlas[2]&&VisibleRoundVitals(near).HP<landed,"Third strike did not reveal the hit");
   AdvanceRoundPreview(true);yamcha.Rage=100;AdvanceRoundPreview(true);
   var wolf=roundMotion;
   AdvanceToMotionTime(wolf.ImpactAt+24);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(roundLast.Target==far&&wolf.Clip.id=="yamchaWolf"&&wolf.Travel.x>0&&wolf.Revealed&&demoFrontFx.GeometryCount>0&&
    roundSkillArt.texture.name=="icons-a","Wolf Fang motion, lowest-defense target or final impact effect failed");

   Stage(Slots(RoundMotionArt.CardId,id),Slots(RoundMotionArt.VegetaCardId));
   roundBattle.Order[0].Crit=0;
   AdvanceRoundPreview(true);
   AdvanceToMotionTime(roundMotion.ImpactAt+10);
   // Goku can take the first knockdown pursuit. Force Yamcha's authored action in an independent core fixture.
   var shown=roundBattle.Order.Single(f=>f.CardId==id);
   StartRoundActionMotion(new RoundAction{Actor=shown,Target=roundBattle.Order[1],Move=RoundMove.Pursuit,
    Name="下段踢",TargetState=RoundHitState.Knockdown},true);
   AssertSmoke(roundMotion.Clip.id=="yamchaSweep"&&roundMotion.Clip.impacts[0].drawing==5&&roundMotion.Travel.x>0,
    "Yamcha pursuit did not bind the low sweep");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();

   Stage(Slots(RoundMotionArt.VegetaCardId),Slots(id,RoundMotionArt.CardId));
   var hitter=roundBattle.Order[0];yamcha=roundBattle.Order[1];var ally=roundBattle.Order[2];
   hitter.Attack=20000;yamcha.HP=1;
   AdvanceRoundPreview(true);
   AdvanceToMotionTime(roundMotion.ImpactAt+15);
   AssertSmoke(roundBattle.ReactionSkillNext&&VisibleRoundVitals(yamcha).HP==0&&demoReactions[yamcha].Track=="defeat","Yamcha did not play the fall before revival");
   AdvanceRoundPreview(true);AdvanceRoundPreview(true);
   var rise=roundMotion;
   AdvanceToMotionTime(480);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(rise.Clip.id=="yamchaRise"&&VisibleRoundVitals(yamcha).HP==0&&demoRearFx.GeometryCount>0&&roundSkillText.text=="装死",
    "Revival windup or skill badge failed");
   AdvanceToMotionTime(rise.ImpactAt+18);
   AssertSmoke(rise.Revealed&&Math.Abs(VisibleRoundVitals(yamcha).HP/yamcha.MaxHP-.2)<.001&&
    demoReactions[yamcha].Pose==null&&ally.AttackChanges.Any(change=>change.Source==rise.Action.Ability)&&
    demoStage.Find("DemoSlot"+ally.Side+"_"+ally.Position+"/Buffs/TeamAttack")!=null,
    "Rise did not resolve HP, clear defeat reaction, or show the allied buff");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyChaseReactionHolds(){
   foreach(var id in Data.RoundRoster){
    var library=BattleReactionLibrary(id);
    var idle=BattleMotionLibrary(id).At(BattleMotionLibrary(id)["idle"],0);
    foreach(string track in new[]{"smallFloat","largeFloat","knockback","knockdown"}){
     var clip=library[track];
     bool floats=track=="smallFloat"||track=="largeFloat";
     string atlas=floats?"FloatPoses":"ReactionPoses";
     AssertSmoke(clip.poses.Length>=2&&clip.poses.All(cue=>library.Atlas[cue.drawing].Texture.name==atlas),
      "Missing the approved first and held reaction drawings for "+id+" "+track);
     var reaction=new DuelReaction(library);
     reaction.Hit(track,1,true,idle);
     if(floats){
      reaction.Advance(18);
      AssertSmoke(reaction.Pose==idle&&reaction.Offset==Vector2.zero,"A fighter started to rise before taking the hit");
      reaction.Advance(45);
      AssertSmoke(reaction.Pose.Texture.name==atlas&&reaction.Offset.y<0,"Float launch did not start on the hit drawing");
     }
     reaction.Advance(2000);
     var pose=reaction.Pose;
     var offset=reaction.Offset;
     AssertSmoke(reaction.Active&&pose!=null&&pose!=idle,"Queued chase lost "+id+" "+track);
     var fixedSpot=track=="smallFloat"?new Vector2(0,-25):track=="largeFloat"?new Vector2(0,-51):
      track=="knockback"?new Vector2(60,0):Vector2.zero;
     AssertSmoke(offset==fixedSpot,"Held reaction is not at its required reference position");
     if(track=="knockback"){
      reaction.Hit("knockdown",1,true,idle);
      reaction.Advance(2000);
      AssertSmoke(reaction.Offset==fixedSpot,"Knockdown moved back to the initial tile");
      pose=reaction.Pose;offset=reaction.Offset;
     }
     reaction.Finish();
     AssertSmoke(reaction.Pose==pose&&reaction.Offset==offset,"Manual step reset a queued chase");
     reaction.Hit("knockback",1,false,idle);
     AssertSmoke(reaction.Pose==pose&&reaction.Offset==offset,"The next reaction did not bridge from the held pose and location");
     reaction.Advance(2000);
     AssertSmoke(!reaction.Active&&reaction.Offset==Vector2.zero,"Completed chase did not return to rest");
     reaction.Hit(track,1,true,idle);reaction.Advance(2000);offset=reaction.Offset;
     reaction.ReleaseChain();
     AssertSmoke(reaction.Offset==offset,"Releasing a broken chase moved the actor in one frame");
     reaction.Advance(2000);
     AssertSmoke(!reaction.Active&&reaction.Pose==null&&reaction.Offset==Vector2.zero,"Broken chase did not settle");
    }
   }
   var raditz=new RoundFighter(RoundMotionArt.RaditzCardId,0,5,1200,100,40,0);
   AssertSmoke(Mathf.Abs(DemoScale(raditz)-.18f*1.09f)<.0001f,"Raditz model scale did not increase consistently");
  }

  void VerifyQuickPursuitHandoff(){
   NavigateGallery("主页");
   BeginRoundDemo();
   roundBattle=MakeGokuMirrorFixture(2);
   StartRoundPresentation();
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   roundBattle.Order[1].Dodge=0;
   AdvanceRoundPreview(false);
   AssertSmoke(roundMotion.Clip.id=="sweep"&&roundBattle.PursuitNext,"Expected the forced knockdown pursuit");
   var first=roundLast;
   AdvanceToMotionTime(roundMotion.Duration+1);
   AssertSmoke(roundMotion==null&&roundLast==first&&Mathf.Abs(roundClock-.20f)<.001f,
    "Follow-up did not use its more readable handoff");
   UpdateRoundPreview(.19f);
   AssertSmoke(roundMotion==null&&roundLast==first,"Follow-up skipped its handoff");
   UpdateRoundPreview(.02f);
   AssertSmoke(roundLast.Move==RoundMove.Pursuit&&roundMotion!=null&&roundMotion.Duration==505,
    "Follow-up did not launch within its short window");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyBrokenPursuitRecovery(){
   for(int scenario=0;scenario<2;scenario++){
    NavigateGallery("主页");BeginRoundDemo();
    roundBattle=MakeGokuMirrorFixture(2);
    StartRoundPresentation();
    while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
    var striker=roundBattle.Order[0];var defender=roundBattle.Order[1];
    defender.Dodge=0;
    if(scenario==1){striker.PursuedRound=roundBattle.Round;striker.PursuitCount=2;}
    AdvanceRoundPreview(true);
    AdvanceToMotionTime(roundMotion.ImpactAt+16);
    AssertSmoke(roundLast.TargetState==RoundHitState.Knockdown&&demoReactions[defender].Track=="knockdown",
     "Recovery fixture did not produce a knockdown");
    AdvanceRoundPreview(true);
    if(scenario==0)defender.Dodge=1;else defender.Rage=100;
    AdvanceRoundPreview(true);
    AssertSmoke(scenario==0?roundLast.Move==RoundMove.Pursuit&&roundLast.Dodged&&defender.HitState==RoundHitState.None:
     roundLast.Move==RoundMove.Ultimate&&roundLast.Actor==defender&&!roundBattle.PursuitNext,
     "Recovery fixture did not enter the interrupted sequence");
    AdvanceRoundPreview(true);
    AssertSmoke(demoReactions[defender].Pose==null&&demoReactions[defender].Offset==Vector2.zero,
     "Interrupted pursuit left the receiver in the impact pose");
   }
   NavigateGallery("主页");
  }

  void VerifyCharacterPageSession(){
   AssertSmoke(catalog.Sort==DefaultCharacterSort&&renderedCards.SequenceEqual(Data.RoundRoster.Select(Data.Index)),"A fresh game must start with number sort");
   filterButtons[2].onClick.Invoke();
   filterDropdown.GetComponentsInChildren<UI.Button>().Single(button=>button.name=="FilterOption1").onClick.Invoke();
   OpenGalleryCard(Data.Index(RoundMotionArt.PiccoloCardId));
   NavigateGallery("主页");
   GalleryTestButton("HomeNav角色").onClick.Invoke();
   AssertSmoke(!galleryDetailOpen&&galleryListRoot.gameObject.activeSelf&&catalog.Sort==1&&detailTab==0,
    "Home entry must show the list while preserving the session sort");
   OpenGalleryCard(Data.Index(RoundMotionArt.VegetaCardId));
   GalleryTestButton("Nav"+FormationRoute).onClick.Invoke();
   AssertSmoke(page==FormationRoute&&formationRoot.gameObject.activeInHierarchy&&catalog.Sort==1,
    "Formation must be available directly below the character entry");
   NavigateGallery("主页");
   GalleryTestButton("HomeNav角色").onClick.Invoke();
   GalleryTestButton("ResetFilters").onClick.Invoke();
   AssertSmoke(catalog.Sort==1,"Clearing filters discarded the chosen session sort");
   BeginRoundDemo();
   GalleryTestButton("Nav角色").onClick.Invoke();
   AssertSmoke(page=="角色"&&!galleryDetailOpen&&catalog.Sort==1&&roundBattle==null,
    "Battle entry did not reopen the character list or retain the session sort");
   filterButtons[2].onClick.Invoke();
   filterDropdown.GetComponentsInChildren<UI.Button>().Single(button=>button.name=="FilterOption4").onClick.Invoke();
  }

  void VerifyAvatarSwipe(){
   var order=renderedCards.ToArray();
   int first=order[0];
   var swipe=detailRoot.GetComponentInChildren<GalleryAvatarSwipe>();
   var strip=(RectTransform)swipe.transform;
   float rest=strip.rect.width*.5f-GalleryAvatarSwipe.CenterSize*.5f;
   AssertSmoke(selectedCard==first&&strip.Find("NextCard2")!=null&&strip.Find("NextCard3")==null,
    "Avatar strip at rest did not preserve the two neighboring previews");
   Vector2 ScreenPoint(Vector2 delta)=>RectTransformUtility.WorldToScreenPoint(null,strip.TransformPoint(strip.rect.center+delta));
   var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){
    button=UnityEngine.EventSystems.PointerEventData.InputButton.Left
   };
   void Begin(){
    pointer.pressPosition=pointer.position=ScreenPoint(Vector2.zero);
    swipe.OnBeginDrag(pointer);
   }
   void Move(float x,float y=0){
    pointer.position=ScreenPoint(new Vector2(x,y));swipe.OnDrag(pointer);
   }
   void Release(){
    int held=selectedCard;
    swipe.OnEndDrag(pointer);swipe.AdvanceRebound(1);
    var current=(RectTransform)strip.Find("CurrentAvatar");
    AssertSmoke(held==selectedCard&&Mathf.Abs(current.anchoredPosition.x-rest)<.01f&&Mathf.Abs(current.localScale.x-1)<.001f,
     "Release changed selection or failed to settle the current avatar");
   }

   Begin();Move(140);
   var stretched=(RectTransform)strip.Find("CurrentAvatar");
   AssertSmoke(selectedCard==first&&stretched.anchoredPosition.x>rest&&stretched.anchoredPosition.x-rest<32,
    "Outward dragging crossed the first card or exceeded the elastic edge");
   Release();
   Begin();Move(-20);AssertSmoke(selectedCard==first,"The first card is still closest after a short drag");Release();
   Begin();Move(-12,78);AssertSmoke(selectedCard==first,"A vertical drag selected another card");Release();

   Begin();Move(-67);
   AssertSmoke(selectedCard==order[1]&&detailTab==1&&detailAvatarStrip==strip,
    "Center crossing must update detail while holding the same drag and tab");
   var active=(RectTransform)strip.Find("CurrentAvatar");
   var neighbor=(RectTransform)strip.Find("PreviousCard");
   AssertSmoke(active.Find("Face").GetComponent<UI.RawImage>().texture==PortraitTexture(order[1])&&
    active.localScale.x>neighbor.localScale.x&&active.GetComponent<UI.Image>().color==InkGold,
    "The center avatar did not enlarge and receive the active frame");
   Move(-177);AssertSmoke(selectedCard==order[2]&&detailAvatarStrip==strip,"A single held drag must reach the second neighbor");
   Move(-255);AssertSmoke(selectedCard==order[3]&&detailAvatarStrip==strip,"A single held drag must cross several cards");
   Move(-83);AssertSmoke(selectedCard==order[1],"Reversing the drag must track the nearest card");
   Move(-900);AssertSmoke(selectedCard==order.Last()&&detailAvatarStrip==strip,"Dragging beyond the last card must stop at it");
   Release();
   Begin();Move(900);AssertSmoke(selectedCard==first&&detailAvatarStrip==strip,"One drag should traverse all cards in the other direction");Release();

   GalleryTestButton("NextCard2").onClick.Invoke();
   AssertSmoke(selectedCard==order[2]&&detailRoot.Find("CharacterSwitch/PreviousCard2")!=null,
    "Clicking a visible neighbor must still select its card");
   SwitchDetailCard(first);
  }

  void VerifyKrillinPresentation(){
   NavigateGallery("主页");
   BeginRoundDemo(FiveCardDemoFixture());
   AssertSmoke(roundBattle.Order.Count(f=>f.CardId==RoundMotionArt.KrillinCardId)==2&&
    KrillinArt.Atlas.Length==18&&KrillinArt.Atlas[16].crop.SequenceEqual(new[]{505,534,350,345})&&
    KrillinArt.Atlas[17].crop.SequenceEqual(new[]{839,796,670,180})&&GokuArt["basic"].runFrames.Distinct().Count()>=2&&VegetaArt["basic"].runFrames.Length==4,
    "Krillin, Goku or Vegeta movement art was not loaded");
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   var krillin=roundBattle.Order.First(f=>f.Side==0&&f.CardId==RoundMotionArt.KrillinCardId);
   var slot=demoStage.Find("DemoSlot0_"+krillin.Position);
   var passive=slot.Find("Buffs/TeamAttack").GetComponent<UI.RawImage>();
   AssertSmoke(passive.texture.name=="skill-icons"&&passive.texture==
    Resources.Load<Texture2D>("Art/V0154/Krillin/skill-icons"),"Krillin aura did not bind its authored skill icon");
   AssertSmoke(roundCueClips.Where(pair=>pair.Key.StartsWith("krillin-")).Count()==14&&
    roundCueClips.Where(pair=>pair.Key.StartsWith("krillin-")).All(pair=>pair.Value.Clip.samples>0),
    "Krillin's audio bank is incomplete");
   var vegeta=roundBattle.Order.First(f=>f.Side==0&&f.CardId==RoundMotionArt.VegetaCardId);
   var front=new[]{RoundMotionArt.CardId,RoundMotionArt.GohanCardId,RoundMotionArt.PiccoloCardId}
    .Select((card,index)=>new RoundFighter(card,1,index+1,2400,100,40,0){Dodge=0});
   roundBattle=RoundCombat.Demo(new[]{krillin,vegeta}.Concat(front),RoundRules(),10);
   StartRoundPresentation();roundPaused=true;
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   foreach(var fighter in roundBattle.Order)fighter.Dodge=0;
   krillin.Rage=100;
   AdvanceRoundPreview(true);
   var disc=roundMotion;
   AssertSmoke(disc.Clip.id=="krillinDisc"&&disc.Action.Hits.Count()==3&&disc.Action.Hits.All(hit=>hit.DefenseDelta<0),
    "Disc did not use the dedicated animation and three front-column targets");
   AdvanceToMotionTime(disc.ReleaseAt-60);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.GeometryCount>120&&disc.RevealedCount==0,"Disc charge was missing or revealed damage early");
   AdvanceToMotionTime(disc.ImpactAt+16);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(disc.RevealedCount==3&&demoFrontFx.GeometryCount>120,
    "Disc did not reveal casts and actual column hits together");
   AdvanceRoundPreview(true);
   vegeta.Rage=100;
   AdvanceRoundPreview(true);
   AdvanceRoundPreview(true);
   AdvanceRoundPreview(true);
   var scatter=roundMotion;
   AssertSmoke(scatter.IsScatter&&scatter.VolleyHits.Length==10&&scatter.Travel.x<0,
    "Scatter did not follow five allied hits or retreat");
   AdvanceToMotionTime(scatter.VolleyLaunch(0)+45);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.VisibleVolleyOrbs==1&&demoFrontFx.GeometryCount>0&&scatter.RevealedCount==0,
    "Scatter did not draw its first launched projectile");
   AdvanceToMotionTime(scatter.VolleyImpact(9)+8);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.VisibleVolleyBursts>0&&scatter.RevealedCount==10&&demoStage.Find("DemoSlot0_"+krillin.Position+"/Buffs/DodgeUp")!=null,
    "Scatter did not reveal all hits and the dodge status");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyCharacterDetailsV162(){
   NavigateGallery("角色");
   int raditz=Data.Index(RoundMotionArt.RaditzCardId);
   AssertSmoke(profile.Owned[raditz],"Raditz is unavailable from the default roster");
   OpenGalleryCard(raditz);
   AssertSmoke(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==null&&
    !GalleryTestButton("BackToCards").GetComponent<UI.Outline>().enabled,"Back was focused on detail entry");
   detailTab=0;DrawGalleryDetail();Canvas.ForceUpdateCanvases();
   var cardTile=cardButtons[raditz].transform;
   var tileRadar=(RectTransform)cardTile.Find("CardPotentialRadar");
   AssertSmoke(tileRadar!=null&&((RectTransform)cardTile.Find("CharacterPower")).anchoredPosition.y<tileRadar.anchoredPosition.y,
    "Catalog tile should place its potential chart above combat power");
   var labels=detailBody.GetComponentsInChildren<UI.Text>().Where(t=>t.name=="StatValue").ToArray();
   var stats=RoundCardPreview(raditz);
   var values=new[]{stats.MaxHP,stats.Attack,stats.Defense,stats.Crit*100,stats.Tenacity*100,stats.Penetration*100,
    stats.Dodge*100,stats.Accuracy*100,stats.Qi*100};
   var chart=detailRoot.Find("PotentialChart");
   var star=(RectTransform)GalleryTestButton("Favorite_"+RoundMotionArt.RaditzCardId).transform;
   AssertSmoke(chart!=null&&chart.Find("PotentialRadar").GetComponent<GalleryPotentialGraphic>()!=null&&
    detailRoot.Find("DetailPower").GetComponent<RectTransform>().anchoredPosition.y<((RectTransform)chart).anchoredPosition.y&&
    star.anchoredPosition.x<250&&labels.Select(t=>t.text).SequenceEqual(values.Select(value=>Math.Floor(value+1e-9).ToString("0"))),
    "Detail chart, favorite placement or numeric formatting failed");
   var detailRadar=chart.Find("PotentialRadar").GetComponent<GalleryPotentialGraphic>();
   var compactRadar=tileRadar.GetComponent<GalleryPotentialGraphic>();
   AssertSmoke(detailRadar.EdgeWidth==1.7f&&compactRadar.EdgeWidth<detailRadar.EdgeWidth&&
    compactRadar.EdgeColor==detailRadar.EdgeColor&&ThemeLuminance(GalleryPotentialGraphic.GridColor)>ThemeLuminance(detailRadar.EdgeColor),
    "Rarity colors or detail and compact radar stroke widths failed");
   var rarity=Data.Cards[raditz].Rarity;
   var backdrop=RarityBackdrop(rarity);
   AssertSmoke(RarityBadge("C")==Resources.Load<Sprite>("Interface/Rarity/V0170/C")&&
    detailRoot.Find("PotentialChart/AttackPotential").GetComponent<UI.Text>().text=="攻击"&&
    detailRoot.Find("PotentialChart/DefensePotential").GetComponent<UI.Text>().text=="防御"&&
    detailRoot.Find("PotentialChart/HpPotential").GetComponent<UI.Text>().text=="生命"&&
    Data.Rarities.Select(rank=>RarityBackdrop(rank)).Distinct().Count()==4&&
    Data.Rarities.All(rank=>RarityBackdrop(rank)!=null)&&
    cardTile.Find("PortraitMat/DragonBallWatermarks").GetComponent<GalleryDragonBallWatermarks>().SmallBallCount==7&&
    cardTile.Find("PortraitMat/RarityBackdrop").GetComponent<UI.RawImage>().texture==backdrop&&
    detailRoot.Find("PortraitMat/RarityBackdrop").GetComponent<UI.RawImage>().texture==backdrop&&
    detailRoot.Find("CharacterSwitch/CurrentAvatar/AvatarMat/RarityBackdrop").GetComponent<UI.RawImage>().texture==backdrop,
    "Rarity textures were not bound to card, portrait and current avatar");
   var info=GalleryTestButton("CombatRulesInfo");
   var glyph=info.transform.Find("InfoIcon").GetComponent<UI.Image>();
   AssertSmoke(glyph.sprite!=null&&glyph.sprite.texture.width==64&&info.targetGraphic==glyph&&info.GetComponent<UI.Image>().color.a==0&&info.colors.normalColor==Color.black&&info.colors.highlightedColor!=info.colors.pressedColor&&info.GetComponentInChildren<UI.Text>()==null&&
    !detailBody.GetComponentsInChildren<UI.Button>().Any(button=>button.name.StartsWith("StatInfo")),
    "The standalone information glyph must be the only attribute explanation entry");
   var events=UnityEngine.EventSystems.EventSystem.current;
   var pointer=new UnityEngine.EventSystems.PointerEventData(events);
   var colors=info.colors;colors.fadeDuration=0;info.colors=colors;
   info.OnPointerEnter(pointer);
   AssertSmoke(glyph.canvasRenderer.GetColor()==colors.highlightedColor,"Info hover has no feedback");
   info.OnPointerDown(pointer);
   AssertSmoke(glyph.canvasRenderer.GetColor()==colors.pressedColor,"Info press has no feedback");
   info.OnPointerUp(pointer);info.OnPointerExit(pointer);
   info.onClick.Invoke();
   Canvas.ForceUpdateCanvases();
   var modal=dialogRoot.Find("Modal");
   var close=modal.Find("Close");
   AssertSmoke(modal.Find("Rule")==null&&modal.GetComponent<UI.Image>().sprite.border!=Vector4.zero&&
    close.GetComponent<UI.Image>().sprite.border!=Vector4.zero&&
    ((RectTransform)close.Find("CloseGlyph")).anchoredPosition==Vector2.zero&&events.currentSelectedGameObject==null,
    "Info dialog top rule, rounding, centered close or initial focus failed");
   var scroll=dialogRoot.Find("Modal/CombatRulesScroll").GetComponent<UI.ScrollRect>();
   var rows=scroll.content.Cast<Transform>().Where(child=>child.name.StartsWith("StatGuideRow")).ToArray();
   AssertSmoke(scroll.vertical&&scroll.content.rect.height>scroll.viewport.rect.height&&scroll.verticalScrollbar!=null&&
    rows.Select(row=>row.Find("StatName").GetComponent<UI.Text>().text).SequenceEqual(GalleryStatGuide.Select(stat=>stat.Name))&&
    rows.Select(row=>row.Find("StatDescription").GetComponent<UI.Text>().text).SequenceEqual(GalleryStatGuide.Select(stat=>stat.Description))&&
    scroll.content.Find("CombatRulesHeading")!=null&&scroll.content.Find("RuleBody5")!=null&&!info.interactable,
    "The combined information dialog lost attribute rows, combat rules or modal isolation");
   CloseGalleryDialog();
   detailTab=1;DrawGalleryDetail();
   var icons=Resources.Load<Texture2D>("Art/V0161/Raditz/skill-icons");
   var friday=Resources.Load<Texture2D>("Art/V0162/Raditz/shining-friday");
   AssertSmoke(PortraitTexture(raditz)==Resources.Load<Texture2D>("Art/V0161/Raditz/portrait")&&icons!=null&&friday!=null&&
    detailRoot.GetComponentsInChildren<UI.RawImage>().Count(image=>image.texture==icons)==3&&
    detailBody.GetComponentsInChildren<UI.RawImage>().Any(image=>image.texture==friday&&image.uvRect==new Rect(0,0,1,1)),
    "Raditz's new basic icon or the three original skill icons did not load");
   ReturnToCards();
  }

  void VerifyRaditzPresentation(){
   const string id=RoundMotionArt.RaditzCardId;
   BeginRoundDemo(new RoundDemoSetup{allies=new[]{id,"","","","","","","",""},
    enemies=new[]{RoundMotionArt.CardId,RoundMotionArt.VegetaCardId,"","","","","","",""}});
   roundPaused=true;
   var raditz=roundBattle.Order[0];
   AssertSmoke(raditz.CardId==id&&RaditzArt.Atlas.Length==18&&
    roundCueClips.Count(pair=>pair.Key.StartsWith("raditz-"))==13&&
    GokuArt["basic"].runFrames.All(frame=>GokuArt.Atlas[frame].Texture.name=="CombatPoses")&&
     GokuArt["basic"].returnHop==0,"Raditz cue/pose library or grounded Goku movement is missing");
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   foreach(var fighter in roundBattle.Order)fighter.Dodge=0;
   raditz.Rage=100;
   AdvanceRoundPreview(true);
   var twin=roundMotion;
   AssertSmoke(twin.Clip.id=="raditzTwin"&&twin.VolleyHits.Length==2&&
    twin.VolleyHits[0].Target!=twin.VolleyHits[1].Target,"Raditz ultimate did not anchor two distinct shots");
   AdvanceToMotionTime(twin.VolleyLaunch(1)+20);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.VisibleVolleyOrbs==2&&demoFrontFx.GeometryCount>115&&twin.RevealedCount==0,"Raditz's paired projectiles did not launch before damage");
   AdvanceToMotionTime(twin.VolleyImpact(1)+10);
   AssertSmoke(twin.RevealedCount==2,"Raditz's paired hits failed to resolve");
   AdvanceRoundPreview(true);
   foreach(bool enhanced in new[]{false,true}){
    StartRoundActionMotion(new RoundAction{Actor=raditz,Target=twin.VolleyHits[0].Target,Name="闪耀金星日",
     Move=RoundMove.Basic,EnhancedBasic=enhanced},true);
    CreateRoundSkillBadge(demoViewport,roundMotion);
    AssertSmoke(roundSkillArt.texture==Resources.Load<Texture2D>("Art/V0162/Raditz/shining-friday")&&roundMotion.Clip.id==(enhanced?"raditzSurge":"raditzShot")&&
     roundMotion.Clip.duration==(enhanced?970:640),"Enhanced basic did not play the longer authored variant");
   }
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyRaditzSingleTargetRays(){
   BeginRoundDemo(new RoundDemoSetup{
    allies=new[]{RoundMotionArt.CardId,"","","","","","","",""},
    enemies=new[]{RoundMotionArt.RaditzCardId,"","","","","","","",""}
   });
   roundPaused=true;
   while(roundMotion!=null||roundOpenings.Count>0)AdvanceRoundPreview(true);
   var raditz=roundBattle.Order[1];
   raditz.Rage=100;roundBattle.Order[0].Dodge=0;
   AdvanceRoundPreview(true);
   var twin=roundMotion;
   AssertSmoke(twin.IsRaditzTwin&&twin.Actor.Side==1&&twin.VolleyHits.Length==1,"Single-target twin should still settle damage once");
   AdvanceToMotionTime(twin.VolleyLaunch(1)+20);
   Canvas.ForceUpdateCanvases();
   AssertSmoke(demoFrontFx.VisibleVolleyOrbs==2&&demoFrontFx.GeometryCount>115&&twin.RevealedCount==0,
    "The right-side single-target cast lost either of its two rays");
   AdvanceToMotionTime(twin.ImpactAt+10);
   AssertSmoke(twin.RevealedCount==1,"A presentation-only second ray repeated damage");
   GalleryTestButton("LeaveRoundPreview").onClick.Invoke();
  }

  void VerifyFavorites(){
   int krillin=Data.Index(RoundMotionArt.KrillinCardId);
   var star=cardButtons[krillin].transform.Find("Favorite_"+RoundMotionArt.KrillinCardId).GetComponent<UI.Button>();
   AssertSmoke(star.GetComponentInChildren<UI.Text>().text=="☆","Unmarked star must be outlined");
   star.onClick.Invoke();
   AssertSmoke(profile.IsFavoriteCard(krillin)&&renderedCards[0]==krillin&&
    cardButtons[krillin].transform.parent.name=="FavoriteGroup"&&
    cardButtons[krillin].transform.Find("CharacterPower").GetComponent<UI.Text>().text.Contains("#27683F"),
    "Favorites did not pin ahead of the roster or turn power green");
   var saved=JsonUtility.FromJson<Profile>(JsonUtility.ToJson(profile));
   AssertSmoke(saved.IsFavoriteCard(krillin),"Favorite did not survive save serialization");
   OpenGalleryCard(krillin);
   star=GalleryTestButton("Favorite_"+RoundMotionArt.KrillinCardId);
   AssertSmoke(star.GetComponentInChildren<UI.Text>().text=="★","Detail did not reflect the filled star");
   star.onClick.Invoke();
   AssertSmoke(!profile.IsFavoriteCard(krillin)&&selectedCard==krillin&&galleryDetailOpen&&renderedCards.Contains(krillin)&&renderedCards[0]!=krillin,
    "Removing a favorite from detail did not update the gallery and keep the selected card");
   ReturnToCards();
  }

  void VerifyStatusExpiry(){
   var goku=new RoundFighter(RoundMotionArt.CardId,0,2,2400,100,40,0){Dodge=0};
   var gohan=new RoundFighter(RoundMotionArt.GohanCardId,1,2,2400,100,40,0){Dodge=0};
   roundBattle=RoundCombat.Demo(new[]{goku,gohan},RoundRules(),1);
   int round=roundBattle.Round;
   var before=new RoundVitals(gohan,freezeDefense:true);
   gohan.ChangeDefense(.20,round,1);
   gohan.ChangeDefense(.10,round,2);
   gohan.ChangeDefense(-.20,round,1);
   var live=new RoundVitals(gohan);
   AssertSmoke(!DisplayRoundStatuses(gohan,before).Any()&&
    DisplayRoundStatuses(gohan,live).Select(status=>status.Icon).SequenceEqual(
     new[]{RoundStatusIcon.DefenseUp,RoundStatusIcon.DefenseDown}),
    "Defense tiles are not separated by direction or leaked into the previous snapshot");
   while(roundBattle.Round==round)roundBattle.Step();
   var remaining=DisplayRoundStatuses(gohan,live).Single();
   AssertSmoke(remaining.Icon==RoundStatusIcon.DefenseUp&&remaining.Title.Contains("<color=#27683F>10</color>")&&!remaining.Description.Contains("%")&&!remaining.Description.Contains("·"),
    "A contribution did not expire independently at the round boundary");
   while(roundBattle.Round==round+1)roundBattle.Step();
   AssertSmoke(!DisplayRoundStatuses(gohan,live).Any(),"Expired defense status continued to display");
   roundBattle=null;
  }

  void VerifySkillDescriptionLayout(){
   var kit=RoundRules().For(RoundMotionArt.CardId);
   var saved=kit.techniques.basicText;
   try{
    foreach(int lines in new[]{1,2,4,6}){
     kit.techniques.basicText=string.Join("\n",Enumerable.Range(1,lines).Select(n=>"第"+n+"行验证"));
     DrawGalleryDetail();
     var row=(RectTransform)detailBody.GetChild(detailBody.childCount-4);
     var name=(RectTransform)row.Find("SectionTitle");
     var box=(RectTransform)row.Find("SkillDescription");
     var icon=(RectTransform)row.Find("SkillIconSlot/RoundSkillIcon");
     var scroll=box.GetComponentInChildren<BoundedScrollRect>();
     var label=scroll.GetComponentInChildren<UI.Text>();
     var measurement=MeasureSkillText(label);
     float line=measurement.First;
     AssertSmoke(measurement.Count==lines&&label.verticalOverflow==VerticalWrapMode.Overflow,
      "Skill layout measurement or clipping mode did not match the generated lines");
     float expectedY=lines==1?-name.anchoredPosition.y:(row.rect.height-icon.rect.height)*.5f;
     AssertSmoke(icon.rect.width==icon.rect.height&&icon.rect.height>50&&
      Mathf.Abs(-((RectTransform)icon.parent).anchoredPosition.y-expectedY)<.1f&&
      Mathf.Abs(scroll.viewport.rect.height-measurement.Preview)<.1f&&
      (lines>4)==(scroll.content.rect.height>scroll.viewport.rect.height+.1f)&&
      scroll.viewport.GetComponent<UI.RectMask2D>()!=null,
      "Skill layout failed for "+lines+" rendered lines: full "+scroll.content.rect.height+", visible "+scroll.viewport.rect.height);
     if(lines==1)AssertSmoke(Mathf.Abs(expectedY+icon.rect.height-(-box.anchoredPosition.y-scroll.viewport.anchoredPosition.y+line))<.1f,
      "Single line icon must align with the title and first description line");
     if(lines>4){
      scroll.verticalNormalizedPosition=0;
      AssertSmoke(scroll.content.anchoredPosition.y>0,"Long skill text could not be scrolled to its final lines");
     }
    }
   }finally{
    kit.techniques.basicText=saved;
    DrawGalleryDetail();
   }
   var order=renderedCards.ToArray();
   foreach(int index in order){
    SwitchDetailCard(index);
    var icons=detailBody.GetComponentsInChildren<UI.RawImage>();
    int skillCount=RoundRules().For(Data.Cards[index].Id).skills.Count(s=>s.trigger!="reserved");
    AssertSmoke(icons.Length==3+skillCount&&icons.All(icon=>icon.rectTransform.rect.width==icon.rectTransform.rect.height&&
     Mathf.Abs(icon.texture.width*icon.uvRect.width-icon.texture.height*icon.uvRect.height)<.02f),
     "Online skill icons must use square source regions and on-screen slots");
   }
   SwitchDetailCard(Data.Index(RoundMotionArt.CardId));
  }

  IEnumerator RunRoundSmoke(string[] args){
   int o=Array.IndexOf(args,"-smokeOut");string dir=o>=0&&o+1<args.Length?args[o+1]:Path.GetDirectoryName(savePath);
   Directory.CreateDirectory(dir);
   BeginShellLoad("主页");
   float deadline=Time.realtimeSinceStartup+20;
   while(shellLoad.Active&&Time.realtimeSinceStartup<deadline)yield return null;
   try{
    AssertSmoke(args.Contains("-savePath")&&saveError==""&&Application.productName==Data.GameTitle&&
     Application.version==Data.Version,"Packaged title/version/save initialization");
    AssertSmoke(page=="主页"&&!shellLoad.Active&&shellLoad.Missing==0&&shellLoad.Completed==Data.RoundRoster.Count+Data.Rarities.Length,"Startup art resources not loaded");
    AssertSmoke(homeRoot.GetComponentsInChildren<UI.Button>().Count(b=>b.name.StartsWith("HomeNav"))==3&&FeatureRoutes.SequenceEqual(new[]{"角色",FormationRoute,DemoBattleRoute}),"Home feature routes failed");
    foreach(string route in new[]{"挑战","养成","装备","召唤"}){NavigateGallery(route);AssertSmoke(page=="主页","Obsolete route entered: "+route);}
    GalleryTestButton("HomeSettings").onClick.Invoke();
    AssertSmoke(dialogRoot!=null&&!GalleryTestButton("HomeNav角色").interactable,"Settings lost modal isolation");
    CloseGalleryDialog();
    AssertSmoke(silentAudio&&!uiSounds.isPlaying&&uiSounds.volume==0,"Smoke must run silent");
    AssertSmoke(Resources.LoadAll("Videos").Length==0,"Authoring reference video shipped in Player");
    var rules=RoundRules();var boards=RoundBoards().boards;
    var krillin=rules.For(RoundMotionArt.KrillinCardId);
    AssertSmoke(Data.Card(RoundMotionArt.PiccoloCardId).HasTag(Data.RivalTag)&&
     Math.Abs(rules.For(RoundMotionArt.VegetaCardId).ultimatePower-.35)<.001&&
     Math.Abs(krillin.pursuitPower-.10)<.001&&krillin.techniques.ultimateDefenseRounds==1&&krillin.techniques.pursuitDodgeRounds==1,
     "Workbook values did not reach actual character and combat configuration");
    AssertSmoke(boards.Select(b=>b.positions.Count(id=>!string.IsNullOrEmpty(id))).SequenceEqual(new[]{5,7,9}),"Board assets missing difficulty sizes");
    AssertSmoke(rules.cards.Length==Data.RoundRoster.Count&&Data.Rarities.All(rank=>RarityBadge(rank)!=null),"Card configuration or badges missing");
    AssertSmoke(Data.RoundRoster.Select(Data.Index).All(i=>PortraitTexture(i)!=null),"Portrait loading regressions");
    GalleryTestButton("HomeNav角色").onClick.Invoke();
    AssertSmoke(page=="角色"&&cardButtons.Count==Data.RoundRoster.Count&&galleryResources.text=="拥有"+RoundOwnedCount+"/"+Data.RoundRoster.Count&&
     GalleryTestButton("Nav角色").GetComponentInChildren<UI.Text>().text=="角色",
     "Online character page or its header is inaccurate");
    VerifyCharacterPageSession();
    VerifyFavorites();
    int unowned=Data.Index(RoundMotionArt.PiccoloCardId);
    var savedGrid=profile.RoundFormationIds;
    int slot=Array.IndexOf(profile.RoundFormation(),unowned)+1;
    if(slot>0)AssertSmoke(profile.RemoveRoundMember(slot),"Unable to prepare the unowned gallery fixture");
    profile.Owned[unowned]=false;
    filterButtons[2].onClick.Invoke();
    AssertSmoke(filterDropdown!=null,"Missing gallery sort dropdown");
    filterDropdown.GetComponentsInChildren<UI.Button>().Single(b=>b.name=="FilterOption4").onClick.Invoke();
    AssertSmoke(catalog.Sort==4&&renderedCards.SequenceEqual(Data.RoundRoster.Select(Data.Index).Where(i=>i!=unowned).Concat(new[]{unowned})),
     "Catalog number sort did not apply");
    AssertSmoke(cardButtons[unowned].transform.parent.name=="UnownedGroup"&&cardButtons[0].transform.parent.name=="OwnedGroup"&&
     cardButtons[unowned].transform.Find("Portrait").GetComponent<UI.RawImage>().material.shader.name=="DragonIdle/UI/PortraitGray"&&
     galleryChrome.Find("Section").GetComponent<UI.Text>().text=="角色"&&galleryResources.text=="拥有"+RoundOwnedCount+"/"+Data.RoundRoster.Count&&
     gallerySearch.textComponent.alignment==TextAnchor.MiddleCenter&&
     ((UI.Text)gallerySearch.placeholder).alignment==TextAnchor.MiddleCenter&&
     filterButtons.All(b=>b.GetComponentInChildren<UI.Text>().alignment==TextAnchor.MiddleCenter)&&
     !galleryListRoot.GetComponentsInChildren<UI.Button>().Any(b=>b.name=="OwnedTab"||b.name=="AllTab"),
     "Gallery groups, grayscale, centered labels or header did not render");
    AssertSmoke(cardButtons.Count==Data.RoundRoster.Count&&!cardButtons[unowned].transform.Find("CharacterPower").gameObject.activeSelf&&
     cardButtons.Keys.All(i=>Data.IsRoundCard(Data.Cards[i].Id))&&
     Data.Cards.Where(c=>!Data.IsRoundCard(c.Id)).All(c=>Resources.Load("Art/V051/"+c.Id+"-portrait")==null),
     "Online gallery leaked offline cards or unowned power");
    profile.Owned[unowned]=true;
    profile.RoundFormationIds=savedGrid;
    RefreshGallery(false);
    AssertSmoke(cardButtons[unowned].transform.parent.name=="OwnedGroup"&&
     cardButtons[unowned].transform.Find("Portrait").GetComponent<UI.RawImage>().material.shader.name!="DragonIdle/UI/PortraitGray",
     "Changed ownership did not repaint and regroup the same card");
    cardButtons[0].onClick.Invoke();
    AssertSmoke(galleryDetailOpen&&!galleryListRoot.gameObject.activeSelf,"Character detail was not reachable");
    AssertSmoke(detailBody.GetComponentsInChildren<UI.Text>().Any(t=>t.text=="速度"),"Accuracy stat is missing from character info");
    var skillTab=GalleryTestButton("DetailTab1");
    AssertSmoke(skillTab.GetComponentInChildren<UI.Text>().text=="技能","Skill tab label did not update");
    skillTab.onClick.Invoke();
    AssertSmoke(galleryDetailScroll is BoundedScrollRect&&galleryDetailScroll.movementType==UI.ScrollRect.MovementType.Elastic&&
     ((RectTransform)detailRoot.Find("CharacterName")).anchoredPosition.y>-30,
     "Skill scroll or upper detail layout regressed");
    var sections=detailBody.GetComponentsInChildren<UI.Text>().Where(t=>t.name=="SectionTitle").Select(t=>t.text).ToArray();
    AssertSmoke(sections.Length==4&&sections.Any(t=>t.StartsWith("追打"))&&sections.Any(t=>t.StartsWith("被动型技能")),
     "The gallery rendered a reserved skill or omitted a configured skill");
    var gokuIcons=detailBody.GetComponentsInChildren<UI.RawImage>().Where(icon=>icon.name=="RoundSkillIcon").ToArray();
    var gokuAtlas=Resources.Load<Texture2D>("Art/V0131/Goku/skill-icons");
    var uppercutIcon=Resources.Load<Texture2D>("Art/V0142/Goku/uppercut-icon");
    AssertSmoke(gokuIcons.Length==3&&gokuIcons.All(icon=>icon.isActiveAndEnabled)&&
     gokuIcons[0].texture==gokuAtlas&&gokuIcons[0].uvRect.x<.1f&&
     gokuIcons[1].texture==gokuAtlas&&gokuIcons[1].uvRect.x>.5f&&uppercutIcon!=null&&
     gokuIcons[2].texture==uppercutIcon&&gokuIcons[2].uvRect==new Rect(0,0,1,1),
     "Goku's combo, beam or pursuit icon is missing or miscropped");
    var paragraphs=detailBody.GetComponentsInChildren<UI.Text>().Where(t=>t.name=="Paragraph").ToArray();
    AssertSmoke(paragraphs.Length==4&&paragraphs.All(t=>t.supportRichText&&t.preferredHeight<=t.rectTransform.rect.height+1)&&
     paragraphs[0].text.Contains("<color=#27683F>60</color>")&&
     paragraphs[2].text.Contains("<color=#286D9A>倒地</color>")&&
     paragraphs[3].text.Contains("<color=#A14B18>劲敌</color>")&&
     FormatGallerySkillText("文字【标签】[20%]{击退} 未闭合[")==
      "文字<color=#A14B18>标签</color><color=#27683F>20%</color><color=#286D9A>击退</color> 未闭合[",
     "Three bracket colors or skill text layout failed");
    VerifySkillDescriptionLayout();
    VerifyAvatarSwipe();
    var training=detailBody.GetComponentsInChildren<UI.RawImage>().Single(i=>i.name=="RoundOpeningSkillIcon");
    AssertSmoke(training.texture==RoundMotionArt.LoadOpeningIcon()&&sections.Contains("被动型技能 · 修炼的成果"),"Gallery did not bind the new opening icon");
    var startingAvatar=(RectTransform)detailRoot.Find("CharacterSwitch/CurrentAvatar");
    var next=GalleryTestButton("NextCard");
    var nextFrame=(RectTransform)next.transform;
    AssertSmoke(!detailRoot.GetComponentsInChildren<UI.Button>().Any(button=>button.name=="PreviousCard")&&
     nextFrame.sizeDelta.x==nextFrame.sizeDelta.y&&nextFrame.localScale.x<startingAvatar.localScale.x&&
     nextFrame.anchoredPosition.x>startingAvatar.anchoredPosition.x+startingAvatar.sizeDelta.x&&
     startingAvatar.Find("Face").GetComponent<UI.RawImage>().texture==PortraitTexture(0)&&
     next.transform.Find("Face").GetComponent<UI.RawImage>().texture==PortraitTexture(renderedCards[1])&&
     next.transform.Find("Rarity_"+Data.Cards[renderedCards[1]].Rarity)?.GetComponent<UI.Image>().sprite!=null&&
     next.GetComponentInChildren<UI.Text>()==null&&detailRoot.Find("ChooseSlot")==null,"Gallery first endpoint or next portrait failed");
    next.onClick.Invoke();AssertSmoke(selectedCard==renderedCards[1],"Detail next failed");
    var previous=GalleryTestButton("PreviousCard");
    var current=(RectTransform)detailRoot.GetComponentsInChildren<UI.Image>().Single(image=>image.name=="CurrentAvatar").transform;
    AssertSmoke(current.Find("Face").GetComponent<UI.RawImage>().texture==PortraitTexture(selectedCard)&&
     previous.transform.Find("Face").GetComponent<UI.RawImage>().texture==PortraitTexture(0)&&
     previous.transform.Find("Rarity_"+Data.Cards[0].Rarity)?.GetComponent<UI.Image>().sprite!=null&&
     previous.GetComponentInChildren<UI.Text>()==null,"Detail avatars did not follow current selection");
    SwitchDetailCard(renderedCards.Last());
    AssertSmoke(!detailRoot.GetComponentsInChildren<UI.Button>().Any(button=>button.name=="NextCard")&&
     GalleryTestButton("PreviousCard").transform.Find("Face").GetComponent<UI.RawImage>().texture==PortraitTexture(renderedCards[renderedCards.Length-2]),
     "Gallery last endpoint did not hide next");
    SwitchDetailCard(Data.Index(RoundMotionArt.GohanCardId));
    var gohanAtlas=Resources.Load<Texture2D>("Art/V0151/Gohan/skill-icons");
    AssertSmoke(selectedCard==Data.Index(RoundMotionArt.GohanCardId)&&
     PortraitTexture(selectedCard)==Resources.Load<Texture2D>("Art/V0153/Gohan/portrait")&&
     GohanArt.Atlas.All(pose=>pose.Texture==Resources.Load<Texture2D>("Art/V0153/Gohan/"+pose.image))&&
     detailBody.GetComponentsInChildren<UI.RawImage>().Count(icon=>icon.texture==gohanAtlas)==4&&
     detailBody.GetComponentsInChildren<BoundedScrollRect>().Length==4,
     "Gohan's online portrait, four gallery skill tiles or nested scrolls are missing");
    var mark=(RectTransform)detailRoot.Find("Rarity_C");
    AssertSmoke(mark.rect.height==((RectTransform)detailRoot.Find("CharacterSwitch/CurrentAvatar")).rect.height&&
     mark.GetComponent<UI.Image>().preserveAspect,"Rarity badge must align with current roster avatar");
    GalleryTestButton("DetailTab0").onClick.Invoke();
    AssertSmoke(!detailBody.GetComponentsInChildren<UI.Text>().Any(text=>text.text=="回合制属性预览"),
     "Removed round attribute heading is still visible");
    GalleryTestButton("DetailTab1").onClick.Invoke();
    SwitchDetailCard(Data.Index(RoundMotionArt.PiccoloCardId));
    AssertSmoke(selectedCard==Data.Index(RoundMotionArt.PiccoloCardId)&&
     PortraitTexture(selectedCard)==Resources.Load<Texture2D>("Art/V0152/Piccolo/portrait")&&
     detailBody.GetComponentsInChildren<UI.Text>().Count(t=>t.name=="Paragraph")==4,
     "Piccolo's revised authored kit or portrait was not loaded");
    SwitchDetailCard(renderedCards[1]);
    var vegetaIcons=detailBody.GetComponentsInChildren<UI.RawImage>().Where(icon=>
     icon.name=="RoundSkillIcon"||icon.name=="RoundOpeningSkillIcon").Select(icon=>icon.texture).ToArray();
    var remainingAtlas=Resources.Load<Texture2D>("Art/V0144/Vegeta/skill-icons");
    AssertSmoke(vegetaIcons.Length==4&&vegetaIcons[0]==remainingAtlas&&vegetaIcons[2]==remainingAtlas&&
     vegetaIcons[1]==Resources.Load<Texture2D>("Art/V0153/Vegeta/continuous-energy-volley")&&
     vegetaIcons[3]==Resources.Load<Texture2D>("Art/V0151/Vegeta/saiyan-elite")&&vegetaIcons.All(icon=>icon!=null),
     "Vegeta's skill gallery did not load the two new icons and the other two existing icons");
    SwitchDetailCard(renderedCards[1]);
    GalleryTestButton("PreviousCard").onClick.Invoke();AssertSmoke(selectedCard==0,"Detail previous failed");
    GalleryTestButton("BackToCards").onClick.Invoke();
    VerifyFormationDrag();
    VerifyChaseReactionHolds();
    int materials=profile.Materials, wins=profile.Wins, crystals=profile.Crystals;
    roundDemo=false;
    roundBattle=profile.NewRoundBattle(RoundRules(),RoundBoards().boards[2],++seed);
    StartRoundPresentation();
    AssertSmoke(page==RoundPreviewPage&&roundBattle.Order.Count==profile.RoundFormation().Count(i=>i>=0)+9,"Nine-enemy preview did not start");
    AssertSmoke(roundBattle.Order[0].Side==0&&roundBattle.Order[1].Side==1,"Round preview disagrees on opening initiative");
    var p1=(RectTransform)roundPreviewRoot.Find("Field0/BattleCell1");
    var e1=(RectTransform)roundPreviewRoot.Find("Field1/BattleCell1");
    AssertSmoke(p1.anchoredPosition.x>e1.anchoredPosition.x,"Enemy board did not mirror player-side columns");
    VerifyGokuOpenings();
    GalleryTestButton("StepRound").onClick.Invoke();
    AssertSmoke(roundPaused&&roundLast.Move==RoundMove.Basic&&roundLast.Actor.Side==0,"Preview manual advance failed");
    int events=1;
    while(roundBattle.Result==0&&events++<1000)roundBattle.Step();
    AssertSmoke(roundBattle.Result!=0&&profile.Materials==materials&&profile.Wins==wins&&profile.Crystals==crystals,"Preview mutated rewards or failed to terminate");
    GalleryTestButton("LeaveRoundPreview").onClick.Invoke();AssertSmoke(page=="主页"&&roundBattle==null,"Preview exit retained battle");
    VerifyCharacterDetailsV162();
    VerifyGokuMoves();
    VerifyPageMusic();
    VerifyGridDemo();
    VerifyVegetaScale();
    VerifyVegetaVolley();
    VerifyPiccoloPresentation();
    VerifyGohanPresentation();
    VerifyStatusExpiry();
    VerifyKrillinPresentation();
    VerifyRaditzPresentation();
    VerifyRaditzSingleTargetRays();
    VerifyYamchaPresentation();
    VerifyTienPresentation();
    VerifyV174Roles();
    VerifyPiccoloApproach();
    VerifyDemoMultiTarget();
    VerifyFullDemoGrid();
    VerifyBeatTiming();
    VerifyRoundTempoAndMeters();
    VerifyQuickPursuitHandoff();
    VerifyBrokenPursuitRecovery();
    File.WriteAllText(Path.Combine(dir,"smoke-result.json"),"{\"roundCombat\":true,\"gokuMotion\":true,\"openingIcon\":true,\"gridDemo\":true,\"gridMultiTarget\":true,\"fullGridReadability\":true,\"groundedMotion\":true,\"contrastTiming\":true,\"waveFeather\":true,\"thickerWave\":true,\"expandedBattlefield\":true,\"hiddenBattleSlotNumbers\":true,\"upwardPursuitSheen\":true,\"ultimateAtlasSize\":true,\"pursuitFlash\":true,\"quickPursuitHandoff\":true,\"roundedVitals\":true,\"slotVitals\":true,\"actionGap\":true,\"actualHitFeedback\":true,\"battleStats\":true,\"shortOpening\":true,\"reactionTransitions\":25,\"gokuAudioCues\":21,\"vegetaAudioCues\":9,\"savedFiveEnemyDemo\":true,\"characterSheetConfiguration\":true,\"vegetaVolley\":true,\"vegetaArt\":true,\"piccoloPresentation\":true,\"piccoloHeldCharge\":true,\"piccoloApproachPacing\":true,\"piccoloPursuitHop\":true,\"galleryOwnershipGroups\":true,\"galleryNumberSort\":true,\"galleryElasticSkills\":true,\"characterPageLoop\":true,\"characterPageSessionSort\":true,\"characterSkillFourLines\":true,\"sharedMenuLoop\":true,\"avatarSwipe\":true,\"roundStatusHud\":true,\"piccoloFingerGlow\":true,\"gohanHairArt\":true,\"gohanHeavyWindup\":true,\"gohanPresentation\":true,\"guardCounterPriority\":true,\"brokenPursuitRecovery\":true,\"raditzPresentation\":true,\"twinRaysAndSingleHit\":true,\"krillinReactionCrop\":true,\"formationDragAndSave\":true,\"formationExplicitSave\":true,\"yamchaPresentation\":true,\"tienPresentation\":true,\"roleArtwork\":true,\"greenBadgeAndRadar\":true,\"combinedStatGuide\":true,\"galleryRarityBackdrops\":true,\"multiCardHeldSwipe\":true,\"groundedGokuKit\":true,\"approvedReactionArt\":true,\"floatLaunchArtwork\":true,\"activeRoster\":"+Data.RoundRoster.Count+",\"krillinPresentation\":true,\"galleryFavorites\":true,\"avatarElasticDrag\":true,\"authoredBuffIcons\":true,\"savedLevel100Demo\":true,\"ultimateCutIn\":true,\"smoothCameraRestore\":true,\"stableSkillBadge\":true,\"authoredSkillIcons\":true,\"beamTailWidened\":true,\"sharedOpeningFx\":true,\"battleSkillBadges\":true,\"congruentGridTiles\":true,\"horizontalRowBeam\":true,\"referenceTiming\":true,\"referenceVideoExcluded\":true,\"formationSave\":true,\"twoRoutes\":true,\"galleryDetails\":true,\"galleryCurrentAvatar\":true,\"gallerySkillColors\":true,\"gokuGalleryIcons\":true,\"neighborRarityBadges\":true,\"startupArt\":true,\"noRewards\":true,\"soundSilent\":true,\"homeAndBattleBgm\":true,\"compactBattleGrid\":true,\"actionRates\":true,\"vegetaScaleStable\":true,\"renderVerified\":false,\"actions\":"+events+"}");
    Application.Quit(0);
   }catch(Exception ex){File.WriteAllText(Path.Combine(dir,"smoke-failure.txt"),ex.ToString());Debug.LogError(ex);Application.Quit(1);}
  }
 }
}
