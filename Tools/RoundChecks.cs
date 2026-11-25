using System;
using System.Linq;
using System.Collections.Generic;
using DragonIdle;
static class RoundChecks {
 static RoundDefinitions Rules(){
  return new RoundDefinitions{cards=Data.Cards.Select(c=>new RoundKit{
   cardId=c.Id,basic="普攻",ultimate="必杀",pursuit="追打",basicPower=1,ultimatePower=2,pursuitPower=.45,
   skills=c.Rarity=="C"?new[]{new RoundAbility{name="补刀",trigger="afterBasic",effect="strike",value=.25}}:
    new[]{new RoundAbility{name="起势",trigger="opening",effect="attack",value=.05},new RoundAbility{name="援助",trigger="afterBasic",effect="aid",value=.1}}
  }).ToArray()};
 }
 static RoundFighter Fighter(int card,int side,int p){return new RoundFighter(Data.Cards[card].Id,side,p,1200,100,40,0){Dodge=0};}
 static RoundFighter[] Fighters(int allies,int enemies){return Enumerable.Range(0,allies).Select(i=>Fighter(i,0,i+1)).Concat(Enumerable.Range(0,enemies).Select(i=>Fighter(i,1,i+1))).ToArray();}
 static void CheckReferenceKit(Action<bool,string> check){
  var rules=Rules();
  var kit=rules.For("goku-test");
  kit.basicPower=.60;kit.ultimatePower=.80;kit.pursuitPower=.20;
  kit.techniques=new RoundTechnique{basicReactionChance=1,basicReaction=RoundHitState.Knockdown,pursuitLimit=2,ultimateFrontRow=true,
   ultimateReaction=RoundHitState.Knockback,pursuitTrigger=RoundHitState.Knockdown,pursuitReaction=RoundHitState.LargeFloat};
  kit.skills=new[]{new RoundAbility{name="修炼的成果",trigger="opening",effect="attack",value=.20,allyTag=Data.RivalTag,allyDefense=.20},
   new RoundAbility{name="待填写",trigger="reserved",effect="none"}};
  var group=Fighters(2,9);
  var goku=group[0];
  var battle=new RoundCombat(group,rules,3);
  check(Math.Abs(goku.Attack-120)<.001&&Math.Abs(goku.Defense-48)<.001,"RR01 training with allied Rival grants both listed modifiers");
  var solo=Fighter(0,0,1);
  var enemy=Fighter(1,1,1);
  battle=RoundCombat.Demo(new[]{solo,enemy},rules,3);
  check(Math.Abs(solo.Attack-120)<.001&&solo.Defense==40,"RR02 enemy Rival does not grant the allied defense modifier");
  var sweep=battle.Step();
  bool readyPursuit=battle.PursuitNext;
  var chase=battle.Step();
  check(readyPursuit&&!battle.PursuitNext&&sweep.TargetState==RoundHitState.Knockdown&&sweep.TriggersPursuit&&
   chase.Move==RoundMove.Pursuit&&chase.Actor==solo&&chase.Target==enemy&&chase.TargetState==RoundHitState.LargeFloat&&
   chase.Amount<sweep.Amount,"RR03 knockdown queues one uppercut into large float");
  solo=Fighter(0,0,1);solo.Rage=100;
  group=new[]{solo}.Concat(Fighters(1,9).Skip(1)).ToArray();
  group.First(f=>f.Side==1&&f.Position==4).Dodge=1;
  battle=new RoundCombat(group,rules,3);
  var beam=battle.Step();
  check(beam.Hits.Count()==3&&beam.Hits.All(hit=>RoundBoard.Row(hit.Target.Position)==0)&&
   beam.Hits.Count(hit=>hit.Dodged)==1&&beam.Hits.Count(hit=>hit.TargetState==RoundHitState.Knockback)==1&&
   group.Where(f=>f.Side==1&&RoundBoard.Row(f.Position)!=0).All(f=>f.HP==1200)&&!beam.TriggersPursuit,
   "RR04 front row resolves once, knockback selects one landed enemy, other rows are untouched");
  check(battle.Step().Move==RoundMove.Basic,"RR05 knockback does not activate the knockdown pursuit");
  kit.techniques.basicReactionChance=0;
  solo=Fighter(0,0,1);enemy=Fighter(0,1,1);solo.Crit=1;
  battle=RoundCombat.Demo(new[]{solo,enemy},rules,3);
  var ordinary=battle.Step();
  check(ordinary.Critical&&ordinary.TargetState==RoundHitState.Standing&&!ordinary.TriggersPursuit,
   "RR06 a critical without knockdown does not start a sweep or uppercut");
  kit.techniques.basicReactionChance=1;
  solo=Fighter(0,0,1);enemy=Fighter(0,1,1);enemy.Dodge=1;
  battle=RoundCombat.Demo(new[]{solo,enemy},rules,3);
  var miss=battle.Step();
  check(miss.Dodged&&miss.TargetState==RoundHitState.None&&!miss.TriggersPursuit,
   "RR07 missed strike cannot knock down or queue a pursuit");
  solo=Fighter(0,0,1);enemy=Fighter(0,1,1);
  battle=RoundCombat.Demo(new[]{solo,enemy},rules,3);
  battle.Step();solo.Rage=100;
  check(!battle.PursuitNext&&battle.Step().Move==RoundMove.Ultimate,"RR08 the quick pursuit prediction never outranks a ready ultimate");
  battle=RoundCombat.Demo(new[]{Fighter(0,0,1),Fighter(0,1,1)},rules,3);
  var interrupted=battle.Step();
  interrupted.Target.HitState=RoundHitState.Standing;
  check(!battle.PursuitNext&&battle.Step().Move==RoundMove.Basic,"RR09 an invalidated follow-up does not receive a quick gap");
 }
 static void CheckPiccoloKit(Action<bool,string> check){
  var rules=Rules();
  foreach(var card in rules.cards.Where(c=>Data.Card(c.cardId).Rarity!="C"))
   card.skills=new[]{new RoundAbility{name="空一",trigger="reserved",effect="none"},new RoundAbility{name="空二",trigger="reserved",effect="none"}};
  var kit=rules.For("piccolo-test");
  kit.basicPower=.50;kit.ultimatePower=2.00;kit.pursuitPower=.40;
  kit.techniques=new RoundTechnique{basicRandomTarget=true,basicReactionChance=.30,basicReaction=RoundHitState.Knockdown,
   ultimateChargeRounds=1,ultimateChargeReduction=.50,pursuitLimit=1,pursuitTrigger=RoundHitState.Knockback,pursuitReaction=RoundHitState.LargeFloat};
  kit.skills[0]=new RoundAbility{name="反魔爆裂光",effect="counter",trigger="onSingleTarget",value=1,reaction=RoundHitState.Knockback};

  var piccolo=Fighter(2,0,1);piccolo.Rage=100;
  var front=Fighter(0,1,1);front.HP=300;
  var rear=new RoundFighter("vegeta-test",1,9,2400,100,40,0){Dodge=0,HP=400};
  var battle=RoundCombat.Demo(new[]{piccolo,front,rear},rules,3);
  var charge=battle.Step();
  check(charge.Charging&&piccolo.IsCharging&&piccolo.Rage==0&&front.HP==300&&rear.HP==400&&
   !rules.OpeningSkills(piccolo.CardId).Any(),"V14301 charge commits no damage or opening counter");
  var incoming=battle.Step();var second=battle.Step();
  check(incoming.Target==piccolo&&!incoming.Dodged&&!incoming.Countered&&Math.Abs(incoming.Amount-30)<.001&&
   second.Target==piccolo&&!second.Countered&&piccolo.HP<piccolo.MaxHP&&!battle.CounterNext,
   "V14302 charge halves ordinary damage and skips its own attacks");
  var beam=battle.Step();
  check(beam.Move==RoundMove.Ultimate&&!beam.Charging&&beam.Actor==piccolo&&beam.Round==2&&beam.Target==rear&&
   !piccolo.IsCharging&&Math.Abs(beam.Amount-120)<.001,"V14303 release takes next round's hand and targets lowest HP percent");

  piccolo=Fighter(2,1,1);front=Fighter(0,0,1);rear=Fighter(1,0,5);
  battle=RoundCombat.Demo(new[]{front,rear,piccolo},rules,3);
  var ordinary=battle.Step();
  check(ordinary.Move==RoundMove.Basic&&!ordinary.Countered&&piccolo.HP<piccolo.MaxHP&&!battle.CounterNext,
   "V14401 ordinary basic attack cannot trigger Piccolo's skill");
  piccolo.HP=piccolo.MaxHP;
  front.Rage=100;
  var dodge=battle.Step();
  check(dodge.Target==piccolo&&dodge.Countered&&dodge.Dodged&&piccolo.HP==piccolo.MaxHP&&battle.CounterNext,
   "V14304 first single-target attack is evaded and queues a counter");
  rear.Rage=100;
  var counter=battle.Step();
  check(counter.Ability==kit.skills[0]&&counter.Actor==piccolo&&counter.Target==front&&
   counter.TargetState==RoundHitState.Knockback&&Math.Abs(counter.Amount-60)<.001,
   "V14402 counter outranks a ready ultimate and returns 100% to its attacker with knockback");
  var fullRage=battle.Step();
  check(fullRage.Actor==rear&&fullRage.Move==RoundMove.Ultimate&&!fullRage.Countered&&piccolo.HP<piccolo.MaxHP,
   "V14403 waiting ultimate resumes after counter, cannot retrigger the same round");
  var followUp=battle.Step();
  check(followUp.Actor==piccolo&&followUp.Move==RoundMove.Pursuit&&followUp.TargetState==RoundHitState.LargeFloat,
   "V14312 counter knockback can lead into the rising slash");
  battle.Step();battle.Step();battle.Step();
  front.Rage=100;
  var again=battle.Step();
  check(again.Round==2&&again.Countered,"V14307 the counter refreshes next round");

  var goku=rules.For("goku-test");
  goku.techniques=new RoundTechnique{ultimateFrontRow=true,basicReactionChance=1,basicReaction=RoundHitState.Knockback};
  front=Fighter(0,0,1);front.Rage=100;piccolo=Fighter(2,1,1);
  battle=RoundCombat.Demo(new[]{front,piccolo},rules,6);
  var area=battle.Step();
  check(area.Hits.Count()==1&&!area.Countered&&!area.Dodged,"V14308 row ability with one survivor remains area damage");
  var volleyKit=rules.For("vegeta-test");
  volleyKit.techniques=new RoundTechnique{ultimateHits=6};
  front=Fighter(1,0,1);front.Rage=100;piccolo=Fighter(2,1,1);
  battle=RoundCombat.Demo(new[]{front,piccolo},rules,6);
  var volley=battle.Step();
  check(volley.Hits.Count()==6&&volley.Hits.All(hit=>!hit.Countered),"V14309 repeated volley shots do not count as single-target moves");

  front=Fighter(0,0,1);piccolo=Fighter(2,0,2);rear=Fighter(1,1,1);
  battle=RoundCombat.Demo(new[]{front,piccolo,rear},rules,8);
  var trigger=battle.Step();
  var pursuit=battle.Step();
  check(trigger.TriggersPursuit&&pursuit.Actor==piccolo&&pursuit.Move==RoundMove.Pursuit&&
   pursuit.TargetState==RoundHitState.LargeFloat,"V14310 knockback dispatches Piccolo's rising slash");

  var caster=Fighter(2,0,1);var defender=Fighter(2,1,1);
  battle=RoundCombat.Demo(new[]{caster,defender},rules,8);
  caster.ChargeReadyRound=battle.Round;
  var mirrorBeam=battle.Step();
  check(mirrorBeam.Countered&&mirrorBeam.CounterAbility==kit.skills[0]&&defender.HP==defender.MaxHP&&battle.CounterNext,
   "V15001 a single-target beam activates the configured counter on the other Piccolo");
  var retaliation=battle.Step();
  check(retaliation.Ability==kit.skills[0]&&retaliation.Actor==defender&&retaliation.Target==caster&&
   !retaliation.Dodged&&caster.HP<caster.MaxHP&&caster.CounterRound==0&&!battle.CounterNext&&retaliation.TriggersPursuit,
   "V15002 a counter cannot be swallowed by a counter to the counter");
  check(battle.Step().Move==RoundMove.Pursuit,"V15003 the resolved mirror counter can still feed the original pursuit");

  var ids=new System.Collections.Generic.HashSet<int>();
  foreach(int seed in Enumerable.Range(0,24)){
   piccolo=Fighter(2,0,1);
   battle=RoundCombat.Demo(new[]{piccolo,Fighter(0,1,1),Fighter(1,1,9)},rules,seed);
   ids.Add(battle.Step().Target.Position);
  }
  check(ids.SetEquals(new[]{1,9}),"V14311 random basic can select front or rear living foes");
 }

