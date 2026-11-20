using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  const string RoundPreviewPage="试阵";
  RectTransform roundFormationRoot,roundPreviewRoot;
  RoundDefinitions roundDefinitions;
  RoundEncounters roundEncounters;
  RoundCombat roundBattle;
  RoundAction roundLast;
  readonly System.Collections.Generic.List<string> roundLog=new System.Collections.Generic.List<string>();
  int roundPosition=1;
  bool roundPaused;
  float roundClock;
  RoundDefinitions RoundRules(){
   if(roundDefinitions==null){
    var file=Resources.Load<TextAsset>("Config/Characters/RoundKits");
    if(file==null)throw new InvalidOperationException("缺少回合技能配置");
    var read=JsonUtility.FromJson<RoundDefinitions>(file.text);read.Validate();roundDefinitions=read;
   }
   return roundDefinitions;
  }
  RoundEncounters RoundBoards(){
   if(roundEncounters==null){
    var file=Resources.Load<TextAsset>("Config/Battle/RoundEncounters");
    if(file==null)throw new InvalidOperationException("缺少试阵配置");
    var read=JsonUtility.FromJson<RoundEncounters>(file.text);read.Validate();roundEncounters=read;
   }
   return roundEncounters;
  }
  RoundFighter RoundCardPreview(int i){
   var card=Data.Cards[i];var model=new RoundFighter(card.Id,0,1,card.HP,card.Attack,card.Defense,.08);
   RoundRules().ApplyOpening(model);return model;
  }
  int RoundPower(int i){
   var m=RoundCardPreview(i);
   double raw=m.Attack*(1+.5*m.Crit)+.065*m.MaxHP+.64*m.Defense+.04*m.Shield;
   return Math.Min(1499,(int)Math.Round(1500*raw/(400+raw),MidpointRounding.AwayFromZero));
  }
  void DrawRoundFormation(){
   var es=EventSystem.current;var selected=es==null?null:es.currentSelectedGameObject;
   string focus=selected!=null&&roundFormationRoot!=null&&selected.transform.IsChildOf(roundFormationRoot)?selected.name:null;
   if(roundFormationRoot==null)roundFormationRoot=UNode(galleryRoot,"RoundFormation",222,112,1336,716);else ClearUI(roundFormationRoot);
   var positions=profile.RoundFormation();int count=positions.Count(i=>i>=0);
   UButton(roundFormationRoot,"RoundBack","‹ 返回图鉴",0,0,170,42,()=>NavigateGallery("角色"));
   UText(roundFormationRoot,"RoundFormationTitle","九宫格编队",190,0,405,44,30,InkText);
   UText(roundFormationRoot,"RoundFormationCount","上阵 "+count+"/"+RoundBoard.PlayerLimit,485,9,160,34,20,InkText,TextAnchor.MiddleRight);
   UText(roundFormationRoot,"RoundFormationHelp","阵位先从上到下，再从右到左编号；可留空。",4,54,640,30,17,InkMuted);
   UPanel(roundFormationRoot,"RoundFormationMat",0,88,650,510,InkPaper);
   for(int position=1;position<=RoundBoard.Cells;position++){
    int p=position,card=positions[p-1];
    float x=12+RoundBoard.Column(p,0)*212,y=100+RoundBoard.Row(p)*164;
    var button=UButton(roundFormationRoot,"RoundSlot"+p,"",x,y,202,150,()=>{roundPosition=p;DrawRoundFormation();},selected:roundPosition==p);
    button.GetComponentInChildren<UI.Text>().gameObject.SetActive(false);
    Color ink=roundPosition==p?InkIvory:InkText;
    UText(button.transform,"Position",""+p,10,8,37,30,19,ink);
    if(card>=0){UArt(button.transform,card,30,8,142,111);UText(button.transform,"Member",Data.Cards[card].Name,5,121,192,23,15,ink,TextAnchor.MiddleCenter);}
    else UText(button.transform,"Empty","空位",42,55,118,38,19,ink,TextAnchor.MiddleCenter);
   }
   var reserve=UPanel(roundFormationRoot,"RoundReserve",670,88,666,510,InkPaper);
   UText(reserve,"RoundReserveTitle","换入 "+roundPosition+" 号位",15,12,360,40,24,InkText);
   UButton(reserve,"RoundRemove","下阵",529,12,121,35,()=>EditRoundFormation(()=>profile.RemoveRoundMember(roundPosition)),enabled:positions[roundPosition-1]>=0&&count>1&&saveError=="");
   var owned=Enumerable.Range(0,Data.Cards.Length).Where(i=>profile.Owned[i]).ToArray();
   for(int i=0;i<owned.Length;i++){
    int card=owned[i],old=Array.IndexOf(positions,card);
    UButton(reserve,"RoundReserve"+card,Data.Cards[card].Name+"\n"+(old<0?"候补":(old+1)+" 号位"),15+i%3*214,60+i/3*71,203,62,
     ()=>EditRoundFormation(()=>profile.SetRoundMember(roundPosition,card)),enabled:saveError==""&&(old>=0||count<5||positions[roundPosition-1]>=0),selected:old==roundPosition-1);
   }
   UText(reserve,"RoundTestNote","试阵 · 验证回合规则，无奖励或消耗",15,300,615,29,17,InkMuted);
   var boards=RoundBoards().boards;
   for(int i=0;i<boards.Length;i++){
    var encounter=boards[i];
    UButton(reserve,"RoundTest"+i,encounter.title,15+i*214,342,203,48,()=>BeginRoundPreview(encounter),primary:i==0);
   }
   UText(reserve,"RoundReserveTip","点阵位后选择角色；已上阵的角色可换位。最多五人，至少保留一人。",15,415,620,64,17,InkMuted);
   if(es!=null){var match=roundFormationRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(b=>b.name==focus&&b.interactable);es.SetSelectedGameObject(match!=null?match.gameObject:ActiveBackButton(roundFormationRoot,"RoundSlot"+roundPosition));}
  }
  void EditRoundFormation(Func<bool> edit){
   if(Change(()=>{if(!edit())throw new InvalidOperationException("阵位不合法或已满");}))GalleryNotice("阵容已保存");
   else GalleryNotice(notice,false);
   DrawRoundFormation();
  }
  void BeginRoundPreview(RoundEncounter encounter){
   roundBattle=profile.NewRoundBattle(RoundRules(),encounter,++seed);
   roundLast=null;roundLog.Clear();roundPaused=false;roundClock=.3f;
   NavigateGallery(RoundPreviewPage);
  }
  void UpdateRoundPreview(float dt){
   if(page!=RoundPreviewPage||roundBattle==null||roundBattle.Result!=0||roundPaused)return;
   roundClock-=dt;if(roundClock>0)return;
   roundClock=.7f;AdvanceRoundPreview();
  }
  void AdvanceRoundPreview(){
   if(roundBattle==null||roundBattle.Result!=0)return;
   roundLast=roundBattle.Step();
   if(roundLast!=null){
    var a=roundLast;
    string kind=a.Move==RoundMove.Basic?"普攻":a.Move==RoundMove.Ultimate?"必杀":a.Move==RoundMove.Pursuit?"追打":"技能";
    roundLog.Insert(0,a.Actor.Name+" · "+kind+" "+a.Name+" → "+a.Target.Name+" "+(a.Dodged?"闪避":(a.Healing?"+":"−")+a.Amount.ToString("0")));
    if(roundLog.Count>3)roundLog.RemoveAt(3);
   }
   DrawRoundPreview();
  }
  void DrawRoundPreview(){
   if(roundBattle==null)return;
   if(roundPreviewRoot==null)roundPreviewRoot=UNode(galleryRoot,"RoundPreview",222,110,1336,720);else ClearUI(roundPreviewRoot);
   UButton(roundPreviewRoot,"LeaveRoundPreview","‹ 返回编队",0,0,170,44,()=>NavigateGallery("编队"));
   string status=roundBattle.Result==0?"第 "+roundBattle.Round+" 回合":roundBattle.Result==1?"我方胜利":roundBattle.Result==-1?"敌方胜利":"回合上限 · 平局";
   UText(roundPreviewRoot,"RoundState",status,229,1,770,43,29,InkText,TextAnchor.MiddleCenter);
   var done=roundBattle.Result!=0;
   UButton(roundPreviewRoot,"PauseRound",roundPaused?"继续":"暂停",1020,0,145,44,()=>{roundPaused=!roundPaused;DrawRoundPreview();},enabled:!done);
   UButton(roundPreviewRoot,"StepRound","下一动作",1176,0,152,44,()=>{roundPaused=true;AdvanceRoundPreview();},enabled:!done);
   for(int side=0;side<2;side++){
    float left=side==0?0:674;
    var field=UPanel(roundPreviewRoot,"Field"+side,left,58,652,532,InkPaper);
    UText(field,"FieldCaption",side==0?"我方":"敌方",14,8,624,29,19,InkText);
    for(int p=1;p<=RoundBoard.Cells;p++){
     var f=roundBattle.Order.FirstOrDefault(x=>x.Side==side&&x.Position==p);
     float x=11+RoundBoard.Column(p,side)*211,y=42+RoundBoard.Row(p)*158;
     bool active=roundLast!=null&&(roundLast.Actor==f||roundLast.Target==f);
     var tile=UPanel(field,"BattleCell"+p,x,y,202,148,active?InkGold:InkLight);
     UText(tile,"Position",""+p,9,8,40,24,17,InkText);
     if(f==null)continue;
     if(f.Alive)UArt(tile,Data.Index(f.CardId),42,4,118,102);
     UText(tile,"Name",f.Name,8,91,186,25,14,InkText,TextAnchor.MiddleCenter);
     if(f.Alive){
      var bar=UPanel(tile,"VitalsBg",13,118,176,6,InkBorder);
      UPanel(bar,"HP",0,0,176*(float)(f.HP/f.MaxHP),6,InkHealth);
      UText(tile,"Rage","怒气 "+f.Rage+(f.Shield>0?" · 盾 "+f.Shield.ToString("0"):""),8,126,186,18,12,InkText,TextAnchor.MiddleCenter);
     }else UText(tile,"Down","退场",12,53,176,31,18,InkMuted,TextAnchor.MiddleCenter);
    }
   }
   var logPanel=UPanel(roundPreviewRoot,"RoundLog",0,604,1326,104,InkDark);
   UText(logPanel,"Events",roundLog.Count==0?"等待开战":string.Join("\n",roundLog),14,10,1298,86,16,InkIvory);
  }
 }
}
