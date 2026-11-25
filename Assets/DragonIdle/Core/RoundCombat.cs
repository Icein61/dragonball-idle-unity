using System;
using System.Collections.Generic;
using System.Linq;
namespace DragonIdle {
 public static class RoundBoard {
  public const int Cells=9,PlayerLimit=5,EnemyMin=5;
  // Positions are one-based. Player columns run right to left, enemy columns mirror.
  public static int Row(int position){return (position-1)%3;}
  public static int Column(int position,int side){int lane=(position-1)/3;return side==0?2-lane:lane;}
 }
 [Serializable] public sealed class RoundSkillIcon {
  public string resource;
  public int[] region;
  public bool framed;
 }
 [Serializable] public sealed class RoundAbility {
  public RoundSkillIcon icon;
  public string name,trigger,effect,allyTag,text;
  public RoundHitState reaction;
  public string[] resistTags,teamAttackTags;
  public double value,allyDefense,finalReduction,chance,lateAttackPenalty,lateDefense,reviveAttackBonus,defeatAttackBonus;
  public int phaseRound,usesPerRound,comboHits;
 }
 [Serializable] public sealed class RoundTechnique {
  public double basicReactionChance,basicEnhancedChance,basicEnhancedPower,ultimateAttackBonus,ultimateHealthCost;
  public int ultimateTargets,ultimateAttackRounds;
  public string pursuitSourceTag;
  public bool ultimateFrontRow,ultimateFrontColumn,basicRandomTarget,basicHealsSelf,ultimateHighestAttack,ultimateLowestDefense,ultimateDistinctVolley;
  public int pursuitLimit,ultimateHits,ultimateChargeRounds,pursuitCombo,pursuitHits;
  public int basicComboHits,ultimateComboHits,pursuitComboHits;
  public double ultimateChargeReduction,ultimateDefenseReduction,pursuitDefenseIncrease,pursuitDodgeIncrease;
  public int ultimateDefenseRounds,pursuitDefenseRounds,pursuitDodgeRounds;
  public RoundHitState basicReaction;
  public string basicText,ultimateText,pursuitText,ultimateNote;
  public RoundHitState ultimateReaction,pursuitTrigger,pursuitReaction;
 }
 [Serializable] public sealed class RoundKit {
  public string cardId;
  public string basic,ultimate,pursuit;
  public double basicPower,ultimatePower,pursuitPower;
  public RoundAbility[] skills;
  public RoundSkillIcon basicIcon,ultimateIcon,pursuitIcon;
  public RoundTechnique techniques;
 }
 [Serializable] public sealed class RoundDefinitions {
  public RoundKit[] cards;
  public RoundKit For(string id){return cards.Single(c=>c.cardId==id);}
  internal IEnumerable<RoundAbility> OpeningSkills(string cardId){ return For(cardId).skills.Where(skill=>skill.trigger=="opening"); }
  public void ApplyOpening(RoundFighter fighter,IEnumerable<RoundFighter> formation=null){
   var skills=OpeningSkills(fighter.CardId).ToArray();
   fighter.OpeningAttackBonus=skills.Where(s=>s.effect=="attack").Sum(s=>s.value);
   fighter.Attack*=1+fighter.OpeningAttackBonus;
   foreach(var s in skills){
    if(s.effect=="defense")fighter.Defense*=1+s.value;
    else if(s.effect=="shield")fighter.Shield+=fighter.ReceivedAid(s.value*fighter.MaxHP);
    else if(s.effect=="penetration")fighter.Penetration+=s.value;
    if(formation!=null&&!string.IsNullOrEmpty(s.allyTag)&&formation.Any(ally=>ally.Side==fighter.Side&&Data.Card(ally.CardId).HasTag(s.allyTag)))
     fighter.Defense*=1+s.allyDefense;
   }
  }
  public void Validate(){
   if(cards==null||cards.Length==0||cards.Any(k=>k==null||Data.Index(k.cardId)<0)||cards.Select(k=>k.cardId).Distinct().Count()!=cards.Length)
    throw new InvalidOperationException("回合技能配置缺卡或重复");
   foreach(var k in cards){
    var c=Data.Card(k.cardId);
    if(string.IsNullOrEmpty(k.basic)||string.IsNullOrEmpty(k.ultimate)||string.IsNullOrEmpty(k.pursuit)||
     !Bounded(k.basicPower,4)||!Bounded(k.ultimatePower,4)||!Bounded(k.pursuitPower,4)||
     k.skills==null||k.skills.Length!=(c.Rarity=="C"?1:2)||k.skills.Any(s=>!ValidSkill(s))||
     k.techniques!=null&&(!(k.techniques.basicReactionChance>=0&&k.techniques.basicReactionChance<=1)||
      k.techniques.pursuitLimit<0||k.techniques.pursuitLimit>2||
      !ValidComboHits(k.techniques.basicComboHits)||!ValidComboHits(k.techniques.ultimateComboHits)||
      !ValidComboHits(k.techniques.pursuitComboHits)||k.techniques.pursuitCombo<0||k.techniques.pursuitCombo>10||
      (k.techniques.pursuitCombo==0?k.techniques.pursuitHits!=0||k.techniques.pursuitDodgeIncrease!=0||k.techniques.pursuitDodgeRounds!=0:
       k.techniques.pursuitHits<1||k.techniques.pursuitHits>10||k.techniques.pursuitTrigger!=RoundHitState.None||
       !Bounded(k.techniques.pursuitDodgeIncrease,1)||k.techniques.pursuitDodgeRounds< -1)||
      k.techniques.ultimateFrontColumn&&(k.techniques.ultimateFrontRow||k.techniques.ultimateHighestAttack||
       k.techniques.ultimateHits>0||k.techniques.ultimateChargeRounds>0)||
      k.techniques.ultimateHits<0||k.techniques.ultimateHits>6||
      k.techniques.ultimateChargeRounds<0||k.techniques.ultimateChargeRounds>1||
      (k.techniques.ultimateChargeRounds==0?k.techniques.ultimateChargeReduction!=0:
       !Bounded(k.techniques.ultimateChargeReduction,1)||k.techniques.ultimateFrontRow||k.techniques.ultimateHits>0)||
      k.techniques.ultimateFrontRow&&k.techniques.ultimateHits>0||
      k.techniques.ultimateHighestAttack&&(k.techniques.ultimateFrontRow||k.techniques.ultimateHits>0||k.techniques.ultimateChargeRounds>0)||
      k.techniques.ultimateLowestDefense&&(k.techniques.ultimateHighestAttack||k.techniques.ultimateFrontRow||
       k.techniques.ultimateFrontColumn||k.techniques.ultimateHits>0||k.techniques.ultimateTargets>0||k.techniques.ultimateChargeRounds>0)||
      !(k.techniques.ultimateHealthCost>=0&&k.techniques.ultimateHealthCost<=1)||
      k.techniques.ultimateHealthCost!=0&&(!k.techniques.ultimateFrontRow||k.techniques.ultimateChargeRounds!=0||k.techniques.ultimateTargets!=0)||
      !ValidTechniqueOptions(k.techniques)||
      !ValidDefenseChange(k.techniques.ultimateDefenseReduction,k.techniques.ultimateDefenseRounds)||
      !ValidDefenseChange(k.techniques.pursuitDefenseIncrease,k.techniques.pursuitDefenseRounds)||
      !Enum.IsDefined(typeof(RoundHitState),k.techniques.basicReaction)||
      !Enum.IsDefined(typeof(RoundHitState),k.techniques.ultimateReaction)||
      !Enum.IsDefined(typeof(RoundHitState),k.techniques.pursuitTrigger)||
      !Enum.IsDefined(typeof(RoundHitState),k.techniques.pursuitReaction)))
     throw new InvalidOperationException("无效回合技能配置："+k.cardId);
   }
  }
  public void ValidateRoster(){
   Validate();
   if(cards.Length!=Data.RoundRoster.Count||cards.Any(k=>!Data.IsRoundCard(k.cardId)))
    throw new InvalidOperationException("回合技能与上线名单不一致");
  }
  static bool ValidTechniqueOptions(RoundTechnique rules){
   bool enhanced=rules.basicEnhancedChance==0?rules.basicEnhancedPower==0:
    Bounded(rules.basicEnhancedChance,1)&&Bounded(rules.basicEnhancedPower,4)&&!rules.basicHealsSelf;
   bool multi=rules.ultimateTargets==0?rules.ultimateAttackBonus==0&&rules.ultimateAttackRounds==0:
    rules.ultimateTargets==2&&Bounded(rules.ultimateAttackBonus,1)&&rules.ultimateAttackRounds>0&&
    rules.ultimateHits==0&&rules.ultimateChargeRounds==0&&!rules.ultimateFrontRow&&!rules.ultimateFrontColumn&&!rules.ultimateHighestAttack;
   bool tagged=string.IsNullOrEmpty(rules.pursuitSourceTag)||
    Data.RoundIdentityTags().Contains(rules.pursuitSourceTag)&&rules.pursuitTrigger==RoundHitState.None&&rules.pursuitCombo==0;
   bool distinctVolley=!rules.ultimateDistinctVolley||rules.ultimateHits>0;
   return enhanced&&multi&&tagged&&distinctVolley;
  }
  static bool ValidComboHits(int count){return count>=0&&count<=10;}
  static bool ValidDefenseChange(double factor,int rounds){
   return rounds==0?factor==0:rounds>=-1&&Bounded(factor,1);
  }
  static bool ValidSkill(RoundAbility s){
   if(s==null||string.IsNullOrEmpty(s.name)||!ValidComboHits(s.comboHits))return false;
   if(s.trigger=="onRoundStart")
    return s.effect=="strike"&&Bounded(s.value,4)&&s.chance==0&&s.finalReduction==0&&s.reviveAttackBonus==0&&
     (s.teamAttackTags==null||s.teamAttackTags.Length==0)&&string.IsNullOrEmpty(s.allyTag)&&s.allyDefense==0;
   if(s.trigger=="roundStart")
    return s.effect=="phaseStats"&&Bounded(s.value,1)&&s.phaseRound>1&&s.phaseRound<=RoundCombat.MaxRounds&&
     Bounded(s.lateAttackPenalty,1)&&Bounded(s.lateDefense,1);
   if(s.trigger=="onDefeat"){
    bool revive=s.effect=="revive"&&Bounded(s.value,1)&&Bounded(s.reviveAttackBonus,1)&&s.defeatAttackBonus==0;
    bool detonate=s.effect=="detonate"&&Bounded(s.value,4)&&Bounded(s.defeatAttackBonus,1)&&s.reviveAttackBonus==0;
    return (revive||detonate)&&ValidTeamTags(s)&&s.chance==0&&s.finalReduction==0&&
     string.IsNullOrEmpty(s.allyTag)&&s.allyDefense==0;
   }
   if(s.reviveAttackBonus!=0||s.defeatAttackBonus!=0)return false;
   if(s.trigger=="whileAlive")
    return s.effect=="teamAttack"&&Bounded(s.value,1)&&ValidTeamTags(s)&&
     string.IsNullOrEmpty(s.allyTag)&&s.allyDefense==0&&s.chance==0&&s.finalReduction==0;
   if(s.teamAttackTags!=null&&s.teamAttackTags.Length>0)return false;
   if(s.trigger=="reserved")return s.effect=="none"&&s.value==0&&string.IsNullOrEmpty(s.allyTag)&&s.allyDefense==0;
   bool opening=s.trigger=="opening"&&(s.effect=="attack"||s.effect=="defense"||s.effect=="shield"||s.effect=="penetration");
   bool action=s.trigger=="afterBasic"&&(s.effect=="strike"||s.effect=="aid");
   bool counter=s.trigger=="onSingleTarget"&&s.effect=="counter"&&s.usesPerRound>=0&&Enum.IsDefined(typeof(RoundHitState),s.reaction);
   bool guard=s.trigger=="onReceive"&&s.effect=="mitigate"&&Bounded(s.chance,1);
   if(!guard&&s.chance!=0)return false;
   bool validReduction=s.resistTags==null||s.resistTags.Length==0?s.finalReduction==0:
    opening&&Bounded(s.finalReduction,1)&&s.resistTags.All(Data.RoundIdentityTags().Contains)&&s.resistTags.Distinct().Count()==s.resistTags.Length;
   return validReduction&&(opening||action||counter||guard)&&Bounded(s.value,1)&&(string.IsNullOrEmpty(s.allyTag)?s.allyDefense==0:
    opening&&Bounded(s.allyDefense,1)&&Data.Cards.Any(c=>c.HasTag(s.allyTag)));
  }
  static bool ValidTeamTags(RoundAbility skill){
   var tags=skill.teamAttackTags;
   return tags!=null&&tags.Length>0&&tags.Distinct().Count()==tags.Length&&tags.All(Data.RoundIdentityTags().Contains);
  }
  static bool Bounded(double value,double max){return value>0&&value<=max;}
 }
 [Serializable] public sealed class RoundEncounter {
  public string id,title;
  public double scale;
  public string[] positions;
 }
 [Serializable] public sealed class RoundEncounters {
  public RoundEncounter[] boards;
  public void Validate(){
   if(boards==null||boards.Length==0||boards.Any(b=>b==null||string.IsNullOrEmpty(b.id)||string.IsNullOrEmpty(b.title)||
    !(b.scale>0&&b.scale<=4)||b.positions==null||b.positions.Length!=RoundBoard.Cells||
    b.positions.Count(id=>!string.IsNullOrEmpty(id))<RoundBoard.EnemyMin||b.positions.Any(id=>!string.IsNullOrEmpty(id)&&Data.Index(id)<0))||
    boards.Select(b=>b.id).Distinct().Count()!=boards.Length)throw new InvalidOperationException("回合试阵配置不完整");
  }
 }
 public enum RoundMove { Basic, Ultimate, Pursuit, Skill }
 public enum RoundHitState { None, Standing, Knockback, SmallFloat, LargeFloat, Knockdown }
 public sealed class RoundFighter {
  public readonly string CardId,Name;
  public readonly int Side,Position,Level;
  public readonly double MaxHP;
  public double HP,Attack,Defense,Crit,Shield,Dodge=.04,Tenacity,Penetration,Accuracy,Qi;
  public int Rage;
  public int Hand {get;internal set;}
  internal int PursuedRound,PursuitCount,CounterRound,ChargeReadyRound,ComboQueuedRound,CounterUses;
  internal RoundAction HitSource;
  internal bool HasRevived,HasDetonated;
  internal double OpeningAttackBonus,PhaseAttackBonus,PhaseDefenseBonus;
  internal struct StatChange {
   internal double Fraction;
   internal int Expiry;
   internal RoundAbility Source;
  }
  readonly List<StatChange> defenseChanges=new List<StatChange>(),dodgeChanges=new List<StatChange>(),attackChanges=new List<StatChange>();
  internal IReadOnlyList<StatChange> AttackChanges {get{return attackChanges;}}
  internal IReadOnlyList<StatChange> DefenseChanges {get{return defenseChanges;}}
  internal IReadOnlyList<StatChange> DodgeChanges {get{return dodgeChanges;}}
  static void ChangeStat(List<StatChange> changes,double fraction,int round,int duration,RoundAbility source=null){
   changes.RemoveAll(change=>change.Expiry<=round);
   changes.Add(new StatChange{Fraction=fraction,Expiry=duration<0?int.MaxValue:round+duration,Source=source});
  }
  internal void ChangeAttack(double fraction,int round,int duration,RoundAbility source=null){ChangeStat(attackChanges,fraction,round,duration,source);}
  internal double AttackBonus(int round){return PhaseAttackBonus+attackChanges.Where(change=>round<change.Expiry).Sum(change=>change.Fraction);}
  internal void ChangeDefense(double fraction,int round,int duration){ChangeStat(defenseChanges,fraction,round,duration);}
  internal void ChangeDodge(double fraction,int round,int duration){ChangeStat(dodgeChanges,fraction,round,duration);}
  internal double EffectiveDefense(int round){
   return Defense*Math.Max(0,1+PhaseDefenseBonus+defenseChanges.Where(change=>round<change.Expiry).Sum(change=>change.Fraction));
  }
  internal double EffectiveDodge(int round,double accuracy=0){
   return UnitProbability(Dodge+dodgeChanges.Where(change=>round<change.Expiry).Sum(change=>change.Fraction)-accuracy);
  }
  internal static double UnitProbability(double value){return Math.Max(0,Math.Min(1,value));}
  internal double ReceivedAid(double amount){return amount*(1+Qi);}
  public bool IsCharging {get{return ChargeReadyRound!=0;}}
  public RoundHitState HitState {get;internal set;}
  public bool Alive{get{return HP>0;}}
  public RoundFighter(string id,int side,int position,double hp,double attack,double defense,double crit,int level=1){
   CardId=id;Name=Data.Card(id).Name;Side=side;Position=position;Level=level;MaxHP=HP=hp;Attack=attack;Defense=defense;Crit=crit;
  }
 }
 public sealed class RoundAction {
  public RoundFighter Actor,Target;
  public RoundMove Move;
  public RoundHitState TargetState;
  internal RoundHitState RequiredState;
  internal RoundAction HitSource;
  internal RoundAction[] additionalHits;
  public IEnumerable<RoundAction> Hits {
   get {yield return this; if(additionalHits!=null)foreach(var hit in additionalHits)yield return hit;}
  }
  public int Round;
  public string Name;
  public double Amount,TargetHPAfter,TargetShieldAfter;
  public double HealthDamage,ShieldDamage;
  public int TargetRageAfter;
  public RoundAbility Ability, CounterAbility, GuardAbility;
  public double DefenseDelta,DodgeDelta,ActorHPCost;
  public string AvoidanceText {get{return CounterAbility==null?"闪避":CounterAbility.name+" · 免伤";}}
  public bool SequentialHits,Charging,Countered,EnhancedBasic;
  public bool ComboAttack {get;internal set;}
  public int ComboHits {get;internal set;}
  public int ComboAfter {get;internal set;}
  public bool Critical,Healing,Dodged,TriggersPursuit;
  public bool Revives {get{return Ability!=null&&Ability.trigger=="onDefeat"&&Ability.effect=="revive";}}
  public bool Detonates {get{return Ability!=null&&Ability.trigger=="onDefeat"&&Ability.effect=="detonate";}}
 }
 public sealed class RoundCombat {
  public const int RageCap=100,MaxRounds=40;
  readonly RoundDefinitions defs;
  readonly double damageScale;
  readonly Random random;
  readonly List<RoundAction> triggered=new List<RoundAction>();
  readonly Queue<RoundAction> pending=new Queue<RoundAction>(), roundStartActions=new Queue<RoundAction>();
  readonly RoundFighter[] order;
  readonly System.Collections.ObjectModel.ReadOnlyCollection<RoundFighter> view;
  int cursor;
  int comboBudget;
  public int ComboSide {get;private set;}
  public int ComboCount {get;private set;}
  public int Round {get;private set;}
  public int Result {get;private set;} // 0 running, 1 player, -1 enemy, 2 draw
  public IReadOnlyList<RoundFighter> Order {get{return view;}}
  public RoundCombat(IEnumerable<RoundFighter> fighters,RoundDefinitions definitions,int seed)
   :this(fighters,definitions,seed,RoundBoard.EnemyMin,1){}
  internal static RoundCombat Demo(IEnumerable<RoundFighter> fighters,RoundDefinitions definitions,int seed,double damageScale=1){
   return new RoundCombat(fighters,definitions,seed,1,damageScale);
  }
  RoundCombat(IEnumerable<RoundFighter> fighters,RoundDefinitions definitions,int seed,int enemyMin,double damageScale) {
   if(!(damageScale>0&&damageScale<=2))throw new ArgumentOutOfRangeException("damageScale");
   this.damageScale=damageScale;
   defs=definitions;defs.Validate();random=new Random(seed);
   var all=fighters.ToArray();
   if(all.Any(f=>f==null||f.Side<0||f.Side>1||f.Position<1||f.Position>RoundBoard.Cells||f.MaxHP<=0)||
    all.GroupBy(f=>f.Side*10+f.Position).Any(g=>g.Count()!=1))throw new InvalidOperationException("非法战场阵位");
   var p=all.Where(f=>f.Side==0).OrderBy(f=>f.Position).ToArray();
   var e=all.Where(f=>f.Side==1).OrderBy(f=>f.Position).ToArray();
   bool invalidSize=p.Length<1||p.Length>RoundBoard.PlayerLimit||e.Length<enemyMin||e.Length>RoundBoard.Cells;
   if(invalidSize||p.Select(f=>f.CardId).Distinct().Count()!=p.Length)throw new InvalidOperationException("非法上阵人数或重复卡");
   var assigned=new List<RoundFighter>();
   for(int i=0;i<Math.Max(p.Length,e.Length);i++){if(i<p.Length)assigned.Add(p[i]);if(i<e.Length)assigned.Add(e[i]);}
   order=assigned.ToArray(); view=Array.AsReadOnly(order);
   for(int i=0;i<order.Length;i++){
    var f=order[i];f.Hand=i+1;defs.ApplyOpening(f,order);
   }
   ComboSide=-1;
   BeginRound();
  }
  void BeginRound(){
   Round++;
   foreach(var fighter in order){
    var skills=defs.For(fighter.CardId).skills;
    var counter=skills.FirstOrDefault(skill=>skill.trigger=="onSingleTarget");
    fighter.CounterUses=counter==null?0:Math.Max(1,counter.usesPerRound);
    var phase=skills.FirstOrDefault(skill=>skill.trigger=="roundStart");
    if(phase==null)continue;
    fighter.PhaseAttackBonus=Round<phase.phaseRound?phase.value:-phase.lateAttackPenalty;
    fighter.PhaseDefenseBonus=Round<phase.phaseRound?0:phase.lateDefense;
   }
   foreach(var fighter in order.Where(f=>f.Alive&&!f.IsCharging))
    foreach(var skill in defs.For(fighter.CardId).skills.Where(s=>s.trigger=="onRoundStart"))
     roundStartActions.Enqueue(new RoundAction{Actor=fighter,Name=skill.name,Move=RoundMove.Skill,
      Ability=skill,Amount=skill.value,Round=Round});
  }
  RoundFighter Enemy(RoundFighter f){
   return order.Where(t=>t.Side!=f.Side&&t.Alive).OrderBy(t=>(t.Position-1)/3)
    .ThenBy(t=>Math.Abs(RoundBoard.Row(t.Position)-RoundBoard.Row(f.Position))).ThenBy(t=>t.Hand).FirstOrDefault();
  }
  internal IEnumerable<RoundAbility> ActiveTeamAttackSkills(RoundFighter recipient,Func<RoundFighter,bool> isAlive=null){
   var card=Data.Card(recipient.CardId);
   return order.Where(source=>source.Side==recipient.Side&&(isAlive==null?source.Alive:isAlive(source)))
    .SelectMany(source=>defs.For(source.CardId).skills)
    .Where(skill=>skill.trigger=="whileAlive"&&skill.teamAttackTags.Any(card.HasTag));
  }
  internal double EffectiveAttack(RoundFighter recipient){
   double opening=recipient.OpeningAttackBonus;
   double temporary=ActiveTeamAttackSkills(recipient).Sum(skill=>skill.value)+recipient.AttackBonus(Round);
   return recipient.Attack*Math.Max(0,1+opening+temporary)/(1+opening);
  }
  void CountCombo(RoundAction hit){
   int side=hit.Actor.Side;
   hit.ComboAttack=true;
   if(ComboSide!=side){ComboSide=side;ComboCount=0;}
   if(!hit.Dodged){
    var rules=defs.For(hit.Actor.CardId).techniques;
    bool grouped=false;
    int configured=hit.Ability==null?0:hit.Ability.comboHits;
    int fallback=1;
    if(rules!=null){
     if(hit.Move==RoundMove.Basic)configured=rules.basicComboHits;
     else if(hit.Move==RoundMove.Ultimate){
      configured=rules.ultimateComboHits;
      fallback=Math.Max(1,Math.Max(rules.ultimateHits,rules.ultimateTargets));
      if(rules.ultimateFrontRow||rules.ultimateFrontColumn)fallback=3;
      grouped=fallback>1;
     }else if(hit.Move==RoundMove.Pursuit){
      configured=rules.pursuitComboHits;
      fallback=Math.Max(1,rules.pursuitHits);
      grouped=fallback>1;
     }
    }
    if(comboBudget<0)comboBudget=configured>0?configured:fallback;
    hit.ComboHits=grouped?Math.Min(1,comboBudget):comboBudget;
    comboBudget-=hit.ComboHits;
    ComboCount+=hit.ComboHits;
   }
   hit.ComboAfter=ComboCount;
   if(hit.ComboHits==0||Result!=0)return;
   foreach(var follower in order){
    if(follower.Side!=side||!follower.Alive||follower.IsCharging||follower.ComboQueuedRound==Round||!CanPursue(follower))continue;
    var kit=defs.For(follower.CardId);
    int threshold=kit.techniques==null?0:kit.techniques.pursuitCombo;
    if(threshold==0||ComboCount<threshold)continue;
    follower.ComboQueuedRound=Round;
    Enqueue(follower,hit.Target,kit.pursuit,kit.pursuitPower,RoundMove.Pursuit,false);
    hit.TriggersPursuit=true;
   }
  }
  RoundFighter ReadyUltimate(){return order.FirstOrDefault(f=>f.Alive&&!f.IsCharging&&f.Rage>=RageCap);}
  RoundFighter RandomEnemy(RoundFighter actor){
   var enemies=order.Where(f=>f.Side!=actor.Side&&f.Alive).ToArray();
   return enemies[random.Next(enemies.Length)];
  }
  bool CanUseQueued(RoundAction action){
   if(action.Revives)return !action.Actor.Alive&&!action.Actor.HasRevived;
   if(action.Detonates)return !action.Actor.Alive&&!action.Actor.HasDetonated;
   if(action.Ability!=null&&action.Ability.trigger=="onRoundStart")
    return action.Actor.Alive&&!action.Actor.IsCharging&&Enemy(action.Actor)!=null;
   return action.Actor.Alive&&!action.Actor.IsCharging&&(!action.Healing||action.Target.Alive)&&
    (action.Ability==null||action.Ability.trigger!="onSingleTarget"||action.Target.Alive)&&
    (action.Move!=RoundMove.Pursuit||CanPursue(action.Actor))&&
    (action.RequiredState==RoundHitState.None||action.Target.Alive&&action.Target.HitState==action.RequiredState&&action.Target.HitSource==action.HitSource);
  }
  bool CanPursue(RoundFighter actor){
   var techniques=defs.For(actor.CardId).techniques;
   int maximum=Math.Max(1,techniques==null?0:techniques.pursuitLimit);
   return actor.PursuedRound!=Round||actor.PursuitCount<maximum;
  }
  void CountPursuit(RoundFighter actor){
   if(actor.PursuedRound!=Round){actor.PursuedRound=Round;actor.PursuitCount=0;}
   actor.PursuitCount++;
  }
  RoundAction NextTriggered(){
   return Result==0?triggered.FirstOrDefault(CanUseQueued):null;
  }
  public RoundFighter NextPursuitTarget {
   get {
    if(Result!=0||ReactionSkillNext||ReadyUltimate()!=null)return null;
    var next=pending.FirstOrDefault(CanUseQueued);
    return next!=null&&next.Move==RoundMove.Pursuit?next.Target:null;
   }
  }
  internal RoundFighter NextRevivingFighter {
   get {var next=NextTriggered();return next!=null&&next.Revives?next.Actor:null;}
  }
  public bool PursuitNext {get{return NextPursuitTarget!=null;}}
  public bool ReactionSkillNext {get{return Result==0&&(NextTriggered()!=null||roundStartActions.Any(CanUseQueued));}}
  public bool CounterNext {
   get {var next=NextTriggered();return next!=null&&next.Ability.trigger=="onSingleTarget";}
  }