 static void CheckVegetaKit(Action<bool,string> check){
  var defs=Rules();
  var goku=defs.For("goku-test");
  var vegeta=defs.For("vegeta-test");
  goku.techniques=new RoundTechnique{basicReactionChance=1,basicReaction=RoundHitState.Knockdown,pursuitLimit=2,
   pursuitTrigger=RoundHitState.Knockdown,pursuitReaction=RoundHitState.LargeFloat};
  vegeta.techniques=new RoundTechnique{basicReactionChance=.3,basicReaction=RoundHitState.Knockback,pursuitLimit=2,
   pursuitTrigger=RoundHitState.LargeFloat,pursuitReaction=RoundHitState.Knockdown,ultimateHits=6};
  vegeta.basicPower=.60;vegeta.ultimatePower=.60;vegeta.pursuitPower=.25;
  vegeta.skills=new[]{new RoundAbility{name="赛亚精英",effect="attack",trigger="opening",value=.20,
   resistTags=new[]{"Z战士",Data.LowerClassTag},finalReduction=.20},
   new RoundAbility{name="待填写",effect="none",trigger="reserved"}};

  var leader=Fighter(0,0,2);
  var partner=Fighter(1,0,4);
  var victim=Fighter(2,1,1);
  var demo=RoundCombat.Demo(new[]{leader,partner,victim},defs,7);
  var basic=demo.Step();
  var chain=Enumerable.Range(0,4).Select(i=>demo.Step()).ToArray();
  check(basic.TriggersPursuit&&chain.All(hit=>hit.Move==RoundMove.Pursuit&&hit.Target==victim)&&
   chain.Select(hit=>hit.Actor).SequenceEqual(new[]{leader,partner,leader,partner})&&
   chain.Select(hit=>hit.TargetState).SequenceEqual(new[]{RoundHitState.LargeFloat,RoundHitState.Knockdown,RoundHitState.LargeFloat,RoundHitState.Knockdown})&&
   !demo.PursuitNext&&leader.PursuitCount==2&&partner.PursuitCount==2,"V1401 two pursuits per fighter, no infinite loop");

  foreach(var positions in new[]{new[]{1,5,9},new[]{3,4,8}}){
   leader=Fighter(1,0,2); leader.Rage=100;
   var allies=new[]{leader,Fighter(0,0,5),Fighter(2,0,7)};
   var enemy=positions.Select((pos,i)=>Fighter(i+2,1,pos)).ToArray();
   demo=RoundCombat.Demo(allies.Concat(enemy),defs,9);
   var volley=demo.Step();
   var hits=volley.Hits.ToArray();
   check(demo.Order.Count==6&&volley.SequentialHits&&hits.Length==6&&hits.Take(3).Select(h=>h.Target).Distinct().Count()==3&&
    hits.GroupBy(h=>h.Target).All(group=>group.Count()==2)&&hits.All(h=>Math.Abs(h.Amount-(120-40)*.60)<.001),
    "V1402 six distinct-then-repeat 60% shots in 3v3 "+string.Join(",",positions));
   check(hits.GroupBy(h=>h.Target).All(group=>group.First().TargetHPAfter>group.Last().TargetHPAfter&&
    group.Last().TargetHPAfter==group.Key.HP),"V1403 per-hit snapshots match final HP "+string.Join(",",positions));
   int events=1;
   while(demo.Result==0&&events<400){demo.Step();events++;}
   check(demo.Result!=0&&events<400,"V1408 full 3v3 completes with fixed initiative "+string.Join(",",positions));
  }
  leader=Fighter(1,0,2);leader.Rage=100;
  var many=Enumerable.Range(1,9).Select(i=>Fighter(2,1,i)).ToArray();
  demo=RoundCombat.Demo(new[]{leader}.Concat(many),defs,3);
  var spread=demo.Step().Hits.ToArray();
  check(spread.Length==6&&spread.Select(hit=>hit.Target).Distinct().Count()==6&&many.Count(f=>f.HP==f.MaxHP)==3,
   "V1404 never strikes more than six living targets");
  leader=Fighter(1,0,2);leader.Rage=100;victim=Fighter(2,1,9);victim.HP=1;
  demo=RoundCombat.Demo(new[]{leader,victim},defs,3);
  check(demo.Step().Hits.Count()==1&&demo.Result==1,"V1405 volley stops resolving damage when no targets remain");

  leader=Fighter(0,0,1);victim=Fighter(1,1,1);victim.Shield=10;
  demo=RoundCombat.Demo(new[]{leader,victim},defs,3);
  var mitigated=demo.Step();
  var expected=(leader.Attack-victim.Defense)*goku.basicPower*.80;
  check(Math.Abs(mitigated.Amount-expected)<.001&&Math.Abs(victim.HP-(1200-expected+10))<.001&&victim.Shield==0&&
   Math.Abs(mitigated.HealthDamage-(expected-10))<.001&&mitigated.ShieldDamage==10,
   "V1406 final 20% modifier applies once before the shield");
  leader=Fighter(1,0,1);victim=Fighter(1,1,1);
  demo=RoundCombat.Demo(new[]{leader,victim},defs,3);
  check(Math.Abs(demo.Step().Amount-(120-40)*.60)<.001&&Data.RoundIdentityTags().Contains(Data.LowerClassTag),
   "V1407 unmatched faction receives ordinary damage and tag is registered");
 }

