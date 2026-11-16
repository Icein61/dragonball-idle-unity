using System;
using System.Linq;
using DragonIdle;
static class ChapterChecks {
 static Unit Enemy(int slot){return new Unit{CardId="dummy"+slot,Name="训练靶"+slot,Role="fighter",Side=1,Slot=slot,HP=10000,MaxHP=10000,Attack=40,Defense=100,Speed=90,Crit=0,Dodge=0,SpecialName="测试绝招",UltimateName="测试必杀"};}
 static Unit Actor(string id){var a=Data.Make(id,0,0,0,0,"",1);a.Progress=1;a.Crit=0;a.Dodge=0;return a;}
 static bool Near(double a,double b){return Math.Abs(a-b)<.000001;}
 public static void Run(Action<bool,string> check){
  var a=Actor("goku-test");var target=Enemy(0);var b=new Battle(new[]{a,target},6);var e=b.Step();
  check(e.Move==Move.Special&&Near(target.Sum("defense"),-.08)&&target.Effects[0].Expires==2,"S01 Goku opens two-opportunity defense window");
  target.Opportunities=1;target.Expire();check(Near(target.Sum("defense"),-.08),"S02 window lasts first target opportunity");
  target.Opportunities=2;target.Expire();check(Near(target.Sum("defense"),0),"S03 window expires independently");
  a=Actor("piccolo-raditz-v030");a.Attack=100;a.Rage=100;target=Enemy(0);target.Add("defense",-.08,2,"goku");b=new Battle(new[]{a,target},7);e=b.Step();
  double expected=Math.Round(100*1.22*100/(100+92*.75));
  check(e.Move==Move.Ultimate&&e.Damage==expected&&a.Opportunities==0&&a.Progress==1&&target.Effects[0].Expires==2,"S04 beam exploits defense window without ticking time");
  a=Actor("raditz-arrival-v030");var front=Enemy(0);var second=Enemy(1);var rear=Enemy(3);second.Dodge=1;front.Rage=95;
  b=new Battle(new[]{a,front,second,rear},9);e=b.Step();
  check(e.Hits.Count==2&&e.Hits[0].Target==front&&e.Hits[1].Target==second&&e.Hits[1].Dodged&&rear.HP==rear.MaxHP,"S05 dual attack distinct front targets and independent dodge");
  check(e.Damage==e.Hits.Sum(h=>h.Damage)&&front.Rage==100&&second.Rage==0&&a.Rage==40&&a.Cooldown==4&&a.Opportunities==1,"S06 dual hit grants normal action rage and cooldown only once");
  check(b.Step().Actor==front,"S07 received-rage ultimate waits for complete dual action");
  a=Actor("raditz-arrival-v030");target=Enemy(0);b=new Battle(new[]{a,target},10);e=b.Step();check(e.Hits.Count==1,"S08 dual attack does not hit lone target twice");
  a=Data.Make("goku-test",0,0,0,0,"pierce",1);a.Progress=1;a.Crit=0;front=Enemy(0);rear=Enemy(3);b=new Battle(new[]{a,front,rear},11);e=b.Step();
  check(e.Target==rear&&front.Sum("defense")==0&&rear.Sum("defense")==0&&a.Cooldown==4,"S09 replacement fully replaces original special");
  a.Rage=100;e=b.Step();check(e.Move==Move.Ultimate&&Near(front.Sum("defense"),-.08),"S10 replacement retains original ultimate");
  a=Actor("krillin-test");target=Enemy(0);target.Dodge=1;b=new Battle(new[]{a,target},12);b.Step();check(!target.Stunned,"S11 dodged solar flare does not stun");
  a=Actor("piccolo-test");target=Enemy(0);target.Dodge=1;double shield=a.Shield;b=new Battle(new[]{a,target},13);b.Step();check(Near(a.Shield-shield,a.MaxHP*.12),"S12 defensive special still shields on dodge");

  var p=new Profile();bool rejected=false;try{p.NewStoryBattle(1,0);}catch(ArgumentException){rejected=true;}check(rejected,"S13 locked chapter rejected");
  int oldUnlocked=p.Unlocked,oldPulls=p.Pulls;
  string reward=p.RewardStory(0,1);int beam=Data.Index("piccolo-raditz-v030");int crystals=p.Crystals;
  check(p.Owned[beam]&&p.StoryUnlocked==1&&p.StoryCleared[0]&&p.Unlocked==oldUnlocked&&reward.Contains("首通"),"S14 first clear awards independent Piccolo and unlocks story only");
  p.RewardStory(0,2);check(p.Shards[beam]==0&&p.Crystals-crystals==6,"S15 replay cannot repeat first-clear card or crystals");
  p.RewardStory(1,3);p.RewardStory(2,4);p.RewardStory(3,5);
  int raditz=Data.Index("raditz-arrival-v030");
  check(p.Owned[raditz]&&p.StoryCleared.All(x=>x)&&p.Items.Any(i=>i.kind==2&&i.locked)&&p.Pulls==oldPulls,"S16 final reward and preserved old pool");
  int locked=p.Items.Count(i=>i.locked);p.RewardStory(3,6);check(p.Shards[raditz]==0&&p.Items.Count(i=>i.locked)==locked,"S17 repeated boss no guaranteed reward duplicate");
  p=new Profile();p.Acquire(beam);p.RewardStory(0,1);check(p.Shards[beam]==1,"S18 already owned story reward converts only matching card");
  p=new Profile();p.SaiyanPulls=19;p.Crystals=50;check(p.SummonSaiyan(3).StartsWith("[SSR]")&&p.Pulls==0&&p.SaiyanPulls==20&&p.Crystals==0,"S19 separate saga pity transaction");
  p.SummonSaiyan(4);check(p.SaiyanPulls==20,"S20 insufficient currency preserves saga pity");
  p=new Profile();p.Crystals=50000;for(int i=0;i<1000;i++)p.SummonSaiyan(i);
  check(Data.Cards.Where(Data.InSaiyanPool).Count()==14&&Enumerable.Range(0,Data.Cards.Length).Where(i=>!Data.InSaiyanPool(Data.Cards[i])).All(i=>p.Shards[i]==0),"S21 saga pool excludes later eras");
  p=new Profile();p.Crystals=5000;for(int i=0;i<100;i++)p.Summon(i);check(Enumerable.Range(20,5).All(i=>!p.Owned[i]),"S22 original pool keeps its twenty IDs");

  // v0.2 array lengths, no chapter fields; preserve all old ownership, equipment, presets.
  p=new Profile();p.CardIds=p.CardIds.Take(20).ToArray();p.Training=p.Training.Take(20).ToArray();p.Training[0]=99;
  p.Shards=p.Shards.Take(20).ToArray();p.Shards[13]=7;p.Resonance=p.Resonance.Take(20).ToArray();p.Policy=p.Policy.Take(20).ToArray();
  p.Owned=p.Owned.Take(20).ToArray();p.Owned[13]=true;p.Skill=p.Skill.Take(20).ToArray();p.Skill2=p.Skill2.Take(20).ToArray();
  p.Team[6]=13;p.Items[0].owner=13;p.StoryCleared=new bool[0];p.Normalize();
  check(p.CardIds.Length==25&&p.Owned[13]&&p.Shards[13]==7&&p.Training[0]==99&&p.Team[6]==13&&p.Items[0].owner==13&&!p.Owned[beam]&&p.StoryUnlocked==0,"S23 v020 migration retains late-era cards and growth");
  p.Normalize();check(p.Training[0]==99&&p.Shards[13]==7&&p.StoryCleared.Length==4,"S24 migration idempotent");
    var invalid=new Profile();invalid.StoryCleared=new bool[0];rejected=false;try{invalid.Normalize();}catch(InvalidOperationException){rejected=true;}check(rejected,"S24b current schema corrupted chapter array rejected");
  foreach(var c in Data.Cards.Where(Data.InSaiyanPool)){
   a=Actor(c.Id);target=Enemy(0);b=new Battle(new[]{a,target},20);e=b.Step();
   check(e.Move==Move.Special&&e.Hits.Count>0&&!string.IsNullOrEmpty(c.SpecialText)&&!string.IsNullOrEmpty(c.UltimateText),"S25 playable saga kit "+c.Id);
  }
  for(int stage=0;stage<4;stage++){
   int wins=0,totalEvents=0;
   for(int seed=0;seed<40;seed++){
    p=new Profile();p.StoryUnlocked=3;p.StoryCleared=new[]{true,true,true,false};
    if(stage>=1){p.Acquire(beam);p.SetMember(6,beam);}
    b=p.NewStoryBattle(stage,seed);int n=0;while(b.Result==0&&n++<4000)b.Step();
    check(b.Result!=0,"S26 chapter terminates "+stage+"/"+seed);
    if(b.Result==1)wins++;totalEvents+=n;
   }
   Console.WriteLine("Story stage "+stage+": wins "+wins+"/40, mean events "+(totalEvents/40.0).ToString("0.0"));
   check(wins>=32,"S27 entry chapter reliably clearable "+stage);
  }
 }
}