  RoundAction TakeTriggered(){
   while(triggered.Count>0){
    var action=triggered[0];
    triggered.RemoveAt(0);
    if(CanUseQueued(action))return action;
   }
   return null;
  }

  RoundAction TakeQueued(Queue<RoundAction> queue){
   while(queue.Count>0){
    var action=queue.Dequeue();
    if(CanUseQueued(action))return action;
   }
   return null;
  }
  static RoundAction ApplyHealing(RoundAction action){
   var target=action.Target;
   action.Amount=Math.Min(target.MaxHP-target.HP,target.ReceivedAid(action.Amount));
   target.HP+=action.Amount;
   action.TargetHPAfter=target.HP;action.TargetShieldAfter=target.Shield;action.TargetRageAfter=target.Rage;
   return action;
  }

  internal bool HasPendingDetonation(RoundFighter fighter){
   return triggered.Any(action=>action.Detonates&&action.Actor==fighter&&CanUseQueued(action));
  }

  void QueueDefeat(RoundFighter fighter,RoundFighter killer){
   var skill=defs.For(fighter.CardId).skills.FirstOrDefault(s=>s.trigger=="onDefeat");
   if(skill==null)return;
   if(skill.effect=="revive"&&!fighter.HasRevived)
    Enqueue(fighter,fighter,skill.name,fighter.MaxHP*skill.value,RoundMove.Skill,true,ability:skill);
   else if(skill.effect=="detonate"&&!fighter.HasDetonated)
    Enqueue(fighter,killer,skill.name,skill.value,RoundMove.Skill,false,ability:skill);
  }

