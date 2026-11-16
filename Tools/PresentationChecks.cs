using System;
using System.Linq;
using DragonIdle;
static class PresentationChecks {
 static Unit Actor(string id,int side){return new Unit{CardId=id,Side=side,Slot=0,HP=1000,MaxHP=1000,Attack=40,Defense=20,Speed=100,Name=id,Role="fighter",SpecialName="special",UltimateName="ultimate",Crit=0,Dodge=0};}
 public static void Run(Action<bool,string> check){
  var kinds=new System.Collections.Generic.HashSet<BattleVisualKind>();
  foreach(var card in Data.Cards)foreach(Move move in new[]{Move.Basic,Move.Special,Move.Ultimate}){
   var a=Actor(card.Id,0);var b=Actor("target",1);var before=ActionPresentation.Capture(new[]{a,b});b.HP=900;
   var e=new BattleEvent{Actor=a,Target=b,Move=move};e.Hits.Add(new BattleHit{Target=b,Damage=100,HealthDamage=100});
   var p=new ActionPresentation(e,before,new[]{a,b});kinds.Add(p.Kind);
   check(p.Vitals(b).HP==1000&&!p.HitVisible(0),"P01 damage withheld until impact "+card.Id+move);
   p.Tick(.2,true);check(p.Elapsed==0,"P02 pause freezes presentation "+card.Id+move);
   p.Tick(p.ImpactAt(0)+.2,false);check(p.Vitals(b).HP==900&&p.Pose(a)>=0&&p.Pose(a)<12,"P03 impact and valid pose "+card.Id+move);
   p.Tick(10,false);check(p.Complete&&p.Vitals(b).HP==900&&b.HP==900&&p.Pose(a)==0,"P04 completion never reapplies damage "+card.Id+move);
  }
  check(kinds.Count==19,"P05 every effect family has a real character move");
  var krillin=Actor("krillin-test",0);check(ActionPresentation.VisualKind(krillin,Move.Ultimate)==BattleVisualKind.Disc&&ActionPresentation.VisualKind(krillin,Move.Special)==BattleVisualKind.SolarFlare,"P06 Krillin effects distinguish disc and flare");
  var blade=Actor("yajirobe-saiyan-v030",0);check(new[]{Move.Basic,Move.Special,Move.Ultimate}.All(m=>ActionPresentation.VisualKind(blade,m)==BattleVisualKind.Slash),"P07 sword never becomes an energy blast");
  krillin.SpecialReplaced=true;check(ActionPresentation.VisualKind(krillin,Move.Special)==BattleVisualKind.GreenBolt&&ActionPresentation.VisualKind(krillin,Move.Ultimate)==BattleVisualKind.Disc,"P08 equipped special replaces only special visuals");
  var actor=Actor("raditz-arrival-v030",0);var t1=Actor("target1",1);var t2=Actor("target2",1);t2.Slot=1;
  var snapshot=ActionPresentation.Capture(new[]{actor,t1,t2});t1.HP=0;t2.Shield=0;
  var multi=new BattleEvent{Actor=actor,Target=t1,Move=Move.Special};multi.Hits.Add(new BattleHit{Target=t1,Damage=1000,HealthDamage=1000});multi.Hits.Add(new BattleHit{Target=t2,Dodged=true});
  var seq=new ActionPresentation(multi,snapshot,new[]{actor,t1,t2});seq.Tick(seq.ImpactAt(0)+.01,false);
  check(seq.HitVisible(0)&&!seq.HitVisible(1)&&seq.Pose(t1)==8,"P09 multiple targets stagger and lethal impact recoils first");
  seq.Tick(.1,false);check(seq.Pose(t2)==7&&seq.Vitals(t2).HP==1000,"P10 dodge has pose and no lost HP");
  seq.Tick(4,false);check(seq.Pose(t1)==10&&seq.Vitals(t1).HP==0,"P11 lethal action ends with defeated pose");
  var shielded=Actor("shield",1);shielded.Shield=30;snapshot=ActionPresentation.Capture(new[]{actor,shielded});shielded.Shield=10;
  var block=new BattleEvent{Actor=actor,Target=shielded,Move=Move.Basic};block.Hits.Add(new BattleHit{Target=shielded,Damage=20,ShieldDamage=20});seq=new ActionPresentation(block,snapshot,new[]{actor,shielded});seq.Tick(seq.ImpactAt(0)+.2,false);
  check(seq.Pose(shielded)==6&&seq.Vitals(shielded).HP==1000&&seq.Vitals(shielded).Shield==10,"P12 full shield absorption blocks without health loss");
  // Identical seeds must remain identical despite presentation speed, pause and skipped frames.
  var profile=new Profile();var baseline=profile.NewBattle(0,120);var animated=profile.NewBattle(0,120);int steps=0;
  while(baseline.Result==0&&steps++<4000){var expected=baseline.Step();var prior=ActionPresentation.Capture(animated.Units);var actual=animated.Step();seq=new ActionPresentation(actual,prior,animated.Units);seq.Tick(.1,true);seq.Tick(steps%2==0?.8:8,false);seq.Tick(8,false);
   if(expected.Text!=actual.Text||expected.Time!=actual.Time)throw new Exception("Presentation changed simulation");
  }
  check(baseline.Result==animated.Result&&baseline.Units.Zip(animated.Units,(a,b)=>a.HP==b.HP&&a.Rage==b.Rage&&a.Opportunities==b.Opportunities).All(x=>x),"P13 frame rate pause and animation speed leave battle outcome unchanged");
 }
}
