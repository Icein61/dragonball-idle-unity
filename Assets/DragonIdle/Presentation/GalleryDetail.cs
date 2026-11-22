using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  static readonly Regex SkillTextTokens=new Regex(@"【[^【】\r\n]+】|\[[^\[\]\r\n]+\]|\{[^{}\r\n]+\}");
  static string FormatGallerySkillText(string text){
   return SkillTextTokens.Replace(text??string.Empty,match=>{
    string color=match.Value[0]=='【'?"#A14B18":match.Value[0]=='['?"#27683F":"#286D9A";
    return "<color="+color+">"+match.Value.Substring(1,match.Length-2)+"</color>";
   });
  }

  RectTransform detailBody;float detailY;UI.ScrollRect galleryDetailScroll;int renderedDetailCard=-2,renderedDetailTab=-1;UI.Button galleryEquipButton;
  RectTransform detailAvatarStrip;
  void DrawGalleryDetail(bool keepAvatarStrip=false){
   var eventSystem=UnityEngine.EventSystems.EventSystem.current;
   var focused=eventSystem!=null?eventSystem.currentSelectedGameObject:null;
   string restoreFocus=focused!=null&&focused.transform.IsChildOf(detailRoot)?focused.name:null;
   float oldScroll=galleryDetailScroll!=null&&renderedDetailCard==selectedCard&&renderedDetailTab==detailTab?galleryDetailScroll.verticalNormalizedPosition:1;
   renderedDetailCard=selectedCard;renderedDetailTab=detailTab;
   if(!keepAvatarStrip)detailAvatarStrip=null;
   ClearUI(detailRoot,detailAvatarStrip);measuredText.Clear();galleryEquipButton=null;
   if(selectedCard<0){UText(detailRoot,"NoSelection","从左侧选择一张角色卡",30,300,544,80,23,uiDim,TextAnchor.MiddleCenter);return;}
   int i=selectedCard;var c=Data.Cards[i];bool owned=profile.Owned[i];
   UButton(detailRoot,"BackToCards","‹ 返回角色",24,18,190,40,ReturnToCards);
   DrawRarityBackdrop(detailRoot,"PortraitMat",c.Rarity,24,67,456,422);
   UArt(detailRoot,i,24,67,456,422,CharacterCardPortraitRegion(i),muted:!owned);
   DrawPotentialChart(c);
   DrawFavoriteButton(detailRoot,i,224,17,lift:4);
   DrawRarityBadge(detailRoot,c.Rarity,520,10,94,80,matchVisibleHeight:true);
   if(detailAvatarStrip==null)DrawDetailNeighbors(i);
   DrawCharacterInfoButton();
   UText(detailRoot,"CharacterName",c.Name,621,10,310,40,29,uiPaper,TextAnchor.MiddleLeft);
   var title=UText(detailRoot,"CharacterTitle",c.Title,623,53,220,25,17,uiDim,TextAnchor.MiddleLeft);
   DrawCharacterRole(detailRoot,c,628+Mathf.Min(220,Mathf.Ceil(title.preferredWidth)),53,25);
   UText(detailRoot,"DetailNumber","No. "+c.CatalogNumber.ToString("D3"),523,87,525,24,16,uiAccent);
   if(owned){
    var power=UText(detailRoot,"DetailPower",GalleryPowerText(i),24,674,456,30,19,uiDim,TextAnchor.MiddleCenter);
    power.supportRichText=true;
   }
   float tabY=DrawDetailTagChips(c.Tags.Distinct().ToArray())+13;
   if(!owned)UText(detailRoot,"Ownership","尚未拥有",24,674,456,30,18,uiDim,TextAnchor.MiddleCenter);
   string[] tabs={"信息","技能"};
   for(int n=0;n<tabs.Length;n++){int tab=n;UButton(detailRoot,"DetailTab"+n,tabs[n],520+n*264,tabY,252,42,()=>{detailTab=tab;DrawGalleryDetail();},selected:detailTab==n);}
   float detailTop=tabY+58,detailHeight=Math.Max(110,709-detailTop);
   galleryDetailScroll=UScroll(detailRoot,"DetailScroll",520,detailTop,788,detailHeight,out detailBody);detailY=0;
   if(detailTab!=1)DrawGalleryStats(i);
   else DrawGallerySkills(i);
   detailBody.sizeDelta=new Vector2(774,Math.Max(detailHeight,detailY+10));
   Canvas.ForceUpdateCanvases();galleryDetailScroll.verticalNormalizedPosition=oldScroll;
   if(restoreFocus!=null&&eventSystem!=null){var control=detailRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(b=>b.name==restoreFocus&&b.interactable);eventSystem.SetSelectedGameObject(control!=null?control.gameObject:ActiveBackButton(detailRoot,"BackToCards"));}
  }
  float DrawDetailTagChips(string[] tags){
   const float left=520,available=788,height=30;
   float x=0,y=119;
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
   var cards=renderedCards??Array.Empty<int>();
   int selected=Array.IndexOf(cards,index);
   if(selected<0)return;
   const float size=GalleryAvatarSwipe.CenterSize,width=324;
   var strip=UPanel(detailRoot,"CharacterSwitch",1257-width,10,width,size,Color.clear,true);
   detailAvatarStrip=strip;
   strip.gameObject.AddComponent<UI.RectMask2D>();
   var swipe=strip.gameObject.AddComponent<GalleryAvatarSwipe>();
   swipe.SelectedFrameColor=InkGold;swipe.RestingFrameColor=InkLight;
   swipe.Initialize(selected,cards.Length,audioSettings!=null&&audioSettings.ReducedMotion,
    position=>CreateDetailAvatar(strip,cards[position]),
    position=>SwitchDetailCard(cards[position],keepAvatarStrip:true));
  }
  void SwitchDetailCard(int index,bool keepAvatarStrip=false){
   if(!galleryDetailOpen||index<0||index==selectedCard)return;
   var events=UnityEngine.EventSystems.EventSystem.current;
   var focused=events!=null?events.currentSelectedGameObject:null;
   if(focused!=null&&detailAvatarStrip!=null&&focused.transform.IsChildOf(detailAvatarStrip))
    events.SetSelectedGameObject(null);
   selectedCard=index;DrawGalleryDetail(keepAvatarStrip);
  }
  RectTransform CreateDetailAvatar(Transform strip,int index){
   const float size=GalleryAvatarSwipe.CenterSize;
   var button=UButton(strip,"Avatar","",0,0,size,size,()=>SwitchDetailCard(index));
   button.transition=UI.Selectable.Transition.None;
   button.GetComponent<GalleryFocus>().Outline=null;
   button.GetComponentInChildren<UI.Text>().gameObject.SetActive(false);
   var frame=(RectTransform)button.transform;
   string rarity=Data.Cards[index].Rarity;
   DrawRarityBackdrop(frame,"AvatarMat",rarity,3,3,size-6,size-6);
   DrawAvatarFace(frame,index,size);
   DrawRarityBadge(frame,rarity,2,2,25,20,leftAligned:true);
   return frame;
  }
  void DrawAvatarFace(Transform frame,int index,float size){
   var texture=PortraitTexture(index);
   if(texture!=null){
    var r=UNode(frame,"Face",3,3,size-6,size-6);
    var image=r.gameObject.AddComponent<UI.RawImage>();image.texture=texture;image.raycastTarget=false;
    var center=DetailAvatarFace(Data.Cards[index].Id);
    const float h=.29f;float w=h/PortraitAspect(index);
    image.uvRect=new Rect(Mathf.Clamp(center.x-w/2,0,1-w),Mathf.Clamp(1-center.y-h/2,0,1-h),w,h);
    if(!profile.Owned[index])MutePortrait(image);
   }else UArt(frame,index,3,3,size-6,size-6);
  }
  static Vector2 DetailAvatarFace(string cardId){
   switch(cardId){
    case "goku-test":return new Vector2(.47f,.28f);
    case "vegeta-test":return new Vector2(.52f,.20f);
    case "piccolo-test":return new Vector2(.34f,.266f);
    case "krillin-test":return new Vector2(.616f,.381f);
    case "gohan-test":return new Vector2(.51f,.27f);
    case "tien-test":return new Vector2(.58f,.21f);
    case "yamcha-saiyan-v020":return new Vector2(.65f,.22f);
    case "chiaotzu-saiyan-v020":return new Vector2(.60f,.29f);
    case "goku-saiyan-kaioken-v020":return new Vector2(.53f,.26f);
    default:return new Vector2(.5f,.24f);
   }
  }
  float DrawSkillParagraph(Transform parent,float top,string title,string content,RoundSkillIcon icon,bool openingIcon,bool measure){
   const float cardWidth=768, textLeft=96, titleTop=10, descTop=43, padding=5, iconLeft=13;
   const float boxWidth=cardWidth-textLeft-13, scrollWidth=boxWidth-2*padding;
   // Reserve scrollbar space up front so showing it never changes which line wraps.
   const float textInset=4, textWidth=scrollWidth-14-2*textInset;
   var card=UPanel(parent,"SkillCard",3,top,cardWidth,88,InkLight);
   UText(card,"SectionTitle",title,textLeft,titleTop,boxWidth,28,19,uiAccent,TextAnchor.MiddleLeft);
   var box=UPanel(card,"SkillDescription",textLeft,descTop,boxWidth,36,InkPaper);
   var label=UText(box,"Paragraph",FormatGallerySkillText(content),textInset,0,textWidth,1,18,uiPaper);
   label.supportRichText=true;
   label.verticalOverflow=VerticalWrapMode.Overflow;

   var lines=MeasureSkillText(label);
   float lineHeight=lines.First;
   float fullHeight=lines.Full;
   float visibleHeight=lines.Preview;
   UScroll(box,"DescriptionScroll",padding,padding,scrollWidth,visibleHeight,out var body,fullHeight>visibleHeight);
   label.rectTransform.SetParent(body,false);
   label.rectTransform.anchoredPosition=new Vector2(textInset,0);
   label.rectTransform.sizeDelta=new Vector2(textWidth,fullHeight);
   body.sizeDelta=new Vector2(body.sizeDelta.x,fullHeight);
   box.sizeDelta=new Vector2(boxWidth,visibleHeight+padding*2);

   float height=descTop+box.sizeDelta.y+padding;
   card.sizeDelta=new Vector2(cardWidth,height);
   if(!string.IsNullOrEmpty(icon?.resource)){
    float size=descTop+padding+lineHeight-titleTop;
    float y=lines.Count>1?(height-size)*.5f:titleTop;
    var slot=UPanel(card,"SkillIconSlot",iconLeft,y,size,size,InkPaper);
    var image=UNode(slot,openingIcon?"RoundOpeningSkillIcon":"RoundSkillIcon",0,0,size,size).gameObject.AddComponent<UI.RawImage>();
    image.raycastTarget=false;
    BindRoundSkillIcon(image,icon);
   }
   if(measure)measuredText.Add(label);
   return height+9;
  }
  static (int Count,float First,float Full,float Preview) MeasureSkillText(UI.Text label){
   var settings=label.GetGenerationSettings(new Vector2(label.rectTransform.rect.width,0));
   settings.scaleFactor=1;
   var generator=label.cachedTextGeneratorForLayout;
   generator.Populate(label.text,settings);
   var lines=generator.lines;
   int count=lines.Count;
   if(count==0)return (0,label.fontSize,label.fontSize,label.fontSize);

   // UI.Text lets the scroll viewport clip. Native line metrics include the font's descent.
   float Bottom(int index)=>Mathf.Ceil(Mathf.Max(0,lines[0].topY)-lines[index].topY+lines[index].height);
   return (count,Bottom(0),Bottom(count-1),Bottom(Mathf.Min(3,count-1)));
  }

  void DrawGalleryStats(int i)=>detailY=DrawCardStats(detailBody,i,0);

  float DrawCardStats(Transform parent,int i,float top){
   var unit=RoundCardPreview(i);
   double[] values={unit.MaxHP,unit.Attack,unit.Defense,unit.Crit,unit.Tenacity,unit.Penetration,unit.Dodge,unit.Accuracy,unit.Qi};
   for(int n=0;n<GalleryStatGuide.Length;n++){
    var tile=UPanel(parent,"StatTile"+n,(n%4)*193,top+7+(n/4)*90,182,78,InkLight);
    UText(tile,"StatLabel",GalleryStatGuide[n].Name,12,7,158,22,15,uiDim);
    double display=n<3?values[n]:values[n]*100;
    UText(tile,"StatValue",Math.Floor(display+1e-9).ToString("0"),12,32,158,35,24,uiPaper);
   }
   return top+((GalleryStatGuide.Length+3)/4)*90+4;
  }
  void DrawGallerySkills(int i)=>detailY=DrawCardSkills(detailBody,i,0,true);

  float DrawCardSkills(Transform parent,int i,float top,bool measure=false){
   void Add(string title,string text,RoundSkillIcon icon=null,bool openingIcon=false){
    top+=DrawSkillParagraph(parent,top,title,text,icon,openingIcon,measure);
   }
   var k=RoundRules().For(Data.Cards[i].Id);
   bool hasAuthoredTechniques=!string.IsNullOrWhiteSpace(k.techniques?.basicText);
   if(hasAuthoredTechniques){
    Add("普攻 · "+k.basic,k.techniques.basicText,icon:k.basicIcon);
    string ultimate=k.techniques.ultimateText;
    if(!string.IsNullOrEmpty(k.techniques.ultimateNote))ultimate+="\n"+k.techniques.ultimateNote;
    Add("必杀 · "+k.ultimate,ultimate,icon:k.ultimateIcon);
    Add("追打 · "+k.pursuit,k.techniques.pursuitText,icon:k.pursuitIcon);
   }else{
    Add("普攻 · "+k.basic,"按固定手位使用。伤害倍率 "+k.basicPower.ToString("0.##")+"。");
    Add("必杀 · "+k.ultimate,"怒气满时插入行动，不消耗本轮普攻手位。伤害倍率 "+k.ultimatePower.ToString("0.##")+"。");
    Add("追打 · "+k.pursuit,"友方普攻会心后，由手位最靠前且本轮未追打的其他存活队友追击。伤害倍率 "+k.pursuitPower.ToString("0.##")+"。");
   }
   foreach(var s in k.skills){
    if(s.trigger=="reserved")continue;
    string effect=s.effect=="attack"?"开场自身攻击增加 "+(s.value*100).ToString("0.#")+"%。":
     s.effect=="defense"?"开场自身防御增加 "+(s.value*100).ToString("0.#")+"%。":
     s.effect=="shield"?"开场获得相当于自身最大生命 "+(s.value*100).ToString("0.#")+"% 的护盾。":
     s.effect=="strike"?"自身普攻命中后，目标生命不高于 50% 时追加攻击，伤害倍率 "+s.value.ToString("0.##")+"。":
     "自身普攻后，为生命比例最低且低于 65% 的队友回复其最大生命的 "+(s.value*100).ToString("0.#")+"%。";
    if(!string.IsNullOrEmpty(s.allyTag))effect+=" 己方编队含【"+s.allyTag+"】角色时，防御增加 "+(s.allyDefense*100).ToString("0.#")+"%。";
    Add((s.trigger=="opening"||s.trigger=="whileAlive"||s.trigger=="roundStart"?"被动型技能":"触发型技能")+" · "+s.name,s.text??effect,
     icon:s.icon,openingIcon:s.trigger=="opening");
   }
   if(!hasAuthoredTechniques)Add("当前配置","回合制验证用配置，角色正式设计将在后续版本填写。");
   return top;
  }
 }
}