  void SettleBattleResult(){
   bool player=order.Any(f=>f.Side==0&&f.Alive);
   bool enemy=order.Any(f=>f.Side==1&&f.Alive);
   if(player&&enemy||triggered.Any(action=>(action.Revives||action.Detonates)&&CanUseQueued(action)))return;
   Result=player?1:enemy?-1:2;
  }

  void GrantDefeatAttack(RoundAction action,double bonus){
   foreach(var ally in order.Where(f=>f.Side==action.Actor.Side&&f.Alive&&action.Ability.teamAttackTags.Any(Data.Card(f.CardId).HasTag)))
    ally.ChangeAttack(bonus,Round,-1,action.Ability);
  }

  RoundAction ResolveDetonation(RoundAction action){
   action.Actor.HasDetonated=true;
   var hit=action.Target.Alive?Strike(action.Actor,action.Target,action.Name,action.Amount,RoundMove.Skill,allowCounter:false,ability:action.Ability):
    new RoundAction{Actor=action.Actor,Target=action.Target,Name=action.Name,Move=RoundMove.Skill,Round=Round,
     TargetHPAfter=action.Target.HP,TargetShieldAfter=action.Target.Shield,TargetRageAfter=action.Target.Rage};
   hit.Ability=action.Ability;
   GrantDefeatAttack(action,action.Ability.defeatAttackBonus);
   ApplyReaction(hit);
   SettleBattleResult();
   return hit;
  }

