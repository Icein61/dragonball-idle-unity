using System;
using System.Linq;
namespace DragonIdle {
 // Numeric effects reuse existing attack/defense/speed categories. No new multiplier.
 public static partial class Data {
  public static string Kit(string id) {
   switch(id) {
    case "goku-test":return "goku";
    case "piccolo-raditz-v030":return "beam";
    case "raditz-arrival-v030":return "raditz";
    case "vegeta-test":return "vegeta";
    case "piccolo-test":return "guard";
    case "krillin-test":return "krillin";
    case "gohan-test":return "gohan";
    case "tien-test":return "tien";
    case "yamcha-saiyan-v020":return "yamcha";
    case "chiaotzu-saiyan-v020":return "chiaotzu";
    case "goku-saiyan-kaioken-v020":return "kaioken";
    case "nappa-invasion-v030":return "nappa";
    case "yajirobe-saiyan-v030":return "yajirobe";
    case "saibaman-saiyan-v030":return "saibaman";
    default:return "";
   }
  }
  public static string KitText(string id,bool ultimate) {
   string k=Kit(id);if(k=="")return null;
   string body="";
   if(ultimate) switch(k) {
    case "goku":body="单体1.22倍攻击；命中使目标防御强化 -8% / 2回合。";break;
    case "beam":body="单体1.22倍攻击；无视10%防御，目标防御强化为负时改为25%。";break;
    case "raditz":body="单体1.24倍攻击；自身速度强化 +4% / 2回合。";break;
    case "vegeta":body="单体1.26倍攻击，无视20%防御。";break;
    case "guard":body="单体1.18倍攻击；生命比例最低队友获得8%最大生命护盾，至耗尽或战斗结束。";break;
    case "krillin":body="单体1.12倍攻击，无视35%防御。";break;
    case "gohan":body="单体1.18倍攻击；自身攻击强化 +12%、其他队友 +6% / 各自2回合。";break;
    case "tien":body="单体1.22倍攻击，无视15%防御。";break;
    case "yamcha":body="单体1.22倍攻击，优先后排，无后排回落。";break;
    case "chiaotzu":body="单体1.12倍攻击；命中使目标速度强化 -8% / 2回合。";break;
    case "kaioken":body="单体1.28倍攻击；自身攻击强化 +4% / 2回合。";break;
    case "nappa":body="单体1.22倍攻击；自身获得8%最大生命护盾，至耗尽或战斗结束。";break;
    case "yajirobe":body="单体1.12倍攻击，无视30%防御。";break;
    case "saibaman":body="单体1.22倍攻击。";break;
   } else switch(k) {
    case "goku":body="单体1.08倍攻击；命中使目标防御强化 -8% / 2回合。";break;
    case "beam":body="单体1.08倍攻击；自身攻击强化 +4% / 2回合。";break;
    case "raditz":body="最近前排优先的最多2个不同目标，各0.67倍攻击；各自判暴击/闪避。";break;
    case "vegeta":body="单体1.12倍攻击，优先后排；自身攻击强化 +4% / 2回合。";break;
    case "guard":body="单体1.00倍攻击；自身获得12%最大生命护盾，至耗尽或战斗结束。";break;
    case "krillin":case "chiaotzu":body="单体0.85倍攻击；命中眩晕目标1回合。";break;
    case "gohan":body="单体1.12倍攻击；额外获得6怒气。";break;
    case "tien":body="单体1.12倍攻击；命中使目标防御强化 -6% / 2回合。";break;
    case "yamcha":body="单体1.10倍攻击；自身速度强化 +5% / 2回合。";break;
    case "kaioken":body="单体1.15倍攻击；自身速度强化 +5% / 2回合。";break;
    case "nappa":body="最近前排优先的最多2个不同目标，各0.60倍攻击；各自判暴击/闪避。";break;
    case "yajirobe":body="单体1.10倍攻击，优先后排，无后排回落。";break;
    case "saibaman":body="单体0.95倍攻击；命中使目标速度强化 -6% / 2回合。";break;
   }
   return body+(ultimate?"消耗100怒气，额外行动。":"冷却"+Card(id).Cooldown+"次正常出手。");
  }
 }
 public partial class Battle {
  double KitPower(string k,Move move) {
   if(move==Move.Basic)return 1;
   if(move==Move.Ultimate) {
    switch(k){case "krillin":case "chiaotzu":case "yajirobe":return 1.12;case "guard":case "gohan":return 1.18;case "raditz":return 1.24;case "vegeta":return 1.26;case "kaioken":return 1.28;default:return 1.22;}
   }
   switch(k){case "goku":case "beam":return 1.08;case "raditz":return .67;case "nappa":return .60;case "guard":return 1;case "krillin":case "chiaotzu":return .85;case "yamcha":case "yajirobe":return 1.10;case "kaioken":return 1.15;case "saibaman":return .95;default:return 1.12;}
  }
  double KitPenetration(string k,Unit target,Move move) {
   if(move!=Move.Ultimate)return 0;
   switch(k){case "beam":return target.Sum("defense")<0?.25:.10;case "vegeta":return .20;case "krillin":return .35;case "tien":return .15;case "yajirobe":return .30;default:return 0;}
  }
  string KitEffects(string k,Unit actor,Unit target,Move move,bool hit) {
   string extra="";if(move==Move.Basic)return extra;
   if(k=="goku"&&hit){target.Add("defense",-.08,2,actor.CardId+":opening");extra=" · 牵制：防御 -8% / 2回合";}
   if(move==Move.Special) {
    if(k=="beam"||k=="vegeta"){actor.Add("attack",.04,2,actor.CardId+":special");extra=" · 攻击 +4% / 2回合";}
    if(k=="guard"){actor.Shield+=actor.MaxHP*.12;extra=" · 12%生命护盾 / 至耗尽";}
    if((k=="krillin"||k=="chiaotzu")&&hit){target.Stun(1);extra=" · 眩晕1回合（受连续眩晕保护限制）";}
    if(k=="gohan"){actor.Rage=Math.Min(100,actor.Rage+6);extra=" · 怒气 +6";}
    if(k=="tien"&&hit){target.Add("defense",-.06,2,actor.CardId+":special");extra=" · 防御 -6% / 2回合";}
    if(k=="yamcha"||k=="kaioken"){actor.Add("speed",.05,2,actor.CardId+":special");extra=" · 速度 +5% / 2回合";}
    if(k=="saibaman"&&hit){target.Add("speed",-.06,2,actor.CardId+":special");extra=" · 速度 -6% / 2回合";}
   } else {
    if(k=="raditz"){actor.Add("speed",.04,2,actor.CardId+":ultimate");extra=" · 速度 +4% / 2回合";}
    if(k=="guard"){var ally=Units.Where(x=>x.Side==actor.Side&&x.Alive).OrderBy(x=>x.HP/x.MaxHP).ThenBy(x=>x.Slot).First();ally.Shield+=ally.MaxHP*.08;extra=" · "+ally.Name+" 护盾8% / 至耗尽";}
    if(k=="gohan"){actor.Add("attack",.12,2,"ultimate");foreach(var ally in Units.Where(x=>x.Side==actor.Side&&x.Alive&&x!=actor))ally.Add("attack",.06,2,actor.CardId);extra=" · 自身攻击 +12%、队友 +6% / 各自2回合";}
    if(k=="chiaotzu"&&hit){target.Add("speed",-.08,2,actor.CardId+":ultimate");extra=" · 速度 -8% / 2回合";}
    if(k=="kaioken"){actor.Add("attack",.04,2,actor.CardId+":ultimate");extra=" · 攻击 +4% / 2回合";}
    if(k=="nappa"){actor.Shield+=actor.MaxHP*.08;extra=" · 8%生命护盾 / 至耗尽";}
    if(k=="beam"||k=="vegeta"||k=="krillin"||k=="tien"||k=="yajirobe")extra=" · 穿透 "+(KitPenetration(k,target,move)*100).ToString("0")+"% 防御";
   }
   return extra;
  }
 }
}
