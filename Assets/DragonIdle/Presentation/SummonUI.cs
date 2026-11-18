using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  RectTransform summonRoot;
  string classicSummonResult="",saiyanSummonResult="";

  void DrawModernSummon() {
   var selected=EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject;
   string focus=selected!=null&&summonRoot!=null&&selected.transform.IsChildOf(summonRoot)?selected.name:null;
   if(summonRoot==null)summonRoot=UNode(galleryRoot,"Summon",222,112,1336,716);
   else ClearUI(summonRoot);
   UText(summonRoot,"SummonTitle","时空召唤",0,0,660,52,32,InkText);
   UButton(summonRoot,"SaiyanPool","赛亚人篇",0,67,230,44,()=>SelectSummonPool(true),selected:saiyanPool);
   UButton(summonRoot,"ClassicPool","基础常驻",242,67,230,44,()=>SelectSummonPool(false),selected:!saiyanPool);

   var pool=UPanel(summonRoot,"PoolOverview",0,131,755,537,InkPaper);
   int count=Data.Cards.Count(saiyanPool?(Func<Card,bool>)Data.InSaiyanPool:Data.InClassicPool);
   int pulls=saiyanPool?profile.SaiyanPulls:profile.Pulls;
   string result=saiyanPool?saiyanSummonResult:classicSummonResult;
   int cycle=pulls%20;
   UText(pool,"PoolTitle",saiyanPool?"赛亚人篇":"基础常驻",28,23,530,40,27,InkText);
   UText(pool,"PoolCount",count+" 张角色 · 同级均分",29,75,680,32,19,uiDim);
   string[] ranks={"C","B","S"},rates={"72%","20%","8%"};
   for(int i=0;i<ranks.Length;i++){
    int x=28+i*237;
    var tile=UPanel(pool,"PoolRate"+ranks[i],x,134,223,99,InkLight);
    DrawRarityBadge(tile,ranks[i],10,14,79,69);
    UText(tile,"Rate",rates[i],88,24,122,50,24,InkText,TextAnchor.MiddleCenter);
   }
   UText(pool,"PityReadout","距保底 S 还需 "+(20-cycle)+" 次",28,270,680,35,21,InkText);
   UPanel(pool,"PityTrack",28,321,698,8,InkBorder);
   if(cycle>0)UPanel(pool,"PityFill",28,321,698f*cycle/20,8,InkCharge);
   UText(pool,"PityRules","本池累计 "+pulls+" 次。两个卡池各自计数，每第 20 次保证 S，提前获得 S 时进度继续累计。",28,354,698,72,19,uiDim);
   UText(pool,"DuplicateRule","重复卡转为该卡的专属成长资源。",28,454,690,44,19,uiDim);

   var pull=UPanel(summonRoot,"SummonAction",778,131,558,537,InkDark);
   UText(pull,"Wallet","持有晶石  "+profile.Crystals,26,28,506,43,24,InkIvory);
   UText(pull,"SummonResultLabel","召唤结果",26,102,506,36,19,InkOnDark);
   var resultPanel=UPanel(pull,"SummonResult",26,147,506,190,InkLight);
   UText(resultPanel,"SummonResultText",result==""?"尚无召唤结果":result,20,23,466,144,23,InkText,TextAnchor.MiddleCenter);
   bool canPull=profile.Crystals>=50&&saveError=="";
   UText(pull,"SummonCost",profile.Crystals<50?"晶石不足 · 每次消耗 50":"每次消耗 50 晶石",26,368,506,35,19,InkOnDark);
   UButton(pull,"SummonOnce","召唤一次",26,438,506,58,PerformSummon,true,canPull);
   if(focus!=null&&EventSystem.current!=null) {
    var control=summonRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(button=>button.name==focus&&button.interactable);
    EventSystem.current.SetSelectedGameObject(control==null?ActiveBackButton(summonRoot,saiyanPool?"SaiyanPool":"ClassicPool"):control.gameObject);
   }
  }

  void SelectSummonPool(bool saiyan) {
   if(saiyan==saiyanPool)return;
   saiyanPool=saiyan;
   DrawModernSummon();
  }

  void PerformSummon() {
   if(saveError!=""||profile.Crystals<50)return;
   string result="";
   if(Change(()=>result=saiyanPool?profile.SummonSaiyan(++seed):profile.Summon(++seed))) {
    if(saiyanPool)saiyanSummonResult=result;
    else classicSummonResult=result;
    PlayUISound(1);
   }else GalleryNotice(notice,false);
   DrawModernSummon();
   RefreshModernFrame();
  }
 }
}