  RoundAction ResolveRevival(RoundAction action){
   var fighter=action.Actor;
   fighter.HasRevived=true;
   fighter.ChargeReadyRound=0;
   fighter.HitState=RoundHitState.None;fighter.HitSource=null;
   fighter.HP=action.Amount;
   GrantDefeatAttack(action,action.Ability.reviveAttackBonus);
   action.TargetHPAfter=fighter.HP;action.TargetShieldAfter=fighter.Shield;action.TargetRageAfter=fighter.Rage;
   return action;
  }

  RoundAction ResolveQueued(RoundAction action){
   if(action.Revives)return ResolveRevival(action);
   if(action.Detonates)return ResolveDetonation(action);
   if(action.Healing)return ApplyHealing(action);
   if(action.Move==RoundMove.Pursuit){
    CountPursuit(action.Actor);
    var techniques=defs.For(action.Actor.CardId).techniques;
    if(techniques!=null&&techniques.pursuitCombo>0)return UseScatterVolley(action,techniques);
   }
   var target=action.Ability!=null&&action.Ability.trigger=="onRoundStart"?RandomEnemy(action.Actor):
    action.Target.Alive?action.Target:Enemy(action.Actor);
   var hit=Strike(action.Actor,target,action.Name,action.Amount,
    action.Move,allowCounter:action.Ability==null||action.Ability.effect!="counter",ability:action.Ability);
   if(hit.Ability!=null&&hit.Ability.trigger=="onSingleTarget"&&!hit.Dodged)hit.TargetState=hit.Ability.reaction;
   if(hit.Move==RoundMove.Pursuit){
    if(hit.Dodged&&action.HitSource!=null&&hit.Target.HitSource==action.HitSource){
     hit.Target.HitState=RoundHitState.None;hit.Target.HitSource=null;
    }
    var rules=defs.For(hit.Actor.CardId).techniques;
    if(!hit.Dodged&&rules!=null&&rules.pursuitDefenseRounds!=0){
     hit.DefenseDelta=rules.pursuitDefenseIncrease;
     hit.Actor.ChangeDefense(hit.DefenseDelta,Round,rules.pursuitDefenseRounds);
    }
   }
   ApplyReaction(hit);
   return hit;
  }

