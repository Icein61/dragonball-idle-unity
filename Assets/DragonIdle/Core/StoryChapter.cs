using System;
using System.Linq;
using System.Collections.Generic;
namespace DragonIdle {
 public static partial class Data {
  public static string[] StoryStages={"联手修炼","不速之客","联手迎击","拉蒂兹决战"};
  public static string[] StoryBriefs={
   "悟空与比克的切磋。首通：迎战拉蒂兹·比克（独立新卡）及50晶石。",
   "拉蒂兹初现。双目标绝招分别结算暴击与闪避，分散装备可减少前排压力。",
   "精英战：提高生命与攻击。建议将新比克换入队伍，利用悟空的防御削弱。",
   "首领战：无隐藏控制免疫。首通解锁拉蒂兹，获得1件聚气核心及100晶石。"};
  public static string[] StoryLoot={
   "素材12 / 晶石6；装备：护腕94% / 核心6%；珠20% / 专注残页25%",
   "素材15 / 晶石7；装备：战斗服94% / 核心6%；珠20% / 守势残页25%",
   "素材18 / 晶石8；装备：护腕46% / 战斗服46% / 核心8%；珠20% / 贯穿残页25%",
   "素材21 / 晶石9；装备：护腕45% / 战斗服45% / 核心10%；珠20% / 随机残页25%"};
  public static bool InSaiyanPool(Card c){return c.Stage=="赛亚人篇";}
  public static bool InClassicPool(Card c){return PreviousCardIds.Take(20).Contains(c.Id)&&c.Stage=="赛亚人篇";}
 }
 public partial class Profile {
  public int StoryUnlocked,StorySelected,SaiyanPulls;
  public bool[] StoryCleared=new bool[4];
  public void NormalizeStory(bool upgrading){if(upgrading&&(StoryCleared==null||StoryCleared.Length==0))StoryCleared=new bool[4];}
  public void ValidateStory(){
   if(StoryCleared==null||StoryCleared.Length!=4||StoryUnlocked<0||StoryUnlocked>3||StorySelected<0||StorySelected>StoryUnlocked||SaiyanPulls<0)throw new InvalidOperationException("章节存档损坏");
   for(int i=0;i<4;i++)if((i<StoryUnlocked&&!StoryCleared[i])||(i>StoryUnlocked&&StoryCleared[i]))throw new InvalidOperationException("章节解锁记录冲突");
  }
  public List<Unit> StoryEnemies(int stage){
   if(stage<0||stage>3)throw new ArgumentOutOfRangeException("stage");
   if(stage==0){
    var a=Data.Make("goku-test",1,0,0,0,"",.8);a.MaxHP=a.HP=2300;
    var b=Data.Make("piccolo-raditz-v030",1,1,0,0,"",.8);b.MaxHP=b.HP=2400;
    return new List<Unit>{a,b};
   }
   var boss=Data.Make("raditz-arrival-v030",1,1,0,0,"",1);
   boss.MaxHP=boss.HP=new[]{0,5100,6400,8000}[stage];
   boss.Attack=new[]{0,150,190,235}[stage];boss.Speed=new[]{0,128,135,142}[stage];
   boss.Name=(stage==3?"首领·":stage==2?"精英·":"")+"拉蒂兹";
   return new List<Unit>{boss};
  }
  public Battle NewStoryBattle(int stage,int seed){
   Validate();if(stage<0||stage>StoryUnlocked||stage>3)throw new ArgumentException("Story locked");
   return new Battle(CreateTeam().Concat(StoryEnemies(stage)),seed);
  }
  public string RewardStory(int stage,int seed){
   Validate();if(stage<0||stage>StoryUnlocked||stage>3)throw new ArgumentException("Story locked");
   var rng=new Random(seed);Wins++;Materials+=12+stage*3;Crystals+=6+stage;
   bool first=!StoryCleared[stage];StoryCleared[stage]=true;if(stage==StoryUnlocked&&stage<3)StoryUnlocked++;
   bool rare=rng.NextDouble()<(stage<2?.06:stage==2?.08:.10);
   var item=new Item{id=Guid.NewGuid().ToString("N"),kind=rare?2:stage<2?stage:rng.Next(2)};Items.Add(item);
   string result="素材 +"+(12+stage*3)+" / 晶石 +"+(6+stage)+" / "+item.Name;
   if(rng.NextDouble()<.2){Gems++;result+=" / 攻击珠 +1";}
   if(rng.NextDouble()<.25){int s=stage==0?1:stage==1?3:stage==2?2:rng.Next(1,4);SkillPages[s]++;result+=" / "+Data.SkillName(Data.SkillIds[s])+"残页 +1";}
   if(first){
    int crystals=stage==3?100:50;Crystals+=crystals;result+="\n首通：晶石 +"+crystals;
    if(stage==0)result+=" / "+Acquire(Data.Index("piccolo-raditz-v030"));
    if(stage==3){result+=" / "+Acquire(Data.Index("raditz-arrival-v030"));Items.Add(new Item{id=Guid.NewGuid().ToString("N"),kind=2,locked=true});result+=" / 已收藏聚气核心 +1";}
   }
   return result;
  }
  public string SummonSaiyan(int seed){
   if(Crystals<50)return "晶石不足，需要50";
   Crystals-=50;SaiyanPulls++;var rng=new Random(seed);double roll=rng.NextDouble();string rarity=SaiyanPulls%20==0||roll<.08?"SSR":roll<.28?"SR":"R";
   var pool=Enumerable.Range(0,Data.Cards.Length).Where(i=>Data.InSaiyanPool(Data.Cards[i])&&Data.Cards[i].Rarity==rarity).ToArray();
   return "["+rarity+"] "+Acquire(pool[rng.Next(pool.Length)]);
  }
 }
}
