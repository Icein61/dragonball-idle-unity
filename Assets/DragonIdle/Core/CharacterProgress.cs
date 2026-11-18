using System;
using System.Collections.Generic;
using System.Linq;
namespace DragonIdle {
 [Serializable] public class TeamPreset {public string name;public string[] cards;public int leader;}
 [Serializable] public class RetiredProgress {
  public string cardId,skill,skill2;
  public bool owned;
  public int training,shards,resonance,policy;
 }
 public partial class Profile {
  public List<RetiredProgress> Retired=new List<RetiredProgress>();
  public bool RosterGift=true;
  public int[] SkillPages=new int[4];
  public bool[] SkillUnlocked={true,true,true,false};
  public List<TeamPreset> Presets=new List<TeamPreset>();
  static T[] Remap<T>(T[] values,string[] oldIds,T fallback){
   if(values==null||values.Length!=oldIds.Length)throw new InvalidOperationException("角色存档数组不完整");
   return Data.Cards.Select(c=>{int old=Array.IndexOf(oldIds,c.Id);return old<0?fallback:values[old];}).ToArray();
  }
  public bool NeedsRosterMigration { get { return schemaVersion < 3 || (CardIds != null && CardIds.Any(Data.RetiredCardIds.Contains)); } }
  public void Normalize(){
   if(schemaVersion!=1&&schemaVersion!=2&&schemaVersion!=3)throw new InvalidOperationException("不支持的存档版本");
   bool reducing=NeedsRosterMigration;
   bool legacy=schemaVersion==1;string[] ids=legacy?Data.LegacyCardIds:CardIds;
   if(ids==null||ids.Length==0||ids.Distinct().Count()!=ids.Length||ids.Any(id=>Data.Index(id)<0&&!(reducing&&Data.RetiredCardIds.Contains(id))))throw new InvalidOperationException("存档含未知或重复角色ID，已保留原文件");
   if(Skill2==null&&legacy)Skill2=Enumerable.Repeat("",ids.Length).ToArray();
   if(Team==null||Team.Length!=7||Team.Any(i=>i<0||i>=ids.Length))throw new InvalidOperationException("编队存档损坏");
   if(Items==null||Items.Any(x=>x==null||x.owner < -1||x.owner>=ids.Length))throw new InvalidOperationException("装备存档损坏");
   if(Training==null||Shards==null||Resonance==null||Policy==null||Owned==null||Skill==null||Skill2==null||new[]{Training.Length,Shards.Length,Resonance.Length,Policy.Length,Owned.Length,Skill.Length,Skill2.Length}.Any(n=>n!=ids.Length))throw new InvalidOperationException("角色存档数组不完整");
   if(Team.Distinct().Count()!=7||Team.Any(i=>!Owned[i])||Leader<0||Leader>6)throw new InvalidOperationException("旧编队存档损坏");
   if(Training.Any(x=>x<0)||Shards.Any(x=>x<0)||Resonance.Any(x=>x<0)||Policy.Any(x=>x<0||x>2))throw new InvalidOperationException("旧角色成长损坏");
   if(Items.Any(x=>x.owner>=0&&!Owned[x.owner])||Items.Where(x=>x.owner>=0).GroupBy(x=>x.owner*3+x.slot).Any(g=>g.Count()>1))throw new InvalidOperationException("旧装备归属损坏");
   if(legacy){SkillPages=new int[4];SkillUnlocked=new[]{true,true,true,false};Presets=new List<TeamPreset>();}
   if(Presets==null||Presets.Count>3||Presets.Any(p=>p==null||p.cards==null||p.cards.Length!=7||p.cards.Distinct().Count()!=7||p.leader<0||p.leader>6||p.cards.Any(id=>Array.IndexOf(ids,id)<0||!Owned[Array.IndexOf(ids,id)])))throw new InvalidOperationException("旧阵容预设损坏");
   if(Retired==null&&reducing)Retired=new List<RetiredProgress>();
   for(int i=0;i<ids.Length;i++)if(Data.RetiredCardIds.Contains(ids[i])){
    if(Retired.Any(r=>r==null||r.cardId==ids[i]))throw new InvalidOperationException("重复归档数据");
    Retired.Add(new RetiredProgress{cardId=ids[i],owned=Owned[i],training=Training[i],shards=Shards[i],resonance=Resonance[i],policy=Policy[i],skill=Skill[i]??"",skill2=Skill2[i]??""});
   }
   Training=Remap(Training,ids,0);Shards=Remap(Shards,ids,0);Resonance=Remap(Resonance,ids,0);Policy=Remap(Policy,ids,0);
   Owned=Remap(Owned,ids,false);Skill=Remap(Skill,ids,"");Skill2=Remap(Skill2,ids,"");
   Team=Team.Select(i=>Data.Index(ids[i])).ToArray();foreach(var item in Items)if(item.owner>=0)item.owner=Data.Index(ids[item.owner]);
   if(legacy||!RosterGift){foreach(string id in new[]{"yamcha-saiyan-v020","chiaotzu-saiyan-v020"})Owned[Data.Index(id)]=true;RosterGift=true;}
   if(reducing){
    foreach(string id in Data.StarterCardIds){if(Owned.Count(x=>x)>=7)break;Owned[Data.Index(id)]=true;}
    Team=RepairRetiredTeam(Team);
    foreach(var preset in Presets)preset.cards=RepairRetiredTeam(preset.cards.Select(Data.Index).ToArray()).Select(i=>Data.Cards[i].Id).ToArray();
   }
   NormalizeStory(legacy||(reducing&&ids.Length==20));CardIds=Data.Cards.Select(c=>c.Id).ToArray();schemaVersion=3;Validate();
  }
  int[] RepairRetiredTeam(int[] team){
   var result=(int[])team.Clone();
   for(int s=0;s<7;s++)if(result[s]<0)result[s]=Enumerable.Range(0,Owned.Length).First(i=>Owned[i]&&!result.Contains(i));
   return result;
  }
  bool Owns(int card){return card>=0&&card<Owned.Length&&Owned[card];}
  bool ValidTeam(int[] team,int leader){return team!=null&&team.Length==7&&team.Distinct().Count()==7&&team.All(Owns)&&leader>=0&&leader<7;}
  public void Validate(){
   ValidateStory();int n=Data.Cards.Length;
   if(schemaVersion!=3)throw new InvalidOperationException("角色存档需要升级");
   if(Retired==null||Retired.Any(r=>r==null||!Data.RetiredCardIds.Contains(r.cardId)||r.training<0||r.shards<0||r.resonance<0||r.policy<0||r.policy>2)||Retired.Select(r=>r.cardId).Distinct().Count()!=Retired.Count)throw new InvalidOperationException("角色归档损坏");
   if(CardIds==null||!CardIds.SequenceEqual(Data.Cards.Select(c=>c.Id))||Training==null||Training.Length!=n||Shards==null||Shards.Length!=n||Resonance==null||Resonance.Length!=n||Policy==null||Policy.Length!=n||Owned==null||Owned.Length!=n||Skill==null||Skill.Length!=n||Skill2==null||Skill2.Length!=n)throw new InvalidOperationException("角色存档需要迁移");
   if(!ValidTeam(Team,Leader))throw new InvalidOperationException("队伍必须包含七张已拥有的不同卡");
   if(Materials<0||Crystals<0||Gems<0||Unlocked<0||Unlocked>=Data.Stages.Length||SelectedStage<0||SelectedStage>Unlocked||Training.Any(x=>x<0)||Shards.Any(x=>x<0)||Resonance.Any(x=>x<0)||Policy.Any(x=>x<0||x>2))throw new InvalidOperationException("无效成长或进度数据");
   if(SkillUnlocked==null||SkillUnlocked.Length!=4||SkillPages==null||SkillPages.Length!=4||SkillPages.Any(x=>x<0))throw new InvalidOperationException("技能收藏数据损坏");
   if(Retired.Any(r=>!CanUseSkill(r.skill)||!CanUseSkill(r.skill2)||(r.skill!=""&&r.skill==r.skill2)))throw new InvalidOperationException("归档技能配置损坏");
   for(int i=0;i<n;i++){
    if(Skill[i]==null)Skill[i]="";if(Skill2[i]==null)Skill2[i]="";
    if(!CanUseSkill(Skill[i])||!CanUseSkill(Skill2[i])||(Skill[i]!=""&&Skill[i]==Skill2[i]))throw new InvalidOperationException("非法技能配置");
   }
   if(Items==null||Items.Any(x=>x==null||string.IsNullOrEmpty(x.id)||x.kind<0||x.kind>2||x.owner < -1||x.owner>=n||x.slot<0||x.slot>2||(x.owner>=0&&!Owns(x.owner)))||Items.Select(x=>x.id).Distinct().Count()!=Items.Count||Items.Where(x=>x.owner>=0).GroupBy(x=>x.owner*3+x.slot).Any(g=>g.Count()>1))throw new InvalidOperationException("装备数据冲突");
   if(Presets==null||Presets.Count>3||Presets.Any(p=>p==null||p.cards==null||!ValidTeam(p.cards.Select(Data.Index).ToArray(),p.leader)))throw new InvalidOperationException("阵容预设损坏");
  }
  public List<Unit> CreateEnemies(int stage){
   if(stage<0||stage>=Data.Stages.Length)throw new ArgumentOutOfRangeException("stage");
   return Data.StageTeams[stage].Select((i,s)=>{var u=Data.Make(Data.Cards[i].Id,1,s,0,0,"",Data.StageScale[stage]);u.Name="试炼·"+u.Name;return u;}).ToList();
  }
  public bool SetMember(int slot,int card){
   if(slot<0||slot>6||!Owns(card))return false;
   int old=Array.IndexOf(Team,card);if(old>=0)Team[old]=Team[slot];Team[slot]=card;return true;
  }
  public bool SavePreset(int index){
   if(index<0||index>2)return false;Validate();
   var p=new TeamPreset{name="阵容 "+(index+1),cards=Team.Select(i=>Data.Cards[i].Id).ToArray(),leader=Leader};
   while(Presets.Count<=index)Presets.Add(new TeamPreset{name="阵容 "+(Presets.Count+1),cards=(string[])p.cards.Clone(),leader=Leader});
   Presets[index]=p;return true;
  }
  public bool LoadPreset(int index){
   if(index<0||index>=Presets.Count)return false;var p=Presets[index];var team=p.cards.Select(Data.Index).ToArray();
   if(!ValidTeam(team,p.leader))return false;Team=team;Leader=p.leader;return true;
  }
  public bool CanUseSkill(string id){int i=Array.IndexOf(Data.SkillIds,id);return i>=0&&(i==0||SkillUnlocked[i]);}
  public bool UnlockSkill(int index){if(index<1||index>=Data.SkillIds.Length||SkillUnlocked[index]||SkillPages[index]<3)return false;SkillPages[index]-=3;SkillUnlocked[index]=true;return true;}
  public string Acquire(int index){
   if(index<0||index>=Data.Cards.Length)throw new ArgumentOutOfRangeException("index");
   if(Owned[index]){Shards[index]++;return Data.Cards[index].DisplayName+" · 本卡专属资源 +1";}
   Owned[index]=true;return "新角色："+Data.Cards[index].DisplayName;
  }
 }
}
