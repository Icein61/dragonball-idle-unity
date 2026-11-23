using System.Linq;
using UnityEngine;
using UI=UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  static readonly string[] DemoMetricNames={"伤害","承伤","治疗"};
  static readonly Color[] DemoMetricColors={
   new Color32(222,119,107,255),new Color32(107,165,216,255),new Color32(111,187,138,255)
  };
  internal const float DemoStatsWidth=151,DemoStatsGap=10,DemoStatsTrackWidth=101;
  readonly RoundBattleStats demoStats=new RoundBattleStats();
  RoundBattleMetric demoMetric;

  void DrawDemoSideStats(RectTransform parent,int team,float x,float y,float height){
   var panel=UPanel(parent,"DemoStats"+team,x,y,DemoStatsWidth,height,InkLight);
   var color=DemoMetricColors[(int)demoMetric];
   for(int i=0;i<DemoMetricNames.Length;i++){
    var metric=(RoundBattleMetric)i;
    var tab=UButton(panel,"Metric"+i,DemoMetricNames[i],5+i*47,8,45,24,()=>{
     demoMetric=metric;
     DrawRoundPreview();
    },selected:demoMetric==metric);
    tab.GetComponentInChildren<UI.Text>().fontSize=12;
   }

   double maximum=roundBattle.Order.Max(item=>demoStats.Value(item,demoMetric));
   int row=0;
   foreach(var fighter in roundBattle.Order.Where(item=>item.Side==team)){
    const float avatarSize=34,trackHeight=23;
    var cell=UNode(panel,"StatsSlot"+fighter.Position,5,39+row*40,DemoStatsWidth-10,avatarSize);
    bool alive=VisibleRoundVitals(fighter).HP>0;
    var face=UPanel(cell,"Avatar",0,0,avatarSize,avatarSize,InkDark);
    DrawDetailAvatarArt(face,Data.Index(fighter.CardId),avatarSize,showOwnership:false);
    if(!alive)face.gameObject.AddComponent<CanvasGroup>().alpha=.62f;

    double value=demoStats.Value(fighter,demoMetric);
    float fill=maximum>0?DemoStatsTrackWidth*(float)(value/maximum):0;
    var track=UPanel(cell,"Track",40,5.5f,DemoStatsTrackWidth,trackHeight,InkPaper);
    RoundMeterCorners(track,trackHeight);
    var bar=UPanel(track,"Fill",0,0,fill,trackHeight,color);
    RoundMeterCorners(bar,trackHeight);
    bar.gameObject.SetActive(fill>0);

    string number=DemoNumber(value);
    float labelWidth=Mathf.Min(DemoStatsTrackWidth,Mathf.Max(20,number.Length*8+6));
    float center=fill>0?Mathf.Clamp(fill*.5f,labelWidth*.5f,DemoStatsTrackWidth-labelWidth*.5f):DemoStatsTrackWidth*.5f;
    UText(cell,"Value",number,40+center-labelWidth*.5f,5.5f,labelWidth,trackHeight,14,InkText,TextAnchor.MiddleCenter);
    row++;
   }
  }
 }
}
