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
 public static void Run(Action<bool,string> check){
  var rules=Rules();rules.Validate();
  check(Enumerable.Range(1,9).All(p=>RoundBoard.Row(p)==(p-1)%3&&RoundBoard.Column(p,0)+RoundBoard.Column(p,1)==2)&&RoundBoard.Column(1,0)==2,"RB01 nine-grid mirrored indexing");
  var profile=new Profile();int[] initial=profile.RoundFormation();
  check(initial.Length==9&&initial.Count(i=>i>=0)==5&&initial[4]==4&&profile.Team.Length==6,"RB02 legacy picks five, retains legacy data");
  check(!profile.SetRoundMember(7,6)&&!profile.SetRoundMember(7,8)&&profile.RoundFormation().SequenceEqual(initial),"RB03 sixth member and unowned data rejected atomically");
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
  check(round.Step().Critical&&round.Step().Move==RoundMove.Ultimate&&round.Step().Move==RoundMove.Pursuit,"RB12 rage interrupts pursuit after crit");
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
  int[] seven=new CatalogFilter{OwnedOnly=false,RoundFormat=true}.Results(new Profile());
  check(seven.Take(5).SequenceEqual(initial.Where(i=>i>=0)),"RB17 gallery lineup order uses new formation");
 }
}
