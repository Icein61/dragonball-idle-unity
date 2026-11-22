using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  RectTransform workshopRoot,workBody;UI.ScrollRect workScroll;
  readonly Dictionary<string,float> workPositions=new Dictionary<string,float>();
  string renderedWorkshop="";int equipmentTab,selectedSkillSlot;
  static bool IsWorkshop(string p){return p=="养成"||p=="装备";}
  static bool IsModernPage(string p){return p=="角色"||p==FormationRoute||p==RoundPreviewPage||IsShellPage(p);}
  void RefreshModernFrame(){
   galleryListRoot.gameObject.SetActive(page=="角色"&&!galleryDetailOpen);
   detailRoot.gameObject.SetActive(page=="角色"&&galleryDetailOpen);
   if(workshopRoot!=null)workshopRoot.gameObject.SetActive(false);
   if(summonRoot!=null)summonRoot.gameObject.SetActive(false);
   if(formationRoot!=null)formationRoot.gameObject.SetActive(page==FormationRoute);
   if(roundPreviewRoot!=null)roundPreviewRoot.gameObject.SetActive(page==RoundPreviewPage);
   if(homeRoot!=null)homeRoot.gameObject.SetActive(page=="主页");
   if(loadingRoot!=null)loadingRoot.gameObject.SetActive(page=="加载");
   galleryChrome.gameObject.SetActive(!IsShellPage(page));
   galleryChrome.Find("Section").GetComponent<UI.Text>().text=page=="角色"?"角色":page==FormationRoute?FormationRoute:roundDemo?DemoBattleRoute:"回合试阵";
   RefreshFeatureHeaderCount();
   foreach(string name in FeatureRoutes)SetButtonActive(galleryChrome.Find("Nav"+name).GetComponent<UI.Button>(),page==name||(name==DemoBattleRoute&&page==RoundPreviewPage&&roundDemo));
   galleryToast.gameObject.SetActive(!IsShellPage(page));
  }
  void RefreshFeatureHeaderCount(){
   galleryResources.text=page=="角色"?"拥有"+RoundOwnedCount+"/"+Data.RoundRoster.Count:"";
  }
  GameObject ActiveBackButton(Transform root,string name){return root.GetComponentsInChildren<UI.Button>().First(b=>b.name==name).gameObject;}
  string WorkshopKey(){return page+":"+selectedCard+":"+equipmentTab;}
  void RememberWorkshop(){if(workScroll!=null&&renderedWorkshop!="")workPositions[renderedWorkshop]=workScroll.verticalNormalizedPosition;}
  void DrawWorkshop(){
   galleryDirty=false;RememberWorkshop();
   var focused=EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject;
   string focus=focused!=null&&workshopRoot!=null&&focused.transform.IsChildOf(workshopRoot)?focused.name:null;
   if(workshopRoot==null)workshopRoot=UNode(galleryRoot,"Workshop",222,112,1336,716);else ClearUI(workshopRoot);
   if(selectedCard<0||!profile.Owned[selectedCard])selectedCard=Enumerable.Range(0,Data.Cards.Length).First(i=>profile.Owned[i]);
   UButton(workshopRoot,"WorkshopBack","‹ 返回图鉴",0,0,194,42,()=>NavigateGallery("角色"));
   UText(workshopRoot,"WorkTitle",Data.Cards[selectedCard].DisplayName,214,0,900,44,26,uiPaper);
   var portrait=UPanel(workshopRoot,"CharacterContext",0,62,300,654,uiPanel);
   UPanel(portrait,"PortraitMat",16,18,268,366,InkMist);UArt(portrait,selectedCard,16,18,268,366);
   UText(portrait,"ContextName",Data.Cards[selectedCard].Name,20,400,260,38,26,uiPaper);
   UText(portrait,"ContextIdentity",Data.Cards[selectedCard].Stage+"\n"+Data.Cards[selectedCard].KeywordText,20,448,260,70,18,uiDim);
   UButton(portrait,"PreviousCharacter","上一位",16,538,128,42,()=>CycleWorkshopCard(-1));
   UButton(portrait,"NextCharacter","下一位",156,538,128,42,()=>CycleWorkshopCard(1));
   UButton(portrait,"SwitchWorkshop",page=="养成"?"装备与技能 ›":"培养与策略 ›",16,596,268,42,()=>NavigateGallery(page=="养成"?"装备":"养成"));
   UPanel(workshopRoot,"WorkPanel",320,62,1016,654,uiPanel);
   workScroll=UScroll(workshopRoot,"WorkScroll",344,82,968,614,out workBody);
   if(page=="养成")DrawModernTraining();else DrawModernEquipment();
   renderedWorkshop=WorkshopKey();Canvas.ForceUpdateCanvases();float position;
   workScroll.verticalNormalizedPosition=workPositions.TryGetValue(renderedWorkshop,out position)?position:1;
   RefreshModernFrame();
   if(EventSystem.current!=null){var b=workshopRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(x=>x.name==focus&&x.interactable);EventSystem.current.SetSelectedGameObject(b!=null?b.gameObject:ActiveBackButton(workshopRoot,"WorkshopBack"));}
  }
  void CycleWorkshopCard(int step){RememberWorkshop();var owned=Enumerable.Range(0,Data.Cards.Length).Where(i=>profile.Owned[i]).ToArray();selectedCard=owned[(Array.IndexOf(owned,selectedCard)+owned.Length+step)%owned.Length];DrawWorkshop();}
  void WorkChange(Action mutation,string message){
   if(saveError!=""){GalleryNotice(saveError,false);return;}
   if(Change(()=>{mutation();battle=null;settled=false;ClearBattlePresentation();}))GalleryNotice(message);else GalleryNotice(notice,false);
   DrawWorkshop();
  }
  float WorkParagraph(string name,string value,float y,int size=19){var t=UText(workBody,name,value,0,y,936,1,size,uiPaper);float height=Math.Max(28,t.preferredHeight+8);t.rectTransform.sizeDelta=new Vector2(936,height);return y+height+16;}
  void DrawModernTraining(){
   int i=selectedCard;var u=profile.InspectCharacter(i,profile.Team.Contains(i));
   float y=WorkParagraph("EffectiveStats",profile.Team.Contains(i)?"当前编队开战属性":"个人开战属性",0,22);
   string[] statNames={"生命","攻击","防御","速度"};
   double[] statValues={u.MaxHP,u.AttackValue,u.DefenseValue,u.SpeedValue};
   for(int stat=0;stat<statNames.Length;stat++){
    var tile=UPanel(workBody,"TrainingStat"+stat,stat*236,y,218,77,InkDark);
    UText(tile,"Name",statNames[stat],14,9,188,25,17,InkOnDark);
    UText(tile,"Value",statValues[stat].ToString("0.0"),14,34,188,35,22,InkIvory);
   }
   y+=97;
   y=WorkParagraph("GrowthInfo","修炼 "+profile.Training[i]+" · 共鸣 "+profile.Resonance[i]+"\n修炼每次增加基础属性0.1%，共鸣每次0.4%；速度不随两项提升。",y);
   int cost=30+profile.Training[i]/10,shards=3+profile.Resonance[i];
   UButton(workBody,"WorkTrain","修炼一次 · "+cost+" 素材",0,y,450,46,()=>WorkChange(()=>{if(!profile.Train(i))throw new InvalidOperationException("素材不足");},"修炼完成 · 本卡基础属性 +0.1%"),true,profile.Materials>=cost&&saveError=="");
   UButton(workBody,"WorkResonance","共鸣 · "+shards+" 专属资源",474,y,450,46,()=>WorkChange(()=>{if(!profile.ResonanceUp(i))throw new InvalidOperationException("专属资源不足");},"共鸣完成 · 本卡基础属性 +0.4%"),false,profile.Shards[i]>=shards&&saveError=="");y+=62;
   y=WorkParagraph("ResourceReason","素材 "+profile.Materials+" / "+cost+"；本卡专属资源 "+profile.Shards[i]+" / "+shards+"。不足时对应操作不可用。",y);
   y=WorkParagraph("PolicyTitle","战前必杀策略",y,22);
   string[] policies={"怒气足够即释放","自身生命 ≤ 60%","目标生命 ≤ 50%"};
   for(int n=0;n<3;n++){int p=n;UButton(workBody,"WorkPolicy"+n,policies[n],n*316,y,304,46,()=>WorkChange(()=>profile.Policy[i]=p,"已保存本卡必杀策略"),enabled:saveError=="",selected:profile.Policy[i]==n);}y+=68;
   y=WorkParagraph("PolicyHelp","策略只控制额外必杀的释放条件，不改变正常行动、绝招冷却和状态计时。装备与策略跟随角色，换阵位时保留。",y);
   workBody.sizeDelta=new Vector2(954,Math.Max(614,y));
  }
  void DrawModernEquipment(){
   UButton(workBody,"EquipmentTab0","装备与珠子",0,0,450,44,()=>{equipmentTab=0;DrawWorkshop();},selected:equipmentTab==0);
   UButton(workBody,"EquipmentTab1","额外技能",474,0,450,44,()=>{equipmentTab=1;DrawWorkshop();},selected:equipmentTab==1);
   if(equipmentTab==1){DrawModernSkills();return;}
   int card=selectedCard;float y=62;
   for(int s=0;s<3;s++){
    int slot=s;var item=profile.Items.FirstOrDefault(x=>x.owner==card&&x.slot==slot);
    var slotPanel=UPanel(workBody,"SlotPanel"+s,0,y,924,53,InkLight);
    UText(slotPanel,"EquipmentSlot"+s,"槽 "+(s+1)+" · "+(item==null?"未装备":item.Name+" · "+(item.kind==1?"防御强化":"攻击强化")+" +"+(item.Bonus*100).ToString("0.#")+"% · "+(item.gem?"攻击珠 +0.8%":"珠孔空")),14,9,896,36,19,InkText,TextAnchor.MiddleLeft);
    y+=63;
    if(item!=null){UButton(workBody,"Gem"+s,item.gem?"拆下攻击珠":"镶嵌攻击珠 · 1 珠子",0,y,450,40,()=>WorkChange(()=>{if(item.gem){item.gem=false;profile.Gems++;}else{if(profile.Gems<1)throw new InvalidOperationException("珠子不足");item.gem=true;profile.Gems--; }},"珠孔已更新 · 拆卸无损"),false,(item.gem||profile.Gems>0)&&saveError=="");UButton(workBody,"Unequip"+s,"卸下装备",474,y,450,40,()=>WorkChange(()=>item.owner=-1,"装备已退回背包"),false,saveError=="");y+=56;}
   }
   string[] kinds={"全部装备","攻击护腕","防护战斗服","稀有核心"},owners={"所有归属","未装备","当前角色"};
   UButton(workBody,"BagKind",kinds[bagKind],0,y,300,40,()=>ShowBagFilter(0));UButton(workBody,"BagOwner",owners[bagOwner],316,y,300,40,()=>ShowBagFilter(1));UButton(workBody,"BagFavorite",bagLock==0?"全部收藏":"仅收藏",632,y,300,40,()=>{bagLock=1-bagLock;DrawWorkshop();});y+=58;
   var items=profile.Items.Where(x=>(bagKind==0||x.kind==bagKind-1)&&(bagOwner==0||(bagOwner==1?x.owner<0:x.owner==card))&&(bagLock==0||x.locked)).OrderByDescending(x=>x.locked).ThenByDescending(x=>x.kind).ToArray();
   y=WorkParagraph("BagCount","背包 "+items.Length+" / "+profile.Items.Count+" · 差值比较包含镶嵌珠",y);
   if(items.Length==0)y=WorkParagraph("EmptyInventory","没有符合条件的装备。可清除筛选，或通过素材试炼获取。",y);
   for(int n=0;n<items.Length;n++){
    var item=items[n];int index=n;
    y=WorkParagraph("Item"+n,item.Name+" · "+(item.owner<0?"未装备":Data.Cards[item.owner].DisplayName+" / 槽 "+(item.slot+1))+(item.gem?" · 已镶珠":""),y);
    UButton(workBody,"Favorite"+n,item.locked?"已收藏":"收藏",0,y,140,42,()=>WorkChange(()=>item.locked=!item.locked,"收藏已更新"),enabled:saveError=="",selected:item.locked);
    for(int s=0;s<3;s++){int slot=s;UButton(workBody,"Equip"+index+"Slot"+s,"比较并装到槽 "+(s+1),156+s*262,y,248,42,()=>ShowEquipComparison(item.id,card,slot),false,saveError=="");}y+=62;
   }
   workBody.sizeDelta=new Vector2(954,Math.Max(614,y));
  }
  void ShowBagFilter(int kind){
   string[] labels=kind==0?new[]{"全部装备","攻击护腕","防护战斗服","稀有核心"}:new[]{"所有归属","未装备","当前角色"};
   var p=NewGalleryDialog(kind==0?"装备类别":"装备归属","选择后立即刷新背包，不改变当前角色。");
   for(int n=0;n<labels.Length;n++){int value=n;UButton(p,"BagOption"+n,labels[n],26,155+n*70,684,50,()=>{if(kind==0)bagKind=value;else bagOwner=value;CloseGalleryDialog();DrawWorkshop();},selected:(kind==0?bagKind:bagOwner)==n);}
  }
  void ShowEquipComparison(string id,int card,int slot){
   var item=profile.Items.First(x=>x.id==id);var current=profile.Items.FirstOrDefault(x=>x.owner==card&&x.slot==slot);
   var clone=JsonUtility.FromJson<Profile>(JsonUtility.ToJson(profile));var before=profile.InspectCharacter(card,profile.Team.Contains(card));clone.Equip(id,card,slot);var after=clone.InspectCharacter(card,clone.Team.Contains(card));
   var p=NewGalleryDialog("装配比较 · 槽 "+(slot+1),Data.Cards[card].DisplayName+"\n"+(current==null?"空槽":current.Name)+" → "+item.Name);
   UText(p,"Comparison",CompareItem(item,current)+"\n\n当前有效攻击 "+before.AttackValue.ToString("0.0")+" → "+after.AttackValue.ToString("0.0")+"\n当前有效防御 "+before.DefenseValue.ToString("0.0")+" → "+after.DefenseValue.ToString("0.0"),26,155,684,150,21,uiPaper);
   UText(p,"TransferWarning",item.owner>=0?"此物品来自 "+Data.Cards[item.owner].DisplayName+" 的槽 "+(item.slot+1)+"。确认后原槽腾空，目标槽原装备退回背包。":"目标槽原装备退回背包，镶嵌珠随物品保留。",26,325,684,80,19,uiDim);
   UButton(p,"ConfirmEquip","确认装配",26,444,330,48,()=>{CloseGalleryDialog();WorkChange(()=>{if(!profile.Equip(id,card,slot))throw new InvalidOperationException("装配失败");},"装备已保存");},true,saveError=="");
   UButton(p,"CancelEquip","取消",382,444,330,48,CloseGalleryDialog);
  }
  void DrawModernSkills(){
   int card=selectedCard;float y=64;
   for(int s=0;s<2;s++){int slot=s;string id=s==0?profile.Skill[card]:profile.Skill2[card];UButton(workBody,"SkillSlot"+s,"槽 "+(s+1)+" · "+Data.SkillName(id),s*474,y,450,44,()=>{selectedSkillSlot=slot;DrawWorkshop();},selected:selectedSkillSlot==s);}y+=64;
   y=WorkParagraph("SkillRule","选择技能槽后装配；同一技能不能重复，最多一项绝招替换。不改变身份标签与固有被动。",y);
   for(int n=0;n<Data.SkillIds.Length;n++){
    int index=n;string id=Data.SkillIds[n],other=selectedSkillSlot==0?profile.Skill2[card]:profile.Skill[card];bool unlocked=profile.CanUseSkill(id),duplicate=id!=""&&id==other;
    y=WorkParagraph("SkillDescription"+n,Data.SkillName(id)+"\n"+Data.SkillDescription(id),y);
    UButton(workBody,"SetWorkSkill"+n,duplicate?"另一槽已装备":unlocked?(id==""?"卸下当前槽":"装到当前槽"):"解锁 · 残页 "+profile.SkillPages[n]+" / 3",0,y,924,42,()=>WorkChange(()=>{if(!unlocked){if(!profile.UnlockSkill(index))throw new InvalidOperationException("残页不足");}else if(!profile.SetSkill(card,selectedSkillSlot,id))throw new InvalidOperationException("技能不可装配");},unlocked?"技能已保存":"技能已解锁，可继续装配"),false,!duplicate&&(unlocked||profile.SkillPages[n]>=3)&&saveError=="");y+=64;
   }
   workBody.sizeDelta=new Vector2(954,Math.Max(614,y));
  }
 }
}

