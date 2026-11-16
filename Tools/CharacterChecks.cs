using System;
using System.Linq;
using DragonIdle;
static class CharacterChecks {
 static bool Near(double a,double b){return Math.Abs(a-b)<.000001;}
 public static void Run(Action<bool,string> check){
  check(Data.Cards.Length==20&&Data.Cards.Select(c=>c.Id).Distinct().Count()==20,"C01 unique 20-card catalog");
  check(Data.Cards.All(c=>!string.IsNullOrEmpty(c.PersonId)&&!string.IsNullOrEmpty(c.Stage)&&!string.IsNullOrEmpty(c.Form)&&!string.IsNullOrEmpty(c.Passive)&&c.Leader>0),"C02 complete identity and base abilities");
  check(Data.Cards.Take(8).Select(c=>c.Id).SequenceEqual(Data.LegacyCardIds),"C03 stable legacy IDs");
  var p=new Profile();check(p.Owned.Count(x=>x)==10,"C04 immediate reserve choices");
  p.Acquire(12);check(p.SetMember(1,12)&&Data.Cards[p.Team[0]].PersonId==Data.Cards[p.Team[1]].PersonId&&Data.Cards[p.Team[0]].Stage==Data.Cards[p.Team[1]].Stage&&p.CreateTeam().Count==7,"C05 same person same stage distinct cards coexist");
  p.SetMember(2,12);check(p.Team[2]==12&&p.Team.Distinct().Count()==7,"C06 existing member swaps without duplicates");
  check(!p.SetMember(3,13)&&!p.SetMember(7,0)&&!p.SetMember(0,-1),"C07 reject locked card and invalid slots");
  int total=p.Shards.Sum();p.Acquire(12);check(p.Shards[12]==1&&p.Shards[0]==0&&p.Shards.Sum()==total+1,"C08 duplicate resources stay on CardId");
  var identity=Data.Cards[0].Tags;identity[0]="人造人";check(Data.Cards[0].HasTag("赛亚人")&&!Data.Cards[0].HasTag("人造人"),"C09 defensive copy protects identity");
  p=new Profile();p.Leader=1;var team=p.CreateTeam();check(Near(team[0].LeaderAttack,.12)&&Near(team[1].LeaderAttack,.12)&&Near(team[4].LeaderAttack,0)&&Near(team[2].LeaderAttack,0),"C10 leader exact tag eligibility");
  team[1].HP=0;check(Near(team[0].LeaderAttack,.12),"C11 leader contribution survives captain death");
  p.Leader=0;p.SavePreset(0);p.SetMember(1,8);p.Leader=1;p.SavePreset(1);p.LoadPreset(0);check(p.Team[1]==1&&p.Leader==0,"C12 preset restores formation and captain");
  p.LoadPreset(1);check(p.Team[1]==8&&p.Leader==1&&p.Presets[0].cards[1]=="vegeta-test","C13 preset stores independent stable IDs");
  check(!p.SavePreset(3)&&!p.LoadPreset(2),"C14 invalid preset no mutation");
  p=new Profile();p.schemaVersion=1;p.CardIds=null;p.Training=new[]{23,0,0,0,0,0,0,0};p.Shards=new[]{0,0,0,0,0,0,0,9};p.Resonance=new int[8];p.Policy=new int[8];p.Owned=new[]{true,true,true,true,true,true,true,true};p.Skill=Enumerable.Repeat("",8).ToArray();p.Skill[0]="focus";p.Skill2=null;p.RosterGift=false;p.SkillPages=null;p.SkillUnlocked=null;p.Presets=null;p.Items[0].owner=7;p.Team[6]=7;p.Materials=481;
  p.Normalize();check(p.schemaVersion==2&&p.Training[0]==23&&p.Shards[7]==9&&p.Owned[7]&&p.Items[0].owner==7&&p.Team[6]==7&&p.Materials==481&&p.Skill[0]=="focus"&&p.Skill2.Length==20,"C15 migrate v1 without losing growth equipment or party");
  check(p.Owned[8]&&p.Owned[9]&&p.Owned[10]&&!p.Owned[11],"C16 one-time reserve gift only");
  string before=string.Join(",",p.Training.Select(x=>x.ToString()).ToArray());int shards=p.Shards.Sum();p.Normalize();check(before==string.Join(",",p.Training.Select(x=>x.ToString()).ToArray())&&shards==p.Shards.Sum(),"C17 migration idempotent");
  // Simulate a previous release storing its catalog in a different order.
  p=new Profile();p.CardIds=p.CardIds.Reverse().ToArray();p.Training=Enumerable.Range(0,20).ToArray();p.Shards=Enumerable.Range(0,20).ToArray();p.Resonance=p.Resonance.Reverse().ToArray();p.Policy=p.Policy.Reverse().ToArray();p.Owned=p.Owned.Reverse().ToArray();p.Skill=p.Skill.Reverse().ToArray();p.Skill2=p.Skill2.Reverse().ToArray();p.Team=p.Team.Select(i=>19-i).ToArray();p.Items[0].owner=19;p.Normalize();
  check(p.Training[0]==19&&p.Shards[19]==0&&p.Items[0].owner==0&&p.Team[0]==0&&p.Owned[0],"C18 reordered save remaps every owner by ID");
  p=new Profile();p.CardIds[0]="unknown-future-card";bool rejected=false;try{p.Normalize();}catch{rejected=true;}check(rejected,"C19 unknown future card rejected without silent loss");
  p=new Profile();p.Skill[0]="unknown-skill";rejected=false;try{p.CreateTeam();}catch{rejected=true;}check(rejected,"C20 battle snapshot rejects invalid equipped skills");
  p=new Profile();check(!p.SetSkill(0,0,"guard"),"C21 locked skill rejected");p.SkillPages[3]=3;check(p.UnlockSkill(3)&&p.SkillPages[3]==0&&p.SetSkill(0,0,"guard"),"C22 skill pages unlock and equip");
  check(!p.UnlockSkill(3)&&Near(p.CreateTeam()[0].Shield,p.CreateTeam()[0].MaxHP*.04),"C23 unlock cannot consume twice and shield works");
  p=new Profile();p.Items.Clear();for(int n=0;n<3;n++)p.Items.Add(new Item{id="def"+n,kind=1,owner=0,slot=n});var u=p.CreateTeam()[0];check(Near(u.DefenseValue/u.Defense,1.054),"C24 same-name defense gear adds before multiplier");
  p=new Profile();p.Unlocked=3;for(int stage=0;stage<4;stage++){
   int pages=p.SkillPages.Sum(),gems=p.Gems;
   for(int i=0;i<200;i++)p.Reward(stage,i);
   var items=p.Items.Skip(p.Items.Count-200).ToArray();check(items.All(x=>x.kind==2||stage>=2||x.kind==stage),"C25 target loot pool stage "+stage);
   check(p.SkillPages.Sum()>pages&&p.Gems>gems,"C26 skill and jewel drops stage "+stage);
  }
  p=new Profile();p.Pulls=19;p.Crystals=50;check(p.Summon(4).StartsWith("[SSR]")&&p.Crystals==0&&p.Pulls==20,"C27 pity counter transaction");int pulls=p.Pulls;p.Summon(5);check(p.Pulls==pulls,"C28 insufficient currency preserves pity");
  p=new Profile();for(int i=0;i<20;i++)p.Acquire(i);p.Unlocked=3;
  for(int i=0;i<20;i++){
   p.SetMember(0,i);p.Leader=0;var b=p.NewBattle(i%4,90+i);int steps=0;while(b.Result==0&&steps++<4000)b.Step();
   check(b.Result!=0,"C29 roster battle terminates card "+i);
  }
 }
}
