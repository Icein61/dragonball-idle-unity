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
 [Serializable] public sealed class RoundAbility {
  public string name,trigger,effect;
  public double value;
 }
 [Serializable] public sealed class RoundKit {
  public string cardId;
  public string basic,ultimate,pursuit;
  public double basicPower,ultimatePower,pursuitPower;
  public RoundAbility[] skills;
 }
 [Serializable] public sealed class RoundDefinitions {
  public RoundKit[] cards;
  public RoundKit For(string id){return cards.Single(c=>c.cardId==id);}
  public void ApplyOpening(RoundFighter fighter){
   foreach(var s in For(fighter.CardId).skills.Where(s=>s.trigger=="opening")){
    if(s.effect=="attack")fighter.Attack*=1+s.value;
    else if(s.effect=="defense")fighter.Defense*=1+s.value;
    else if(s.effect=="shield")fighter.Shield+=s.value*fighter.MaxHP;
   }
  }
  public void Validate(){
   if(cards==null||cards.Length!=Data.Cards.Length||cards.Any(k=>k==null||Data.Index(k.cardId)<0)||cards.Select(k=>k.cardId).Distinct().Count()!=cards.Length)
    throw new InvalidOperationException("回合技能配置缺卡或重复");
   foreach(var k in cards){
    var c=Data.Card(k.cardId);
    if(string.IsNullOrEmpty(k.basic)||string.IsNullOrEmpty(k.ultimate)||string.IsNullOrEmpty(k.pursuit)||
     !Bounded(k.basicPower,4)||!Bounded(k.ultimatePower,4)||!Bounded(k.pursuitPower,4)||
     k.skills==null||k.skills.Length!=(c.Rarity=="C"?1:2)||k.skills.Any(s=>s==null||string.IsNullOrEmpty(s.name)||
      !(s.trigger=="opening"&&(s.effect=="attack"||s.effect=="defense"||s.effect=="shield")||
        s.trigger=="afterBasic"&&(s.effect=="strike"||s.effect=="aid"))||!Bounded(s.value,1)))
     throw new InvalidOperationException("无效回合技能配置："+k.cardId);
   }
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
 public sealed class RoundFighter {
  public readonly string CardId,Name;
  public readonly int Side,Position;
  public readonly double MaxHP;
  public double HP,Attack,Defense,Crit,Shield,Dodge=.04;
  public int Rage;
  public int Hand {get;internal set;}
  internal int PursuedRound;
  public bool Alive{get{return HP>0;}}
  public RoundFighter(string id,int side,int position,double hp,double attack,double defense,double crit){
   CardId=id;Name=Data.Card(id).Name;Side=side;Position=position;MaxHP=HP=hp;Attack=attack;Defense=defense;Crit=crit;
  }
 }
 public sealed class RoundAction {
  public RoundFighter Actor,Target;
  public RoundMove Move;
  public int Round;
  public string Name;
  public double Amount;
  public bool Critical,Healing,Dodged;
 }
 public sealed class RoundCombat {
  public const int RageCap=100,MaxRounds=40;
  readonly RoundDefinitions defs;
  readonly Random random;
  readonly Queue<RoundAction> pending=new Queue<RoundAction>();
  readonly RoundFighter[] order;
  readonly System.Collections.ObjectModel.ReadOnlyCollection<RoundFighter> view;
  int cursor;
  public int Round {get;private set;}
  public int Result {get;private set;} // 0 running, 1 player, -1 enemy, 2 draw
  public IReadOnlyList<RoundFighter> Order {get{return view;}}
  public RoundCombat(IEnumerable<RoundFighter> fighters,RoundDefinitions definitions,int seed) {
   defs=definitions;defs.Validate();random=new Random(seed);
   var all=fighters.ToArray();
   if(all.Any(f=>f==null||f.Side<0||f.Side>1||f.Position<1||f.Position>RoundBoard.Cells||f.MaxHP<=0)||
    all.GroupBy(f=>f.Side*10+f.Position).Any(g=>g.Count()!=1))throw new InvalidOperationException("非法战场阵位");
   var p=all.Where(f=>f.Side==0).OrderBy(f=>f.Position).ToArray();
   var e=all.Where(f=>f.Side==1).OrderBy(f=>f.Position).ToArray();
   if(p.Length<1||p.Length>RoundBoard.PlayerLimit||e.Length<RoundBoard.EnemyMin||e.Length>RoundBoard.Cells||
    p.Select(f=>f.CardId).Distinct().Count()!=p.Length)throw new InvalidOperationException("非法上阵人数或重复卡");
   var assigned=new List<RoundFighter>();
   for(int i=0;i<Math.Max(p.Length,e.Length);i++){if(i<p.Length)assigned.Add(p[i]);if(i<e.Length)assigned.Add(e[i]);}
   order=assigned.ToArray(); view=Array.AsReadOnly(order);
   for(int i=0;i<order.Length;i++){
    var f=order[i];f.Hand=i+1;defs.ApplyOpening(f);
   }
   Round=1;
  }
  RoundFighter Enemy(RoundFighter f){
   return order.Where(t=>t.Side!=f.Side&&t.Alive).OrderBy(t=>(t.Position-1)/3)
    .ThenBy(t=>Math.Abs(RoundBoard.Row(t.Position)-RoundBoard.Row(f.Position))).ThenBy(t=>t.Hand).FirstOrDefault();
  }
  public RoundAction Step(){
   if(Result!=0)return null;
   var ready=order.FirstOrDefault(f=>f.Alive&&f.Rage>=RageCap);
   if(ready!=null){
    ready.Rage-=RageCap;var kit=defs.For(ready.CardId);
    return Strike(ready,Enemy(ready),kit.ultimate,kit.ultimatePower,RoundMove.Ultimate);
   }
   while(pending.Count>0){
    var action=pending.Dequeue();
    if(!action.Actor.Alive)continue;
    if(action.Healing){
     if(!action.Target.Alive)continue;
     action.Amount=Math.Min(action.Target.MaxHP-action.Target.HP,action.Amount);action.Target.HP+=action.Amount;
     return action;
    }
    return Strike(action.Actor,action.Target.Alive?action.Target:Enemy(action.Actor),action.Name,action.Amount,action.Move);
   }
   while(true){
    if(cursor==order.Length){
     if(Round>=MaxRounds){Result=2;return null;}
     cursor=0;Round++;
    }
    var f=order[cursor++];if(!f.Alive)continue;
    var k=defs.For(f.CardId);var target=Enemy(f);
    var applied=Strike(f,target,k.basic,k.basicPower,RoundMove.Basic);
    if(Result!=0)return applied;
    if(applied.Critical){
     var follow=order.FirstOrDefault(a=>a.Side==f.Side&&a!=f&&a.Alive&&a.PursuedRound!=Round);
     if(follow!=null){follow.PursuedRound=Round;var p=defs.For(follow.CardId);Enqueue(follow,target,p.pursuit,p.pursuitPower,RoundMove.Pursuit,false);}
    }
    foreach(var s in k.skills.Where(s=>s.trigger=="afterBasic")){
     if(s.effect=="strike"&&!applied.Dodged&&target.HP<=target.MaxHP*.5)Enqueue(f,target,s.name,s.value,RoundMove.Skill,false);
     else if(s.effect=="aid"){
      var ally=order.Where(a=>a.Side==f.Side&&a.Alive&&a.HP<a.MaxHP*.65).OrderBy(a=>a.HP/a.MaxHP).ThenBy(a=>a.Hand).FirstOrDefault();
      if(ally!=null)Enqueue(f,ally,s.name,s.value*ally.MaxHP,RoundMove.Skill,true);
     }
    }
    return applied;
   }
  }
  void Enqueue(RoundFighter actor,RoundFighter target,string name,double amount,RoundMove move,bool healing){
   pending.Enqueue(new RoundAction{Actor=actor,Target=target,Name=name,Amount=amount,Move=move,Healing=healing,Round=Round});
  }
  RoundAction Strike(RoundFighter actor,RoundFighter target,string name,double power,RoundMove move){
   if(move==RoundMove.Basic)actor.Rage=Math.Min(RageCap,actor.Rage+35);
   if(random.NextDouble()<target.Dodge)
    return new RoundAction{Actor=actor,Target=target,Name=name,Dodged=true,Move=move,Round=Round};
   bool crit=move==RoundMove.Basic&&random.NextDouble()<Math.Max(0,Math.Min(1,actor.Crit));
   double damage=Math.Max(1,actor.Attack*power*100/(100+Math.Max(0,target.Defense))*(crit?1.5:1));
   double shield=Math.Min(target.Shield,damage);target.Shield-=shield;
   target.HP=Math.Max(0,target.HP-(damage-shield));
   if(move==RoundMove.Basic&&target.Alive)target.Rage=Math.Min(RageCap,target.Rage+15);
   if(!order.Any(f=>f.Side==target.Side&&f.Alive))Result=actor.Side==0?1:-1;
   return new RoundAction{Actor=actor,Target=target,Name=name,Amount=damage,Critical=crit,Move=move,Round=Round};
  }
 }
}
