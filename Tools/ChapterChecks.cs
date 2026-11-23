using System;
using System.Linq;
using DragonIdle;
static class ChapterChecks {
 static Unit Enemy(int slot){return new Unit{CardId="dummy"+slot,Name="训练靶"+slot,Role="fighter",Side=1,Slot=slot,HP=10000,MaxHP=10000,Attack=40,Defense=100,Speed=90,Crit=0,Dodge=0,SpecialName="测试绝招",UltimateName="测试必杀"};}
 static Unit Actor(string id){var a=Data.Make(id,0,0,0,0,"",1);a.Progress=1;a.Crit=0;a.Dodge=0;return a;}
 static bool Near(double a,double b){return Math.Abs(a-b)<.000001;}
 public static void Run(Action<bool,string> check){
  var a=Actor("goku-test");var target=Enemy(0);var b=new Battle(new[]{a,target},6);var e=b.Step();
  check(e.Move==Move.Special&&Near(target.Sum("defense"),0)&&Near(a.Sum("attack"),.08),"S01 user Goku strengthens himself without old defense debuff");
  a.Opportunities=2;a.Expire();check(Near(a.Sum("attack"),.08),"S02 self enhancement lasts first following opportunity");
  a.Opportunities=3;a.Expire();check(Near(a.Sum("attack"),.04),"S03 temporary self enhancement expires leaving base passive");
  Unit front, rear;
  a=Data.Make("goku-test",0,0,0,0,"pierce",1);a.Progress=1;a.Crit=0;front=Enemy(0);rear=Enemy(3);b=new Battle(new[]{a,front,rear},11);e=b.Step();
  check(e.Target==rear&&front.Sum("defense")==0&&rear.Sum("defense")==0&&a.Cooldown==4,"S09 replacement fully replaces original special");
  a.Rage=100;e=b.Step();check(e.Move==Move.Ultimate&&e.Target==front&&e.Damage>0&&Near(front.Sum("defense"),0),"S10 replacement retains user ultimate without retired debuff");
  a=Actor("krillin-test");target=Enemy(0);target.Dodge=1;b=new Battle(new[]{a,target},12);b.Step();check(!target.Stunned,"S11 new Krillin special does not stun");
  a=Actor("piccolo-test");target=Enemy(0);target.Dodge=1;double shield=a.Shield;b=new Battle(new[]{a,target},13);b.Step();check(Near(a.Shield,shield)&&Near(a.Sum("defense"),.12),"S12 user defensive special strengthens same row including self on dodge");

  // User rework boundary cases, independent of chapter win rates.
  a=Actor("goku-test");var deadPiccolo=Data.Make("piccolo-test",0,1,0,0,"",1);deadPiccolo.HP=0;target=Enemy(0);b=new Battle(new[]{a,deadPiccolo,target},18);
  check(Near(a.Sum("defense"),.08),"W01 Goku recognizes fallen Piccolo by person identity");
  b.Step();check(Near(a.Sum("defense"),.08)&&Near(target.Sum("defense"),0),"W02 linkage persists and old debuff absent");
  a=Actor("vegeta-test");var sameRow=Data.Make("gohan-test",0,2,0,0,"",1);var otherRow=Data.Make("yamcha-saiyan-v020",0,3,0,0,"",1);target=Enemy(0);b=new Battle(new[]{a,sameRow,otherRow,target},19);e=b.Step();
  check(e.Recipients.Count==2&&e.Recipients.Contains(a)&&e.Recipients.Contains(sameRow)&&!e.Recipients.Contains(otherRow),"W03 row buff includes self and excludes other row");
  check(Near(a.Sum("attack"),.12)&&Near(sameRow.Sum("attack"),.08),"W04 additive base and temporary attack contributions");
  sameRow.Opportunities=2;sameRow.Expire();check(Near(sameRow.Sum("attack"),.04),"W05 recipient expires on its own opportunities");
  a=Actor("vegeta-test");a.Rage=100;front=Enemy(0);front.HP=500;front.MaxHP=1000;rear=Enemy(3);rear.HP=1000;rear.MaxHP=10000;b=new Battle(new[]{a,front,rear},20);e=b.Step();
  check(e.Target==rear&&e.Move==Move.Ultimate&&Near(a.Progress,1)&&a.Opportunities==0,"W06 ultimate targets lowest percent across rows without using normal action");
  a=Actor("piccolo-test");a.Rage=100;a.HP=50;sameRow=Data.Make("gohan-test",0,1,0,0,"",1);sameRow.HP=sameRow.MaxHP*.20;otherRow=Data.Make("yamcha-saiyan-v020",0,3,0,0,"",1);otherRow.HP=otherRow.MaxHP*.60;target=Enemy(0);double enemyHP=target.HP;b=new Battle(new[]{a,sameRow,otherRow,target},21);e=b.Step();
  check(a.HP==1&&e.HealthCost==49&&e.Healing==0,"W07 life payment clamps to one and ultimate does not regenerate");
  check(e.Hits.Count==0&&target.HP==enemyHP&&target.Rage==0,"W08 shield ultimate neither attacks nor grants enemy received rage");
  check(e.Recipients.Count==2&&e.Recipients.Contains(a)&&e.Recipients.Contains(sameRow)&&Near(a.Shield,a.MaxHP*.12)&&Near(sameRow.Shield,sameRow.MaxHP*.12)&&otherRow.Shield==0,"W09 two shield recipients use their own maximum health");
  a=Actor("piccolo-test");a.HP=a.MaxHP*.50;a.Stun(1);target=Enemy(0);b=new Battle(new[]{a,target},22);double beforeHP=a.HP;e=b.Step();
  check(e.Move==Move.Stunned&&Near(a.HP-beforeHP,a.MaxHP*.03)&&a.Cooldown==0,"W10 skipped normal opportunity regenerates without ticking cooldown");
  a=Actor("piccolo-test");a.HP=a.MaxHP-1;target=Enemy(0);b=new Battle(new[]{a,target},23);e=b.Step();check(Near(a.HP,a.MaxHP)&&Near(e.Healing,1),"W11 natural regeneration cannot overheal");
  check(Data.UserSkill("piccolo-test",2)=="损失中生命，对己方生命最低两人释放【护盾】中"&&Data.Card("goku-test").PassiveText=="【攻击强化】低 / 己方存在【比克】时，【防御强化】中","W12 user skill wording remains verbatim");
  a=Actor("krillin-test");a.Rage=100;front=Enemy(0);rear=Enemy(5);
  var front2=Enemy(1);front2.Dodge=1;var front3=Enemy(2);
  e=new Battle(new[]{a,front,front2,front3,rear},31).Step();
  check(e.Hits.Select(hit=>hit.Target.Slot).SequenceEqual(new[]{0,1,2})&&e.Hits[1].Dodged&&rear.HP==rear.MaxHP,"W13 Krillin hits each front slot separately");
  a=Actor("gohan-test");target=Enemy(0);target.Rage=10;
  e=new Battle(new[]{a,target},32).Step();
  check(e.Move==Move.Special&&target.Rage==1&&a.Rage==44,"W14 Gohan transfers rage rather than generating it");
  a=Actor("tien-test");a.Rage=100;a.HP=2;target=Enemy(0);
  e=new Battle(new[]{a,target},33).Step();
  check(e.HealthCost==1&&a.HP==1&&e.Move==Move.Ultimate&&e.Damage>0,"W15 life cost leaves one health");
  a=Actor("goku-saiyan-kaioken-v020");target=Enemy(0);target.Shield=1000;
  e=new Battle(new[]{a,target},34).Step();
  check(e.HealthDamage==0&&e.Healing==0&&e.HealthCost>0,"W16 life steal uses health damage only");
  var yamcha=Actor("yamcha-saiyan-v020");var max=yamcha.MaxHP;
  var five=new[]{yamcha.CardId,"goku-test","piccolo-test","tien-test","chiaotzu-saiyan-v020"};
  Data.ApplyTeamDesign(yamcha,five);Data.ApplyTeamDesign(yamcha,five);
  check(Near(yamcha.Sum("attack"),.12)&&Near(yamcha.Sum("defense"),.12)&&Near(yamcha.MaxHP,max*1.08),"W17 formation passives stack once at the declared thresholds");
  var p=new Profile();bool rejected=false;try{p.NewStoryBattle(1,0);}catch(ArgumentException){rejected=true;}check(rejected,"S13 locked chapter rejected");
  int oldUnlocked=p.Unlocked,oldPulls=p.Pulls;
  string reward=p.RewardStory(0,1);
  int crystals=p.Crystals;
  check(p.Owned.Count(x=>x)==Data.StarterCardIds.Length&&p.StoryUnlocked==1&&p.StoryCleared[0]&&p.Unlocked==oldUnlocked&&reward.Contains("首通"),"S14 first clear unlocks story without awarding removed cards");
  p.RewardStory(0,2);
  check(p.Crystals-crystals==6,"S15 replay cannot repeat first-clear crystals");
  p.RewardStory(1,3);p.RewardStory(2,4);p.RewardStory(3,5);
  check(p.StoryCleared.All(x=>x)&&p.Items.Any(i=>i.kind==2&&i.locked)&&p.Pulls==oldPulls,"S16 final reward and preserved pool counter");
  int locked=p.Items.Count(i=>i.locked);p.RewardStory(3,6);
  check(p.Items.Count(i=>i.locked)==locked,"S17 repeated boss no guaranteed reward duplicate");
  p=new Profile();p.SaiyanPulls=19;p.Crystals=50;check(p.SummonSaiyan(3).StartsWith("[A]")&&p.Pulls==0&&p.SaiyanPulls==20&&p.Crystals==0,"S19 separate saga pity transaction");
  p.SummonSaiyan(4);check(p.SaiyanPulls==20,"S20 insufficient currency preserves saga pity");
  p=new Profile();p.Crystals=50000;for(int i=0;i<1000;i++)p.SummonSaiyan(i);
  check(Data.Cards.Where(Data.InSaiyanPool).Count()==Data.Cards.Length&&Enumerable.Range(0,Data.Cards.Length).Where(i=>!Data.InSaiyanPool(Data.Cards[i])).All(i=>p.Shards[i]==0),"S21 saga pool excludes later eras");
  p=new Profile();p.Crystals=5000;for(int i=0;i<100;i++)p.Summon(i);check(Data.Cards.Where(Data.InClassicPool).Count()==9,"S22 classic pool contains only nine configured saga IDs");
  p=ScopeChecks.Previous(20);p.Training[0]=99;p.Shards[13]=7;p.Owned[13]=true;p.Team[6]=13;p.Items[0].owner=13;p.StoryCleared=new bool[0];p.Normalize();
  check(p.CardIds.Length==Data.Cards.Length&&p.Retired.Single(r=>r.cardId=="goku-namek-ssj-v020").shards==7&&p.Training[0]==99&&p.Team.Length==Data.TeamSlots&&p.Items[0].owner==-1&&p.StoryUnlocked==0,"S23 v020 migration archives removed cards without erasing growth");
  p.Normalize();check(p.Training[0]==99&&p.Retired.Single(r=>r.cardId=="goku-namek-ssj-v020").shards==7&&p.StoryCleared.Length==4,"S24 migration idempotent");
    var invalid=new Profile();invalid.StoryCleared=new bool[0];rejected=false;try{invalid.Normalize();}catch(InvalidOperationException){rejected=true;}check(rejected,"S24b current schema corrupted chapter array rejected");
  foreach(var c in Data.Cards.Where(Data.InSaiyanPool)){
   a=Actor(c.Id);target=Enemy(0);b=new Battle(new[]{a,target},20);e=b.Step();
   check(e.Move==Move.Special&&e.Hits.Count>0&&!string.IsNullOrEmpty(c.SpecialText)&&!string.IsNullOrEmpty(c.UltimateText),"S25 playable saga kit "+c.Id);
  }
  for(int stage=0;stage<4;stage++){
   int wins=0,totalEvents=0;
   for(int seed=0;seed<40;seed++){
    p=new Profile();p.StoryUnlocked=3;p.StoryCleared=new[]{true,true,true,false};
    b=p.NewStoryBattle(stage,seed);int n=0;while(b.Result==0&&n++<4000)b.Step();
    check(b.Result!=0,"S26 chapter terminates "+stage+"/"+seed);
    if(b.Result==1)wins++;totalEvents+=n;
   }
   Console.WriteLine("Story stage "+stage+": wins "+wins+"/40, mean events "+(totalEvents/40.0).ToString("0.0"));
   check(wins>=32,"S27 entry chapter reliably clearable "+stage);
  }
 }
}
