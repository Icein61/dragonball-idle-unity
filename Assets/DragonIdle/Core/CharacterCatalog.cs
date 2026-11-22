using System;
using System.Linq;
namespace DragonIdle {
 // Identity is definition data. Equipment and save data cannot rewrite it.
 public enum CharacterType { Output, Defense, Support }
 public sealed class Card {
  public readonly string Id,Name,Role,Tag,Special,Ultimate,Color,PersonId,Stage,Form,Title,Rarity,Release,Passive;
  public readonly double HP,Attack,Defense,Speed;
  public readonly int Cooldown;
  public readonly CharacterType Type;
  public readonly int CatalogNumber;
  public string TypeText {get{return Type==CharacterType.Output?"输出":Type==CharacterType.Defense?"防御":"援助";}}
  public readonly CharacterDesign Design;
  public string[] Keywords {get{return Design.Keywords;}}
  public string KeywordText {get{return string.Join(" / ",Keywords);}}
  public LeaderSkill Captain {get{return Design.Captain;}}
  readonly string[] tags;
  public string[] Tags {get{return (string[])tags.Clone();}}
  public string DisplayName {get{return Name+" · "+Title;}}
  public Card(string id,string name,string person,string stage,string form,string title,string rarity,string role,string race,string faction,
   string special,string ultimate,string color,double hp,double atk,double def,double speed,string passive,CharacterType type,int cd=3,string extraTag=null,int catalogNumber=0) {
   Id=id;Name=name;PersonId=person;Stage=stage;Form=form;Title=title;Rarity=rarity;Role=role;Tag=race;
   Special=special;Ultimate=ultimate;Color=color;HP=hp;Attack=atk;Defense=def;Speed=speed;Design=Data.DesignFor(id);Passive=passive;Cooldown=cd;Type=type;
   if(id=="goku-test")Special="蓄势连击";
   if(id=="vegeta-test")Ultimate="决胜强袭";
   if(id=="piccolo-test")Ultimate="舍身守护";
   Release=id.EndsWith("-v030")?"0.3.0":id.EndsWith("-test")?"0.1.0":"0.2.0";tags=extraTag==null?new[]{race,faction,stage}:new[]{race,faction,stage,extraTag};
   CatalogNumber=catalogNumber;
   RoundSheetCard sheet;
   if(RoundSheetCatalog.Cards.TryGetValue(id,out sheet)){
    Name=sheet.Name;Title=sheet.Title;Rarity=sheet.Rarity;Type=sheet.Type;CatalogNumber=sheet.Number;tags=sheet.Tags;
    Tag=tags.FirstOrDefault(label=>label=="赛亚人"||label=="混血赛亚人"||label=="地球人"||label=="那美克星人")??race;
   }
   Captain.ValidateOwner(tags);
  }
  public bool HasTag(string tag){return tag=="全体"||tags.Contains(tag);}
  public string LeaderText {get{return "「"+Title+"」："+Captain.Text;}}
  public string PassiveText {get{
   if(Data.Reworked(Id))return Data.UserSkill(Id,3);
   switch(Passive){
    case "guard":return "守势：开战获得生命上限 6% 护盾，持续至消耗完或战斗结束。";
    case "haste":return "先机：速度强化 +3% / 整场。";
    case "resolve":return "决意：开战获得 12 怒气。";
    case "focus":return "洞察：暴击率 +2% / 整场。";
    default:return "积蓄：攻击强化 +2% / 整场。";
   }
  }}
  public string SpecialText {get{
   if(Data.Reworked(Id))return Data.UserSkill(Id,1);
   var kit=Data.KitText(Id,false);if(kit!=null)return Special+"："+kit;
   string extra=Role=="sniper"?"优先最近后排，无后排时回落。":Role=="tank"?"自身获得生命上限 15% 护盾，至消耗完或战斗结束。":Role=="healer"?"治疗我方生命比例最低者 14% 最大生命。":Role=="control"?"命中后眩晕目标 1 次行动机会。":"";
   return Special+"：单体 1.14 倍攻击；"+extra+"冷却 "+Cooldown+" 次自身正常出手。";
  }}
  public string UltimateText {get{if(Data.Reworked(Id))return Data.UserSkill(Id,2);var kit=Data.KitText(Id,true);if(kit!=null)return Ultimate+"："+kit;return Ultimate+"：单体 1.30 倍攻击，消耗 100 怒气；额外行动。自身攻击强化 +12% / 2 次自身行动机会。"+(Role=="support"?"其他队友攻击强化 +6% / 各自 2 次行动机会。":"");}}
 }
 public static partial class Data {
  public const string RivalTag="劲敌",LowerClassTag="下级战士";
  internal const string RaditzId="raditz-arrival-v030";
  public const string YamchaId="yamcha-saiyan-v020";
  internal const string TienId="tien-test";
  public static readonly System.Collections.ObjectModel.ReadOnlyCollection<string> RoundRoster=
   RoundSheetCatalog.Online;
  public static bool IsRoundCard(string id){return RoundRoster.Contains(id);}
  public static string[] RoundIdentityTags(){
   return RoundRoster.Select(id=>Card(id)).SelectMany(card=>card.Tags).Distinct().ToArray();
  }
  // Historical IDs only support migration; removed cards have no live definition.
  public static readonly string[] LegacyCardIds={"goku-test","vegeta-test","piccolo-test","krillin-test","gohan-test","tien-test","dende-test","trunks-test"};
  public static readonly string[] PreviousCardIds={"goku-test","vegeta-test","piccolo-test","krillin-test","gohan-test","tien-test","dende-test","trunks-test","yamcha-saiyan-v020","chiaotzu-saiyan-v020","roshi-tournament-v020","goku-saiyan-kaioken-v020","goku-saiyan-guard-v020","goku-namek-ssj-v020","vegeta-namek-v020","piccolo-namek-v020","gohan-cell-ssj2-v020","frieza-namek-final-v020","android18-cell-v020","trunks-future-base-v020","piccolo-raditz-v030","raditz-arrival-v030","nappa-invasion-v030","yajirobe-saiyan-v030","saibaman-saiyan-v030"};
  public static readonly string[] RetiredCardIds={"dende-test","trunks-test","roshi-tournament-v020","goku-namek-ssj-v020","vegeta-namek-v020","piccolo-namek-v020","gohan-cell-ssj2-v020","frieza-namek-final-v020","android18-cell-v020","trunks-future-base-v020","goku-saiyan-guard-v020","piccolo-raditz-v030","nappa-invasion-v030","yajirobe-saiyan-v030","saibaman-saiyan-v030"};
  public static readonly string[] StarterCardIds={"goku-test","vegeta-test","piccolo-test","krillin-test","gohan-test","tien-test","yamcha-saiyan-v020","chiaotzu-saiyan-v020",RaditzId};
  public static readonly Card[] Cards={
   new Card("goku-test","孙悟空","goku","赛亚人篇","常态","全新的故事","B","fighter","赛亚人","Z战士","牵制连击","全力龟派气功","FF9F43",1050,116,42,110,"attack",CharacterType.Output,extraTag:LowerClassTag,catalogNumber:1),
   new Card("vegeta-test","贝吉塔","vegeta","赛亚人篇","常态","激烈的超级精英","B","sniper","赛亚人","赛亚人军团","伽力克炮","全力爆发","599AFF",940,124,36,116,"focus",CharacterType.Output,extraTag:RivalTag,catalogNumber:2),
   new Card("piccolo-test","比克","piccolo","赛亚人篇","常态","困境中的突破口","B","tank","那美克星人","Z战士","魔光波","魔贯光杀炮","7BCF92",1340,90,65,89,"guard",CharacterType.Output,catalogNumber:3),
   new Card("krillin-test","克林","krillin","赛亚人篇","常态","改变战况的好手","C","control","地球人","Z战士","太阳拳","气圆斩","FFD580",950,95,40,106,"haste",CharacterType.Support,catalogNumber:5),
   new Card("gohan-test","孙悟饭【幼年】","gohan","赛亚人篇","常态","蕴藏的可能性","C","support","混血赛亚人","Z战士","连击","潜力爆发","BC9CFF",1010,104,44,102,"resolve",CharacterType.Defense,catalogNumber:4),
   new Card("tien-test","天津饭","tien","赛亚人篇","常态","取胜的气势","C","fighter","地球人","Z战士","洞洞波","气功炮","86D9DF",1020,111,46,99,"focus",CharacterType.Output),
   new Card("yamcha-saiyan-v020","雅木茶","yamcha","赛亚人篇","常态","地球顶尖的尖锐","C","fighter","地球人","Z战士","狼牙风风拳","操气弹","E4AA73",990,109,42,117,"haste",CharacterType.Output),
   new Card("chiaotzu-saiyan-v020","饺子","chiaotzu","赛亚人篇","常态","各式各样的拿手绝活","C","control","地球人","Z战士","超能力束缚","全力念动","C7D7E7",930,88,43,112,"resolve",CharacterType.Support),
   new Card("goku-saiyan-kaioken-v020","孙悟空【界王拳】","goku","赛亚人篇","界王拳","挑战极限","A","fighter","赛亚人","Z战士","界王拳突击","龟派气功","FF6870",980,125,35,119,"resolve",CharacterType.Output),
   new Card(RaditzId,"拉蒂兹","raditz","赛亚人篇","常态","天外来敌","C","fighter","赛亚人","赛亚人军团","闪耀金星日","双重太阳日","BD83C8",960,110,43,100,"attack",CharacterType.Output,extraTag:LowerClassTag,catalogNumber:6),
  };
  public static int Index(string id){return Array.FindIndex(Cards,x=>x.Id==id);}
  public static string[] Rarities={"C","B","A","S"};
  public static string[] Roles={"fighter","tank","healer","support","control","sniper"};
  public static string[] SkillIds={"","focus","pierce","guard"};
  public static string SkillName(string id){return id=="focus"?"专注":id=="pierce"?"贯穿气弹":id=="guard"?"守势训练":"空";}
  public static string SkillDescription(string id){return id=="focus"?"暴击率 +3.5% / 整场":id=="pierce"?"替换绝招，优先后排，冷却 4 次正常出手":id=="guard"?"开战获得 4% 最大生命护盾，至消耗完或战斗结束":"卸下当前技能";}
  public static string[] LootDescriptions={"护腕 92% / 核心 8%；珠子 20%；专注残页 25%","战斗服 92% / 核心 8%；珠子 20%；守势残页 25%","护腕 46% / 战斗服 46% / 核心 8%；珠子 20%；贯穿残页 25%","护腕 44% / 战斗服 44% / 核心 12%；珠子 20%；随机残页 25%"};
 }
}
