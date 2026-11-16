using System;
using System.Linq;
namespace DragonIdle {
 // Identity is definition data. Equipment and save data cannot rewrite it.
 public sealed class Card {
  public readonly string Id,Name,Role,Tag,Special,Ultimate,Color,PersonId,Stage,Form,Title,Rarity,Release,LeaderTag,Passive;
  public readonly double HP,Attack,Defense,Speed,Leader;
  public readonly int Cooldown;
  readonly string[] tags;
  public string[] Tags {get{return (string[])tags.Clone();}}
  public string DisplayName {get{return Name+" · "+Title;}}
  public Card(string id,string name,string person,string stage,string form,string title,string rarity,string role,string race,string faction,
   string special,string ultimate,string color,double hp,double atk,double def,double speed,double leader,string leaderTag,string passive,int cd=3) {
   Id=id;Name=name;PersonId=person;Stage=stage;Form=form;Title=title;Rarity=rarity;Role=role;Tag=race;
   Special=special;Ultimate=ultimate;Color=color;HP=hp;Attack=atk;Defense=def;Speed=speed;Leader=leader;LeaderTag=leaderTag;Passive=passive;Cooldown=cd;
   Release=id.EndsWith("-test")?"0.1.0":"0.2.0";tags=new[]{race,faction,stage};
  }
  public bool HasTag(string tag){return tag=="全体"||tags.Contains(tag);}
  public string LeaderText {get{return "「"+Title+"」："+LeaderTag+"攻击 +"+(Leader*100).ToString("0")+"% / 整场，独立队长乘区";}}
  public string PassiveText {get{
   switch(Passive){
    case "guard":return "守势：开战获得生命上限 6% 护盾，持续至消耗完或战斗结束。";
    case "haste":return "先机：速度强化 +3% / 整场。";
    case "resolve":return "决意：开战获得 12 怒气。";
    case "focus":return "洞察：暴击率 +2% / 整场。";
    default:return "积蓄：攻击强化 +2% / 整场。";
   }
  }}
  public string SpecialText {get{
   string extra=Role=="sniper"?"优先最近后排，无后排时回落。":Role=="tank"?"自身获得生命上限 15% 护盾，至消耗完或战斗结束。":Role=="healer"?"治疗我方生命比例最低者 14% 最大生命。":Role=="control"?"命中后眩晕目标 1 次行动机会。":"";
   return Special+"：单体 1.14 倍攻击；"+extra+"冷却 "+Cooldown+" 次自身正常出手。";
  }}
  public string UltimateText {get{return Ultimate+"：单体 1.30 倍攻击，消耗 100 怒气；额外行动。自身攻击强化 +12% / 2 次自身行动机会。"+(Role=="support"?"其他队友攻击强化 +6% / 各自 2 次行动机会。":"");}}
 }
 public static partial class Data {
  // The first eight IDs are retained permanently for v0.1 saves; display names are never migration keys.
  public static readonly string[] LegacyCardIds={"goku-test","vegeta-test","piccolo-test","krillin-test","gohan-test","tien-test","dende-test","trunks-test"};
  public static readonly Card[] Cards={
   new Card("goku-test","孙悟空","goku","赛亚人篇","常态","重返战场","R","fighter","赛亚人","Z战士","龟派气功","全力冲击","FF9F43",1050,116,42,110,.08,"全体","attack"),
   new Card("vegeta-test","贝吉塔","vegeta","赛亚人篇","常态","孤傲王子","SR","sniper","赛亚人","赛亚人军团","伽力克炮","全力爆发","599AFF",940,124,36,116,.12,"赛亚人","focus",4),
   new Card("piccolo-test","比克","piccolo","赛亚人篇","常态","无声守护","R","tank","那美克星人","Z战士","魔光波","魔贯光杀炮","7BCF92",1340,90,65,89,.10,"Z战士","guard"),
   new Card("krillin-test","克林","krillin","赛亚人篇","常态","破局之光","R","control","地球人","Z战士","太阳拳","气圆斩","FFD580",950,95,40,106,.11,"地球人","haste",4),
   new Card("gohan-test","孙悟饭","gohan","赛亚人篇","常态","觉醒勇气","R","support","混血赛亚人","Z战士","连击","潜力爆发","BC9CFF",1010,104,44,102,.11,"混血赛亚人","resolve"),
   new Card("tien-test","天津饭","tien","赛亚人篇","常态","背水之阵","R","fighter","地球人","Z战士","洞洞波","气功炮","86D9DF",1020,111,46,99,.09,"Z战士","focus"),
   new Card("dende-test","丹迪","dende","那美克星篇","常态","生命的接力","R","healer","那美克星人","那美克星守护者","守护之光","祈愿之力","96E6AE",900,79,35,97,.13,"那美克星人","guard"),
   new Card("trunks-test","特兰克斯","trunks-future","人造人篇","超级赛亚人","未来来客","SSR","sniper","混血赛亚人","Z战士","闪光冲击","燃烧攻击","E897CB",1060,128,45,113,.12,"Z战士","focus"),
   new Card("yamcha-saiyan-v020","雅木茶","yamcha","赛亚人篇","常态","狼牙之志","R","fighter","地球人","Z战士","狼牙风风拳","操气弹","E4AA73",990,109,42,117,.12,"地球人","haste"),
   new Card("chiaotzu-saiyan-v020","饺子","chiaotzu","赛亚人篇","常态","坚定意志","R","control","地球人","Z战士","超能力束缚","全力念动","C7D7E7",930,88,43,112,.10,"地球人","resolve",4),
   new Card("roshi-tournament-v020","龟仙人","roshi","天下第一武道会篇","常态","武道传承","R","support","地球人","Z战士","残像拳","龟派气功","E9BD75",1080,98,51,93,.08,"全体","guard"),
   new Card("goku-saiyan-kaioken-v020","孙悟空","goku","赛亚人篇","界王拳","极限应战","SR","fighter","赛亚人","Z战士","界王拳突击","龟派气功","FF6870",980,125,35,119,.12,"赛亚人篇","resolve",4),
   new Card("goku-saiyan-guard-v020","孙悟空","goku","赛亚人篇","常态","同伴的希望","SR","support","赛亚人","Z战士","牵制连击","元气弹","FFC166",1110,108,49,102,.11,"Z战士","guard"),
   new Card("goku-namek-ssj-v020","孙悟空","goku","那美克星篇","超级赛亚人","传说觉醒","SSR","fighter","赛亚人","Z战士","疾风连击","龟派气功","FFDE6B",1100,126,45,111,.14,"赛亚人","attack"),
   new Card("vegeta-namek-v020","贝吉塔","vegeta","那美克星篇","常态","不屈野心","SR","sniper","赛亚人","独行战士","连续气弹","爆发冲击","779EFF",1000,119,42,111,.12,"那美克星篇","resolve",4),
   new Card("piccolo-namek-v020","比克","piccolo","那美克星篇","内鲁同化","守护者的意志","SR","tank","那美克星人","Z战士","激烈光弹","魔贯光杀炮","6FD098",1390,91,64,88,.14,"那美克星人","guard"),
   new Card("gohan-cell-ssj2-v020","孙悟饭","gohan","沙鲁篇","超级赛亚人2","觉醒的决意","SSR","fighter","混血赛亚人","Z战士","瞬身连击","龟派气功","D9ABFF",1090,129,43,109,.14,"混血赛亚人","resolve"),
   new Card("frieza-namek-final-v020","弗利萨","frieza","那美克星篇","最终形态","宇宙帝王","SSR","sniper","弗利萨一族","弗利萨军团","死亡光束","死亡能量弹","D99BF0",1080,127,48,110,.13,"那美克星篇","focus",4),
   new Card("android18-cell-v020","人造人18号","android18","人造人篇","常态","无限能量","SR","sniper","人造人","人造人","精准气弹","全力能量波","8DCBF2",1070,114,50,109,.12,"人造人篇","haste"),
   new Card("trunks-future-base-v020","特兰克斯","trunks-future","人造人篇","常态","归来的希望","SR","fighter","混血赛亚人","Z战士","剑击","燃烧攻击","DEA1CA",1040,114,45,114,.12,"混血赛亚人","haste")
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
