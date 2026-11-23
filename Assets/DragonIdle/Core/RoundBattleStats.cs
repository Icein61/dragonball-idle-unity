using System.Collections.Generic;

namespace DragonIdle {
 internal enum RoundBattleMetric { Damage, Taken, Healing }

 internal sealed class RoundBattleStats {
  sealed class Totals {
   internal double Damage,Taken,Healing;
  }

  readonly Dictionary<RoundFighter,Totals> fighters=new Dictionary<RoundFighter,Totals>();

  internal void Reset(){fighters.Clear();}

  Totals For(RoundFighter fighter){
   Totals totals;
   if(!fighters.TryGetValue(fighter,out totals))fighters.Add(fighter,totals=new Totals());
   return totals;
  }

  internal void RecordHit(RoundAction hit){
   if(hit.Dodged||hit.Charging)return;
   if(hit.Healing){
    For(hit.Actor).Healing+=hit.Amount;
    return;
   }
   double impact=hit.HealthDamage+hit.ShieldDamage;
   For(hit.Actor).Damage+=impact;
   For(hit.Target).Taken+=impact;
  }

  internal void RecordCost(RoundAction action){
   if(action.ActorHPCost>0)For(action.Actor).Taken+=action.ActorHPCost;
  }

  internal double Value(RoundFighter fighter,RoundBattleMetric metric){
   Totals totals;
   if(!fighters.TryGetValue(fighter,out totals))return 0;
   return metric==RoundBattleMetric.Damage?totals.Damage:metric==RoundBattleMetric.Taken?totals.Taken:totals.Healing;
  }
 }
}
