using System;
using System.Linq;
using UnityEngine;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  RectTransform detailBody;float detailY;UI.ScrollRect galleryDetailScroll;int renderedDetailCard=-2,renderedDetailTab=-1;UI.Button galleryTrainButton,galleryEquipButton,gallerySwapButton;
  void DrawGalleryDetail(){
   var eventSystem=UnityEngine.EventSystems.EventSystem.current;
   var focused=eventSystem!=null?eventSystem.currentSelectedGameObject:null;
   string restoreFocus=focused!=null&&focused.transform.IsChildOf(detailRoot)?focused.name:null;
   float oldScroll=galleryDetailScroll!=null&&renderedDetailCard==selectedCard&&renderedDetailTab==detailTab?galleryDetailScroll.verticalNormalizedPosition:1;
   renderedDetailCard=selectedCard;renderedDetailTab=detailTab;
   ClearUI(detailRoot);measuredText.Clear();galleryTrainButton=galleryEquipButton=gallerySwapButton=null;
   if(selectedCard<0){UText(detailRoot,"NoSelection","从左侧选择一张角色卡",30,300,544,80,23,uiDim,TextAnchor.MiddleCenter);return;}
   int i=selectedCard;var c=Data.Cards[i];bool owned=profile.Owned[i];
   UButton(detailRoot,"BackToCards","‹ 返回人物志",24,18,190,40,ReturnToCards);
   UArt(detailRoot,i,24,72,456,532);
   UText(detailRoot,"CardRarity",c.Rarity+"    "+c.KeywordText,520,24,788,32,18,uiAccent);
   UText(detailRoot,"CharacterName",c.Name,520,68,788,45,32,uiPaper);
   UText(detailRoot,"CharacterTitle",c.Title,520,116,788,32,23,uiPaper);
   UText(detailRoot,"Identity",c.Stage+"  ·  "+c.Form+"    "+string.Join(" / ",c.Tags.Distinct().ToArray()),520,158,788,32,17,uiDim);
   int slot=Array.IndexOf(profile.Team,i);
   UText(detailRoot,"Ownership",owned?(slot>=0?(slot<3?"前排 ":"后排 ")+(slot+1)+(slot==profile.Leader?" · 队长":" · 已上阵"):"已拥有 · 候补"):"尚未拥有 · 可查看全部信息",24,613,456,30,18,owned?uiAccent:uiDim);
   UButton(detailRoot,"Acquisition","查看获取途径",24,654,456,42,()=>ShowAcquisition(i));
   string[] tabs={"概览与培养","招式与队长","装备与技能"};
   for(int n=0;n<3;n++){int tab=n;UButton(detailRoot,"DetailTab"+n,tabs[n],520+n*264,202,252,42,()=>{detailTab=tab;DrawGalleryDetail();},detailTab==n);}
   galleryDetailScroll=UScroll(detailRoot,"DetailScroll",520,260,788,370,out detailBody);detailY=0;
   if(detailTab==0)DrawGalleryStats(i);
   else if(detailTab==1)DrawGallerySkills(i);
   else DrawGalleryLoadout(i);
   detailBody.sizeDelta=new Vector2(774,Math.Max(370,detailY+10));
   Canvas.ForceUpdateCanvases();galleryDetailScroll.verticalNormalizedPosition=oldScroll;
   gallerySwapButton=UButton(detailRoot,"ChooseSlot",owned?"选择阵位":"查看获取途径",520,654,382,42,()=>{if(owned)ShowFormationPicker(i);else ShowAcquisition(i);},true,saveError=="");
   galleryEquipButton=UButton(detailRoot,"OpenEquipment","前往装备工坊",918,654,390,42,()=>NavigateGallery("装备"),false,owned&&saveError=="");
   if(restoreFocus!=null&&eventSystem!=null){var control=detailRoot.GetComponentsInChildren<UI.Button>().FirstOrDefault(b=>b.name==restoreFocus&&b.interactable);eventSystem.SetSelectedGameObject(control!=null?control.gameObject:ActiveBackButton(detailRoot,"BackToCards"));}
  }
  void DetailParagraph(string title,string content,Color? color=null){
   UText(detailBody,"SectionTitle",title,0,detailY,750,29,20,color??uiAccent);detailY+=36;
   var t=UText(detailBody,"Paragraph",content,0,detailY,748,1,19,uiPaper);
   float height=Math.Max(28,t.preferredHeight+6);t.rectTransform.sizeDelta=new Vector2(748,height);measuredText.Add(t);detailY+=height+24;
  }
  void DrawGalleryStats(int i){
   var c=Data.Cards[i];bool owned=profile.Owned[i];var personal=profile.InspectCharacter(i);var team=profile.InspectCharacter(i,true);
   if(!statsExpanded){
    var effective=owned&&profile.Team.Contains(i)?team:personal;
    DetailParagraph(owned&&profile.Team.Contains(i)?"当前编队开战属性":"个人开战属性","生命  "+effective.MaxHP.ToString("0.0")+"      攻击  "+effective.AttackValue.ToString("0.0")+"\n防御  "+effective.DefenseValue.ToString("0.0")+"      速度  "+effective.SpeedValue.ToString("0.0"));
   }
   UButton(detailBody,"ToggleStatBreakdown",statsExpanded?"收起属性拆解":"展开基础 / 个人 / 编队属性",0,detailY,748,40,()=>{statsExpanded=!statsExpanded;DrawGalleryDetail();});detailY+=56;
   if(statsExpanded){
   DetailParagraph(owned?"开战属性拆解":"未培养预览","个人包含培养、装备、技能及已生效常驻被动，不计临时战斗强化；未上阵角色不计队长加成。");
   UText(detailBody,"ColumnName","属性",0,detailY,100,25,15,uiDim);
   UText(detailBody,"ColumnBase","基础",124,detailY,123,25,15,uiDim);
   UText(detailBody,"ColumnPersonal","个人",269,detailY,123,25,15,uiDim);
   UText(detailBody,"ColumnTeam","编队",414,detailY,123,25,15,uiDim);detailY+=32;
   string[] names={"生命","攻击","防御","速度"};
   double[] basis={c.HP,c.Attack,c.Defense,c.Speed},one={personal.MaxHP,personal.AttackValue,personal.DefenseValue,personal.SpeedValue},two={team.MaxHP,team.AttackValue,team.DefenseValue,team.SpeedValue};
   for(int n=0;n<4;n++){
    UText(detailBody,"StatName"+n,names[n],0,detailY,100,30,18,uiPaper);
    UText(detailBody,"Base"+n,basis[n].ToString("0.0"),124,detailY,123,30,18,uiDim);
    UText(detailBody,"Personal"+n,one[n].ToString("0.0"),269,detailY,123,30,18,uiPaper);
    UText(detailBody,"Team"+n,owned&&profile.Team.Contains(i)?two[n].ToString("0.0"):"未上阵",414,detailY,123,30,18,uiAccent);detailY+=36;
   }
   }
   DetailParagraph("战前效果","暴击 "+(personal.Crit*100).ToString("0.#")+"% · 闪避 "+(personal.Dodge*100).ToString("0.#")+"%\n个人开战护盾 "+personal.Shield.ToString("0")+" · 开战怒气 "+personal.Rage.ToString("0")+"\n"+(profile.Team.Contains(i)?"编队开战护盾 "+team.Shield.ToString("0")+"\n当前队长 生/攻/防 +"+(team.LeaderHealth*100).ToString("0")+"% / +"+(team.LeaderAttack*100).ToString("0")+"% / +"+(team.LeaderDefense*100).ToString("0")+"%":"候补不计队长加成；上阵后按队长条件计算。"));
   if(!owned){DetailParagraph("获取后开始培养","图鉴展示初始形态与基础机制。获得卡片后可修炼、共鸣、装配和编队。");return;}
   int cost=30+profile.Training[i]/10;
   DetailParagraph("修炼 "+profile.Training[i]+" · 共鸣 "+profile.Resonance[i],"修炼每次增加基础属性0.1%；共鸣每次增加基础属性0.4%。速度不随这两项提升。培养资源保存在本张卡片上。");
   galleryTrainButton=UButton(detailBody,"TrainOnce","修炼一次 · "+cost+" 素材",0,detailY,260,42,()=>{
    var before=profile.InspectCharacter(i);
    if(Change(()=>{if(!profile.Train(i))throw new InvalidOperationException("素材不足");})){
     var after=profile.InspectCharacter(i);GalleryNotice("修炼完成 · 个人攻击 "+before.AttackValue.ToString("0.0")+" → "+after.AttackValue.ToString("0.0")+"，生命 "+before.MaxHP.ToString("0.0")+" → "+after.MaxHP.ToString("0.0"));RefreshGallery(false);
    }else GalleryNotice(notice,false);
   },true,profile.Materials>=cost&&saveError=="");
   UButton(detailBody,"Resonance","共鸣 · "+(3+profile.Resonance[i])+" 专属资源",278,detailY,260,42,()=>{
    if(Change(()=>{if(!profile.ResonanceUp(i))throw new InvalidOperationException("专属资源不足");})){GalleryNotice("共鸣提升 · 本卡基础属性 +0.4%");RefreshGallery(false);}else GalleryNotice(notice,false);
   },false,profile.Shards[i]>=3+profile.Resonance[i]&&saveError=="");detailY+=54;
   DetailParagraph("当前资源","素材 "+profile.Materials+" / "+cost+"；本卡专属资源 "+profile.Shards[i]+" / "+(3+profile.Resonance[i])+"。按钮变暗表示资源不足或存档不可写。",uiDim);
  }
  void DrawGallerySkills(int i){
   var c=Data.Cards[i];
   DetailParagraph("定位 · "+c.KeywordText,string.Join("\n",c.Keywords.Select(k=>k+"："+Data.KeywordDescription(k)).ToArray()));
   DetailParagraph("战术特点",TacticalSummary(c.Id,c.Role));
   DetailParagraph("普通攻击",Data.Reworked(c.Id)?Data.UserSkill(c.Id,0):"单体1.00倍攻击。占用一次正常行动；绝招冷却期间使用，完成出手获得16怒气。");
   bool replaced=profile.Owned[i]&&(profile.Skill[i]=="pierce"||profile.Skill2[i]=="pierce");
   DetailParagraph(replaced?"当前绝招 · 贯穿气弹":"绝招 · "+c.Special,replaced?"单体1.14倍攻击，优先最近后排，无后排时回落；冷却4次正常出手。完成出手获得28怒气。已替换原绝招，不叠加原绝招效果。":c.SpecialText+" 完成出手获得28怒气。");
   if(replaced){DetailParagraph("配置提醒","定位关键词概括固有技能组。原绝招已被替换，仅来自原绝招的能力当前不生效；必杀与被动保持。",uiAccent);DetailParagraph("原绝招 · 当前未装备",c.SpecialText,uiDim);}
   DetailParagraph("必杀 · "+c.Ultimate,c.UltimateText);
   DetailParagraph("被动",c.PassiveText);
   if(Data.Reworked(c.Id))DetailParagraph("数值与结算补充（代理默认）","伤害低/中/高：1.00/1.14/1.30倍攻击；强化低/中：4%/8%；护盾中：受益者最大生命12%；比克损血：自身最大生命10%，至少保留1生命；自然回复低：最大生命3% / 每次自身正常行动机会结束。未标时长整场。原技能文字保持用户原稿；同排含自身，最低生命按剩余百分比。悟空与比克联动仅要求队伍存在，不要求存活。");
   DetailParagraph("本卡队长技",c.LeaderText);
   var captain=Data.Cards[profile.Team[profile.Leader]];var matches=captain.Captain.Matches(c);
   string state=matches.Length==0?"不满足当前队长的任何标签条件。":"匹配「"+string.Join(" / ",matches)+"」；只获得一次加成。";
   DetailParagraph("当前队伍适配",captain.DisplayName+"\n"+state+"\n"+(profile.Team.Contains(i)?"当前已上阵。":"尚未上阵；匹配只表示上阵后可受益。")+"\n"+captain.Captain.Text);
   DetailParagraph("本卡担任队长时","对当前七人名单覆盖 "+profile.Team.Count(n=>c.Captain.Applies(Data.Cards[n]))+" / 7 人。此处仅检查标签，换人后按新名单重算。");
   DetailParagraph("回合与状态","持续回合按状态持有者的正常行动机会计数，额外必杀不推进时长。同名贡献各自计时；未标时长为整场。");
  }
  string TacticalSummary(string id,string role){
   if(id=="goku-test")return "自身攻击强化；编队中存在比克即可获得防御强化，不要求比克存活。";
   if(id=="vegeta-test")return "强化同排存活队友（含自身），必杀追击剩余生命百分比最低的敌人。";
   if(id=="piccolo-test")return "强化同排防御，以自身生命为代价保护低生命比例的两名队友，并在自身行动机会结束自然回复。";
   switch(Data.Kit(id)){
    case "goku":return "牵制目标，制造减防窗口。可以配合迎战拉蒂兹版本比克的魔贯光杀炮。";
    case "beam":return "目标防御强化合计为负时，必杀穿防更高。天津饭等减防来源可以提供窗口。";
    case "raditz":case "nappa":return "绝招覆盖最多两个不同目标，分散压迫前排；单个目标不会被同一招重复命中。";
    case "krillin":case "chiaotzu":return "利用眩晕打断正常行动。目标有连续眩晕保护，不能永久锁住敌人。";
    case "guard":return "自身护盾与低生命队友保护兼顾，适合承受前排压力。";
    case "yamcha":case "yajirobe":case "vegeta":return "具有后排攻击手段。阵位与敌方前后排构成会影响目标选择。";
    default:return "定位："+RoleText(role)+"。结合招式条件、队长覆盖与现有装备选择队伍位置。";
   }
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
   UButton(detailBody,"OpenTraining","前往养成与策略",0,detailY,538,42,()=>NavigateGallery("养成"));detailY+=58;
  }
 }
}

