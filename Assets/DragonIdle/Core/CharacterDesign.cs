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
  public string[] Matches(Card card){return tags.Where(card.HasTag).ToArray();}
  public bool Applies(Card card){return tags.Any(card.HasTag);}
  public string Bonuses {get{return "生命 +"+(Health*100).ToString("0.#")+"% / 攻击 +"+(Attack*100).ToString("0.#")+"% / 防御 +"+(Defense*100).ToString("0.#")+"%";}}
  public string Text {get{return "属于「"+string.Join("」或「",tags)+"」任一标签的角色："+Bonuses+"。持续整场；命中多个标签仅生效一次，各属性使用独立队长乘区。";}}
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
    case "goku-test":return new CharacterDesign("破防/自我强化","Z战士/赛亚人",.04,.06,.04);
    case "vegeta-test":return new CharacterDesign("后排突破/穿防/自我强化","赛亚人/赛亚人军团",.03,.08,.03);
    case "piccolo-test":return new CharacterDesign("护盾/援护","Z战士/那美克星人",.06,.03,.06);
    case "krillin-test":return new CharacterDesign("控制/穿防/先手","地球人/混血赛亚人",.04,.05,.04);
    case "gohan-test":return new CharacterDesign("怒气加速/团队强化","混血赛亚人/那美克星人",.05,.06,.03);
    case "tien-test":return new CharacterDesign("破防/穿防","地球人/Z战士",.03,.06,.05);
    case "yamcha-saiyan-v020":return new CharacterDesign("加速/后排突破","地球人/地球援军",.04,.05,.03);
    case "chiaotzu-saiyan-v020":return new CharacterDesign("控制/减速","地球人/Z战士",.05,.03,.04);
    case "goku-saiyan-kaioken-v020":return new CharacterDesign("加速/自我强化/怒气加速","赛亚人/Z战士",.03,.08,.04);
    case "goku-saiyan-guard-v020":return new CharacterDesign("护盾/团队强化","Z战士/地球援军",.05,.04,.05);
    case "piccolo-raditz-v030":return new CharacterDesign("破绽穿透/自我强化","那美克星人/赛亚人",.03,.07,.04);
    case "raditz-arrival-v030":return new CharacterDesign("双目标/加速/怒气加速","赛亚人/赛亚人军团",.04,.06,.04);
    case "nappa-invasion-v030":return new CharacterDesign("双目标/护盾","赛亚人军团/栽培生物",.06,.05,.04);
    case "yajirobe-saiyan-v030":return new CharacterDesign("后排突破/穿防","地球人/地球援军",.03,.06,.03);
    case "saibaman-saiyan-v030":return new CharacterDesign("减速/先手","栽培生物/赛亚人军团",.04,.04,.04);
    default:throw new ArgumentException("Missing character design: "+id);
   }
  }
  public static string KeywordDescription(string word){
   switch(word){
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
    case "团队强化":return "必杀能提高其他队友攻击。";
    case "双目标":return "绝招最多攻击两个不同目标，各自判定命中与暴击。";
    default:throw new ArgumentException("Unknown keyword: "+word);
   }
  }
 }
}
