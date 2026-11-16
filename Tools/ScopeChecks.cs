using System;
using System.Linq;
using DragonIdle;
static class ScopeChecks {
 public static Profile Previous(int count){
  var p=new Profile{schemaVersion=2};p.CardIds=Data.PreviousCardIds.Take(count).ToArray();
  p.Training=new int[count];p.Shards=new int[count];p.Resonance=new int[count];p.Policy=new int[count];
  p.Owned=Enumerable.Range(0,count).Select(i=>i<7||i==8||i==9||i==10).ToArray();
  p.Skill=Enumerable.Repeat("",count).ToArray();p.Skill2=Enumerable.Repeat("",count).ToArray();
  p.Team=new[]{0,1,2,3,4,5,6};return p;
 }
 static bool Near(double a,double b){return Math.Abs(a-b)<.000001;}
 public static void Run(Action<bool,string> check){
  check(Data.Cards.Length==15&&Data.Cards.All(c=>c.Stage=="赛亚人篇")&&Data.RetiredCardIds.All(id=>Data.Index(id)==-1),"R01 removed cards have no live definition");
  foreach(var card in Data.Cards){
   check(card.Keywords.Length>=2&&card.Keywords.Length<=3&&card.Keywords.Distinct().Count()==card.Keywords.Length&&card.Keywords.All(k=>Data.KeywordDescription(k).Length>0),"R02 grounded keyword definition "+card.Id);
   check(card.Captain.Tags.Length>=1&&card.Captain.Tags.Distinct().Count()==card.Captain.Tags.Length&&card.Captain.Tags.All(t=>card.Tags.Contains(t)),"R03 captain tags belong to owner "+card.Id);
   foreach(var target in Data.Cards)check(card.Captain.Applies(target)==card.Captain.Tags.Intersect(target.Tags).Any(),"R04 OR coverage "+card.Id+"/"+target.Id);
   var p=new Profile();p.Owned=Enumerable.Repeat(true,15).ToArray();int leader=Data.Index(card.Id);p.SetMember(0,leader);
   p.SetSkill(leader,0,"focus");p.SkillPages[3]=3;p.UnlockSkill(3);p.SetSkill(leader,1,"guard");
   p.Items.Add(new Item{id="defense",kind=1,owner=leader,slot=2});
   var team=p.CreateTeam();
   for(int s=0;s<7;s++){
    var personal=p.InspectCharacter(p.Team[s]);var actual=team[s];bool eligible=card.Captain.Tags.Intersect(Data.Cards[p.Team[s]].Tags).Any();
    double h=eligible?card.Captain.Health:0,a=eligible?card.Captain.Attack:0,d=eligible?card.Captain.Defense:0;
    check(Near(actual.MaxHP,personal.MaxHP*(1+h))&&Near(actual.HP,actual.MaxHP)&&Near(actual.AttackValue,personal.AttackValue*(1+a))&&Near(actual.DefenseValue,personal.DefenseValue*(1+d))&&Near(actual.Shield,personal.Shield*(1+h))&&Near(actual.SpeedValue,personal.SpeedValue),"R05 captain stat/shield budget applies once "+card.Id+"/"+s);
   }
   var copied=card.Captain.Tags;copied[0]="fake";var words=card.Keywords;words[0]="fake";
   check(!card.Captain.Tags.Contains("fake")&&!card.Keywords.Contains("fake"),"R06 defensive copies "+card.Id);
  }
  var goku=Data.Card("goku-test");check(goku.Captain.Matches(goku).Length==2,"R07 dual-match captain case exercised");
  check(Data.Card("krillin-test").Captain.Tags.SequenceEqual(new[]{"地球人"})&&!Data.Card("krillin-test").Captain.Applies(Data.Card("gohan-test")),"R21 Krillin cannot lead mixed Saiyans by foreign tag");
  check(Data.Card("nappa-invasion-v030").Captain.Applies(Data.Card("saibaman-saiyan-v030")),"R22 shared army tag legitimately covers another race");
  foreach(var invalidTags in new[]{new[]{"混血赛亚人"},new[]{"地球人","混血赛亚人"},new[]{"地球人","地球人"},new string[0]}){
   bool denied=false;try{new LeaderSkill(invalidTags,.04,.05,.04).ValidateOwner(Data.Card("krillin-test").Tags);}catch(ArgumentException){denied=true;}
   check(denied,"R23 reject foreign duplicate or empty captain tags "+string.Join(",",invalidTags));
  }
  var filter=new CatalogFilter{OwnedOnly=false,Role="穿防"};var fresh=new Profile();
  check(filter.Results(fresh).Length==4&&filter.Results(fresh).All(i=>Data.Cards[i].Keywords.Contains("穿防")),"R08 keyword filter is reusable and exact");
  filter.Role="";filter.Search="破绽穿透";check(filter.Results(fresh).Single()==Data.Index("piccolo-raditz-v030"),"R09 exclusive keyword search");
  var old=Previous(25);old.Pulls=19;old.SaiyanPulls=39;old.Materials=928;old.Crystals=432;old.Training[13]=88;old.Shards[13]=11;old.Resonance[13]=2;old.Policy[13]=2;old.Owned[13]=true;old.Skill[13]="focus";
  old.Team[0]=13;old.Leader=0;old.Items[0].owner=13;old.Items[0].gem=true;old.Items[0].locked=true;
  old.Presets.Add(new TeamPreset{name="旧阵容",leader=0,cards=old.Team.Select(i=>old.CardIds[i]).ToArray()});
  old.Training[8]=12;old.Items.Add(new Item{id="yamcha",kind=1,owner=8,slot=2});
  old.Normalize();var archived=old.Retired.Single(r=>r.cardId=="goku-namek-ssj-v020");
  check(old.schemaVersion==3&&old.Retired.Count==10&&archived.owned&&archived.training==88&&archived.shards==11&&archived.resonance==2&&archived.policy==2&&archived.skill=="focus","R10 v040 removed growth archived by CardId");
  check(old.Items[0].owner==-1&&old.Items[0].gem&&old.Items[0].locked&&old.Items[1].owner==Data.Index("yamcha-saiyan-v020")&&old.Training[Data.Index("yamcha-saiyan-v020")]==12,"R11 removed gear unequipped surviving gear remapped");
  check(old.Team.Distinct().Count()==7&&old.Team.All(i=>old.Owned[i])&&old.Presets[0].cards.All(id=>Data.Index(id)>=0)&&old.Leader==0&&old.Presets[0].leader==0,"R12 active team and preset repaired retaining captain slot");
  check(old.Pulls==19&&old.SaiyanPulls==39&&old.Materials==928&&old.Crystals==432,"R13 economy and both pity counters preserved");
  string teamIds=string.Join(",",old.Team.Select(i=>Data.Cards[i].Id).ToArray());old.Normalize();
  check(old.Retired.Count==10&&old.Retired.Single(r=>r.cardId=="goku-namek-ssj-v020").shards==11&&teamIds==string.Join(",",old.Team.Select(i=>Data.Cards[i].Id).ToArray()),"R14 reduction idempotent");
  var few=Previous(25);few.Owned=Enumerable.Repeat(false,25).ToArray();few.Team=new[]{6,7,10,13,14,15,16};foreach(int n in few.Team)few.Owned[n]=true;few.Items[0].owner=6;few.Normalize();
  check(few.Owned.Count(x=>x)==7&&few.Team.Distinct().Count()==7&&few.Team.All(i=>few.Owned[i]),"R15 restore playable team from entirely retired lineup");
  var invalid=Previous(25);invalid.Shards[13]=-1;bool rejected=false;try{invalid.Normalize();}catch(InvalidOperationException){rejected=true;}check(rejected,"R16 corrupt retired growth rejected");
  invalid=Previous(25);invalid.CardIds[13]="unknown";rejected=false;try{invalid.Normalize();}catch(InvalidOperationException){rejected=true;}check(rejected,"R17 only known retired IDs allowed");
  invalid=Previous(25);invalid.Team[1]=invalid.Team[0];rejected=false;try{invalid.Normalize();}catch(InvalidOperationException){rejected=true;}check(rejected,"R18 corrupt team not silently repaired");
  var summoner=new Profile();summoner.Crystals=100000;for(int seed=0;seed<1000;seed++){summoner.Summon(seed);summoner.SummonSaiyan(seed);}
  check(summoner.Retired.Count==0&&summoner.CardIds.All(id=>Data.Card(id).Stage=="赛亚人篇")&&summoner.Pulls==1000&&summoner.SaiyanPulls==1000,"R19 both live pools restricted to saga");
  check(Data.StageTeams.All(t=>t.Length==7&&t.Distinct().Count()==7&&t.All(i=>i>=0&&i<15)),"R20 trial teams only active cards");
 }
}
