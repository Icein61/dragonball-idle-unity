using System;
using System.Collections.Generic;
using System.Linq;
namespace DragonIdle {
 public enum BattleVisualKind { Impact,Combo,BlueBeam,PurpleBeam,SpiralBeam,GreenBolt,Disc,SolarFlare,Masenko,FingerBeam,TriBeam,SpiritBall,Psychic,KaiokenRush,SpiritBomb,DualBlast,Explosion,Slash,Acid }
 public sealed class VisualVitals {
  public double HP,Shield,Rage;
  public VisualVitals(Unit u){HP=u.HP;Shield=u.Shield;Rage=u.Rage;}
 }
 // A read-only projection of an already resolved action. Never rolls damage or calls Battle.Step.
 public sealed class ActionPresentation {
  public readonly BattleEvent Event;
  public readonly BattleVisualKind Kind;
  public readonly double Duration;
  public double Elapsed {get;private set;}
  readonly Dictionary<Unit,VisualVitals> before,after;
  public ActionPresentation(BattleEvent e,Dictionary<Unit,VisualVitals> snapshot,IEnumerable<Unit> units){
   Event=e;before=snapshot;after=Capture(units);Kind=VisualKind(e.Actor,e.Move);
   Duration=e.Move==Move.Stunned?.7:e.Move==Move.Basic?1.05:e.Move==Move.Ultimate?2.05:1.55;
  }
  public static Dictionary<Unit,VisualVitals> Capture(IEnumerable<Unit> units){return units.ToDictionary(u=>u,u=>new VisualVitals(u));}
  public double Progress {get{return Math.Min(1,Elapsed/Duration);}}
  public bool Complete {get{return Elapsed>=Duration;}}
  public bool Melee {get{return Kind==BattleVisualKind.Impact||Kind==BattleVisualKind.Combo||Kind==BattleVisualKind.KaiokenRush||Kind==BattleVisualKind.Slash;}}
  // Poses, travel and reactions share one action-local clock.
  public double ReleaseAt {get{return Duration*(Melee?.40:Kind==BattleVisualKind.SpiritBomb?.56:Kind==BattleVisualKind.Disc?.46:.38);}}
  public double TravelDuration {get{return Melee?Duration*.12:Kind==BattleVisualKind.SpiritBomb?.38:Kind==BattleVisualKind.SpiritBall?.34:.22;}}
  public double ImpactAt(int hit){return ReleaseAt+TravelDuration+Math.Max(0,hit)*.09;}
  public double ReactionAge(int hit){return Elapsed-ImpactAt(hit);}
  public double RecoveryAt {get{return Math.Min(Duration-.16,Math.Max(Duration*.76,ImpactAt(Math.Max(0,Event.Hits.Count-1))+.22));}}
  public double Approach {get{return Math.Max(0,Math.Min(1,Elapsed/ReleaseAt));}}
  public double Return {get{return Math.Max(0,Math.Min(1,(Elapsed-RecoveryAt)/(Duration-RecoveryAt)));}}
  public void Tick(double seconds,bool paused){if(!paused&&seconds>0&&!double.IsNaN(seconds))Elapsed=Math.Min(Duration,Elapsed+seconds);}
  public bool HitVisible(int i){return Elapsed>=ImpactAt(i);}
  public VisualVitals Vitals(Unit u){
   if(!before.ContainsKey(u)||!after.ContainsKey(u))return new VisualVitals(u);
   int hit=Event.Hits.FindIndex(h=>h.Target==u);double at=ImpactAt(Math.Max(0,hit));
   double t=Math.Max(0,Math.Min(1,(Elapsed-at)/.16));var a=before[u];var b=after[u];
   return new VisualVitals(u){HP=a.HP+(b.HP-a.HP)*t,Shield=a.Shield+(b.Shield-a.Shield)*t,Rage=a.Rage+(b.Rage-a.Rage)*t};
  }
  public int Pose(Unit u){
   double p=Progress;int hit=Event.Hits.FindIndex(h=>h.Target==u);
   if(hit>=0&&HitVisible(hit)){
    if(after[u].HP<=0)return ReactionAge(hit)<.24?8:10;
    if(ReactionAge(hit)<.34)return Event.Hits[hit].Dodged?7:Event.Hits[hit].ShieldDamage>0&&Event.Hits[hit].HealthDamage<=0?6:8;
   }
   if(u==Event.Actor&&!Complete){
    if(Event.Move==Move.Stunned)return 9;
    if(Melee){if(p<.10)return 0;if(Elapsed<ReleaseAt)return Kind==BattleVisualKind.Slash&&p>.22?4:1;if(Elapsed<RecoveryAt)return Kind==BattleVisualKind.Slash?5:Kind==BattleVisualKind.Combo&&Elapsed>ImpactAt(0)+.08?3:2;return 11;}
    if(Elapsed>=RecoveryAt)return 11;
    if(Kind==BattleVisualKind.SolarFlare||Kind==BattleVisualKind.TriBeam)return 4;
    if(Kind==BattleVisualKind.Disc)return 5;
    return Elapsed<ReleaseAt?4:5;
   }
   var v=Vitals(u);return v.HP<=0?10:v.HP/u.MaxHP<.25?9:0;
  }
  public static BattleVisualKind VisualKind(Unit u,Move move){
   if(move==Move.Stunned)return BattleVisualKind.Impact;
   string k=Data.Kit(u.CardId);
   if(move==Move.Basic)return k=="yajirobe"?BattleVisualKind.Slash:BattleVisualKind.Impact;
   if(move==Move.Special&&u.SpecialReplaced)return BattleVisualKind.GreenBolt;
   bool ult=move==Move.Ultimate;
   switch(k){
    case "goku":return ult?BattleVisualKind.BlueBeam:BattleVisualKind.Combo;
    case "vegeta":return ult?BattleVisualKind.Explosion:BattleVisualKind.PurpleBeam;
    case "guard":return ult?BattleVisualKind.SpiralBeam:BattleVisualKind.GreenBolt;
    case "krillin":return ult?BattleVisualKind.Disc:BattleVisualKind.SolarFlare;
    case "gohan":return ult?BattleVisualKind.Masenko:BattleVisualKind.Combo;
    case "tien":return ult?BattleVisualKind.TriBeam:BattleVisualKind.FingerBeam;
    case "yamcha":return ult?BattleVisualKind.SpiritBall:BattleVisualKind.Combo;
    case "chiaotzu":return BattleVisualKind.Psychic;
    case "kaioken":return ult?BattleVisualKind.BlueBeam:BattleVisualKind.KaiokenRush;
    case "beam":return ult?BattleVisualKind.SpiralBeam:BattleVisualKind.GreenBolt;
    case "raditz":return BattleVisualKind.DualBlast;
    case "nappa":return BattleVisualKind.Explosion;
    case "yajirobe":return BattleVisualKind.Slash;
    case "saibaman":return ult?BattleVisualKind.Combo:BattleVisualKind.Acid;
    default:return u.CardId=="goku-saiyan-guard-v020"?(ult?BattleVisualKind.SpiritBomb:BattleVisualKind.Combo):BattleVisualKind.GreenBolt;
   }
  }
 }
}
