using System;
using System.Linq;
namespace DragonIdle {
 // Identity is definition data. Equipment and save data cannot rewrite it.
 public sealed class Card {
  public readonly string Id,Name,Role,Tag,Special,Ultimate,Color,PersonId,Stage,Form,Title,Rarity,Release,Passive;
  public readonly double HP,Attack,Defense,Speed;
  public readonly int Cooldown;
  public readonly CharacterDesign Design;
  public string[] Keywords {get{return Design.Keywords;}}
  public string KeywordText {get{return string.Join(" / ",Keywords);}}
  public LeaderSkill Captain {get{return Design.Captain;}}
  readonly string[] tags;
  public string[] Tags {get{return (string[])tags.Clone();}}
  public string DisplayName {get{return Name+" · "+Title;}}
  public Card(string id,string name,string person,string stage,string form,string title,string rarity,string role,string race,string faction,
   string special,string ultimate,string color,double hp,double atk,double def,double speed,string passive,int cd=3) {
   Id=id;Name=name;PersonId=person;Stage=stage;Form=form;Title=title;Rarity=rarity;Role=role;Tag=race;
   Special=special;Ultimate=ultimate;Color=color;HP=hp;Attack=atk;Defense=def;Speed=speed;Design=Data.DesignFor(id);Passive=passive;Cooldown=cd;
   if(id=="goku-test")Special="蓄势连击";
   if(id=="vegeta-test")Ultimate="决胜强袭";
   if(id=="piccolo-test")Ultimate="舍身守护";
   Release=id.EndsWith("-v030")?"0.3.0":id.EndsWith("-test")?"0.1.0":"0.2.0";tags=new[]{race,faction,stage};
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
  // Historical IDs only support migration; removed cards have no live definition.
  public static readonly string[] LegacyCardIds={"goku-test","vegeta-test","piccolo-test","krillin-test","gohan-test","tien-test","dende-test","trunks-test"};
  public static readonly string[] PreviousCardIds={"goku-test","vegeta-test","piccolo-test","krillin-test","gohan-test","tien-test","dende-test","trunks-test","yamcha-saiyan-v020","chiaotzu-saiyan-v020","roshi-tournament-v020","goku-saiyan-kaioken-v020","goku-saiyan-guard-v020","goku-namek-ssj-v020","vegeta-namek-v020","piccolo-namek-v020","gohan-cell-ssj2-v020","frieza-namek-final-v020","android18-cell-v020","trunks-future-base-v020","piccolo-raditz-v030","raditz-arrival-v030","nappa-invasion-v030","yajirobe-saiyan-v030","saibaman-saiyan-v030"};
  public static readonly string[] RetiredCardIds={"dende-test","trunks-test","roshi-tournament-v020","goku-namek-ssj-v020","vegeta-namek-v020","piccolo-namek-v020","gohan-cell-ssj2-v020","frieza-namek-final-v020","android18-cell-v020","trunks-future-base-v020"};
  public static readonly string[] StarterCardIds={"goku-test","vegeta-test","piccolo-test","krillin-test","gohan-test","tien-test","yamcha-saiyan-v020","chiaotzu-saiyan-v020"};
  public static readonly Card[] Cards={
   new Card("goku-test","孙悟空","goku","赛亚人篇","常态","重返战场","SR","fighter","赛亚人","Z战士","牵制连击","全力龟派气功","FF9F43",1050,116,42,110,"attack"),
   new Card("vegeta-test","贝吉塔","vegeta","赛亚人篇","常态","孤傲王子","SSR","sniper","赛亚人","赛亚人军团","伽力克炮","全力爆发","599AFF",940,124,36,116,"focus",4),
   new Card("piccolo-test","比克","piccolo","赛亚人篇","常态","无声守护","SSR","tank","那美克星人","Z战士","魔光波","魔贯光杀炮","7BCF92",1340,90,65,89,"guard"),
   new Card("krillin-test","克林","krillin","赛亚人篇","常态","破局之光","SR","control","地球人","Z战士","太阳拳","气圆斩","FFD580",950,95,40,106,"haste",4),
   new Card("gohan-test","孙悟饭","gohan","赛亚人篇","常态","觉醒勇气","SR","support","混血赛亚人","Z战士","连击","潜力爆发","BC9CFF",1010,104,44,102,"resolve"),
   new Card("tien-test","天津饭","tien","赛亚人篇","常态","背水之阵","SR","fighter","地球人","Z战士","洞洞波","气功炮","86D9DF",1020,111,46,99,"focus"),
   new Card("yamcha-saiyan-v020","雅木茶","yamcha","赛亚人篇","常态","狼牙之志","R","fighter","地球人","Z战士","狼牙风风拳","操气弹","E4AA73",990,109,42,117,"haste"),
   new Card("chiaotzu-saiyan-v020","饺子","chiaotzu","赛亚人篇","常态","坚定意志","R","control","地球人","Z战士","超能力束缚","全力念动","C7D7E7",930,88,43,112,"resolve",4),
   new Card("goku-saiyan-kaioken-v020","孙悟空","goku","赛亚人篇","界王拳","极限应战","SSR","fighter","赛亚人","Z战士","界王拳突击","龟派气功","FF6870",980,125,35,119,"resolve",4),
   new Card("goku-saiyan-guard-v020","孙悟空","goku","赛亚人篇","常态","同伴的希望","SR","support","赛亚人","Z战士","牵制连击","元气弹","FFC166",1110,108,49,102,"guard"),
   new Card("piccolo-raditz-v030","比克","piccolo","赛亚人篇","常态","迎战拉蒂兹","SR","fighter","那美克星人","Z战士","蓄气光弹","魔贯光杀炮","8DC979",1040,118,44,101,"focus",4),
   new Card("raditz-arrival-v030","拉蒂兹","raditz","赛亚人篇","常态","天外来敌","SR","fighter","赛亚人","赛亚人军团","双手能量波","周末强袭","C8A77D",1120,115,46,108,"resolve",4),
   new Card("nappa-invasion-v030","那巴","nappa","赛亚人篇","常态","凶悍侵袭","SSR","tank","赛亚人","赛亚人军团","爆破波","破坏冲击","E8B15F",1300,110,57,92,"guard",4),
   new Card("yajirobe-saiyan-v030","亚奇洛贝","yajirobe","赛亚人篇","常态","关键一刀","R","sniper","地球人","地球援军","迂回斩击","决意斩","D09658",990,105,42,104,"focus",4),
   new Card("saibaman-saiyan-v030","栽培人","saibaman","赛亚人篇","常态","潜伏爪牙","R","control","栽培生物","赛亚人军团","酸液突袭","扑击","95B862",950,98,39,115,"haste")
  };
  public static int Index(string id){return Array.FindIndex(Cards,x=>x.Id==id);}
  public static string[] Rarities={"R","SR","SSR"};
  public static string[] Roles={"fighter","tank","healer","support","control","sniper"};
  public static string[] SkillIds={"","focus","pierce","guard"};
  public static string SkillName(string id){return id=="focus"?"专注":id=="pierce"?"贯穿气弹":id=="guard"?"守势训练":"空";}
  public static string SkillDescription(string id){return id=="focus"?"暴击率 +3.5% / 整场":id=="pierce"?"替换绝招，优先后排，冷却 4 次正常出手":id=="guard"?"开战获得 4% 最大生命护盾，至消耗完或战斗结束":"卸下当前技能";}
  public static string[] LootDescriptions={"护腕 92% / 核心 8%；珠子 20%；专注残页 25%","战斗服 92% / 核心 8%；珠子 20%；守势残页 25%","护腕 46% / 战斗服 46% / 核心 8%；珠子 20%；贯穿残页 25%","护腕 44% / 战斗服 44% / 核心 12%；珠子 20%；随机残页 25%"};
 }
}