 static void CheckCounterAcrossCharge(Action<bool,string> check){
  var rules=Rules();
  var kit=rules.For("piccolo-test");
  kit.techniques=new RoundTechnique{ultimateChargeRounds=1,ultimateChargeReduction=.50};
  var counter=new RoundAbility{name="反魔爆裂光",trigger="onSingleTarget",effect="counter",value=1,reaction=RoundHitState.Knockback};
  kit.skills[0]=counter;
  var piccolo=Fighter(2,0,2);
  var rival=Fighter(0,1,2);
  var battle=RoundCombat.Demo(new[]{rival,piccolo},rules,17);
  piccolo.Rage=rival.Rage=100;
  check(battle.Step().Charging&&piccolo.IsCharging,"V16001 charge arms without consuming the original hand");
  var first=battle.Step();
  check(first.Target==piccolo&&!first.Countered&&!first.Dodged&&first.Amount>0&&piccolo.CounterRound==0&&!battle.CounterNext,
   "V16002 a single-target skill hits a charging Piccolo with no counter or counter budget");
  rival.Rage=100;
  var second=battle.Step();
  check(!second.Countered&&!second.Dodged&&Math.Abs(second.Amount-first.Amount)<.001&&piccolo.IsCharging,
   "V16003 a second hit gets the same charge mitigation and queues no counter");
  RoundAction launch;
  do{launch=battle.Step();}while(launch!=null&&piccolo.IsCharging);
  check(launch!=null&&launch.Actor==piccolo&&launch.Move==RoundMove.Ultimate&&!launch.Charging&&battle.Round==2&&!battle.CounterNext,
   "V16004 the original hand fires with no stored counter");
  rival.Rage=100;
  check(battle.Step().Countered&&battle.CounterNext&&piccolo.CounterRound==2&&battle.Step().Ability==counter,
   "V16005 the first eligible skill after release triggers a fresh counter");
 }

 static void CheckProgression(Action<bool,string> check){
  string id="goku-test";
  string[] grades={"C","B","A","S"};
  double[] edges={.7,.8,1,1.2,1.5};
  for(int i=0;i<grades.Length;i++){
   double atOne=RoundProgression.Stat(id,"defense",46,grades[i],"B",1);
   double atHundred=RoundProgression.Stat(id,"defense",46,grades[i],"B",100);
   double atPrior=RoundProgression.Stat(id,"defense",46,grades[i],"B",99);
   check(atOne>=46*edges[i]&&atOne<=46*edges[i+1]&&atHundred>atPrior&&atPrior>atOne&&
    atHundred-atPrior<RoundProgression.Stat(id,"defense",46,grades[i],"B",2)-atOne,"V16006 bounded rarity with a diminishing positive growth tail "+grades[i]);
  }
  var marks=Enumerable.Range(0,128).Select(n=>"balance-card-"+n).ToArray();
  var sampledLevels=new[]{1,25,50,75,100};
  check(sampledLevels.All(level=>Enumerable.Range(0,grades.Length-1).All(tier=>
   marks.Max(key=>RoundProgression.Stat(key,"attack",170,grades[tier],"S",level))<
   marks.Min(key=>RoundProgression.Stat(key,"attack",170,grades[tier+1],"C",level)))),
   "V16013 adjacent rarity tiers do not cross even at opposite potential extremes");
  var potential=grades.Select(g=>RoundProgression.Stat(id,"attack",170,"B",g,100)).ToArray();
  check(potential.SequenceEqual(potential.OrderBy(v=>v))&&potential.Distinct().Count()==4&&
   RoundProgression.PersonalRoll(id,"hp")!=RoundProgression.PersonalRoll("vegeta-test","hp")&&
   RoundProgression.LevelFromTraining(int.MaxValue)==100&&RoundProgression.LevelFromTraining(0)==1,"V16007 deterministic card rolls and increasing potential cap");
  var rules=Rules();
  var attacker=Fighter(3,0,1);var target=Fighter(4,1,1);
  attacker.Rage=100;attacker.Attack=150;attacker.Penetration=.25;attacker.Crit=1;attacker.Accuracy=.20;
  target.Dodge=.20;
  var attack=RoundCombat.Demo(new[]{attacker,target},rules,1).Step();
  check(attack.Critical&&!attack.Dodged&&Math.Abs(attack.Amount-(150-40*.75)*2*1.5)<.001,
   "V16008 critical, penetration and speed apply to direct ultimate damage");
  attacker=Fighter(3,0,1);target=Fighter(4,1,1);
  attacker.Crit=.10;attacker.Attack=20;target.Tenacity=.10;
  attack=RoundCombat.Demo(new[]{attacker,target},rules,2).Step();
  check(!attack.Critical&&attack.Amount==1,"V16009 tenacity cancels crit and a hit has minimum damage");
  attacker=Fighter(0,0,1);target=Fighter(1,1,1);
  attacker.Attack=1000;attacker.Crit=0;target.HP=8;target.Shield=11;
  var overkill=RoundCombat.Demo(new[]{attacker,target},rules,67).Step();
  check(overkill.Amount>19&&overkill.HealthDamage==8&&overkill.ShieldDamage==11&&target.HP==0,
   "V18001 hit feedback reports actual health and shield loss on overkill");
  var healKit=rules.For("gohan-test");
  healKit.basicPower=.5;
  healKit.techniques=new RoundTechnique{basicHealsSelf=true};
  var receiver=Fighter(4,0,1);receiver.HP=900;receiver.Qi=.10;
  check(Math.Abs(RoundCombat.Demo(new[]{receiver,Fighter(3,1,1)},rules,2).Step().Amount-55)<.001&&
   Math.Abs(receiver.ReceivedAid(100)-110)<.001,"V16010 qi increases received healing and shield");
 }

 static void CheckBattlePanelTotals(Action<bool,string> check){
  var totals=new RoundBattleStats();
  var actor=Fighter(0,0,1);var mirror=Fighter(0,1,1);
  var damage=new RoundAction{Actor=actor,Target=mirror,Amount=110,HealthDamage=74,ShieldDamage=13,ActorHPCost=5};
  totals.RecordHit(damage);
  totals.RecordHit(new RoundAction{Actor=actor,Target=mirror,Dodged=true});
  totals.RecordCost(damage);
  totals.RecordHit(new RoundAction{Actor=actor,Target=actor,Healing=true,Amount=30});
  totals.RecordHit(new RoundAction{Actor=mirror,Target=mirror,Healing=true,Amount=20});
  check(totals.Value(actor,RoundBattleMetric.Damage)==87&&totals.Value(mirror,RoundBattleMetric.Taken)==87&&
   totals.Value(actor,RoundBattleMetric.Taken)==5&&totals.Value(actor,RoundBattleMetric.Healing)==30&&
   totals.Value(mirror,RoundBattleMetric.Healing)==20&&totals.Value(mirror,RoundBattleMetric.Damage)==0,
   "V18002 stats use actual HP plus shield, charge self-cost to taken, and attribute recovery to the source");
  totals.Reset();
  check(totals.Value(actor,RoundBattleMetric.Damage)==0&&totals.Value(mirror,RoundBattleMetric.Healing)==0,
   "V18003 restart clears the previous battle stats");
 }

