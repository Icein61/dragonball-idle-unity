using System;
using System.Linq;
using System.Collections.Generic;
using DragonIdle;
class CoreChecks {
 static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);count++;Console.WriteLine("PASS "+name);}
 static bool Near(double a,double b){return Math.Abs(a-b)<.000001;}
 static Unit U(int side,int slot=0){return new Unit{CardId="test"+slot,Name="test"+slot,Side=side,Slot=slot,MaxHP=100000,HP=100000,Attack=10,Defense=10,Speed=100,SpecialName="special",UltimateName="ult",Role="fighter",Crit=0,Dodge=0};}
 static Battle Pair(Unit a,Unit b){return new Battle(new[]{a,b},42);}
 static void Main(){try{Run();}catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);Environment.Exit(1);}}
 static void Run(){
  string legacySave=System.IO.Path.GetFullPath(System.IO.Path.Combine("profiles","Icein61","Dragon Idle Demo","profile-v1.json"));
  Check(Data.DefaultSavePath(System.IO.Path.Combine("profiles","Icein61",Data.GameTitle))==legacySave,"Rename preserves existing profile and audio storage directory");
  Check(Data.DefaultSavePath(System.IO.Path.Combine("profiles","Icein61","Dragon Idle Demo"))==legacySave,"Legacy product storage remains stable");
  var u=U(0);u.Add("attack",.2,0,"passive");u.Add("attack",.1,3,"equipment");u.Add("attack",.15,2,"ally");
  double[] expected={.45,.45,.30,.35,.35,.20};
  for(int turn=1;turn<=6;turn++){if(turn==4)u.Add("attack",.15,2,"ultimate");Check(Near(u.Sum("attack"),expected[turn-1]),"T19 turn "+turn);u.Opportunities++;u.Expire();}
  u=U(0);u.Add("attack",.2,0,"permanent");u.Add("attack",.3,2,"temp");u.Opportunities=2;u.Expire();Check(Near(u.Sum("attack"),.2),"T20 independent expiry");
  u=U(0);u.Add("attack",.1,2,"same");u.Opportunities=1;u.Add("attack",.1,2,"same");u.Opportunities=2;u.Expire();Check(Near(u.Sum("attack"),.1),"T21 repeated source independent expiry");
  u=U(0);u.LeaderAttack=.2;u.Add("attack",.3,0,"normal");Check(Near(u.AttackValue/u.Attack,1.56),"T24 leader separate multiplier");
  u.Add("defense",.7,0,"unrelated");Check(Near(u.AttackValue/u.Attack,1.56),"T25 unrelated category");
  var a=U(0);var b=U(1);a.Rage=-10000;b.Rage=-10000;
  var battle=Pair(a,b);var moves=new List<Move>();
  while(moves.Count<5){var e=battle.Step();if(e.Actor==a&&e.Move!=Move.Ultimate)moves.Add(e.Move);}
  Check(moves.SequenceEqual(new[]{Move.Special,Move.Basic,Move.Basic,Move.Basic,Move.Special}),"T05 exact cooldown");
  var units=new List<Unit>();for(int i=0;i<7;i++){units.Add(U(0,i));units.Add(U(1,i));}
  battle=new Battle(units,44);var order=new List<int>();for(int i=0;i<14;i++){var e=battle.Step();order.Add(e.Actor.Slot*2+e.Actor.Side);}
  Check(order.SequenceEqual(Enumerable.Range(0,14)),"T02 simultaneous slot order");
  a=U(0);b=U(1);a.Speed=99;b.Speed=100;b.Slot=6;
  battle=Pair(a,b);Check(battle.Step().Actor==b,"T02 earlier time before slot order");
  a=U(0);b=U(1);a.Progress=.7;a.Add("speed",-.5,0,"slow");Check(Near(a.Progress,.7)&&Near(a.SpeedValue,50),"T03 speed retains progress");
  a=U(0);b=U(1);a.Cooldown=3;a.Rage=12;a.Stun(1);battle=Pair(a,b);var ev=battle.Step();
  Check(ev.Move==Move.Stunned&&a.Cooldown==3&&a.Rage==12&&a.Opportunities==1,"T06 stun opportunity vs cooldown");
  Check(a.StunProtection&&!a.Stunned,"T18 stun ends with protection");a.Stun(1);Check(!a.Stunned,"T18 repeat stun protection");
  a=U(0);b=U(1);a.Rage=100;a.Progress=.4;a.Cooldown=2;a.Add("attack",.1,2,"temp");
  battle=Pair(a,b);ev=battle.Step();
  Check(ev.Move==Move.Ultimate&&Near(a.Progress,.4)&&a.Cooldown==2&&a.Opportunities==0,"T07 ultimate extra action");
  Check(a.Rage==0&&a.Effects.First(x=>x.Source=="temp").Expires==2,"T08 ultimate no rage or timer tick");
  a=U(0);b=U(1);b.Rage=95;b.Progress=.2;a.Progress=1;b.Speed=1;b.Cooldown=2;
  battle=Pair(a,b);ev=battle.Step();Check(ev.Actor==a&&b.Rage==100,"T07 attack gives received rage");ev=battle.Step();
  Check(ev.Actor==b&&ev.Move==Move.Ultimate&&Near(b.Progress,.2),"T07 received rage inserts ultimate");
  a=U(0);b=U(1);b.Shield=999;b.Rage=0;b.Progress=0;double before=b.HP;
  battle=Pair(a,b);ev=battle.Step();Check(b.HP==before&&ev.HealthDamage==0&&ev.ShieldDamage>0&&b.Rage==7,"T15 shield hit vs HP damage");
  a=U(0);b=U(1);b.Dodge=1;battle=Pair(a,b);ev=battle.Step();Check(ev.Dodged&&b.Rage==0&&ev.Damage==0,"T16 dodge not hit");
  a=U(0);b=U(1);a.Role="control";b.Shield=999;battle=Pair(a,b);battle.Step();Check(b.Stunned,"T17 shield does not block control");
  a=U(0);var f=U(1,2);var rear=U(1,3);battle=new Battle(new[]{a,f,rear},2);Check(battle.Target(a,false)==f,"T13 front priority");f.HP=0;Check(battle.Target(a,false)==rear,"T13 rear fallback");
  f.HP=100;battle=new Battle(new[]{a,f,rear},2);Check(battle.Target(a,true)==rear,"T14 explicit rear skill");
  a=U(0);b=U(1);a.Rage=100;a.SealUntil=1;a.Progress=1;battle=Pair(a,b);ev=battle.Step();Check(ev.Move==Move.Basic&&a.Rage==100,"T10 sealed unit cannot ultimate/special");
  a=U(0);b=U(1);a.Rage=100;a.UltimatePolicy=1;a.Progress=1;battle=Pair(a,b);Check(battle.Step().Move!=Move.Ultimate,"strategy holds ultimate");
  var p=new Profile();Check(p.CreateTeam().Count==7&&p.CreateTeam().All(x=>x.Progress==0&&x.Cooldown==0&&x.Rage==(Data.Card(x.CardId).Passive=="resolve"?12:0)),"T11 start state with declared opening passives");
  p.Team[1]=p.Team[0];bool rejected=false;try{p.CreateTeam();}catch{rejected=true;}Check(rejected,"T30 duplicate card rejected");
  p=new Profile();p.Items.Add(new Item{id="x",kind=0});Check(p.Equip("x",1,2)&&p.Items.Last().owner==1,"T34 equip valid slot");
  Check(!p.Equip("x",1,3),"T34 reject fourth slot");p.Equip("x",2,0);Check(p.Items.Count(x=>x.id=="x"&&x.owner==2)==1,"T36 single item owner");
  Check(p.SetSkill(0,0,"focus")&&p.SetSkill(0,1,"pierce"),"T34 two skill slots coexist");
  Check(!p.SetSkill(0,0,"pierce"),"T34 prevent duplicate replacement");
  Check(p.CreateTeam()[0].SpecialRole=="sniper"&&Near(p.CreateTeam()[0].Crit,.115),"T35 replacement and passive both effective");
  var supportProfile=new Profile();supportProfile.SetSkill(4,0,"pierce");var supportBattle=supportProfile.NewBattle(0,55);
  var support=supportBattle.Units.First(x=>x.Side==0&&x.Slot==4);support.Rage=100;
  Check(support.Role=="support"&&support.SpecialRole=="sniper","T35 replacement preserves original role");
  supportBattle.Step();Check(supportBattle.Units.First(x=>x.Side==0&&x.Slot==1).Effects.Any(x=>x.Source==support.CardId&&Near(x.Value,.06)),"T35 replacement preserves ultimate support effect");
  string tag=Data.Cards[0].Tag;p.Skill2[0]="";p.Skill[0]="pierce";var changed=p.CreateTeam()[0];Check(changed.SpecialName=="贯穿气弹"&&changed.UltimateName==Data.Cards[0].Ultimate&&Data.Cards[0].Tag==tag,"T31 T35 replacement preserves identity");
  p=new Profile();int owned=p.Owned.Count(x=>x);p.Crystals=10000;string pull="";for(int i=0;i<20;i++)pull=p.Summon(i);Check(pull.StartsWith("[SSR]"),"summon guaranteed twentieth SSR");Check(p.Shards.Sum()==20-(p.Owned.Count(x=>x)-owned),"T33 duplicates convert");
  p=new Profile();int mat=p.Materials;p.Train(0);Check(p.Materials==mat-30&&p.Training[0]==1,"training transaction");
  p.LastClaimTicks=DateTime.UtcNow.Ticks;p.Wins=1;Check(p.Offline(p.LastClaimTicks-TimeSpan.TicksPerHour)==0,"T39 backwards clock");
  Check(p.Offline(p.LastClaimTicks+24*TimeSpan.TicksPerHour)==144,"T39 offline cap");
  var signatures=new List<string>();
  for(int pass=0;pass<2;pass++){battle=new Profile().NewBattle(0,123);var trace=new List<string>();int n=0;while(battle.Result==0&&n++<4000){ev=battle.Step();trace.Add(ev.Text);}Check(battle.Result==1,"T04 deterministic complete fight "+pass);signatures.Add(string.Join("|",trace.ToArray()));}
  Check(signatures[0]==signatures[1],"T04 deterministic replay independent presentation");
  int[] wins=new int[4];for(int stage=0;stage<4;stage++)for(int n=0;n<30;n++){
   p=new Profile();p.Unlocked=3;battle=p.NewBattle(stage,n);int steps=0;while(battle.Result==0&&steps++<4000)battle.Step();
   Check(battle.Result!=0,"terminates stage "+stage+" seed "+n);if(battle.Result==1)wins[stage]++;
  }
  Console.WriteLine("Baseline win counts /30: "+string.Join(",",wins.Select(x=>x.ToString()).ToArray()));
  CharacterChecks.Run(Check);
  ChapterChecks.Run(Check);
  CatalogChecks.Run(Check);
  ScopeChecks.Run(Check);
  PresentationChecks.Run(Check);MenuChecks.Run(Check);
  Console.WriteLine("ALL PASS "+count);
 }
}
