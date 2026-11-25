using System.Collections.Generic;
using System.Linq;

namespace DragonIdle {
 internal struct RoundStep {
  internal readonly RoundAction Action;
  // A presentation-owned copy. Volley playback can update it as individual hits are revealed.
  internal readonly Dictionary<RoundFighter,RoundVitals> Before;

  internal RoundStep(RoundAction action,Dictionary<RoundFighter,RoundVitals> before){
   Action=action;
   Before=before;
  }
 }

 internal struct RoundVitals {
  internal readonly double HP, Shield;
  internal readonly int Rage;
  internal readonly IReadOnlyList<RoundFighter.StatChange> DefenseChanges,DodgeChanges,AttackChanges;
  internal RoundVitals(RoundFighter fighter,bool freezeChanges=false) {
   HP = fighter.HP;
   Shield = fighter.Shield;
   Rage = fighter.Rage;
   AttackChanges=freezeChanges?fighter.AttackChanges.ToArray():fighter.AttackChanges;
   DodgeChanges=freezeChanges?fighter.DodgeChanges.ToArray():fighter.DodgeChanges;
   DefenseChanges=freezeChanges?fighter.DefenseChanges.ToArray():fighter.DefenseChanges;
  }
  internal RoundVitals(RoundAction hit,RoundVitals previous){
   HP=hit.TargetHPAfter;
   Shield=hit.TargetShieldAfter;
   Rage=hit.TargetRageAfter;
   AttackChanges=previous.AttackChanges;
   DefenseChanges=previous.DefenseChanges;
   DodgeChanges=previous.DodgeChanges;
  }
 }
}
