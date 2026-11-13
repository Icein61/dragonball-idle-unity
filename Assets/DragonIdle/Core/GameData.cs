using System;
using System.Collections.Generic;
using System.Linq;
namespace DragonIdle {
 public class Card {
  public string Id,Name,Role,Tag,Special,Ultimate,Color;
  public double HP,Attack,Defense,Speed,Leader;public int Cooldown;
  public Card(string id,string name,string role,string tag,string sp,string ult,string color,double hp,double atk,double def,double speed,double lead,int cd=3) {
   Id=id;Name=name;Role=role;Tag=tag;Special=sp;Ultimate=ult;Color=color;HP=hp;Attack=atk;Defense=def;Speed=speed;Leader=lead;Cooldown=cd;
  }
 }
 public static class Data {
  public const string Version="0.1.0";
  public static Card[] Cards={
   new Card("goku-test","孙悟空","fighter","赛亚人","龟派气功","全力冲击","FF9F43",1050,116,42,110,.08),
   new Card("vegeta-test","贝吉塔","sniper","赛亚人","伽力克炮","全力爆发","599AFF",940,124,36,116,.09,4),
   new Card("piccolo-test","比克","tank","那美克星人","魔光波","魔贯光杀炮","7BCF92",1340,90,65,89,.07),
   new Card("krillin-test","克林","control","地球人","太阳拳","气圆斩","FFD580",950,95,40,106,.06,4),
   new Card("gohan-test","孙悟饭","support","混血赛亚人","连击","潜力爆发","BC9CFF",1010,104,44,102,.08),
   new Card("tien-test","天津饭","fighter","地球人","洞洞波","气功炮","86D9DF",1020,111,46,99,.07),
   new Card("dende-test","丹迪","healer","那美克星人","守护之光","祈愿之力","96E6AE",900,79,35,97,.06),
   new Card("trunks-test","特兰克斯","sniper","混血赛亚人","闪光冲击","燃烧攻击","E897CB",1060,128,45,113,.1)
  };
  public static Card Card(string id){return Cards.First(x=>x.Id==id);}
  public static string[] Stages={"荒野修炼","前线试炼","精英交锋","极限演武"};
  public static double[] StageScale={.68,.84,1.0,1.17};
  public static Unit Make(string id,int side,int slot,int training,int resonance,string skill,double scale) {
   var c=Card(id);double growth=1+training*.001+resonance*.004;
   var u=new Unit {CardId=id,Name=c.Name,Role=c.Role,Side=side,Slot=slot,MaxHP=c.HP*growth*scale,HP=c.HP*growth*scale,Attack=c.Attack*growth*scale,Defense=c.Defense*growth,Speed=c.Speed,SpecialName=c.Special,UltimateName=c.Ultimate,CooldownLength=c.Cooldown};
   if(skill=="pierce"){u.Role="sniper";u.SpecialName="贯穿气弹";u.CooldownLength=4;}
   if(skill=="focus")u.Crit+=.035;
   return u;
  }
 }
 [Serializable] public class Item {
  public string id; public int kind;public int owner=-1,slot;public bool gem;
  public string Name {get{return new[]{"修炼护腕","防护战斗服","稀有·聚气核心"}[kind];}}
  public double Bonus {get{return new[]{.012,.018,.055}[kind];}}
 }
 [Serializable] public class Profile {
  public int schemaVersion=1,Materials=120,Crystals=150,Gems=3,Unlocked=0,Wins=0,Pulls=0,Leader=0,SelectedStage=0;
  public int[] Training=new int[8],Shards=new int[8],Resonance=new int[8],Policy=new int[8];
  public bool[] Owned={true,true,true,true,true,true,true,false};
  public int[] Team={0,1,2,3,4,5,6};
  public string[] Skill={"","","","","","","",""};
  public string[] Skill2={"","","","","","","",""};
  public int OfflineBank;
  public List<Item> Items=new List<Item>();
  public long LastClaimTicks;
  public bool FirstMission;
  public Profile() {Items.Add(new Item{id="starter-bracer",kind=0,owner=0,slot=0});}
  public List<Unit> CreateTeam(int side=0) {
   if(Team.Length!=7||Team.Distinct().Count()!=7||Team.Any(i=>i<0||i>=8||!Owned[i]))throw new InvalidOperationException("Invalid team");
   var result=new List<Unit>();double leader=Data.Cards[Team[Math.Max(0,Math.Min(6,Leader))]].Leader;
   for(int s=0;s<7;s++){
    int i=Team[s];string replacement=Skill[i]=="pierce"||Skill2[i]=="pierce"?"pierce":"";
    var u=Data.Make(Data.Cards[i].Id,side,s,Training[i],Resonance[i],replacement,1);
    if(Skill[i]=="focus"||Skill2[i]=="focus")u.Crit+=.035;
    u.LeaderAttack=leader;u.UltimatePolicy=Policy[i];
    foreach(var item in Items.Where(x=>x.owner==i)){
     if(item.kind==1)u.Defense*=1+item.Bonus;else u.Add("attack",item.Bonus,0,item.id);
     if(item.gem)u.Add("attack",.008,0,item.id+":gem");
    }
    result.Add(u);
   }return result;
  }
  public Battle NewBattle(int stage,int seed){
   if(stage<0||stage>Unlocked||stage>=Data.Stages.Length)throw new ArgumentException("Stage locked");
   var units=CreateTeam();for(int i=0;i<7;i++){
    var u=Data.Make(Data.Cards[i].Id,1,i,0,0,"",Data.StageScale[stage]);u.Name="试炼·"+u.Name;units.Add(u);
   }return new Battle(units,seed);
  }
  public bool Train(int card){int cost=30+Training[card]/10;if(Materials<cost)return false;Materials-=cost;Training[card]++;return true;}
  public bool SetSkill(int card,int slot,string id){
   if(card<0||card>=8||!Owned[card]||slot<0||slot>1||!(id==""||id=="focus"||id=="pierce"))return false;
   string other=slot==0?Skill2[card]:Skill[card];if(id!=""&&id==other)return false;
   if(slot==0)Skill[card]=id;else Skill2[card]=id;return true;
  }
  public bool ResonanceUp(int card){int cost=3+Resonance[card];if(Shards[card]<cost)return false;Shards[card]-=cost;Resonance[card]++;return true;}
  public bool Equip(string itemId,int card,int slot) {
   if(card<0||card>=Owned.Length||!Owned[card]||slot<0||slot>2)return false;
   var item=Items.FirstOrDefault(x=>x.id==itemId);if(item==null)return false;
   foreach(var old in Items.Where(x=>x.owner==card&&x.slot==slot))old.owner=-1;
   item.owner=card;item.slot=slot;return true;
  }
  public string Reward(int stage,int seed) {
   var rng=new Random(seed);Wins++;Materials+=12+stage*3;Crystals+=8+stage*2;
   if(stage==Unlocked&&Unlocked<3)Unlocked++;
   bool rare=rng.NextDouble()<.08;var item=new Item{id=Guid.NewGuid().ToString("N"),kind=rare?2:rng.Next(2)};
   Items.Add(item);
   if(rng.NextDouble()<.2)Gems++;
   return "修炼素材 +"+(12+stage*3)+"  ·  召唤晶石 +"+(8+stage*2)+"\n获得 "+item.Name+(rare?"  ★ 稀有掉落":"");
  }
  public string Summon(int seed) {
   if(Crystals<50)return "晶石不足，需要 50";
   Crystals-=50;Pulls++;var rng=new Random(seed);int index=Pulls%20==0||rng.NextDouble()<.08?7:rng.Next(7);
   if(Owned[index]){Shards[index]++;return Data.Cards[index].Name+" · 重复卡转为专属资源 +1";}
   Owned[index]=true;return "新角色解锁："+Data.Cards[index].Name;
  }
  public int Offline(long ticks) {
   if(Wins==0)return 0;
   double hours=Math.Max(0,Math.Min(12,new TimeSpan(ticks-LastClaimTicks).TotalHours));return (int)(hours*12);
  }
 }
}