  // Capture display state before advancing the simulation exactly once.
  internal RoundStep ResolveStep(bool captureVitals){
   var before=captureVitals?order.ToDictionary(fighter=>fighter,fighter=>new RoundVitals(fighter,freezeChanges:true)):null;
   return new RoundStep(Step(),before);
  }

  public RoundAction Step(){
   if(Result!=0)return null;
   comboBudget=-1;
   var interrupt=TakeTriggered();
   if(interrupt!=null)return ResolveQueued(interrupt);
   var opening=TakeQueued(roundStartActions);
   if(opening!=null)return ResolveQueued(opening);
   var ready=ReadyUltimate();
   if(ready!=null){
    ready.Rage-=RageCap;var kit=defs.For(ready.CardId);
    return UseUltimate(ready,kit);
   }
   var next=TakeQueued(pending);
   if(next!=null)return ResolveQueued(next);
   while(true){
    if(cursor==order.Length){
     if(Round>=MaxRounds){Result=2;return null;}
     cursor=0;BeginRound();return Step();
    }
    var f=order[cursor++];if(!f.Alive)continue;
    f.HitState=RoundHitState.None;f.HitSource=null;
    var k=defs.For(f.CardId);
    if(f.IsCharging){
     if(Round<f.ChargeReadyRound)continue;
     f.ChargeReadyRound=0;
     var weakest=order.Where(enemy=>enemy.Side!=f.Side&&enemy.Alive)
      .OrderBy(enemy=>enemy.HP/enemy.MaxHP).ThenBy(enemy=>enemy.Hand).First();
     var release=Strike(f,weakest,k.ultimate,k.ultimatePower,RoundMove.Ultimate);
     ApplyReaction(release);
     return release;
    }
    if(k.techniques!=null&&k.techniques.basicHealsSelf){
     f.Rage=Math.Min(RageCap,f.Rage+35);
     return ApplyHealing(new RoundAction{Actor=f,Target=f,Name=k.basic,Move=RoundMove.Basic,
      Healing=true,Amount=k.basicPower*EffectiveAttack(f),Round=Round});
    }
    var target=k.techniques!=null&&k.techniques.basicRandomTarget?RandomEnemy(f):Enemy(f);
    bool enhanced=k.techniques!=null&&k.techniques.basicEnhancedChance>0&&random.NextDouble()<k.techniques.basicEnhancedChance;
    var applied=Strike(f,target,k.basic,enhanced?k.techniques.basicEnhancedPower:k.basicPower,RoundMove.Basic);
    applied.EnhancedBasic=enhanced;
    ApplyReaction(applied);
    if(Result!=0)return applied;
    if(applied.Critical&&!applied.TriggersPursuit){
     var follow=order.FirstOrDefault(a=>a.Side==f.Side&&a!=f&&a.Alive&&!a.IsCharging&&CanPursue(a)&&defs.For(a.CardId).techniques==null);
     if(follow!=null){
      var p=defs.For(follow.CardId);
      Enqueue(follow,target,p.pursuit,p.pursuitPower,RoundMove.Pursuit,false);
      applied.TriggersPursuit=true;
     }
    }
    foreach(var s in k.skills.Where(s=>s.trigger=="afterBasic")){
     if(s.effect=="strike"&&!applied.Dodged&&target.HP<=target.MaxHP*.5)Enqueue(f,target,s.name,s.value,RoundMove.Skill,false,ability:s);
     else if(s.effect=="aid"){
      var ally=order.Where(a=>a.Side==f.Side&&a.Alive&&a.HP<a.MaxHP*.65).OrderBy(a=>a.HP/a.MaxHP).ThenBy(a=>a.Hand).FirstOrDefault();
      if(ally!=null)Enqueue(f,ally,s.name,s.value*ally.MaxHP,RoundMove.Skill,true,ability:s);
     }
    }
    return applied;
   }
  }
  RoundAction UseUltimate(RoundFighter actor,RoundKit kit){
   if(kit.techniques!=null&&kit.techniques.ultimateChargeRounds>0){
    actor.ChargeReadyRound=Round+kit.techniques.ultimateChargeRounds;
    return new RoundAction{Actor=actor,Target=actor,Name=kit.ultimate,Move=RoundMove.Ultimate,Round=Round,Charging=true};
   }
   if(kit.techniques!=null&&kit.techniques.ultimateTargets>0)return UseDistinctArea(actor,kit);
   var front=Enemy(actor);
   var candidates=order.Where(target=>target.Alive&&target.Side!=actor.Side);
   if(kit.techniques!=null&&kit.techniques.ultimateHighestAttack)
    front=candidates.OrderByDescending(EffectiveAttack).ThenBy(target=>target.Hand).First();
   else if(kit.techniques!=null&&kit.techniques.ultimateLowestDefense)
    front=candidates.OrderBy(target=>target.EffectiveDefense(Round)).ThenBy(target=>target.Hand).First();
   if(kit.techniques!=null&&kit.techniques.ultimateHits>0)return UseEnergyVolley(actor,kit);
   if(kit.techniques==null)return Strike(actor,front,kit.ultimate,kit.ultimatePower,RoundMove.Ultimate);

   var rules=kit.techniques;
   bool area=rules.ultimateFrontRow||rules.ultimateFrontColumn;
   // Area attacks anchor on the ordinary front target, then cover its row or column.
   var others=area?order.Where(target=>target.Alive&&target.Side!=actor.Side&&target!=front&&
    (rules.ultimateFrontRow?RoundBoard.Row(target.Position)==RoundBoard.Row(front.Position):
     RoundBoard.Column(target.Position,target.Side)==RoundBoard.Column(front.Position,front.Side))).ToArray():new RoundFighter[0];
   double cost=Math.Min(Math.Max(0,actor.HP-1),actor.MaxHP*rules.ultimateHealthCost);
   actor.HP-=cost;
   var first=Strike(actor,front,kit.ultimate,kit.ultimatePower,RoundMove.Ultimate,allowCounter:!area);
   first.ActorHPCost=cost;
   if(others.Length>0)first.additionalHits=others.Select(target=>
    Strike(actor,target,kit.ultimate,kit.ultimatePower,RoundMove.Ultimate,allowCounter:false)).ToArray();
   var eligible=first.Hits.Where(hit=>!hit.Dodged&&hit.Target.Alive).ToArray();
   if(eligible.Length>0&&kit.techniques.ultimateReaction!=RoundHitState.None)
    eligible[random.Next(eligible.Length)].TargetState=kit.techniques.ultimateReaction;
   foreach(var hit in first.Hits){
    if(!hit.Dodged&&hit.Target.Alive&&kit.techniques.ultimateDefenseRounds!=0){
     hit.DefenseDelta=-kit.techniques.ultimateDefenseReduction;
     hit.Target.ChangeDefense(hit.DefenseDelta,Round,kit.techniques.ultimateDefenseRounds);
    }
    ApplyReaction(hit);
   }
   return first;
  }