 static void CheckGohanKit(Action<bool,string> check){
  var rules=Rules();
  var kit=rules.For("gohan-test");
  kit.basicPower=.30;kit.ultimatePower=.60;kit.pursuitPower=.30;
  kit.techniques=new RoundTechnique{basicHealsSelf=true,ultimateHighestAttack=true,ultimateDefenseReduction=.20,
   ultimateDefenseRounds=2,pursuitLimit=1,pursuitTrigger=RoundHitState.SmallFloat,pursuitReaction=RoundHitState.LargeFloat,
   pursuitDefenseIncrease=.20,pursuitDefenseRounds=2};
  var guard=new RoundAbility{name="超级防御",trigger="onReceive",effect="mitigate",value=.30,chance=1};
  kit.skills=new[]{guard};
  var gohan=Fighter(Data.Index("gohan-test"),0,1);gohan.HP=1100;
  var rival=new RoundFighter("goku-test",1,7,1200,160,40,0){Dodge=0};
  var front=Fighter(0,1,1);
  var tie=new RoundFighter("goku-test",1,9,1200,160,40,0){Dodge=0};
  var battle=RoundCombat.Demo(new[]{gohan,front,rival,tie},rules,3);
  var heal=battle.Step();
  check(heal.Healing&&heal.Target==gohan&&heal.Amount==30&&heal.TargetHPAfter==1130&&gohan.Rage==35&&front.HP==1200,
   "V15101 Rally heals only Gohan and grants basic rage");
  gohan.Rage=100;
  var heavy=battle.Step();
  check(heavy.Target==rival&&Math.Abs(heavy.Amount-36)<.001&&heavy.DefenseDelta==-.20&&
   rival.EffectiveDefense(1)==32&&rival.EffectiveDefense(2)==32&&rival.EffectiveDefense(3)==40,
   "V15102 Heavy strike selects highest attack, breaks ties by hand and weakens defense for two rounds");
  gohan.HP=1194;gohan.Shield=10;
  var guarded=battle.Step();
  check(guarded.GuardAbility==guard&&Math.Abs(guarded.Amount-(front.Attack-gohan.Defense)*.70)<.001&&gohan.Shield==0&&Math.Abs(gohan.HP-(1204-guarded.Amount))<.001,
   "V15103 Guard reduces damage before shield absorption");
  gohan=Fighter(Data.Index("gohan-test"),0,1);gohan.HP=1197;
  battle=RoundCombat.Demo(new[]{gohan,Fighter(0,1,2)},rules,4);
  check(battle.Step().Amount==3&&gohan.HP==gohan.MaxHP,"V15104 healing does not exceed missing HP");
  gohan=Fighter(Data.Index("gohan-test"),0,1);gohan.Rage=100;
  front=Fighter(0,1,1);front.Dodge=1;
  battle=RoundCombat.Demo(new[]{gohan,front},rules,4);
  check(battle.Step().Dodged&&front.EffectiveDefense(1)==40,"V15105 a dodged ultimate cannot reduce defense");

  var goku=rules.For("goku-test");
  var vegeta=rules.For("vegeta-test");
  goku.techniques=new RoundTechnique{basicReaction=RoundHitState.Knockdown,basicReactionChance=1,
   pursuitTrigger=RoundHitState.Knockdown,pursuitReaction=RoundHitState.SmallFloat,pursuitLimit=2};
  vegeta.techniques=new RoundTechnique{pursuitTrigger=RoundHitState.LargeFloat,pursuitReaction=RoundHitState.Knockdown,pursuitLimit=2};
  gohan=Fighter(Data.Index("gohan-test"),0,3);
  var defender=Fighter(0,1,1);
  battle=RoundCombat.Demo(new[]{Fighter(0,0,1),Fighter(1,0,2),gohan,defender},rules,6);
  var chain=Enumerable.Range(0,5).Select(i=>battle.Step()).ToArray();
  check(chain.Select(hit=>hit.Actor.CardId).SequenceEqual(new[]{"goku-test","goku-test","gohan-test","vegeta-test","goku-test"})&&
   chain[1].TargetState==RoundHitState.SmallFloat&&chain[2].TargetState==RoundHitState.LargeFloat&&
   chain[2].DefenseDelta==.20&&gohan.PursuitCount==1&&!battle.PursuitNext&&gohan.EffectiveDefense(2)==48,
   "V15106 changed Goku launch feeds once-per-round Gohan into Vegeta");
  gohan.ChangeDefense(.14,2,2);
  check(Math.Abs(gohan.EffectiveDefense(2)-53.6)<.001&&Math.Abs(gohan.EffectiveDefense(3)-45.6)<.001&&
   gohan.EffectiveDefense(4)==40,"V15107 same defense stat stacks with independent expirations");
  guard.chance=.30;
  var outcomes=new HashSet<bool>();
  for(int seed=1;seed<=30;seed++){
   gohan=Fighter(Data.Index("gohan-test"),1,1);
   var attacker=Fighter(0,0,1);
   battle=RoundCombat.Demo(new[]{attacker,gohan},rules,seed);
   outcomes.Add(battle.Step().GuardAbility!=null);
  }
  check(outcomes.SetEquals(new[]{true,false}),"V15108 guard requires a successful probability roll");
 }

 static void CheckKrillinKit(Action<bool,string> check){
  var rules=Rules();
  var kit=rules.For("krillin-test");
  kit.techniques=new RoundTechnique{basicReactionChance=.30,basicReaction=RoundHitState.SmallFloat,
   ultimateFrontColumn=true,ultimateDefenseReduction=.20,ultimateDefenseRounds=-1,pursuitCombo=5,
   pursuitHits=10,pursuitLimit=1,pursuitDodgeIncrease=.10};
  kit.basicPower=.60;kit.ultimatePower=.60;kit.pursuitPower=.05;
  kit.skills=new[]{new RoundAbility{name="战前动员",trigger="whileAlive",effect="teamAttack",value=.10,
   teamAttackTags=new[]{"Z战士","赛亚人篇"}}};
  var volley=rules.For("vegeta-test");
  volley.techniques=new RoundTechnique{ultimateHits=6};
  volley.ultimatePower=.6;
  int krillinIndex=Data.Index("krillin-test");
  var krillin=Fighter(krillinIndex,0,9);
  var vegeta=Fighter(1,0,1);
  var goku=Fighter(0,0,2);
  var front=Fighter(4,1,1);var center=Fighter(4,1,2);var rear=Fighter(4,1,6);
  var battle=RoundCombat.Demo(new[]{krillin,vegeta,goku,front,center,rear},rules,47);
  check(Math.Abs(battle.EffectiveAttack(krillin)-110)<.001&&Math.Abs(battle.EffectiveAttack(goku)-115)<.001&&
   Math.Abs(battle.EffectiveAttack(front)-100)<.001,"V15401 Krillin attack aura includes owner with OR tags and adds to opening attack");
  krillin.Rage=100;
  var disc=battle.Step();
  check(disc.Actor==krillin&&disc.Hits.Count()==2&&disc.Hits.Select(h=>h.Target).ToArray()
   .All(target=>target==front||target==center)&&disc.Hits.All(hit=>Math.Abs(hit.Amount-42)<.001&&
    hit.DefenseDelta==-.20)&&rear.HP==rear.MaxHP&&!battle.PursuitNext,
   "V15402 disc damages the front column at 60 and applies whole-battle defense reduction");
  vegeta.Rage=100;
  var fired=battle.Step();
  check(fired.Hits.Count()==6&&fired.Hits.ElementAt(2).TriggersPursuit&&!fired.Hits.ElementAt(1).TriggersPursuit&&battle.PursuitNext,
   "V15403 the fifth allied hit crosses threshold across different actors and moves");
  var scatter=battle.Step();
  var shots=scatter.Hits.ToArray();
  check(scatter.Actor==krillin&&scatter.SequentialHits&&shots.Length==10&&shots.Select(hit=>hit.Target).Distinct().Count()>1&&
   shots.All(hit=>hit.Target.Side==1&&!hit.Dodged&&Math.Abs(hit.Amount-(110-hit.Target.EffectiveDefense(1))*.05)<.001),
   "V15404 each of ten shots draws a live enemy and uses the five-point multiplier");
  check(Math.Abs(krillin.EffectiveDodge(1)-.10)<.001&&krillin.PursuitCount==1&&!battle.PursuitNext&&
    front.DefenseChanges.Single().Expiry==int.MaxValue,"V15405 pursuit persists additive dodge, avoids looping and disc debuff lasts the battle");
  krillin.HP=0;
  check(!battle.ActiveTeamAttackSkills(goku).Any()&&Math.Abs(battle.EffectiveAttack(goku)-105)<.001,
   "V15406 Krillin's defeat immediately ends the aura");

  var alternate=Fighter(4,1,1);
  var ally=Fighter(0,0,1);krillin=Fighter(krillinIndex,0,9);
  var separate=RoundCombat.Demo(new[]{ally,krillin,alternate},rules,47);
  for(int move=0;move<3;move++){
   var attack=separate.Step();
   check(attack.ComboAttack&&attack.ComboHits==1&&separate.ComboSide==attack.Actor.Side&&separate.ComboCount==1,
    "V1820 combo resets at each opposing attack and counts one landed segment");
  }
  alternate.Dodge=1;
  var nextSide=separate.Step();
  check(nextSide.Dodged&&nextSide.ComboAfter==1&&nextSide.ComboHits==0&&separate.ComboSide==0,
   "V1821 a dodge adds nothing and keeps the current side's accumulated count");
  check(!separate.PursuitNext&&krillin.PursuitCount==0,"V1822 alternating sides cannot build Krillin's five-hit chain");

  var single=Fighter(4,1,1);
  krillin=Fighter(krillinIndex,0,9);vegeta=Fighter(1,0,1);vegeta.Rage=100;
  battle=RoundCombat.Demo(new[]{krillin,vegeta,single},rules,7);
  battle.Step();
  check(battle.Step().Hits.Count()==10,"V15407 all ten shots can independently repeat a sole live target");
 }

