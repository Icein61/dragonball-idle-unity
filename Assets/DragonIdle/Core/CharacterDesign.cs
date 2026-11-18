using System;
using System.Linq;
namespace DragonIdle {
 public sealed class LeaderSkill {
  readonly string[] tags;
  public readonly double Health,Attack,Defense;
  public string[] Tags {get{return (string[])tags.Clone();}}
  public LeaderSkill(string[] eligible,double health,double attack,double defense){
   tags=(string[])eligible.Clone();Health=health;Attack=attack;Defense=defense;
  }
  public void ValidateOwner(string[] identity){
   if(tags.Length==0||tags.Distinct().Count()!=tags.Length||tags.Any(t=>string.IsNullOrEmpty(t)||!identity.Contains(t)))throw new ArgumentException("队长技只能使用队长自身拥有的身份标签，不得为空或重复");
  }
  public string[] Matches(Card card){return tags.Where(card.HasTag).ToArray();}
  public bool Applies(Card card){return tags.Any(card.HasTag);}
  public string Bonuses {get{return "生命 +"+(Health*100).ToString("0.#")+"% / 攻击 +"+(Attack*100).ToString("0.#")+"% / 防御 +"+(Defense*100).ToString("0.#")+"%";}}
  public string Text {get{return "属于「"+string.Join("」或「",tags)+(tags.Length==1?"」标签的角色：":"」任一标签的角色：")+Bonuses+"。持续整场；"+(tags.Length==1?"":"命中多个标签仅生效一次，")+"各属性使用独立队长乘区。";}}
 }
 public sealed class CharacterDesign {
  readonly string[] keywords;
  public string[] Keywords {get{return (string[])keywords.Clone();}}
  public readonly LeaderSkill Captain;
  public CharacterDesign(string words,string tags,double health,double attack,double defense){keywords=words.Split('/');Captain=new LeaderSkill(tags.Split('/'),health,attack,defense);}
 }
 public static partial class Data {
  public static CharacterDesign DesignFor(string id){
   switch(id){
    case "goku-test":return new CharacterDesign("自我强化/协同防御","Z战士/赛亚人",.04,.06,.04);
    case "vegeta-test":return new CharacterDesign("团队强化/低血追击","赛亚人/赛亚人军团",.03,.08,.03);
    case "piccolo-test":return new CharacterDesign("团队防御/护盾/自然回复","Z战士/那美克星人",.06,.03,.06);
    case "krillin-test":return new CharacterDesign("前排群攻/先手","地球人",.04,.05,.04);
    case "gohan-test":return new CharacterDesign("怒气汲取/高攻追击/自我强化","混血赛亚人",.05,.06,.03);
    case "tien-test":return new CharacterDesign("生命消耗/自我强化","地球人/Z战士",.03,.06,.05);
    case "yamcha-saiyan-v020":return new CharacterDesign("编队联动/自我强化","地球人",.04,.05,.03);
    case "chiaotzu-saiyan-v020":return new CharacterDesign("团队强化/团队防御","地球人/Z战士",.05,.03,.04);
    case "goku-saiyan-kaioken-v020":return new CharacterDesign("生命消耗/生命偷取/自我强化","赛亚人/Z战士",.03,.08,.04);
    default:throw new ArgumentException("Missing character design: "+id);
   }
  }
  public static string KeywordDescription(string word){
   switch(word){
    case "协同防御":return "队伍中存在任一比克卡时获得整场防御强化，不要求比克存活。";
    case "低血追击":return "必杀选择剩余生命百分比最低的存活敌人。";
    case "团队防御":return "为符合本卡技能条件的队友施加防御强化。";
    case "前排群攻":return "必杀同时攻击敌方存活前排。";
    case "怒气汲取":return "绝招命中后转移目标的部分怒气。";
    case "高攻追击":return "必杀优先攻击有效攻击最高的敌人。";
    case "生命消耗":return "发动指定招式时扣除自身部分最大生命，至少保留 1 点。";
    case "生命偷取":return "按造成的实际生命伤害回复自身。";
    case "编队联动":return "开战时按上阵身份人数获得常驻强化。";
    case "自然回复":return "自身正常行动机会结束回复生命，眩晕跳过也触发，额外必杀不触发。";
    case "破防":return "命中后降低目标防御，为后续攻击制造窗口。";
    case "穿防":return "招式忽略部分目标防御，不等同于施加减防状态。";
    case "破绽穿透":return "魔贯光杀炮在目标防御强化为负时获得更高穿防比例。";
    case "自我强化":return "固有招式或被动提高自身攻击，数值与时长见对应效果。";
    case "后排突破":return "有优先攻击后排的招式；没有合法后排时按规则回落。";
    case "护盾":return "能为自身提供吸收伤害的护盾。";
    case "援护":return "能为生命比例最低的队友提供护盾。";
    case "控制":return "招式可施加眩晕，受命中与连续眩晕保护规则限制。";
    case "减速":return "命中后降低目标推进速度。";
    case "先手":return "开战即有整场速度强化，不代表必定第一个行动。";
    case "加速":return "招式提供限时速度强化。";
    case "怒气加速":return "开战怒气或招式额外怒气帮助更快达到必杀条件。";
    case "团队强化":return "为符合本卡技能条件的队友施加攻击强化。";
    case "双目标":return "绝招最多攻击两个不同目标，各自判定命中与暴击。";
    default:throw new ArgumentException("Unknown keyword: "+word);
   }
  }
 }
}
