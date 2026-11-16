using System;
using System.Linq;
using System.Collections.Generic;
namespace DragonIdle {
 public static partial class Data {
  // Adjustable budgets; user wording is retained separately, verbatim.
  public const double ReworkLow=.04,ReworkMedium=.08,ReworkShield=.12,ReworkCost=.10,ReworkRegen=.03;
  public static bool Reworked(string id){return id=="goku-test"||id=="vegeta-test"||id=="piccolo-test";}
  public static string UserSkill(string id,int move){
   if(id=="goku-test")return new[]{"对正面敌方造成低伤害","对正面敌方造成中伤害，对自身施加【攻击强化】低，持续2回合","对正面敌方造成高伤害","【攻击强化】低 / 己方存在【比克】时，【防御强化】中"}[move];
   if(id=="vegeta-test")return new[]{"对正面敌人造成低伤害","对正面敌人造成中伤害，对己方同排队友施加【攻击强化】低，持续2回合","对生命最低敌方造成高伤害","【攻击强化】中 / 【防御强化】中"}[move];
   if(id=="piccolo-test")return new[]{"对正面敌人造成低伤害","对正面敌人造成中伤害，对己方同排队友施加【防御强化】低，持续2回合","损失中生命，对己方生命最低两人释放【护盾】中","【防御强化】中 / 【自然回复】低"}[move];
   return null;
  }
  public static void ReworkPassive(Unit u){
   if(u.CardId=="goku-test")u.Add("attack",ReworkLow,0,"rework:base");
   if(u.CardId=="vegeta-test"){u.Add("attack",ReworkMedium,0,"rework:base");u.Add("defense",ReworkMedium,0,"rework:base");}
   if(u.CardId=="piccolo-test")u.Add("defense",ReworkMedium,0,"rework:base");
  }
  public static void GokuLink(Unit u,IEnumerable<string> team){
   if(u.CardId=="goku-test"&&!u.Effects.Any(x=>x.Source=="rework:piccolo-link")&&team.Any(id=>Index(id)>=0&&Card(id).PersonId=="piccolo"))u.Add("defense",ReworkMedium,0,"rework:piccolo-link");
  }
 }
 public partial class Battle {
  BattleEvent ReworkAct(Unit actor,Move move){
   var e=new BattleEvent{Actor=actor,Move=move,Time=Time};
   if(actor.CardId=="piccolo-test"&&move==Move.Ultimate){
    var allies=Units.Where(u=>u.Side==actor.Side&&u.Alive).OrderBy(u=>u.HP/u.MaxHP).ThenBy(u=>u.Slot).Take(2).ToList();
    e.HealthCost=Math.Min(Math.Max(0,actor.HP-1),actor.MaxHP*Data.ReworkCost);actor.HP-=e.HealthCost;
    foreach(var ally in allies){ally.Shield+=ally.MaxHP*Data.ReworkShield;e.Recipients.Add(ally);}
    e.Target=allies.FirstOrDefault();e.Text=actor.Name+" · "+actor.UltimateName+" · 生命代价 "+e.HealthCost.ToString("0")+" → "+string.Join(" / ",allies.Select(u=>u.Name+" 护盾 +12%最大生命").ToArray());return e;
   }
   var target=actor.CardId=="vegeta-test"&&move==Move.Ultimate?Units.Where(u=>u.Side!=actor.Side&&u.Alive).OrderBy(u=>u.HP/u.MaxHP).ThenBy(u=>u.Slot).FirstOrDefault():Target(actor,false);
   e.Target=target;if(target==null)return e;
   e.Hits.Add(Hit(actor,target,move==Move.Basic?1:move==Move.Special?1.14:1.30,0));
   var h=e.Hits[0];e.Damage=h.Damage;e.HealthDamage=h.HealthDamage;e.ShieldDamage=h.ShieldDamage;e.Dodged=h.Dodged;e.Critical=h.Critical;
   if(move==Move.Special){
    var recipients=actor.CardId=="goku-test"?new[]{actor}:Units.Where(u=>u.Side==actor.Side&&u.Alive&&(u.Slot<3)==(actor.Slot<3)).ToArray();
    foreach(var ally in recipients){ally.Add(actor.CardId=="piccolo-test"?"defense":"attack",Data.ReworkLow,2,actor.CardId+":user-special");e.Recipients.Add(ally);}
   }
   e.Text=actor.Name+" · "+(move==Move.Basic?"普通攻击":move==Move.Special?actor.SpecialName:actor.UltimateName)+" → "+target.Name+" "+(h.Dodged?"闪避":h.Damage.ToString("0"))+(e.Recipients.Count>0?" · 同步强化 "+e.Recipients.Count+"人 / 各自2次行动机会":"");return e;
  }
 }
}
