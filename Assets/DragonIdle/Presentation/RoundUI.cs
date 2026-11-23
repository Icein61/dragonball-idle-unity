using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  const string RoundPreviewPage="试阵";
  const float RoundActionGap=.78f,RoundOpeningGap=.18f,RoundPursuitGap=.14f,RoundReactionGap=.075f;
  static readonly Color RoundHealthFill=new Color32(76,216,116,255);
  static readonly Color RoundShieldFill=new Color32(75,164,248,255);
  static readonly Color RoundRageFill=new Color32(249,157,55,255);
  static readonly Color RoundMeterTrack=new Color32(28,40,42,230);
  RectTransform roundPreviewRoot;
  RoundDefinitions roundDefinitions;
  RoundEncounters roundEncounters;
  RoundCombat roundBattle;
  RoundAction roundLast;
  readonly System.Collections.Generic.List<string> roundLog=new System.Collections.Generic.List<string>();
  bool roundPaused;
  float roundClock;
  RoundDefinitions RoundRules(){
   if(roundDefinitions==null){
    var file=Resources.Load<TextAsset>("Config/Characters/RoundKits");
    if(file==null)throw new InvalidOperationException("缺少回合技能配置");
    var read=JsonUtility.FromJson<RoundDefinitions>(file.text);read.ValidateRoster();roundDefinitions=read;
   }
   return roundDefinitions;
  }
  RoundEncounters RoundBoards(){
   if(roundEncounters==null){
    var file=Resources.Load<TextAsset>("Config/Battle/RoundEncounters");
    if(file==null)throw new InvalidOperationException("缺少试阵配置");
    var read=JsonUtility.FromJson<RoundEncounters>(file.text);read.Validate();
    foreach(var board in read.boards)
     for(int slot=0;slot<board.positions.Length;slot++)
      if(!string.IsNullOrEmpty(board.positions[slot])&&!Data.IsRoundCard(board.positions[slot]))
       board.positions[slot]=Data.RoundRoster[slot%Data.RoundRoster.Count];
    roundEncounters=read;
   }
   return roundEncounters;
  }
  RoundFighter RoundCardPreview(int i){
   var model=RoundProgression.Create(Data.Cards[i],RoundProgression.LevelFromTraining(profile.Training[i]),0,1);
   RoundRules().ApplyOpening(model);return model;
  }
  int RoundPower(int i){
   var m=RoundCardPreview(i);
   double raw=m.Attack*(1+.5*m.Crit)+.065*m.MaxHP+.64*m.Defense+.04*m.Shield;
   return Math.Min(1499,(int)Math.Round(1500*raw/(400+raw),MidpointRounding.AwayFromZero));
  }
  void StartRoundPresentation(){
   roundLast=null;roundLog.Clear();roundPaused=false;roundClock=RoundOpeningGap;
   PrepareRoundMotion();
   NavigateGallery(RoundPreviewPage);
  }
  void UpdateRoundPreview(float dt){
   if(page!=RoundPreviewPage||roundBattle==null)return;
   bool presenting=roundMotion!=null;
   UpdateRoundMotion(dt);
   if(presenting||roundPaused||roundBattle.Result!=0)return;
   roundClock-=dt;
   if(roundClock<=0)AdvanceRoundPreview(false);
  }
  float NormalRoundActionGap=>roundDemo&&DemoScenes().demo.actionGap>0?DemoScenes().demo.actionGap:RoundActionGap;
  float NextRoundActionGap()=>roundBattle.ReactionSkillNext?RoundReactionGap:roundBattle.PursuitNext?RoundPursuitGap:NormalRoundActionGap;
  void AdvanceRoundPreview(bool manual){
   if(roundBattle==null)return;
   if(roundMotion!=null){FinishRoundMotion(true);return;}
   if(StartNextRoundOpening(manual)){DrawRoundPreview();return;}
   if(roundBattle.Result!=0)return;
   var before=ReducedRoundMotion?null:roundBattle.Order.ToDictionary(f=>f,f=>new RoundVitals(f,freezeDefense:true));
   roundLast=roundBattle.Step();
   roundClock=NextRoundActionGap();
   if(roundLast!=null){
    if(StartRoundActionMotion(roundLast,manual))roundBefore=before;
    else {
     PlayPursuitCue(roundLast);
     PlayUnanimatedRoundCue(roundLast);
     if(roundLast.Detonates&&roundDemo)CompleteDemoDetonation(roundLast.Actor);
     if(roundLast.Revives){
      if(roundDemo)ApplyDemoHit(roundLast);
      PlayRoundCue("yamcha-rise");
     }
     if(!roundLast.Charging)foreach(var hit in roundLast.Hits){
      if(roundDemo)RecordDemoHit(hit);
      PlayRoundHitVoice(hit);
     }
     if(roundDemo)demoStats.RecordCost(roundLast);
     AddRoundLog(roundLast);
    }
   }
   DrawRoundPreview();
  }
  void AddRoundLog(RoundAction a){
   string kind=a.Move==RoundMove.Basic?"普攻":a.Move==RoundMove.Ultimate?"必杀":a.Move==RoundMove.Pursuit?"追打":"技能";
   string targets=a.Charging?"蓄力":a.SequentialHits
    ?string.Join(" / ",a.Hits.GroupBy(hit=>hit.Target).Select(group=>group.Key.Name+" "+
      (group.All(hit=>hit.Dodged)?"闪避":"−"+group.Sum(hit=>hit.Amount).ToString("0"))))
    :string.Join(" / ",a.Hits.Select(hit=>hit.Target.Name+" "+
      (hit.Dodged?hit.AvoidanceText:(hit.Healing?"+":"−")+hit.Amount.ToString("0"))));
   if(a.ActorHPCost>0)targets+=" / 自身生命 −"+a.ActorHPCost.ToString("0");
   roundLog.Insert(0,a.Actor.Name+" · "+kind+" "+a.Name+" → "+targets);
   if(roundLog.Count>3)roundLog.RemoveAt(3);
  }
  static readonly Color RoundMeterPaper=new Color32(249,244,226,136);
  void DrawRoundMeterFrame(Transform parent,string name,float x,float y,float width,float bar,RoundFighter fighter,RoundVitals vitals){
   const float padding=3;
   var statuses=DisplayRoundStatuses(fighter,vitals).ToArray();
   float contentWidth=width-padding*2;
   float icon=statuses.Length==0?0:Mathf.Min(11,(contentWidth-2*(statuses.Length-1))/statuses.Length);
   float metersBottom=padding+bar*3+4;
   float frameHeight=metersBottom+padding+(statuses.Length==0?0:icon+2);
   var frame=UPanel(parent,name,x,y,width,frameHeight,RoundMeterPaper);
   RoundMeterCorners(frame,8);
   DrawRoundMeters(frame,padding,padding,contentWidth,bar,fighter,vitals);
   if(statuses.Length>0)DrawRoundStatuses(frame,statuses,width,metersBottom+2,icon);
  }
  void DrawRoundMeters(Transform parent,float x,float y,float width,float height,RoundFighter fighter,RoundVitals vitals){
   DrawRoundMeter(parent,"Health",x,y,width,height,vitals.HP/fighter.MaxHP,RoundHealthFill);
   DrawRoundMeter(parent,"Shield",x,y+height+2,width,height,vitals.Shield/fighter.MaxHP,RoundShieldFill);
   DrawRoundMeter(parent,"Rage",x,y+(height+2)*2,width,height,(double)vitals.Rage/RoundCombat.RageCap,RoundRageFill);
  }
  void DrawRoundMeter(Transform parent,string name,float x,float y,float width,float height,double amount,Color color){
   var track=UPanel(parent,name,x,y,width,height,RoundMeterTrack);
   RoundMeterCorners(track,height);
   var fill=UPanel(track,"Fill",0,0,width*Mathf.Clamp01((float)amount),height,color);
   RoundMeterCorners(fill,height);
  }
  void RoundMeterCorners(RectTransform rect,float height){
   var image=rect.GetComponent<UI.Image>();
   image.sprite=RoundedSurface(false);
   image.type=UI.Image.Type.Sliced;
   image.pixelsPerUnitMultiplier=2*image.sprite.border.x/height;
  }
  void DrawRoundPreview(){
   if(roundBattle==null)return;
   if(roundPreviewRoot==null)roundPreviewRoot=UNode(galleryRoot,"RoundPreview",222,110,1336,720);else ClearUI(roundPreviewRoot);
   UButton(roundPreviewRoot,"LeaveRoundPreview","‹ 返回主页",0,0,170,44,()=>NavigateGallery(RoundBackPage()));
   if(roundDemo)UButton(roundPreviewRoot,"RestartDemo","重开",176,0,94,44,BeginRoundDemo);
   roundStanding.Clear();
   int result=VisibleRoundResult;
   string status=result==0?"第 "+roundBattle.Round+" 回合":result==1?"我方胜利":result==-1?"敌方胜利":"回合上限 · 平局";
   UText(roundPreviewRoot,"RoundState",status,229,1,770,43,29,InkText,TextAnchor.MiddleCenter);
   bool done=result!=0&&roundMotion==null;
   bool freeze=roundPaused&&(roundMotion==null||!roundMotion.Manual);
   UButton(roundPreviewRoot,"PauseRound",freeze?"继续":"暂停",1020,0,145,44,()=>{
    roundPaused=!freeze;
    if(roundMotion!=null)roundMotion.Manual=false;
    PauseRoundCueAudio(roundPaused);
    DrawRoundPreview();
   },enabled:!done);
   UButton(roundPreviewRoot,"StepRound",roundMotion!=null?"跳过演出":"下一动作",1176,0,152,44,()=>{
    roundPaused=true;
    PauseRoundCueAudio(false);
    AdvanceRoundPreview(true);
   },enabled:!done);
   if(roundDemo){
    DrawDemoBoard();
    return;
   }
   for(int side=0;side<2;side++){
    float left=side==0?0:674;
    var field=UPanel(roundPreviewRoot,"Field"+side,left,58,652,532,InkPaper);
    UText(field,"FieldCaption",side==0?"我方":"敌方",14,8,624,29,19,InkText);
    for(int p=1;p<=RoundBoard.Cells;p++){
     var f=roundBattle.Order.FirstOrDefault(x=>x.Side==side&&x.Position==p);
     float x=11+RoundBoard.Column(p,side)*211,y=42+RoundBoard.Row(p)*158;
     bool active=f!=null&&(roundMotion?.Actor==f||roundLast?.Actor==f||roundLast?.Hits.Any(hit=>hit.Target==f)==true);
     var tile=UPanel(field,"BattleCell"+p,x,y,202,148,active?InkGold:InkLight);
     if(f==null)continue;
     var vitals=VisibleRoundVitals(f);
     if(vitals.HP>0){
      if(f.CardId==RoundMotionArt.CardId||f.CardId==RoundMotionArt.GohanCardId||f.CardId==RoundMotionArt.KrillinCardId)DrawRoundStanding(tile,f);
      else UArt(tile,Data.Index(f.CardId),42,4,118,102);
      if(f.CardId==RoundMotionArt.PiccoloCardId&&f.IsCharging){
       var aura=UNode(tile,"HeldCharge",0,0,202,148).gameObject.AddComponent<RoundMotionFx>();
       aura.raycastTarget=false;
       var portrait=(RectTransform)tile.Find("Portrait");
       var tip=new Vector2(portrait.anchoredPosition.x+754f/1024*portrait.rect.width,
        -portrait.anchoredPosition.y+484f/1536*portrait.rect.height);
       aura.PresentHeldCharges(new[]{new RoundMotionFx.ChargePulse{Center=tip}});
      }
     }
     UText(tile,"Name",f.Name,8,91,186,22,14,InkText,TextAnchor.MiddleCenter);
     if(vitals.HP>0){
      DrawRoundMeterFrame(tile,"Vitals",19,113,164,4,f,vitals);
     }else UText(tile,"Down","退场",12,53,176,31,18,InkMuted,TextAnchor.MiddleCenter);
    }
   }
   var logPanel=UPanel(roundPreviewRoot,"RoundLog",0,604,1326,104,InkDark);
   UText(logPanel,"Events",roundLog.Count==0?"等待开战":string.Join("\n",roundLog),14,10,1298,86,16,InkIvory);
   DrawRoundFocus();
  }
 }
}