 static void CheckSavedDemo(Action<bool,string> check){
  var profile=new Profile();
  profile.SetRoundMember(8,0);
  var saved=profile.RoundFormation().Select(id=>id<0?null:Data.Cards[id].Id).ToArray();
  var formations=Enumerable.Range(0,64).Select(seed=>RoundDemoSetup.DrawSavedFormation(profile,seed)).ToArray();
  check(formations.All(f=>f.allies.SequenceEqual(saved)&&f.enemies.Count(id=>id!=null)==5&&
   f.enemies.Where(id=>id!=null).Distinct().Count()==5&&f.enemies.Where(id=>id!=null).All(Data.IsRoundCard)),
   "V17101 Demo keeps the saved allied slots and draws five distinct online enemies");
  var actual=new RoundDemoSetup{useSavedFormation=true}.CreateBattle(Rules(),11,profile);
  check(actual.Order.Count==saved.Count(id=>id!=null)+5&&actual.Order.All(f=>f.Level==100)&&
   actual.Order.Where(f=>f.Side==0).All(f=>saved[f.Position-1]==f.CardId),
   "V17102 live Demo uses saved positions and level 100 on both sides");
  check(formations.Select(f=>string.Join(",",f.enemies)).Distinct().Count()>1&&
   formations.Select(f=>string.Join(",",f.enemies.Where(id=>id!=null).OrderBy(id=>id))).Distinct().Count()>1,
   "V17103 both the opposing roster and its cells change across seeds");
  var a=RoundCombat.Demo(new[]{Fighter(0,0,1),Fighter(1,1,1)},Rules(),9);
  var b=RoundCombat.Demo(new[]{Fighter(0,0,1),Fighter(1,1,1)},Rules(),9,1.2);
  check(Math.Abs(b.Step().Amount/a.Step().Amount-1.2)<.001,"V15503 Demo-only damage scale preserves the combat formula");
  var fighter=Fighter(3,0,1);
  fighter.ChangeDodge(.10,1,1);
  fighter.ChangeDodge(.15,1,2);
  fighter.ChangeDodge(.05,1,-1);
  check(Math.Abs(fighter.EffectiveDodge(1)-.3)<.001&&Math.Abs(fighter.EffectiveDodge(2)-.2)<.001&&
   Math.Abs(fighter.EffectiveDodge(3)-.05)<.001,"V15504 dodge contributions still expire independently");
 }

 static void CheckRaditz(Action<bool,string> check){
  const string id="raditz-arrival-v030";
  var rules=Rules();
  var kit=rules.For(id);
  check(Data.IsRoundCard(id)&&Data.Card(id).Type==CharacterType.Output&&Data.Card(id).Rarity=="C"&&
   Data.Card(id).HasTag(Data.LowerClassTag),
   "V16101 Raditz is online with authored type and identity");
  kit.basicPower=.50;kit.ultimatePower=.80;kit.pursuitPower=.20;
  kit.techniques=new RoundTechnique{basicRandomTarget=true,basicEnhancedChance=.30,basicEnhancedPower=.80,
   ultimateTargets=2,ultimateAttackBonus=.20,ultimateAttackRounds=2,
   pursuitSourceTag="赛亚人",pursuitReaction=RoundHitState.LargeFloat,pursuitLimit=1};
  kit.skills[0]=new RoundAbility{name="无情的侵略",trigger="roundStart",effect="phaseStats",value=.30,phaseRound=5,
   lateAttackPenalty=.20,lateDefense=.20};
  var expectedRolls=new[]{50,80};
  var samples=Enumerable.Range(1,96).Select(seed=>{
   var actor=new RoundFighter(id,0,1,100000,100,0,0){Dodge=0};
   var enemy=Fighter(0,1,1);enemy.Defense=0;enemy.Dodge=0;
   var battle=RoundCombat.Demo(new[]{actor,enemy},rules,seed);
   var hit=battle.Step();
   return new{hit.EnhancedBasic,Ratio=(int)Math.Round(hit.Amount/battle.EffectiveAttack(actor)*100)};
  }).ToArray();
  check(samples.Select(hit=>hit.Ratio).Distinct().OrderBy(value=>value).SequenceEqual(expectedRolls)&&
   samples.All(hit=>hit.Ratio==(hit.EnhancedBasic?80:50)),"V16102 normal and enhanced basic use 50/80 coefficients");
  var raditz=new RoundFighter(id,0,1,100000,100,50,0){Rage=100,Dodge=0};
  var rivals=new[]{Fighter(0,1,1),Fighter(0,1,3)};
  foreach(var rival in rivals){rival.Attack=1;rival.Defense=0;rival.Dodge=0;rival.HP=100000;}
  var match=RoundCombat.Demo(new[]{raditz}.Concat(rivals),rules,17);
  var burst=match.Step();
  check(burst.Hits.Count()==2&&burst.Hits.Select(hit=>hit.Target).Distinct().Count()==2&&
   burst.Hits.All(hit=>Math.Abs(hit.Amount-104)<.001)&&Math.Abs(match.EffectiveAttack(raditz)-150)<.001,
   "V16103 distinct area targets use pre-buff ATK and grant a timed enhancement");
  while(match.Round<2){match.Step();foreach(var fighter in match.Order)fighter.Rage=0;}
  check(Math.Abs(match.EffectiveAttack(raditz)-150)<.001,"V16104 ultimate bonus lasts current and next round");
  while(match.Round<3){match.Step();foreach(var fighter in match.Order)fighter.Rage=0;}
  check(Math.Abs(match.EffectiveAttack(raditz)-130)<.001,"V16105 attack bonus expires independently of passive");
  while(match.Round<5){match.Step();foreach(var fighter in match.Order)fighter.Rage=0;}
  check(Math.Abs(match.EffectiveAttack(raditz)-80)<.001&&Math.Abs(raditz.EffectiveDefense(match.Round)-60)<.001,
   "V16106 passive transitions at the start of round five");

  rules.For("vegeta-test").techniques=new RoundTechnique{basicReactionChance=1,basicReaction=RoundHitState.Knockdown};
  rules.For("piccolo-test").skills[0]=new RoundAbility{name="反魔爆裂光",trigger="onSingleTarget",effect="counter",value=1,
   reaction=RoundHitState.Knockback,usesPerRound=1};
  foreach(bool evade in new[]{false,true}){
   var starter=Fighter(1,0,1);starter.Crit=0;
   var target=Fighter(2,1,1);target.Dodge=evade?1:0;
   var follower=new RoundFighter(id,0,2,10000,100,40,0){Dodge=0};
   var chain=RoundCombat.Demo(new[]{starter,follower,target},rules,15);
   var hit=chain.Step();
   if(evade){
    check(hit.Dodged&&hit.Amount==0&&!chain.PursuitNext&&follower.PursuitCount==0&&target.CounterUses==1,
     "V16107 ordinary dodge cancels the opener without consuming counter or pursuit uses");
   }else{
    var pursuit=chain.Step();
    check(hit.TargetState==RoundHitState.Knockdown&&pursuit.Actor==follower&&pursuit.Countered&&
     !pursuit.TriggersPursuit&&target.CounterUses==0&&chain.CounterNext,
     "V16108 Saiyan-applied state links to Raditz and Piccolo can dodge the linked strike once");
   }
  }

  var p=new Profile();
  int unlisted=Array.IndexOf(p.CardIds,id);
  p.CardIds=p.CardIds.Where((_,index)=>index!=unlisted).ToArray();
  p.Owned=p.Owned.Where((_,index)=>index!=unlisted).ToArray();
  p.Training=p.Training.Where((_,index)=>index!=unlisted).ToArray();p.Shards=p.Shards.Where((_,index)=>index!=unlisted).ToArray();
  p.Resonance=p.Resonance.Where((_,index)=>index!=unlisted).ToArray();p.Policy=p.Policy.Where((_,index)=>index!=unlisted).ToArray();
  p.Skill=p.Skill.Where((_,index)=>index!=unlisted).ToArray();p.Skill2=p.Skill2.Where((_,index)=>index!=unlisted).ToArray();
  check(new Profile().SetRoundMember(1,Data.Index(id)),"V16117 a new profile can deploy Raditz as a reserve");
  var oldGrid=p.RoundFormation();
  p.Retired.Add(new RetiredProgress{cardId=id,owned=true,training=2,shards=5,skill="",skill2=""});
  check(p.NeedsRosterMigration,"V16109 detection refreshes an older active ID set");
  p.Normalize();p.Normalize();
  check(!p.NeedsRosterMigration&&p.Owned[Data.Index(id)]&&p.Shards[Data.Index(id)]==5&&p.Training[Data.Index(id)]==2&&
   p.RoundFormation().SequenceEqual(oldGrid)&&p.Retired.All(entry=>entry.cardId!=id),"V16110 returning stable ID restores the original archived progress once");
 }

