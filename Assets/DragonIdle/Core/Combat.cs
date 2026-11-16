using System;
using System.Collections.Generic;
using System.Linq;
namespace DragonIdle {
 public enum Move { Basic, Special, Ultimate, Stunned }
 [Serializable] public class Contribution {
  public string Type, Source; public double Value; public int Expires;
 }
 public class Unit {
  public string CardId, Name, Role, SpecialRole, SpecialName, UltimateName;
  public int Side, Slot, Cooldown, CooldownLength=3, Opportunities, StunUntil, SealUntil;
  public int UltimatePolicy; // 0 ready; 1 HP <= 60%; 2 enemy HP <= 50%
  public double MaxHP, HP, Attack, Defense, Speed, Progress, Rage, Shield, LeaderAttack, LeaderHealth, LeaderDefense;
  public double Crit=.08, Dodge=.04, Reduction;
  public bool StunProtection, BasicOnly, SpecialReplaced;
  public List<Contribution> Effects=new List<Contribution>();
  public bool Alive { get { return HP>0; } }
  public void Add(string type,double value,int duration,string source) {
   Effects.Add(new Contribution {Type=type,Value=value,Expires=duration<=0?int.MaxValue:Opportunities+duration,Source=source});
  }
  public double Sum(string type) { return Effects.Where(x=>x.Type==type).Sum(x=>x.Value); }
  public double AttackValue { get { return Attack*(1+LeaderAttack)*(1+Sum("attack"))*(1+Sum("amplify")); } }
  public double SpeedValue { get { return Math.Max(1,Speed*(1+Sum("speed"))); } }
  public double DefenseValue {get{return Defense*(1+LeaderDefense)*(1+Sum("defense"));}}
  public bool Stunned { get {return StunUntil>Opportunities;} }
  public bool Sealed { get {return SealUntil>Opportunities;} }
  public void Stun(int turns) {if(!StunProtection) StunUntil=Math.Max(StunUntil,Opportunities+turns);}
  public void Expire() { Effects.RemoveAll(x=>x.Expires<=Opportunities); }
 }
 public class BattleHit {
  public Unit Target;public double Damage,HealthDamage,ShieldDamage;public bool Critical,Dodged;
 }
 public class BattleEvent:BattleHit {
  public Unit Actor;public Move Move;public double Time;public string Text;
  public List<BattleHit> Hits=new List<BattleHit>();
 }
 public partial class Battle {
  public List<Unit> Units; public double Time; public int Events; public Random Random;
  readonly Queue<Unit> ultimates=new Queue<Unit>();
  int chain;
  public Battle(IEnumerable<Unit> units,int seed) {
   Units=units.ToList(); Random=new Random(seed);
   if(Units.GroupBy(x=>x.Side*10+x.Slot).Any(g=>g.Count()>1)||Units.Any(x=>x.Slot<0||x.Slot>6)) throw new ArgumentException("Invalid formation");
   if(Units.GroupBy(x=>x.Side).Any(g=>g.GroupBy(x=>x.CardId).Any(c=>c.Count()>1))) throw new ArgumentException("Duplicate card");
  }
  public int Result {get {if(!Units.Any(x=>x.Side==0&&x.Alive))return -1;if(!Units.Any(x=>x.Side==1&&x.Alive))return 1;return 0;}}
  public static double Y(int slot) {return slot<3 ? (slot-1)*1.3 : (slot-4-1.5)*1.1;}
  public Unit Target(Unit actor,bool rear) {
   var candidates=Units.Where(x=>x.Side!=actor.Side&&x.Alive).ToList();
   var row=candidates.Where(x=>rear?x.Slot>=3:x.Slot<3).ToList();
   if(row.Count==0)row=candidates;
   return row.OrderBy(x=>Math.Abs(Y(x.Slot)-Y(actor.Slot))).ThenBy(x=>x.Slot).FirstOrDefault();
  }
  bool Eligible(Unit u) {
   if(!u.Alive||u.Stunned||u.Sealed||u.Rage<100)return false;
   if(u.UltimatePolicy==1)return u.HP/u.MaxHP<=.6;
   if(u.UltimatePolicy==2) {var t=Target(u,false);return t!=null&&t.HP/t.MaxHP<=.5;}
   return true;
  }
  void QueueUltimates() {
   var candidates=Units.Where(x=>Eligible(x)&&!ultimates.Contains(x)).Select(x=>new {Unit=x,Tie=Random.Next()})
    .OrderByDescending(x=>x.Unit.SpeedValue).ThenBy(x=>x.Tie).ToList();
   foreach(var c in candidates)ultimates.Enqueue(c.Unit);
  }
  public BattleEvent Step() {
   if(Result!=0)return null;
   QueueUltimates();
   while(ultimates.Count>0) {
    var u=ultimates.Dequeue(); if(!Eligible(u))continue;
    if(++chain>128)throw new InvalidOperationException("Invalid unlimited extra-action chain");
    u.Rage-=100; var e=Act(u,Move.Ultimate); QueueUltimates();return e;
   }
   chain=0;
   var ready=Units.Where(x=>x.Alive&&x.Progress>=1-1e-9).OrderBy(x=>x.Slot*2+x.Side).FirstOrDefault();
   if(ready==null) {
    double dt=Units.Where(x=>x.Alive).Min(x=>(1-x.Progress)*1000/x.SpeedValue);
    Time+=dt;
    foreach(var x in Units.Where(x=>x.Alive))x.Progress=Math.Min(1,x.Progress+dt*x.SpeedValue/1000);
    ready=Units.Where(x=>x.Alive&&x.Progress>=1-1e-9).OrderBy(x=>x.Slot*2+x.Side).First();
   }
   bool stunned=ready.Stunned, sealedNow=ready.Sealed;
   ready.Progress=0;ready.Opportunities++;
   BattleEvent result;
   if(stunned) {
    result=new BattleEvent {Actor=ready,Move=Move.Stunned,Time=Time,Text=ready.Name+" 眩晕，跳过行动"};
    if(ready.StunUntil<=ready.Opportunities)ready.StunProtection=true;
    Events++;
   } else {
    bool special=ready.Cooldown==0&&!sealedNow&&!ready.BasicOnly;
    result=Act(ready,special?Move.Special:Move.Basic);
    if(special)ready.Cooldown=ready.CooldownLength;
    else if(ready.Cooldown>0)ready.Cooldown--;
    ready.Rage=Math.Min(100,ready.Rage+(special?28:16));
    ready.StunProtection=false;
   }
   ready.Expire();QueueUltimates();return result;
  }
  BattleHit Hit(Unit actor,Unit target,double power,double penetration) {
   var h=new BattleHit{Target=target};
   h.Dodged=Random.NextDouble()<target.Dodge;
   h.Critical=!h.Dodged&&Random.NextDouble()<actor.Crit;
   if(!h.Dodged){
    double defense=Math.Max(0,target.DefenseValue)*(1-penetration);
    double damage=Math.Max(1,actor.AttackValue*power*(1+actor.Sum("damage"))*100/(100+defense)*(1-Math.Min(.9,target.Reduction))*(h.Critical?1.5:1));
    if(double.IsInfinity(damage)||double.IsNaN(damage))throw new InvalidOperationException("Non-finite damage");
    h.Damage=Math.Round(damage);h.ShieldDamage=Math.Min(target.Shield,h.Damage);
    target.Shield-=h.ShieldDamage;h.HealthDamage=Math.Min(target.HP,h.Damage-h.ShieldDamage);target.HP-=h.HealthDamage;
    target.Rage=Math.Min(100,target.Rage+7);
   }
   return h;
  }
  BattleEvent Act(Unit actor,Move move) {
   Events++;
   string specialRole=actor.SpecialRole??actor.Role;
   string k=Data.Kit(actor.CardId);
   if(move==Move.Special&&actor.SpecialReplaced)k="";
   bool rear=move==Move.Special&&specialRole=="sniper"||move==Move.Ultimate&&k=="yamcha";
   var target=Target(actor,rear);
   var e=new BattleEvent{Actor=actor,Target=target,Move=move,Time=Time};
   if(target==null)return e;
   double power=k==""?(move==Move.Basic?1:move==Move.Special?1.14:1.3):KitPower(k,move);
   var targets=new List<Unit>{target};
   if(move==Move.Special&&(k=="raditz"||k=="nappa")){
    var next=Units.Where(x=>x.Side!=actor.Side&&x.Alive&&x!=target).OrderBy(x=>x.Slot<3?0:1).ThenBy(x=>Math.Abs(Y(x.Slot)-Y(actor.Slot))).ThenBy(x=>x.Slot).FirstOrDefault();
    if(next!=null)targets.Add(next);
   }
   foreach(var t in targets)e.Hits.Add(Hit(actor,t,power,KitPenetration(k,t,move)));
   e.Damage=e.Hits.Sum(x=>x.Damage);e.HealthDamage=e.Hits.Sum(x=>x.HealthDamage);e.ShieldDamage=e.Hits.Sum(x=>x.ShieldDamage);
   e.Dodged=e.Hits.All(x=>x.Dodged);e.Critical=e.Hits.Any(x=>x.Critical);
   string extra="";
   if(k!="")extra=KitEffects(k,actor,target,move,!e.Hits[0].Dodged);
   else {
    if(move==Move.Special&&specialRole=="control"&&!e.Dodged)target.Stun(1);
    if(move==Move.Special&&specialRole=="tank"){actor.Shield+=actor.MaxHP*.15;extra=" · 自身获得15%护盾 / 至耗尽";}
    if(move==Move.Special&&specialRole=="healer"){
     var ally=Units.Where(x=>x.Side==actor.Side&&x.Alive).OrderBy(x=>x.HP/x.MaxHP).ThenBy(x=>x.Slot).First();
     ally.HP=Math.Min(ally.MaxHP,ally.HP+ally.MaxHP*.14);extra=" · 治疗 "+ally.Name;
    }
    if(move==Move.Ultimate){
     actor.Add("attack",.12,2,"ultimate");extra=" · 攻击强化 +12% / 2回合";
     if(actor.Role=="support")foreach(var ally in Units.Where(x=>x.Side==actor.Side&&x.Alive&&x!=actor))ally.Add("attack",.06,2,actor.CardId);
    }
   }
   string name=move==Move.Basic?"普通攻击":move==Move.Special?actor.SpecialName:actor.UltimateName;
   e.Text=actor.Name+" · "+name+" → "+string.Join(" / ",e.Hits.Select(h=>h.Target.Name+" "+(h.Dodged?"闪避":(h.Critical?"暴击 ":"")+h.Damage.ToString("0"))).ToArray())+extra;
   return e;
  }
 }
}
