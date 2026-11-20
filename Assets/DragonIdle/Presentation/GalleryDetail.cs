using System;
using System.Linq;
using UnityEngine;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  RectTransform detailBody;float detailY;UI.ScrollRect galleryDetailScroll;int renderedDetailCard=-2,renderedDetailTab=-1;UI.Button galleryEquipButton,gallerySwapButton;
  void DrawGalleryDetail(){
   var eventSystem=UnityEngine.EventSystems.EventSystem.current;
   var focused=eventSystem!=null?eventSystem.currentSelectedGameObject:null;
   string restoreFocus=focused!=null&&focused.transform.IsChildOf(detailRoot)?focused.name:null;
   float oldScroll=galleryDetailScroll!=null&&renderedDetailCard==selectedCard&&renderedDetailTab==detailTab?galleryDetailScroll.verticalNormalizedPosition:1;
   renderedDetailCard=selectedCard;renderedDetailTab=detailTab;
   ClearUI(detailRoot);measuredText.Clear();galleryEquipButton=gallerySwapButton=null;
   if(selectedCard<0){UText(detailRoot,"NoSelection","从左侧选择一张角色卡",30,300,544,80,23,uiDim,TextAnchor.MiddleCenter);return;}
   int i=selectedCard;var c=Data.Cards[i];bool owned=profile.Owned[i];
   UButton(detailRoot,"BackToCards","‹ 返回图鉴",24,18,190,40,ReturnToCards);
   UPanel(detailRoot,"PortraitMat",24,72,456,532,InkMist);
   UArt(detailRoot,i,24,72,456,532);
   DrawRarityBadge(detailRoot,c.Rarity,520,14,62,46,leftAligned:true);
   UText(detailRoot,"DetailType",c.TypeText,591,24,210,32,18,uiAccent);
   DrawDetailNeighbors(i);
   UText(detailRoot,"CharacterName",c.Name,520,68,530,45,32,uiPaper);
   UText(detailRoot,"CharacterTitle",c.Title,520,116,788,28,17,uiDim).fontStyle=FontStyle.Normal;
   float tabY=DrawDetailTagChips(c.Tags.Distinct().ToArray())+16;
   if(!owned)UText(detailRoot,"Ownership","尚未拥有",24,613,456,30,18,uiDim);
   string[] tabs={"信息","招式"};
   for(int n=0;n<tabs.Length;n++){int tab=n;UButton(detailRoot,"DetailTab"+n,tabs[n],520+n*264,tabY,252,42,()=>{detailTab=tab;DrawGalleryDetail();},selected:detailTab==n);}
   float detailTop=tabY+58,detailHeight=Math.Max(110,630-detailTop);
   galleryDetailScroll=UScroll(detailRoot,"DetailScroll",520,detailTop,788,detailHeight,out detailBody);detailY=0;
   if(detailTab!=1)DrawGalleryStats(i);
   else DrawGallerySkills(i);
   detailBody.sizeDelta=new Vector2(774,Math.Max(detailHeight,detailY+10));
   Canvas.ForceUpdateCanvases();galleryDetailScroll.verticalNormalizedPosition=oldScroll;
   if(owned)gallerySwapButton=UButton(detailRoot,"ChooseSlot","选择阵位",520,654,382,42,()=>ShowFormationPicker(i),true,saveError=="");
   if(restoreFocus!=null&&eventSystem!=null){var control=detailRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(b=>b.name==restoreFocus&&b.interactable);eventSystem.SetSelectedGameObject(control!=null?control.gameObject:ActiveBackButton(detailRoot,"BackToCards"));}
  }
  float DrawDetailTagChips(string[] tags){
   const float left=520,available=788,height=30;
   float x=0,y=151;
   var tray=UNode(detailRoot,"DetailTags",left,0,available,0);
   for(int n=0;n<tags.Length;n++){
    var chip=UPanel(tray,"TagChip"+n,0,0,100,height,InkLight);
    var label=UText(chip,"TagText",tags[n],10,0,available-20,height,15,uiPaper,TextAnchor.MiddleCenter);
    float w=Mathf.Clamp(Mathf.Ceil(label.preferredWidth)+20,52,available);
    if(x>0&&x+w>available){x=0;y+=height+6;}
    chip.anchoredPosition=new Vector2(x,-y);chip.sizeDelta=new Vector2(w,height);
    label.rectTransform.sizeDelta=new Vector2(w-20,height);x+=w+8;
   }
   return y+height;
  }
  void DrawDetailNeighbors(int index){
   var cards=renderedCards??Array.Empty<int>();int pos=Array.IndexOf(cards,index);
   int prev=pos>0?cards[pos-1]:-1,next=pos>=0&&pos+1<cards.Length?cards[pos+1]:-1;
   UButton(detailRoot,"PreviousCard","‹",1058,27,44,42,()=>SwitchDetailCard(prev),enabled:prev>=0);
   DrawNeighborAvatar("PreviousAvatar",prev,1108);
   DrawNeighborAvatar("NextAvatar",next,1180);
   UButton(detailRoot,"NextCard","›",1250,27,44,42,()=>SwitchDetailCard(next),enabled:next>=0);
  }
  void SwitchDetailCard(int index){
   if(!galleryDetailOpen||index<0||index==selectedCard)return;
   selectedCard=index;DrawGalleryDetail();
  }
  void DrawNeighborAvatar(string name,int index,float x){
   const float size=64;
   var frame=UButton(detailRoot,name,"",x,16,size,size,()=>SwitchDetailCard(index),enabled:index>=0);
   frame.GetComponentInChildren<UI.Text>().gameObject.SetActive(false);
   if(index<0)return;
   var texture=PortraitTexture(index);
   if(texture!=null){
    var r=UNode(frame.transform,"Face",3,3,size-6,size-6);
    var image=r.gameObject.AddComponent<UI.RawImage>();image.texture=texture;image.raycastTarget=false;
    var center=DetailAvatarFace(Data.Cards[index].Id);
    const float h=.29f;float w=h/PortraitAspect(index);
    image.uvRect=new Rect(Mathf.Clamp(center.x-w/2,0,1-w),Mathf.Clamp(1-center.y-h/2,0,1-h),w,h);
   }else UArt(frame.transform,index,3,3,size-6,size-6);
   DrawRarityBadge(frame.transform,Data.Cards[index].Rarity,1,1,24,19,leftAligned:true);
  }
  static Vector2 DetailAvatarFace(string cardId){
   switch(cardId){
    case "goku-test":return new Vector2(.47f,.28f);
    case "vegeta-test":return new Vector2(.35f,.22f);
    case "piccolo-test":return new Vector2(.56f,.20f);
    case "krillin-test":return new Vector2(.68f,.36f);
    case "gohan-test":return new Vector2(.51f,.27f);
    case "tien-test":return new Vector2(.58f,.21f);
    case "yamcha-saiyan-v020":return new Vector2(.65f,.22f);
    case "chiaotzu-saiyan-v020":return new Vector2(.60f,.29f);
    case "goku-saiyan-kaioken-v020":return new Vector2(.53f,.26f);
    default:return new Vector2(.5f,.24f);
   }
  }
  void DetailParagraph(string title,string content,Color? color=null){
   UText(detailBody,"SectionTitle",title,0,detailY,750,29,20,color??uiAccent);detailY+=36;
   var t=UText(detailBody,"Paragraph",content,0,detailY,748,1,19,uiPaper);
   float height=Math.Max(28,t.preferredHeight+6);t.rectTransform.sizeDelta=new Vector2(748,height);measuredText.Add(t);detailY+=height+24;
  }
  void DrawGalleryStats(int i){
   var unit=RoundCardPreview(i);
   UText(detailBody,"StatsContext","回合制属性预览",0,0,760,30,18,uiAccent);
   string[] labels={"生命","攻击","防御","会心","韧性","穿透","闪避"};
   // Current combat has no permanent tenacity or penetration stat. Skill penetration is separate.
   string[] values={unit.MaxHP.ToString("0.#"),unit.Attack.ToString("0.#"),unit.Defense.ToString("0.#"),
    (unit.Crit*100).ToString("0.#")+"%","0%","0%",(unit.Dodge*100).ToString("0.#")+"%"};
   for(int n=0;n<labels.Length;n++){
    var tile=UPanel(detailBody,"StatTile"+n,(n%4)*193,39+(n/4)*90,182,78,InkLight);
    UText(tile,"StatLabel",labels[n],12,7,158,22,15,uiDim);
    UText(tile,"StatValue",values[n],12,32,158,35,24,uiPaper);
   }
   detailY=218;
  }
  void DrawGallerySkills(int i){
   var k=RoundRules().For(Data.Cards[i].Id);
   DetailParagraph("普攻 · "+k.basic,"按固定手位使用。伤害倍率 "+k.basicPower.ToString("0.##")+"。");
   DetailParagraph("必杀 · "+k.ultimate,"怒气满时插入行动，不消耗本轮普攻手位。伤害倍率 "+k.ultimatePower.ToString("0.##")+"。");
   DetailParagraph("追打 · "+k.pursuit,"友方普攻会心后，由手位最靠前且本轮未追打的其他存活队友追击。伤害倍率 "+k.pursuitPower.ToString("0.##")+"。");
   foreach(var s in k.skills){
    string effect=s.effect=="attack"?"开场自身攻击增加 "+(s.value*100).ToString("0.#")+"%。":
     s.effect=="defense"?"开场自身防御增加 "+(s.value*100).ToString("0.#")+"%。":
     s.effect=="shield"?"开场获得相当于自身最大生命 "+(s.value*100).ToString("0.#")+"% 的护盾。":
     s.effect=="strike"?"自身普攻命中后，目标生命不高于 50% 时追加攻击，伤害倍率 "+s.value.ToString("0.##")+"。":
     "自身普攻后，为生命比例最低且低于 65% 的队友回复其最大生命的 "+(s.value*100).ToString("0.#")+"%。";
    DetailParagraph((s.trigger=="opening"?"被动型技能":"行动型技能")+" · "+s.name,effect);
   }
   DetailParagraph("当前配置","回合制验证用配置，角色正式设计将在后续版本填写。");
  }
 }
}