 static void CheckCounterBudget(Action<bool,string> check){
  var rules=Rules();
  var piccolo=Fighter(2,0,1);piccolo.Dodge=1;
  var caster=Fighter(1,1,1);caster.Accuracy=0;caster.Dodge=0;
  var partner=Fighter(0,1,2);partner.Accuracy=0;partner.Dodge=0;
  rules.For("goku-test").techniques=new RoundTechnique();
  rules.For("vegeta-test").techniques=new RoundTechnique{ultimateFrontRow=true};
  rules.For("piccolo-test").techniques=new RoundTechnique{ultimateChargeRounds=1,ultimateChargeReduction=.50};
  rules.For("piccolo-test").skills[0]=new RoundAbility{name="反魔爆裂光",trigger="onSingleTarget",effect="counter",value=1,
   reaction=RoundHitState.Knockback,usesPerRound=1};
  var battle=RoundCombat.Demo(new[]{piccolo,caster,partner},rules,2);
  caster.Rage=100;var aoe=battle.Step();
  check(aoe.Hits.All(hit=>!hit.Countered)&&piccolo.CounterUses==1,"V16111 area attacks cannot spend a counter even when only one target is alive");
  partner.Rage=100;var natural=battle.Step();
  check(natural.Dodged&&!natural.Countered&&piccolo.CounterUses==1,"V16112 ordinary single-target dodge leaves the special counter ready");
  piccolo.Dodge=0;partner.Rage=100;var first=battle.Step();
  check(first.Countered&&piccolo.CounterUses==0&&battle.CounterNext,"V16113 the first eligible single-target attack consumes exactly one counter");
  battle.Step();partner.Rage=100;var repeated=battle.Step();
  check(!repeated.Countered&&repeated.Amount>0&&piccolo.CounterUses==0,"V16114 further single-target skills in the same round independently resolve");
  piccolo.ChargeReadyRound=3;
  while(battle.Round<2){battle.Step();foreach(var fighter in battle.Order)fighter.Rage=0;}
  check(piccolo.IsCharging&&piccolo.CounterUses==1,"V16115 recharge occurs at the beginning of a charging round");
  partner.Rage=100;var charging=battle.Step();
  check(!charging.Countered&&piccolo.CounterUses==1,"V16116 a skill received while charging leaves the replenished counter untouched");
 }

 static RoundDefinitions YamchaRules(){
  var rules=Rules();
  var kit=rules.For(Data.YamchaId);
  kit.basicPower=.60;kit.ultimatePower=1;kit.pursuitPower=.30;
  kit.techniques=new RoundTechnique{basicReactionChance=.30,basicReaction=RoundHitState.Knockback,
   ultimateLowestDefense=true,pursuitTrigger=RoundHitState.Knockdown,pursuitReaction=RoundHitState.Knockdown,pursuitLimit=1};
  kit.skills[0]=new RoundAbility{name="装死",trigger="onDefeat",effect="revive",value=.20,reviveAttackBonus=.20,teamAttackTags=new[]{"Z战士"}};
  rules.For("vegeta-test").techniques=new RoundTechnique{ultimateHits=6};
  return rules;
 }

 static void CheckYamcha(Action<bool,string> check){
  var rules=YamchaRules();
  var kit=rules.For(Data.YamchaId);
  var card=Data.Card(Data.YamchaId);
  var profile=new Profile();
  int yamcha=Data.Index(Data.YamchaId);
  check(Data.IsRoundCard(card.Id)&&card.CatalogNumber==7&&card.Rarity=="C"&&card.Type==CharacterType.Support&&profile.Owned[yamcha]&&
   kit.basicPower==.60&&kit.techniques.basicReactionChance==.30&&kit.techniques.basicReaction==RoundHitState.Knockback&&
   kit.ultimatePower==1&&kit.techniques.ultimateLowestDefense&&kit.pursuitPower==.30&&
   kit.techniques.pursuitTrigger==RoundHitState.Knockdown&&kit.techniques.pursuitReaction==RoundHitState.Knockdown,
   "V17201 Yamcha follows his online sheet and is available in the roster");
  var initial=profile.RoundFormation();
  var draft=(int[])initial.Clone();
  check(!profile.TryPlaceRoundMember(draft,7,yamcha)&&draft.SequenceEqual(initial),
   "V17202 a benched card cannot take an empty sixth slot");
  int replaced=draft[1];
  check(profile.TryPlaceRoundMember(draft,2,yamcha)&&draft[1]==yamcha&&!draft.Contains(replaced)&&profile.RoundFormation().SequenceEqual(initial),
   "V17203 replacing while drafting leaves stored formation unchanged");
  var yamchaIndex=Array.IndexOf(draft,yamcha);
  int original=draft[0];
  check(profile.TryPlaceRoundMember(draft,1,yamcha)&&draft[0]==yamcha&&draft[yamchaIndex]==original&&
   profile.TrySaveRoundFormation(draft)&&profile.RoundFormation().SequenceEqual(draft),
   "V17204 opening occupied cells swaps the roster and a single commit saves the draft");
  check(!profile.TrySaveRoundFormation(new int[1])&&profile.RoundFormation().SequenceEqual(draft),
   "V17205 a malformed draft is rejected without altering saved formation");

  var wolf=new RoundFighter(card.Id,0,1,2000,100,40,0){Rage=100,Dodge=0};
  var first=Fighter(0,1,1);
  first.Defense=20;first.Dodge=0;
  var low=Fighter(1,1,8);
  low.Defense=40;low.Dodge=0;low.ChangeDefense(-.75,1,2);
  var match=RoundCombat.Demo(new[]{wolf,first,low},rules,12);
  var bite=match.Step();
  check(bite.Move==RoundMove.Ultimate&&bite.Target==low&&bite.Hits.Count()==1&&Math.Abs(bite.Amount-90)<.001,
   "V17206 Wolf Fang Fist chooses a live far target by current effective defense");

  var vegeta=Fighter(1,0,1);vegeta.Crit=0;
  var enemy=Fighter(0,1,1);enemy.Dodge=0;
  wolf=new RoundFighter(card.Id,0,2,2000,100,40,0){Dodge=0};
  rules.For(vegeta.CardId).techniques=new RoundTechnique{basicReactionChance=1,basicReaction=RoundHitState.Knockdown};
  match=RoundCombat.Demo(new[]{vegeta,wolf,enemy},rules,13);
  var opener=match.Step();var sweep=match.Step();
  check(opener.TriggersPursuit&&sweep.Actor==wolf&&sweep.Target==enemy&&sweep.Move==RoundMove.Pursuit&&
   sweep.TargetState==RoundHitState.Knockdown&&wolf.PursuitCount==1&&!match.PursuitNext,
   "V17207 Low Sweep follows and reapplies knockdown within the once-a-round limit");

  rules=YamchaRules();
  vegeta=Fighter(1,0,1);vegeta.Attack=10000;vegeta.Crit=0;vegeta.Rage=100;
  wolf=new RoundFighter(card.Id,1,1,500,100,0,0){Dodge=0,Qi=1};
  match=RoundCombat.Demo(new[]{vegeta,wolf},rules,14);
  var fatal=match.Step();
  check(!wolf.Alive&&match.ReactionSkillNext&&match.Result==0&&fatal.Hits.Count()==1,
   "V17208 a volley ends when the last living target falls and schedules revival before the result");
  var revive=match.Step();
  check(revive.Revives&&revive.Target==wolf&&revive.Amount==100&&wolf.HP==100&&wolf.Hand==2&&
   Math.Abs(match.EffectiveAttack(wolf)-120)<.001,"V17209 revival restores exactly 20% HP and buffs its own Z Warrior");
  match.Step();
  check(!wolf.Alive&&match.Result==1&&!match.ReactionSkillNext&&match.Step()==null,
   "V17210 a second lethal hit ends the battle after the once-per-battle revival");

  var ally=Fighter(0,1,4);ally.Dodge=0;
  var nonMember=new RoundFighter(Data.RaditzId,1,7,10000,100,40,0){Dodge=0};
  wolf=new RoundFighter(card.Id,1,1,500,100,0,0){Dodge=0};
  vegeta=Fighter(1,0,1);vegeta.Attack=10000;vegeta.Crit=0;
  match=RoundCombat.Demo(new[]{vegeta,wolf,ally,nonMember},rules,15);
  wolf.HP=1;
  double alliesBefore=match.EffectiveAttack(ally),nonMemberBefore=match.EffectiveAttack(nonMember);
  match.Step();revive=match.Step();
  check(revive.Revives&&Math.Abs(ally.AttackBonus(1)-.2)<.001&&Math.Abs(match.EffectiveAttack(ally)-alliesBefore-ally.Attack/(1+ally.OpeningAttackBonus)*.2)<.01&&
   match.EffectiveAttack(nonMember)==nonMemberBefore&&ally.AttackChanges.Any(stat=>stat.Source==revive.Ability&&stat.Expiry==int.MaxValue),
   "V17211 only allied Z Warriors receive the persistent revival attack contribution");
 }