  RoundAction UseDistinctArea(RoundFighter actor,RoundKit kit){
   var rules=kit.techniques;
   var enemies=order.Where(target=>target.Alive&&target.Side!=actor.Side).ToList();
   var hits=new List<RoundAction>();
   for(int shot=0;shot<rules.ultimateTargets&&enemies.Count>0;shot++){
    int index=random.Next(enemies.Count);
    var hit=Strike(actor,enemies[index],kit.ultimate,kit.ultimatePower,RoundMove.Ultimate,allowCounter:false);
    enemies.RemoveAt(index);
    ApplyReaction(hit);hits.Add(hit);
   }
   actor.ChangeAttack(rules.ultimateAttackBonus,Round,rules.ultimateAttackRounds);
   hits[0].additionalHits=hits.Skip(1).ToArray();
   return hits[0];
  }

  RoundAction UseEnergyVolley(RoundFighter actor,RoundKit kit){
   var enemySide=order.Where(target=>target.Side!=actor.Side).ToArray();
   var available=enemySide.Where(target=>target.Alive).ToList();
   var hits=new List<RoundAction>();
   for(int shot=0;shot<kit.techniques.ultimateHits&&Result==0;shot++){
    available.RemoveAll(target=>!target.Alive);
    if(available.Count==0&&!kit.techniques.ultimateDistinctVolley)available.AddRange(enemySide.Where(target=>target.Alive));
    if(available.Count==0)break;
    int index=random.Next(available.Count);
    var recipient=available[index];
    available.RemoveAt(index);
    var hit=Strike(actor,recipient,kit.ultimate,kit.ultimatePower,RoundMove.Ultimate,allowCounter:false);
    ApplyReaction(hit);
    hits.Add(hit);
   }
   var first=hits[0];
   first.additionalHits=hits.Skip(1).ToArray();
   first.SequentialHits=true;
   return first;
  }

