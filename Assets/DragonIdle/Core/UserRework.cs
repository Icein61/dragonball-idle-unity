using System;
using System.Linq;
using System.Collections.Generic;
namespace DragonIdle {
 public static partial class Data {
  // Adjustable budgets; user wording is retained separately, verbatim.
  public const double ReworkLow=.04,ReworkMedium=.08,ReworkShield=.12,ReworkCost=.10,ReworkRegen=.03;
  public const double LowLifeCost=.05,RageDrain=16,LifeSteal=.02,VeryHighDamage=1.55;
  public static void ReworkPassive(Unit u){
   if(u.CardId=="goku-test")u.Add("attack",ReworkLow,0,"rework:base");
   if(u.CardId=="vegeta-test"){u.Add("attack",ReworkMedium,0,"rework:base");u.Add("defense",ReworkMedium,0,"rework:base");}
   if(u.CardId=="piccolo-test")u.Add("defense",ReworkMedium,0,"rework:base");
   if(u.CardId=="krillin-test")u.Add("speed",ReworkLow,0,"rework:base");
   if(u.CardId=="gohan-test"||u.CardId=="tien-test")u.Add("attack",ReworkLow,0,"rework:base");
   if(u.CardId=="goku-saiyan-kaioken-v020")u.Add("attack",ReworkMedium,0,"rework:base");
  }
  public static void ApplyTeamDesign(Unit unit,IEnumerable<string> teamIds){
   if(!Reworked(unit.CardId))return;
   var team=teamIds.Select(Index).Where(index=>index>=0).Select(index=>Cards[index]).ToArray();
   bool piccolo=team.Any(card=>card.PersonId=="piccolo");
   if(unit.CardId=="goku-test"&&piccolo)AddTeamEffect(unit,"defense",ReworkMedium,"piccolo");
   if(unit.CardId=="gohan-test"&&piccolo)AddTeamEffect(unit,"attack",ReworkMedium,"piccolo");
   if(unit.CardId=="tien-test"&&team.Any(card=>card.PersonId=="chiaotzu"))AddTeamEffect(unit,"health",ReworkMedium,"chiaotzu");
   if(unit.CardId=="yamcha-saiyan-v020"){
    int count=team.Count(card=>card.HasTag("Z战士"));
    if(count>=2)AddTeamEffect(unit,"attack",ReworkMedium,"yamcha");
    if(count>=4)AddTeamEffect(unit,"defense",ReworkMedium,"yamcha");
    if(count>=5)AddTeamEffect(unit,"health",ReworkMedium,"yamcha");
   }
   if(Card(unit.CardId).HasTag("Z战士")&&team.Any(card=>card.Id=="chiaotzu-saiyan-v020")){
    AddTeamEffect(unit,"attack",ReworkLow,"z-warrior");
    AddTeamEffect(unit,"defense",ReworkLow,"z-warrior");
   }
  }
  static void AddTeamEffect(Unit unit,string type,double value,string source){
   string key="rework:"+source+":"+type;
   if(unit.Effects.Any(effect=>effect.Source==key))return;
   unit.Add(type,value,0,key);
   if(type=="health"){
    unit.MaxHP*=1+value;
    unit.HP*=1+value;
    unit.Shield*=1+value;
   }
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
   bool ultimate=move==Move.Ultimate,special=move==Move.Special;
   string id=actor.CardId;
   var enemies=Units.Where(unit=>unit.Side!=actor.Side&&unit.Alive);
   var target=Target(actor,false);
   if(ultimate&&id=="vegeta-test")target=enemies.OrderBy(unit=>unit.HP/unit.MaxHP).ThenBy(unit=>unit.Slot).FirstOrDefault();
   if(ultimate&&id=="gohan-test")target=enemies.OrderByDescending(unit=>unit.AttackValue).ThenBy(unit=>unit.Slot).FirstOrDefault();
   e.Target=target;
   if(target==null)return e;
   if((ultimate&&id=="tien-test")||(id=="goku-saiyan-kaioken-v020"&&(special||ultimate))){
    e.HealthCost=Math.Min(Math.Max(0,actor.HP-1),actor.MaxHP*Data.LowLifeCost);
    actor.HP-=e.HealthCost;
   }
   double power=move==Move.Basic?1:special?1.14:1.30;
   if(ultimate&&id=="chiaotzu-saiyan-v020")power=1.14;
   if(id=="goku-saiyan-kaioken-v020"&&special)power=1.30;
   if(ultimate&&(id=="tien-test"||id=="goku-saiyan-kaioken-v020"))power=Data.VeryHighDamage;
   var targets=new[]{target};
   if(ultimate&&id=="krillin-test"){
    targets=enemies.Where(unit=>unit.Slot<Data.FrontSlots).OrderBy(unit=>unit.Slot).ToArray();
    if(targets.Length==0)targets=new[]{target};
    e.Target=targets[0];
   }
   foreach(var enemy in targets)e.Hits.Add(Hit(actor,enemy,power,0));
   e.Damage=e.Hits.Sum(hit=>hit.Damage);
   e.HealthDamage=e.Hits.Sum(hit=>hit.HealthDamage);
   e.ShieldDamage=e.Hits.Sum(hit=>hit.ShieldDamage);
   e.Dodged=e.Hits.All(hit=>hit.Dodged);
   e.Critical=e.Hits.Any(hit=>hit.Critical);
   if(special&&(id=="goku-test"||id=="vegeta-test"||id=="piccolo-test")){
    var recipients=actor.CardId=="goku-test"?new[]{actor}:Units.Where(u=>u.Side==actor.Side&&u.Alive&&(u.Slot<3)==(actor.Slot<3)).ToArray();
    foreach(var ally in recipients){ally.Add(actor.CardId=="piccolo-test"?"defense":"attack",Data.ReworkLow,2,actor.CardId+":user-special");e.Recipients.Add(ally);}
   }
   string extra="";
   if(special&&id=="gohan-test"&&!e.Dodged){
    var drained=Math.Min(Data.RageDrain,Math.Min(target.Rage,100-actor.Rage));
    target.Rage-=drained;actor.Rage+=drained;
    if(drained>0)extra=" · 吸取怒气 "+drained.ToString("0");
   }
   e.Text=actor.Name+" · "+(move==Move.Basic?"普通攻击":special?actor.SpecialName:actor.UltimateName)+" → "+
    string.Join(" / ",e.Hits.Select(hit=>hit.Target.Name+" "+(hit.Dodged?"闪避":hit.Damage.ToString("0"))).ToArray())+
    (e.Recipients.Count>0?" · 同步强化 "+e.Recipients.Count+"人 / 各自2次行动机会":"")+extra;
   return e;
  }
 }
}
