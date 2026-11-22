using System;
using System.Collections.Generic;
using System.Linq;
namespace DragonIdle {
 // Card identity and the expanded catalog live in CharacterCatalog.cs.
 public static partial class Data {
  public const string Version="0.17.3";
  public const string GameTitle="龙珠爆裂激斗";
  public const int FrontSlots=3,TeamSlots=6;
  // Keep the original storage identity when the public game title changes.
  public static string DefaultSavePath(string persistentDirectory){return System.IO.Path.Combine(System.IO.Directory.GetParent(persistentDirectory).FullName,"Dragon Idle Demo","profile-v1.json");}
  public static Card Card(string id){return Cards.First(x=>x.Id==id);}
  public static string[] Stages={"荒野修炼","防线试炼","后排突破","极限演武"};
  public static double[] StageScale={.68,.84,1.0,1.17};
  public static int[][] StageTeams={new[]{0,1,2,3,4,5},new[]{2,5,4,3,6,7},new[]{0,2,6,1,3,8},new[]{1,2,5,6,8,0}};
  public static Unit Make(string id,int side,int slot,int training,int resonance,string skill,double scale) {
   var c=Card(id);double growth=1+training*.001+resonance*.004;
   var u=new Unit {CardId=id,Name=c.Name,Role=c.Role,Side=side,Slot=slot,MaxHP=c.HP*growth*scale,HP=c.HP*growth*scale,Attack=c.Attack*growth*scale,Defense=c.Defense*growth,Speed=c.Speed,SpecialName=c.Special,UltimateName=c.Ultimate,CooldownLength=c.Cooldown};
   u.SpecialRole=c.Role;
   if(Data.Reworked(id))Data.ReworkPassive(u);
   else if(c.Passive=="guard")u.Shield=u.MaxHP*.06;
   else if(c.Passive=="haste")u.Add("speed",.03,0,id+":passive");
   else if(c.Passive=="resolve")u.Rage=12;
   else if(c.Passive=="focus")u.Crit+=.02;
   else u.Add("attack",.02,0,id+":passive");
   if(skill=="pierce"){u.SpecialReplaced=true;u.SpecialRole="sniper";u.SpecialName="贯穿气弹";u.CooldownLength=4;}
   if(skill=="focus")u.Crit+=.035;
   return u;
  }
 }
 [Serializable] public class Item {
  public string id; public int kind;public int owner=-1,slot;public bool gem,locked;
  public string Name {get{return new[]{"修炼护腕","防护战斗服","稀有·聚气核心"}[kind];}}
  public double Bonus {get{return new[]{.012,.018,.055}[kind];}}
 }
 [Serializable] public partial class Profile {
  public int schemaVersion=4,Materials=120,Crystals=150,Gems=3,Unlocked=0,Wins=0,Pulls=0,Leader=0,SelectedStage=0;
  public string[] CardIds=Data.Cards.Select(c=>c.Id).ToArray();
  public int[] Training=new int[Data.Cards.Length],Shards=new int[Data.Cards.Length],Resonance=new int[Data.Cards.Length],Policy=new int[Data.Cards.Length];
  public bool[] Owned=Data.Cards.Select(c=>Data.StarterCardIds.Contains(c.Id)).ToArray();
  public int[] Team={0,1,2,3,4,5};
  public string[] Skill=Enumerable.Repeat("",Data.Cards.Length).ToArray();
  public string[] Skill2=Enumerable.Repeat("",Data.Cards.Length).ToArray();
  public int OfflineBank;
  public List<Item> Items=new List<Item>();
  public long LastClaimTicks;
  public bool FirstMission;
  public Profile() {Items.Add(new Item{id="starter-bracer",kind=0,owner=0,slot=0});}
  public List<Unit> CreateTeam(int side=0) {
   Validate();
   return Enumerable.Range(0,Team.Length).Where(slot=>Team[slot]>=0).Select(slot=>BuildCharacter(Team[slot],side,slot,true)).ToList();
  }
  public Battle NewBattle(int stage,int seed){
   if(stage<0||stage>Unlocked||stage>=Data.Stages.Length)throw new ArgumentException("Stage locked");
   return new Battle(CreateTeam().Concat(CreateEnemies(stage)),seed);
  }
  public bool Train(int card){if(!Owns(card))return false;int cost=30+Training[card]/10;if(Materials<cost)return false;Materials-=cost;Training[card]++;return true;}
  public bool SetSkill(int card,int slot,string id){
   if(!Owns(card)||slot<0||slot>1||!CanUseSkill(id))return false;
   string other=slot==0?Skill2[card]:Skill[card];if(id!=""&&id==other)return false;
   if(slot==0)Skill[card]=id;else Skill2[card]=id;return true;
  }
  public bool ResonanceUp(int card){if(!Owns(card))return false;int cost=3+Resonance[card];if(Shards[card]<cost)return false;Shards[card]-=cost;Resonance[card]++;return true;}
  public bool Equip(string itemId,int card,int slot) {
   if(card<0||card>=Owned.Length||!Owned[card]||slot<0||slot>2)return false;
   var item=Items.FirstOrDefault(x=>x.id==itemId);if(item==null)return false;
   foreach(var old in Items.Where(x=>x.owner==card&&x.slot==slot))old.owner=-1;
   item.owner=card;item.slot=slot;return true;
  }
  public string Reward(int stage,int seed) {
   if(stage<0||stage>Unlocked||stage>=Data.Stages.Length)throw new ArgumentException("Stage locked");
   var rng=new Random(seed);Wins++;Materials+=12+stage*3;Crystals+=8+stage*2;
   if(stage==Unlocked&&Unlocked<3)Unlocked++;
   bool rare=rng.NextDouble()<(stage==3?.12:.08);var item=new Item{id=Guid.NewGuid().ToString("N"),kind=rare?2:stage==0?0:stage==1?1:rng.Next(2)};
   Items.Add(item);
   string extra="";if(rng.NextDouble()<.2){Gems++;extra+=" · 攻击珠 +1";}
   if(rng.NextDouble()<.25){int skill=stage==0?1:stage==1?3:stage==2?2:rng.Next(1,4);SkillPages[skill]++;extra+="\n"+Data.SkillName(Data.SkillIds[skill])+"残页 +1（3 页解锁；多余保留）";}
   return "修炼素材 +"+(12+stage*3)+"  ·  召唤晶石 +"+(8+stage*2)+"\n获得 "+item.Name+(rare?"  ★ 稀有掉落":"")+extra;
  }
  public string Summon(int seed) {
   if(Crystals<50)return "晶石不足，需要 50";
   var result=DrawSummonCard(seed,Pulls+1,Data.InClassicPool);
   Crystals-=50;Pulls++;
   return result;
  }
  string DrawSummonCard(int seed,int pull,Func<Card,bool> eligible){
   var rng=new Random(seed);
   double roll=rng.NextDouble();
   string rank=pull%20==0||roll<.08?Data.TopSummonRank(eligible):roll<.28?"B":"C";
   var pool=Enumerable.Range(0,Data.Cards.Length).Where(index=>eligible(Data.Cards[index])&&Data.Cards[index].Rarity==rank).ToArray();
   return "["+rank+"] "+Acquire(pool[rng.Next(pool.Length)]);
  }
  public int Offline(long ticks) {
   if(Wins==0)return 0;
   double hours=Math.Max(0,Math.Min(12,new TimeSpan(ticks-LastClaimTicks).TotalHours));return (int)(hours*12);
  }
 }
}