  RoundAction UseScatterVolley(RoundAction action,RoundTechnique techniques){
   var hits=new List<RoundAction>();
   for(int shot=0;shot<techniques.pursuitHits&&Result==0;shot++){
    if(Enemy(action.Actor)==null)break;
    var hit=Strike(action.Actor,RandomEnemy(action.Actor),action.Name,action.Amount,RoundMove.Pursuit,allowCounter:false);
    ApplyReaction(hit);
    hits.Add(hit);
   }
   var first=hits[0];
   first.additionalHits=hits.Skip(1).ToArray();
   first.SequentialHits=true;
   first.DodgeDelta=techniques.pursuitDodgeIncrease;
   action.Actor.ChangeDodge(first.DodgeDelta,Round,techniques.pursuitDodgeRounds==0?-1:techniques.pursuitDodgeRounds);
   return first;
  }

  void ApplyReaction(RoundAction hit){
   if(hit.Dodged||hit.Healing)return;
   hit.Target.HitState=hit.TargetState;hit.Target.HitSource=hit;
   if(!hit.Target.Alive||hit.TargetState==RoundHitState.None)return;
   var follower=order.FirstOrDefault(f=>{
    if(f.Side!=hit.Actor.Side||!f.Alive||f.IsCharging||!CanPursue(f))return false;
    var rules=defs.For(f.CardId).techniques;
    if(rules==null)return false;
    if(string.IsNullOrEmpty(rules.pursuitSourceTag))return rules.pursuitTrigger==hit.TargetState;
    return hit.Actor!=f&&Data.Card(hit.Actor.CardId).HasTag(rules.pursuitSourceTag)&&
     hit.TargetState!=RoundHitState.Standing;
   });
   if(follower==null)return;
   var kit=defs.For(follower.CardId);
   Enqueue(follower,hit.Target,kit.pursuit,kit.pursuitPower,RoundMove.Pursuit,false,hit.TargetState,source:hit);
   hit.TriggersPursuit=true;
  }

