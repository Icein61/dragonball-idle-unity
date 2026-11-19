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
   UButton(detailRoot,"Acquisition","查看获取途径",24,654,456,42,()=>ShowAcquisition(i));
   string[] tabs={"信息","招式与队长","装备与技能"};
   for(int n=0;n<3;n++){int tab=n;UButton(detailRoot,"DetailTab"+n,tabs[n],520+n*264,tabY,252,42,()=>{detailTab=tab;DrawGalleryDetail();},selected:detailTab==n);}
   float detailTop=tabY+58,detailHeight=Math.Max(110,630-detailTop);
   galleryDetailScroll=UScroll(detailRoot,"DetailScroll",520,detailTop,788,detailHeight,out detailBody);detailY=0;
   if(detailTab==0)DrawGalleryStats(i);
   else if(detailTab==1)DrawGallerySkills(i);
   else DrawGalleryLoadout(i);
   detailBody.sizeDelta=new Vector2(774,Math.Max(detailHeight,detailY+10));
   Canvas.ForceUpdateCanvases();galleryDetailScroll.verticalNormalizedPosition=oldScroll;
   gallerySwapButton=UButton(detailRoot,"ChooseSlot",owned?"选择阵位":"查看获取途径",520,654,382,42,()=>{if(owned)ShowFormationPicker(i);else ShowAcquisition(i);},true,saveError=="");
   galleryEquipButton=UButton(detailRoot,"OpenEquipment","前往装备工坊",918,654,390,42,()=>NavigateGallery("装备"),false,owned&&saveError=="");
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
   bool teamed=profile.Owned[i]&&profile.Team.Contains(i);
   var unit=profile.InspectCharacter(i,teamed);
   UText(detailBody,"StatsContext",teamed?"当前编队属性":profile.Owned[i]?"个人属性":"基础属性预览",0,0,760,30,18,uiAccent);
   string[] labels={"生命","攻击","防御","速度","会心","韧性","穿透","闪避"};
   // Current combat has no permanent tenacity or penetration stat. Skill penetration is separate.
   string[] values={unit.MaxHP.ToString("0.#"),unit.AttackValue.ToString("0.#"),unit.DefenseValue.ToString("0.#"),unit.SpeedValue.ToString("0.#"),
    (unit.Crit*100).ToString("0.#")+"%","0%","0%",(unit.Dodge*100).ToString("0.#")+"%"};
   for(int n=0;n<labels.Length;n++){
    var tile=UPanel(detailBody,"StatTile"+n,(n%4)*193,39+(n/4)*90,182,78,InkLight);
    UText(tile,"StatLabel",labels[n],12,7,158,22,15,uiDim);
    UText(tile,"StatValue",values[n],12,32,158,35,24,uiPaper);
   }
   detailY=218;
  }
  void DrawGallerySkills(int i){
   var card=Data.Cards[i];
   bool replaced=profile.Owned[i]&&(profile.Skill[i]=="pierce"||profile.Skill2[i]=="pierce");
   DetailParagraph("普攻",Data.UserSkill(card.Id,0));
   DetailParagraph(replaced?"原绝招 · 已替换":"绝招 · "+card.Special,card.SpecialText);
   if(replaced)DetailParagraph("当前绝招 · 贯穿气弹",Data.SkillDescription("pierce"));
   DetailParagraph("必杀 · "+card.Ultimate,card.UltimateText);
   DetailParagraph("被动",card.PassiveText);
   DetailParagraph("队长技",card.LeaderText);
   DetailParagraph("出招规则","绝招冷却 "+card.Cooldown+" 次正常行动。必杀消耗 100 怒气，额外释放。");
  }
  void DrawGalleryLoadout(int i){
   if(!profile.Owned[i]){DetailParagraph("尚未拥有","获得后可配置3件装备、2个额外技能，并在装备孔位镶嵌珠子。");return;}
   for(int s=0;s<3;s++){
    int slot=s;var item=profile.Items.FirstOrDefault(x=>x.owner==i&&x.slot==slot);
    DetailParagraph("装备槽 "+(s+1),item==null?"空槽 · 前往装备工坊选择已拥有的装备。":item.Name+"\n"+(item.kind==1?"防御强化":"攻击强化")+" +"+(item.Bonus*100).ToString("0.#")+"% / 整场"+(item.gem?"\n攻击珠：攻击强化 +0.8% / 整场":"\n珠孔：空"));
   }
   foreach(var id in new[]{profile.Skill[i],profile.Skill2[i]})DetailParagraph("额外技能 · "+Data.SkillName(id),Data.SkillDescription(id));
   string[] policy={"怒气足够即释放","自身生命 ≤ 60%","目标生命 ≤ 50%"};
   DetailParagraph("必杀策略",policy[profile.Policy[i]]+"；可在养成页调整。");
  }
 }
}

