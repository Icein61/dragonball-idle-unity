using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using DragonIdle;

static class RoundTrace {
 static readonly JavaScriptSerializer Json=new JavaScriptSerializer();

 static int Main(string[] args){
  Thread.CurrentThread.CurrentCulture=CultureInfo.InvariantCulture;
  string config=args[0];
  var rules=Json.Deserialize<RoundDefinitions>(File.ReadAllText(Path.Combine(config,"Characters","RoundKits.json")));
  var scenes=Json.Deserialize<RoundSceneDefinitions>(File.ReadAllText(Path.Combine(config,"Battle","Scenes.json")));
  rules.ValidateRoster();
  scenes.Validate();

  var ids=Data.RoundRoster.ToArray();
  var fixture=new RoundDemoSetup{
   allies=new[]{ids[0],null,ids[1],ids[2],null,ids[3],null,null,ids[4]},
   enemies=new[]{ids[5],ids[6],null,null,ids[8],null,ids[7],null,ids[0]},
   damageScale=scenes.demo.damageScale
  };
  int battles=0,actions=0;
  using(var writer=new StreamWriter(args[1])){
   foreach(int seed in new[]{17,1025,2026,8713}){
    actions+=Trace(writer,"saved-formation",seed,scenes.demo.CreateBattle(rules,seed,new Profile()));
    actions+=Trace(writer,"all-online-cards",seed,fixture.CreateBattle(rules,seed));
    battles+=2;
   }
  }
  Console.WriteLine("ROUND_TRACE_PASS battles={0} actions={1}",battles,actions);
  return 0;
 }

 static int Trace(StreamWriter writer,string scenario,int seed,RoundCombat battle){
  writer.WriteLine(Json.Serialize(new{scenario,seed,initial=battle.Order.Select(Fighter).ToArray()}));
  int actions=0,steps=0;
  while(battle.Result==0){
   if(++steps>2000)throw new InvalidOperationException("Fixed-seed combat did not terminate");
   var before=battle.Order.Select(FighterVitals).ToArray();
#if PUBLISHED_BASELINE
   var action=battle.Step();
#else
   var step=battle.ResolveStep(true);
   var action=step.Action;
   var captured=battle.Order.Select(fighter=>CapturedVitals(step.Before[fighter])).ToArray();
   if(Json.Serialize(before)!=Json.Serialize(captured))
    throw new InvalidOperationException("Presentation snapshot changed during settlement");
   before=captured;
#endif
   if(action!=null)actions++;
   writer.WriteLine(Json.Serialize(new{
    before,battle.Round,battle.Result,battle.ComboSide,battle.ComboCount,
    battle.ReactionSkillNext,battle.PursuitNext,reviving=Key(battle.NextRevivingFighter),
    hits=action==null?null:action.Hits.Select(Hit).ToArray(),
    after=battle.Order.Select(Fighter).ToArray()
   }));
  }
  return actions;
 }

 static string Key(RoundFighter fighter){
  return fighter==null?null:fighter.Side+":"+fighter.Position+":"+fighter.CardId;
 }

 static object Fighter(RoundFighter fighter){
  return new{
   id=Key(fighter),fighter.Hand,fighter.Level,fighter.MaxHP,fighter.Attack,fighter.Defense,
   fighter.Crit,fighter.Dodge,fighter.Tenacity,fighter.Penetration,fighter.Accuracy,fighter.Qi,
   fighter.HitState,fighter.IsCharging,fighter.PursuitCount,fighter.CounterUses,
   fighter.OpeningAttackBonus,fighter.PhaseAttackBonus,fighter.PhaseDefenseBonus,
   fighter.HasRevived,fighter.HasDetonated,vitals=FighterVitals(fighter)
  };
 }

 static object FighterVitals(RoundFighter fighter){
  return Vitals(fighter.HP,fighter.Shield,fighter.Rage,fighter.AttackChanges,fighter.DefenseChanges,fighter.DodgeChanges);
 }

#if !PUBLISHED_BASELINE
 static object CapturedVitals(RoundVitals vitals){
  return Vitals(vitals.HP,vitals.Shield,vitals.Rage,vitals.AttackChanges,vitals.DefenseChanges,vitals.DodgeChanges);
 }
#endif

 static object Vitals(double hp,double shield,int rage,IEnumerable<RoundFighter.StatChange> attack,
  IEnumerable<RoundFighter.StatChange> defense,IEnumerable<RoundFighter.StatChange> dodge){
  return new{hp,shield,rage,attack=Changes(attack),defense=Changes(defense),dodge=Changes(dodge)};
 }

 static object[] Changes(IEnumerable<RoundFighter.StatChange> changes){
  return changes.Select(change=>(object)new{change.Fraction,change.Expiry,source=Ability(change.Source)}).ToArray();
 }

 static string Ability(RoundAbility ability){
  return ability==null?null:ability.trigger+":"+ability.effect+":"+ability.name;
 }

 static object Hit(RoundAction hit){
  return new{
   actor=Key(hit.Actor),target=Key(hit.Target),hit.Move,hit.Name,hit.Round,hit.TargetState,hit.RequiredState,
   hit.Amount,hit.HealthDamage,hit.ShieldDamage,hit.TargetHPAfter,hit.TargetShieldAfter,hit.TargetRageAfter,
   hit.DefenseDelta,hit.DodgeDelta,hit.ActorHPCost,hit.Critical,hit.Healing,hit.Dodged,hit.Countered,
   hit.Charging,hit.EnhancedBasic,hit.SequentialHits,hit.TriggersPursuit,hit.ComboAttack,hit.ComboHits,
   hit.ComboAfter,hit.Revives,hit.Detonates,ability=Ability(hit.Ability),
   counter=Ability(hit.CounterAbility),guard=Ability(hit.GuardAbility)
  };
 }
}
