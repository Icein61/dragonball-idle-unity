using UnityEngine;
using UI=UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  static readonly (string Name,string Description)[] GalleryStatGuide={
   ("生命","可承受的生命伤害。降为 0 时退场，跳过它的手位。"),
   ("攻击","攻击越高，扣除目标有效防御后可乘上技能倍率的伤害越高。"),
   ("防御","减少来袭攻击的基础伤害。对手的穿透会按比例忽略防御。"),
   ("会心","抵消对方韧性后决定暴击概率。暴击伤害倍率为 1.5。"),
   ("韧性","降低对手对自己造成暴击的概率。"),
   ("穿透","按比例忽略目标的防御。面板数值 10 对应 10% 的比例。"),
   ("闪避","扣除对方速度后，决定完全避开伤害和中断本次追打的概率。"),
   ("速度","抵消目标的闪避。出手顺序由开场阵位手位决定。"),
   ("气","按比例提高自己收到的治疗和护盾。")
  };

  Sprite infoIcon;
  void DrawCharacterInfoButton(){
   const float size=38;
   var button=UButton(detailRoot,"CombatRulesInfo","",1268,15,size,size,ShowCombatRules);
   button.GetComponentInChildren<UI.Text>().gameObject.SetActive(false);
   var rect=(RectTransform)button.transform;
   button.targetGraphic.color=Color.clear;
   var icon=UNode(rect,"InfoIcon",4,4,30,30).gameObject.AddComponent<UI.Image>();
   icon.sprite=CharacterInfoIcon();icon.preserveAspect=true;icon.raycastTarget=false;
   button.targetGraphic=icon;
   var colors=button.colors;
   colors.normalColor=colors.selectedColor=Color.black;
   colors.highlightedColor=new Color32(31,101,91,255);
   colors.pressedColor=new Color32(149,85,36,255);
   button.colors=colors;
   icon.canvasRenderer.SetColor(colors.normalColor);
  }

  Sprite CharacterInfoIcon(){
   if(infoIcon!=null)return infoIcon;
   const int size=64;
   var source=new Texture2D(size,size,TextureFormat.RGBA32,false){name="CharacterInfoGlyph",wrapMode=TextureWrapMode.Clamp};
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    var point=new Vector2(x+.5f,y+.5f);
    float ring=2.1f-Mathf.Abs(Vector2.Distance(point,new Vector2(32,32))-26);
    float dot=3.3f-Vector2.Distance(point,new Vector2(32,43));
    var stem=new Vector2(32,Mathf.Clamp(point.y,21,33));
    float bar=3.3f-Vector2.Distance(point,stem);
    source.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(Mathf.Max(ring,dot,bar)+.5f)));
   }
   source.Apply(false,true);
   infoIcon=Sprite.Create(source,new Rect(0,0,size,size),Vector2.one*.5f);
   return infoIcon;
  }

  void DrawPotentialChart(Card card){
   if(!RoundSheetCatalog.Cards.TryGetValue(card.Id,out var sheet))return;
   var chart=UPanel(detailRoot,"PotentialChart",24,502,456,164,InkMist);
   DrawPotentialRadar(chart,"PotentialRadar",new Vector2(228,87),47,sheet,false);
  }

  void DrawCardPotentialChart(Transform card,Card definition){
   if(!RoundSheetCatalog.Cards.TryGetValue(definition.Id,out var sheet))return;
   DrawPotentialRadar(card,"CardPotentialRadar",new Vector2(238,401),29,sheet,true);
  }

  void DrawPotentialRadar(Transform parent,string name,Vector2 center,float radius,RoundSheetCard card,bool compact){
   var graphic=UNode(parent,name,center.x-radius-3,center.y-radius-3,2*radius+6,2*radius+6).gameObject.AddComponent<GalleryPotentialGraphic>();
   graphic.SetGrades(card.AttackPotential,card.DefensePotential,card.HpPotential,radius,card.Rarity,compact);
   string[] labels=compact?new[]{"攻","防","生"}:new[]{"攻击","防御","生命"};
   string[] axes={"AttackPotential","DefensePotential","HpPotential"};
   float width=compact?23:61,height=compact?18:24, gap=compact?5:9;
   for(int i=0;i<labels.Length;i++){
    var direction=GalleryPotentialGraphic.Axis(i);
    direction.y=-direction.y;
    // Keep the near edge of each label the same radial distance from the outer triangle.
    float support=(Mathf.Abs(direction.x)*width+Mathf.Abs(direction.y)*height)*.5f;
    var at=center+direction*(radius+gap+support);
    UText(parent,(compact?"Card":"")+axes[i],labels[i],at.x-width*.5f,at.y-height*.5f,width,height,
     compact?13:16,compact?InkMuted:InkText,TextAnchor.MiddleCenter);
   }
  }

  void ShowCombatRules(){
   var modal=NewGalleryDialog("属性与伤害计算","查看各项属性的作用，再向下滚动查看战斗结算。",focusClose:false,headerRule:false);
   UScroll(modal,"CombatRulesScroll",26,131,688,376,out var content);
   float y=8;
   UText(content,"StatGuideHeading","属性说明",12,y,632,32,22,InkText);
   y+=38;
   for(int i=0;i<GalleryStatGuide.Length;i++){
    var stat=GalleryStatGuide[i];
    var row=UPanel(content,"StatGuideRow"+i,3,y,650,34,i%2==0?InkLight:InkPaper);
    UText(row,"StatName",stat.Name,12,0,80,34,17,InkText,TextAnchor.MiddleLeft);
    UText(row,"StatDescription",stat.Description,94,0,542,34,16,uiDim,TextAnchor.MiddleLeft);
    y+=36;
   }
   y+=12;
   UText(content,"CombatRulesHeading","伤害计算",12,y,632,33,22,InkText);
   y+=43;
   string[] headings={"1  闪避判定","2  基础伤害","3  暴击判定","4  减伤和护盾","5  攻击与防御加成","6  受击状态与追打"};
   string[] rules={
    "有效闪避 = 目标闪避 − 攻击方速度，最低 0，最高 100%。闪避后本次攻击不扣血。普通闪避未触发时，符合条件的技能闪避仍可触发并反击。",
    "有效防御 = 目标防御 ×（1 − 攻击方穿透）。基础伤害 =（攻击 − 有效防御）× 技能倍率，最低 1。",
    "暴击概率 = 攻击方会心 − 目标韧性，最低 0，最高 100%。暴击将伤害乘以 1.5。",
    "各来源的最终减伤按当前技能规则结算，减免上限为 100%。伤害先由护盾吸收，剩余部分扣除生命。",
    "同名普通加成相加，各自独立到期。队长加成使用独立乘区。面板向下取整；进阶属性显示百分点。",
    "命中并造成指定状态后，符合条件的队友才可追打。闪避使对应起手或链内一击无法继续衔接；未实际出手的追打不消耗次数。"
   };
   for(int i=0;i<headings.Length;i++){
    UText(content,"RuleTitle"+i,headings[i],12,y,626,28,18,InkText);
    UText(content,"RuleBody"+i,rules[i],12,y+33,609,69,17,InkText);
    y+=111;
   }
   content.sizeDelta=new Vector2(674,y+10);
  }
 }

 [RequireComponent(typeof(CanvasRenderer))]
 internal sealed class GalleryPotentialGraphic : UI.MaskableGraphic {
  static readonly float[] Rings={.20f,.43f,.70f,1f};
  static readonly Vector2[] Axes={Vector2.up,new Vector2(-.8660254f,-.5f),new Vector2(.8660254f,-.5f)};
  readonly int[] grades=new int[3];
  float radius;
  internal Color EdgeColor {get;private set;}
  internal float EdgeWidth {get;private set;}
  internal static readonly Color GridColor=new Color32(219,226,218,255);

  internal static Vector2 Axis(int index)=>Axes[index];
  internal void SetGrades(string attack,string defense,string hp,float plotRadius,string rarity,bool compact){
   grades[0]=GradeIndex(attack);grades[1]=GradeIndex(defense);grades[2]=GradeIndex(hp);
   radius=plotRadius;
   EdgeWidth=compact?.85f:1.7f;
   switch(rarity){
    case "S": EdgeColor=new Color32(151,97,21,255);break;
    case "A": EdgeColor=new Color32(99,62,137,255);break;
    case "B": EdgeColor=new Color32(35,89,145,255);break;
    default: EdgeColor=new Color32(40,111,67,255);break;
   }
   useLegacyMeshGeneration=false;raycastTarget=false;SetAllDirty();
  }

  static int GradeIndex(string grade)=>Mathf.Max(0,System.Array.IndexOf(Data.Rarities,grade));

  protected override void OnPopulateMesh(UI.VertexHelper mesh){
   mesh.Clear();
   var center=rectTransform.rect.center;
   Color outline=GridColor;
   foreach(float ring in Rings)
    for(int i=0;i<3;i++)Stroke(mesh,center+Axes[i]*radius*ring,center+Axes[(i+1)%3]*radius*ring,outline,.85f);
   for(int i=0;i<3;i++)Stroke(mesh,center,center+Axes[i]*radius,outline,.85f);

   var points=new Vector2[3];
   Color fill=new Color(EdgeColor.r,EdgeColor.g,EdgeColor.b,.40f);
   int start=mesh.currentVertCount;
   for(int i=0;i<3;i++){
    points[i]=center+Axes[i]*radius*Rings[grades[i]];
    mesh.AddVert(points[i],fill,Vector2.zero);
   }
   mesh.AddTriangle(start,start+1,start+2);
   for(int i=0;i<3;i++)Stroke(mesh,points[i],points[(i+1)%3],EdgeColor,EdgeWidth);
  }

  static void Stroke(UI.VertexHelper mesh,Vector2 from,Vector2 to,Color color,float width){
   var direction=(to-from).normalized;
   var normal=new Vector2(-direction.y,direction.x);
   float half=width*.5f, outer=half+.95f;
   int start=mesh.currentVertCount;
   Color clear=new Color(color.r,color.g,color.b,0);
   for(int i=0;i<4;i++){
    float offset=i==0?-outer:i==1?-half:i==2?half:outer;
    Color tint=i==0||i==3?clear:color;
    mesh.AddVert(from+normal*offset,tint,Vector2.zero);
    mesh.AddVert(to+normal*offset,tint,Vector2.zero);
    if(i==0)continue;
    int a=start+2*(i-1);
    mesh.AddTriangle(a,a+1,a+3);mesh.AddTriangle(a,a+3,a+2);
   }
  }
 }
}
