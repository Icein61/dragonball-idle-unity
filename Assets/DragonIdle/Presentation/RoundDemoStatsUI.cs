using System.Linq;
using UnityEngine;
using UI=UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  static readonly string[] DemoMetricNames={"伤害","承伤","治疗"};
  const float DemoStatsWidth=151,DemoStatsGap=10;
  readonly RoundBattleStats demoStats=new RoundBattleStats();
  RoundBattleMetric demoMetric;

  void DrawDemoSideStats(RectTransform parent,int team,float x,float y,float height){
   var panel=UPanel(parent,"DemoStats"+team,x,y,DemoStatsWidth,height,InkLight);
   UText(panel,"Team",team==0?"我方数据":"敌方数据",7,7,DemoStatsWidth-14,21,16,InkText,TextAnchor.MiddleCenter);
   for(int i=0;i<DemoMetricNames.Length;i++){
    var metric=(RoundBattleMetric)i;
    var tab=UButton(panel,"Metric"+i,DemoMetricNames[i],5+i*47,31,45,24,()=>{
     demoMetric=metric;
     DrawRoundPreview();
    },selected:demoMetric==metric);
    tab.GetComponentInChildren<UI.Text>().fontSize=12;
   }

   int row=0;
   foreach(var fighter in roundBattle.Order.Where(item=>item.Side==team)){
    float top=62+row*37;
    var cell=UPanel(panel,"StatsSlot"+fighter.Position,5,top,DemoStatsWidth-10,34,row%2==0?InkPaper:InkLight);
    bool alive=VisibleRoundVitals(fighter).HP>0;
    UText(cell,"Name",fighter.Name,5,0,DemoStatsWidth-20,17,12,alive?InkText:InkMuted,TextAnchor.MiddleLeft);
    UText(cell,"Value",demoStats.Value(fighter,demoMetric).ToString("0"),5,16,DemoStatsWidth-20,17,14,InkText,TextAnchor.MiddleRight);
    row++;
   }
  }
 }
}