 static void CheckSaibaman(Action<bool,string> check){
  const string id="saibaman-saiyan-v030";
  if(!Data.IsRoundCard(id))return;
  var rules=Rules();
  var kit=rules.For(id);
  kit.ultimatePower=.50;
  kit.techniques=new RoundTechnique{ultimateHits=3,ultimateDistinctVolley=true,
   pursuitTrigger=RoundHitState.SmallFloat,pursuitReaction=RoundHitState.Knockdown,pursuitLimit=1};
  kit.skills[0]=new RoundAbility{name="自爆",trigger="onDefeat",effect="detonate",value=2,
   teamAttackTags=new[]{"赛亚人军团"},defeatAttackBonus=.20};
  var saibaman=Fighter(Data.Index(id),0,1);saibaman.Rage=100;
  var targets=Fighters(1,5).Skip(1).ToArray();
  var battle=RoundCombat.Demo(new[]{saibaman}.Concat(targets),rules,25);
  var acid=battle.Step();
  check(acid.SequentialHits&&acid.Hits.Count()==3&&acid.Hits.Select(h=>h.Target).Distinct().Count()==3&&
   acid.Hits.All(h=>Math.Abs(h.Amount-30)<.001),"V18101 Acid launches three distinct 50 percent hits");
  saibaman=Fighter(Data.Index(id),0,1);saibaman.Rage=100;
  battle=RoundCombat.Demo(new[]{saibaman,Fighter(0,1,1),Fighter(1,1,2)},rules,25);
  check(battle.Step().Hits.Count()==2,"V18102 Acid attacks each target once with fewer than three survivors");

  var killer=Fighter(0,0,1);
  killer.Rage=100;
  var goku=rules.For(killer.CardId);
  goku.techniques=new RoundTechnique{ultimateFrontRow=true};
  saibaman=Fighter(Data.Index(id),1,1);saibaman.HP=1;
  var linked=Fighter(1,1,4);var outsider=Fighter(4,1,6);
  battle=RoundCombat.Demo(new[]{killer,saibaman,linked,outsider},rules,25);
  var lethal=battle.Step();
  check(lethal.Hits.Count()==2&&!saibaman.Alive&&linked.HP<linked.MaxHP&&battle.HasPendingDetonation(saibaman)&&
   Math.Abs(linked.AttackBonus(battle.Round))<.001,"V18103 Every hit in the originating action settles before detonation");
  var detonation=battle.Step();
  check(detonation.Detonates&&detonation.Actor==saibaman&&detonation.Target==killer&&
   Math.Abs(detonation.Amount-120)<.001&&Math.Abs(linked.AttackBonus(battle.Round)-.20)<.001&&
   linked.AttackChanges.Any(c=>c.Source==detonation.Ability&&c.Expiry==int.MaxValue)&&
   outsider.AttackBonus(battle.Round)==0&&!battle.HasPendingDetonation(saibaman),
   "V18104 Detonation strikes the original killer and buffs only living allied army members");

  killer=Fighter(0,0,1);killer.HP=15;
  saibaman=Fighter(Data.Index(id),1,1);saibaman.HP=1;
  battle=RoundCombat.Demo(new[]{killer,saibaman},rules,25);
  battle.Step();
  check(battle.Result==0&&battle.ReactionSkillNext,"V18105 The last fighter's detonation is resolved before the verdict");
  check(battle.Step().Detonates&&battle.Result==2&&battle.Step()==null,"V18106 A double knockout is a draw");
 }
 static void CheckTien(Action<bool,string> check){
  var profile=new Profile();int id=Data.Index(Data.TienId);
  var card=Data.Cards[id];
  check(Data.IsRoundCard(card.Id)&&card.CatalogNumber==8&&card.Type==CharacterType.Output&&card.Rarity=="C"&&
   profile.Owned[id]&&profile.TryPlaceRoundMember(profile.RoundFormation(),2,id),
   "V17301 Tien keeps his stable ID and can be placed in formation");
  var rules=Rules();var kit=rules.For(card.Id);
  kit.basicPower=.8;kit.ultimatePower=1;kit.pursuitPower=.2;
  kit.techniques=new RoundTechnique{basicReactionChance=1,basicReaction=RoundHitState.Knockdown,
   ultimateFrontRow=true,ultimateHealthCost=.05,pursuitTrigger=RoundHitState.LargeFloat,pursuitReaction=RoundHitState.SmallFloat};
  kit.skills[0]=new RoundAbility{name="一指功",trigger="onRoundStart",effect="strike",value=.6};
  var actor=Fighter(id,0,2);var near=Fighter(0,1,2);var far=Fighter(0,1,8);var outside=Fighter(0,1,3);
  actor.Rage=100;
  var battle=RoundCombat.Demo(new[]{actor,near,far,outside},rules,23);
  var finger=battle.Step();
  check(finger.Move==RoundMove.Skill&&finger.Ability==kit.skills[0]&&finger.Round==1&&finger.Actor==actor&&
   finger.Hits.Count()==1&&Math.Abs(finger.Amount-36)<.001&&actor.Rage==100,
   "V17302 round-start technique runs before full rage without spending a normal hand");
  near.Shield=200;actor.Shield=60;actor.HP=700;
  var blast=battle.Step();
  check(blast.Move==RoundMove.Ultimate&&blast.Target==near&&blast.Hits.Select(h=>h.Target).SequenceEqual(new[]{near,far})&&
   blast.ActorHPCost==60&&actor.HP==640&&actor.Shield==60&&outside!=blast.Target,
   "V17303 cannon pays 5 percent max HP once, ignores own shield and resolves the facing row");
  var basic=battle.Step();
  check(basic.Actor==actor&&basic.Move==RoundMove.Basic&&basic.Target==near&&basic.TargetState==RoundHitState.Knockdown&&
   Math.Abs(basic.Amount-48)<.001,"V17304 fixed hand still attacks after the opening and ultimate");
  var nextRound=new List<RoundAction>();
  while(battle.Round<2){foreach(var unit in battle.Order)unit.Rage=0;nextRound.Add(battle.Step());}
  check(nextRound.Last().Ability==kit.skills[0]&&nextRound.Last().Round==2&&nextRound.Count(a=>a.Ability==kit.skills[0])==1,
   "V17305 the opener repeats once at the next round boundary");

  actor=Fighter(id,0,2);var rival=Fighter(id,1,2);actor.Rage=rival.Rage=100;actor.HP=4;
  battle=RoundCombat.Demo(new[]{actor,rival},rules,12);
  rival.Dodge=1;actor.Dodge=1;
  var first=battle.Step();var second=battle.Step();blast=battle.Step();
  check(first.Actor==actor&&second.Actor==rival&&first.Move==RoundMove.Skill&&second.Move==RoundMove.Skill&&
   first.Dodged&&second.Dodged&&blast.ActorHPCost==3&&actor.HP==1&&blast.Dodged,
   "V17306 opposing round-start triggers follow hand order and a missed cannon still costs HP down to one");

  var goku=Fighter(0,0,1);actor=Fighter(id,0,2);near=Fighter(1,1,1);
  rules.For(goku.CardId).techniques=new RoundTechnique{basicReactionChance=1,basicReaction=RoundHitState.LargeFloat};
  battle=RoundCombat.Demo(new[]{goku,actor,near},rules,22);
  battle.Step();
  var opening=battle.Step();
  var set=battle.Step();
  check(opening.TargetState==RoundHitState.LargeFloat&&opening.TriggersPursuit&&set.Actor==actor&&set.Move==RoundMove.Pursuit&&
   set.TargetState==RoundHitState.SmallFloat&&!battle.PursuitNext,
   "V17307 upward set follows large float once and produces small float");

  actor=Fighter(id,0,1);var piccolo=Fighter(2,1,1);
  rules.For(piccolo.CardId).skills[0]=new RoundAbility{name="反魔爆裂光",trigger="onSingleTarget",effect="counter",value=1,
   reaction=RoundHitState.Knockback,usesPerRound=1};
  battle=RoundCombat.Demo(new[]{actor,piccolo},rules,5);
  var stopped=battle.Step();
  check(stopped.Countered&&battle.CounterNext&&battle.Step().Actor==piccolo,
   "V17308 the single-target round opener participates in existing counter priority");
 }