  void Enqueue(RoundFighter actor,RoundFighter target,string name,double amount,RoundMove move,bool healing,
   RoundHitState required=RoundHitState.None,RoundAbility ability=null,RoundAction source=null){
   var action=new RoundAction{Actor=actor,Target=target,Name=name,Amount=amount,Move=move,Healing=healing,
    Round=Round,RequiredState=required,Ability=ability,HitSource=source};
   if(ability==null)pending.Enqueue(action);
   else triggered.Add(action);
  }
  RoundAbility TryCounter(RoundFighter attacker,RoundFighter defender){
   if(defender.IsCharging||defender.CounterUses<=0)return null;
   var ability=defs.For(defender.CardId).skills.FirstOrDefault(skill=>skill.trigger=="onSingleTarget");
   if(ability==null)return null;
   defender.CounterUses--;defender.CounterRound=Round;
   Enqueue(defender,attacker,ability.name,ability.value,RoundMove.Skill,false,ability:ability);
   return ability;
  }
  RoundAction Strike(RoundFighter actor,RoundFighter target,string name,double power,RoundMove move,
   bool allowCounter=true,RoundAbility ability=null){
   if(move==RoundMove.Basic)actor.Rage=Math.Min(RageCap,actor.Rage+35);
   bool dodged=random.NextDouble()<target.EffectiveDodge(Round,actor.Accuracy);
   var counter=!dodged&&allowCounter&&move!=RoundMove.Basic?TryCounter(actor,target):null;
   if(dodged||counter!=null){
    var avoided=new RoundAction{Actor=actor,Target=target,Name=name,Dodged=true,Countered=counter!=null,CounterAbility=counter,
     Ability=ability,Move=move,Round=Round,TargetHPAfter=target.HP,TargetShieldAfter=target.Shield,TargetRageAfter=target.Rage};
    CountCombo(avoided);
    return avoided;
   }
   double critChance=RoundFighter.UnitProbability(actor.Crit-target.Tenacity);
   bool crit=(move==RoundMove.Basic||critChance>0)&&random.NextDouble()<critChance;
   double defense=target.EffectiveDefense(Round)*(1-RoundFighter.UnitProbability(actor.Penetration));
   double damage=Math.Max(1,(EffectiveAttack(actor)-defense)*power*damageScale)*(crit?1.5:1);
   var attacker=Data.Card(actor.CardId);
   double reduction=defs.OpeningSkills(target.CardId)
    .Where(skill=>skill.resistTags!=null&&skill.resistTags.Any(attacker.HasTag)).Sum(skill=>skill.finalReduction);
   if(target.IsCharging)reduction+=defs.For(target.CardId).techniques.ultimateChargeReduction;
   var guard=defs.For(target.CardId).skills.FirstOrDefault(skill=>skill.trigger=="onReceive");
   if(guard!=null&&random.NextDouble()<guard.chance)reduction+=guard.value;
   else guard=null;
   damage*=1-Math.Min(1,reduction);
   double shield=Math.Min(target.Shield,damage);target.Shield-=shield;
   double healthBefore=target.HP;
   target.HP=Math.Max(0,target.HP-(damage-shield));
   if(move==RoundMove.Basic&&target.Alive)target.Rage=Math.Min(RageCap,target.Rage+15);
   if(healthBefore>0&&!target.Alive){
    QueueDefeat(target,actor);
    SettleBattleResult();
   }
   var hit=new RoundAction{Actor=actor,Target=target,Name=name,Amount=damage,Critical=crit,Move=move,Round=Round,
    Ability=ability,GuardAbility=guard,TargetState=HitState(actor,move),TargetHPAfter=target.HP,TargetShieldAfter=target.Shield,
    TargetRageAfter=target.Rage,HealthDamage=healthBefore-target.HP,ShieldDamage=shield};
   CountCombo(hit);
   return hit;
  }
  RoundHitState HitState(RoundFighter actor,RoundMove move){
   var rules=defs.For(actor.CardId).techniques;
   if(rules==null)return RoundHitState.None;
   if(move==RoundMove.Pursuit)return rules.pursuitCombo>0?RoundHitState.Standing:rules.pursuitReaction;
   if(move==RoundMove.Basic&&random.NextDouble()<rules.basicReactionChance)return rules.basicReaction;
   return RoundHitState.Standing;
  }
 }
}
