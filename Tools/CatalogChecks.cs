using System;
using System.Linq;
using DragonIdle;
static class CatalogChecks {
 static bool Near(double a,double b){return Math.Abs(a-b)<.000001;}
 public static void Run(Action<bool,string> check){
  var p=new Profile();var f=new CatalogFilter();var results=f.Results(p);
  check(results.Length==8&&results.All(i=>p.Owned[i]),"U01 owned collection");
  f.OwnedOnly=false;check(f.Results(p).Length==9,"U02 full collection");
  f.Search="  界王拳  ";results=f.Results(p);check(results.Length==1&&Data.Cards[results[0]].Id=="goku-saiyan-kaioken-v020","U03 trim search and match form");
  f.Search="";f.Tag="赛亚人";f.Rarity="S";results=f.Results(p);check(results.Length>0&&results.All(i=>Data.Cards[i].HasTag("赛亚人")&&Data.Cards[i].Rarity=="S"),"U04 combined exact filters");
  check(results.All(i=>Data.Cards[i].Tag!="混血赛亚人"),"U05 identity filters do not replace or broaden race");
  check(CatalogFilter.Reconcile(4,results)==results[0],"U06 filtered-out selection reconciles");
  check(CatalogFilter.Reconcile(results.Last(),results)==results.Last(),"U07 retain matching selection");
  f.Search="不存在的角色";results=f.Results(p);check(results.Length==0&&CatalogFilter.Reconcile(0,results)==-1,"U08 empty search no stale detail");
  f=new CatalogFilter{OwnedOnly=false,Sort=3};p.Training[7]=29;p.Training[0]=3;check(f.Results(p)[0]==7,"U09 cultivation sort");
  f.Sort=0;check(f.Results(p).Take(7).SequenceEqual(p.Team),"U10 equipped formation first");
  p.SetMember(1,7);check(f.Results(p)[1]==7,"U11 formation sort follows swap");
  f.Sort=1;results=f.Results(p);check(Data.Cards[results[0]].Rarity=="S"&&Data.Cards[results.Last()].Rarity=="C","U12 rarity sort");
  f.Sort=2;check(f.Results(p).SequenceEqual(f.Results(p)),"U13 deterministic ordering");
  p=new Profile();p.Training[0]=17;p.SetSkill(0,0,"focus");p.SkillPages[3]=3;p.UnlockSkill(3);p.SetSkill(0,1,"guard");
  p.Items.Add(new Item{id="preview-defense",kind=1,owner=0,slot=1,gem=true});
  var team=p.CreateTeam();
  for(int slot=0;slot<7;slot++){
   int i=p.Team[slot];var preview=p.InspectCharacter(i,true);var actual=team[slot];
   check(Near(preview.MaxHP,actual.MaxHP)&&Near(preview.AttackValue,actual.AttackValue)&&Near(preview.DefenseValue,actual.DefenseValue)&&Near(preview.SpeedValue,actual.SpeedValue)&&Near(preview.Crit,actual.Crit)&&Near(preview.Shield,actual.Shield),"U14 preview agrees with combat "+slot);
  }
  var one=p.InspectCharacter(0);check(one.LeaderAttack==0&&Near(one.AttackValue*(1+team[0].LeaderAttack),team[0].AttackValue),"U15 separate personal and captain columns");
  var reserve=p.InspectCharacter(7,true);check(reserve.LeaderAttack==0,"U16 reserve no applied leader");
  one.Add("attack",100,1,"ui-probe");check(p.InspectCharacter(0).Sum("attack")<1,"U17 snapshot cannot mutate profile");
  int locked=Data.Index("goku-saiyan-kaioken-v020");p.Training[locked]=100;var unowned=p.InspectCharacter(locked);check(Near(unowned.MaxHP,Data.Cards[locked].HP),"U18 unowned shows base preview");
 }
}