 public static void Run(Action<bool,string> check){
  CheckTien(check);
  CheckSaibaman(check);
  CheckYamcha(check);
  CheckRaditz(check);
  CheckCounterBudget(check);
  CheckReferenceKit(check);
  CheckVegetaKit(check);
  CheckPiccoloKit(check);
  CheckCounterAcrossCharge(check);
  CheckProgression(check);
  CheckGohanKit(check);
  CheckBattlePanelTotals(check);
  CheckKrillinKit(check);
  CheckSavedDemo(check);
  var rules=Rules();rules.Validate();
  check(Enumerable.Range(1,9).All(p=>RoundBoard.Row(p)==(p-1)%3&&RoundBoard.Column(p,0)+RoundBoard.Column(p,1)==2)&&RoundBoard.Column(1,0)==2,"RB01 nine-grid mirrored indexing");
  var profile=new Profile();int[] initial=profile.RoundFormation();
  check(initial.Length==9&&initial.Count(i=>i>=0)==5&&initial[2]==2&&initial[3]==3&&initial[4]==4&&profile.Team.Length==6,"RB02 default keeps only online cards and preserves legacy team history");
  check(!profile.SetRoundMember(7,6)&&!profile.SetRoundMember(7,8)&&profile.RoundFormation().SequenceEqual(initial),"RB03 sixth member and unowned data rejected atomically");
  var archive=new Profile{RoundFormationIds=new[]{"chiaotzu-saiyan-v020","","","","","piccolo-test","","",""}};
  check(archive.NeedsRoundRosterMigration&&archive.RoundFormation()[5]==2&&archive.RoundFormation().Count(i=>i>=0)==1,
   "V14404 saved formations remove offline cards without moving remaining online members");
  archive.MigrateRoundRoster();
  check(!archive.NeedsRoundRosterMigration&&archive.Owned[5]&&archive.RoundFormationIds[0]=="",
   "V14405 migrating the grid preserves retired card ownership history");
  archive.RoundFormationIds=new[]{"chiaotzu-saiyan-v020","","","","","","","",""};
  archive.MigrateRoundRoster();
  check(archive.RoundFormation()[0]==0&&Data.Card("goku-test").HasTag(Data.LowerClassTag),
   "V14406 all-offline grid falls back to first owned online card and uses the new Goku tag");
  check(profile.SetRoundMember(8,0)&&profile.RoundFormation()[7]==0&&profile.RoundFormation()[0]==-1&&profile.RoundFormationIds[7]=="goku-test","RB04 moving creates stable-ID grid");
  check(profile.SetRoundMember(2,0)&&profile.RoundFormation()[1]==0&&profile.RoundFormation()[7]==1,"RB05 occupied cells swap");
  foreach(int pos in new[]{2,3,4,5})profile.RemoveRoundMember(pos);
  check(!profile.RemoveRoundMember(8)&&profile.RoundFormation().Count(i=>i>=0)==1,"RB06 cannot remove final ally");
  var round=new RoundCombat(Fighters(5,9).Reverse(),rules,3);
  check(round.Order.Select(f=>f.Side).SequenceEqual(new[]{0,1,0,1,0,1,0,1,0,1,1,1,1,1})&&
   round.Order.Select(f=>f.Hand).SequenceEqual(Enumerable.Range(1,14)),"RB07 5v9 player first alternating then enemy tail");
  check(round.Order[0].Attack>100&&round.Order[6].Attack==100,"RB08 passive starts without consuming a hand");
  var first=round.Step();check(first.Actor.Hand==1&&first.Move==RoundMove.Basic,"RB09 order starts with first normal");
  round.Order[1].Rage=100;round.Order[3].Rage=100;
  var u0=round.Step();var u1=round.Step();var b1=round.Step();
  check(u0.Move==RoundMove.Ultimate&&u0.Actor.Hand==2&&u1.Move==RoundMove.Ultimate&&u1.Actor.Hand==4&&b1.Move==RoundMove.Basic&&b1.Actor.Hand==2,"RB10 simultaneous rage follows hands and preserves basic");
  round=new RoundCombat(Fighters(5,5),rules,2);
  round.Order[1].HP=1;
  var killed=round.Step();var next=round.Step();
  check(!killed.Target.Alive&&killed.Target.Hand==2&&next.Actor.Hand==3&&next.Actor.Side==0,"RB11 dead hand skipped, survivors not renumbered");
  round=new RoundCombat(Fighters(2,5),rules,2);
  round.Order[0].Crit=1;round.Order[0].Rage=65;
  var combo=round.Step();
  check(combo.Critical&&combo.TriggersPursuit&&round.Step().Move==RoundMove.Ultimate&&round.Step().Move==RoundMove.Pursuit,"RB12 rage interrupts the queued pursuit after crit");
  round=new RoundCombat(Fighters(1,5),rules,2);
  round.Order[0].Crit=1;
  var solo=round.Step();
  check(solo.Critical&&!solo.TriggersPursuit,"RB18 solo crit has no pursuit cue");
  round=new RoundCombat(Fighters(4,5),rules,2);
  var wounded=round.Order[2];wounded.HP=250;
  check(round.Step().Move==RoundMove.Basic&&round.Step().Healing&&wounded.HP>250,"RB13 conditional action skill");
  var bored=Fighters(1,5);foreach(var f in bored){f.Dodge=1;}
  round=new RoundCombat(bored,rules,2);int guard=0;
  while(round.Result==0&&guard++<1000)round.Step();
  check(round.Result==2&&round.Round==RoundCombat.MaxRounds,"RB14 draw after bounded rounds with no hits");
  bool bad=false;try{new RoundCombat(Fighters(6,5),rules,2);}catch(InvalidOperationException){bad=true;}
  check(bad,"RB15 rejects illegal PVE capacity");
  bad=false;try{var badKit=Rules();badKit.cards[0].skills=new RoundAbility[0];badKit.Validate();}catch(InvalidOperationException){bad=true;}
  check(bad,"RB16 rarity skill counts validated");
  check(Data.Card("vegeta-test").HasTag(Data.RivalTag)&&Data.Card("piccolo-test").HasTag(Data.RivalTag)&&
   Data.Cards.Count(card=>card.HasTag(Data.RivalTag))==2,"RD01 rival identity matches the current worksheet");
  var blank=Enumerable.Repeat("",RoundBoard.Cells).ToArray();
  var setup=new RoundDemoSetup{scene="rocky-training",allies=(string[])blank.Clone(),enemies=(string[])blank.Clone()};
  setup.allies[1]="goku-test";setup.enemies[1]="goku-test";
  var demo=setup.CreateBattle(rules,4);
  check(demo.Order.Count==2&&demo.Order[0].Side==0&&demo.Order[1].Side==1&&demo.Order.All(f=>f.Position==2),
   "RD02 configured roster uses both slot twos without reading the saved formation");
  setup.allies[4]="piccolo-test";setup.enemies[2]="vegeta-test";setup.enemies[7]="krillin-test";
  demo=setup.CreateBattle(rules,4);
  check(demo.Order.Select(f=>f.Position).SequenceEqual(new[]{2,2,5,3,8})&&
   demo.Order.Select(f=>f.Side).SequenceEqual(new[]{0,1,0,1,1}),"RD03 demo uses normal fixed initiative and omits all empty slots");
  var lead=demo.Order[0];var front=demo.Order[1];
  var rear=demo.Order.First(f=>f.Side==1&&f.Position==8);
  foreach(var fighter in demo.Order)fighter.Dodge=0;
  var demoHit=demo.Step();
  check(demoHit.Target==front&&demoHit.Amount>0&&rear.HP==rear.MaxHP,"RD04 demo targets live front first and applies normal damage");
  front.HP=0;
  demoHit=demo.Step();
  check(demoHit.Actor==demo.Order[2]&&demoHit.Target.Position==3,"RD05 skips defeated fighter and still prefers front before aligned rear");
  bad=false;try{new RoundCombat(Fighters(1,1),rules,3);}catch(InvalidOperationException){bad=true;}
  check(bad,"RD06 regular trial retains the five-enemy minimum");
  bad=false;setup.enemies=(string[])blank.Clone();
  try{setup.CreateBattle(rules,3);}catch(InvalidOperationException){bad=true;}
  check(bad,"RD07 demo rejects an empty side");
  bad=false;setup.enemies[1]="goku-test";setup.allies[0]="goku-test";
  try{setup.CreateBattle(rules,3);}catch(InvalidOperationException){bad=true;}
  check(bad,"RD08 demo rejects duplicate allied cards");
  var scene=new RoundSceneDefinitions{
   demo=new RoundDemoSetup{scene="rocky-training",allies=new[]{"","goku-test","","","","","","",""},enemies=(string[])setup.enemies.Clone()},
   scenes=new[]{new RoundScene{id="rocky-training",title="高原",background="Art/V0132/Goku/arena"}}
  };
  scene.Validate();
  check(scene.Scene(scene.demo.scene).background=="Art/V0132/Goku/arena","RD09 scene resolves the configured runtime background");
  bad=false;scene.scenes[0].background="Art/../external";
  try{scene.Validate();}catch(InvalidOperationException){bad=true;}
  check(bad,"RD10 scene rejects a background outside its resource path");
  int[] seven=new CatalogFilter{OwnedOnly=false,RoundFormat=true}.Results(new Profile());
  check(seven.Take(initial.Count(i=>i>=0)).SequenceEqual(initial.Where(i=>i>=0)),"RB17 gallery lineup order uses new formation");
 }
}
