using System.Collections.Generic;

namespace DragonIdle {
 public static partial class Data {
  // The four descriptions are copied verbatim from the published-card rows of the design workbook.
  static readonly Dictionary<string,string[]> DesignSkills=new Dictionary<string,string[]> {
    {"goku-test",new[]{"对正面敌方造成低伤害","对正面敌方造成中伤害，对自身施加【攻击强化】低，持续2回合","对正面敌方造成高伤害","【攻击强化】低 / 己方存在【比克】时，【防御强化】中"}},
    {"vegeta-test",new[]{"对正面敌人造成低伤害","对正面敌人造成中伤害，对己方同排队友施加【攻击强化】低，持续2回合","对生命最低敌方造成高伤害","【攻击强化】中 / 【防御强化】中"}},
    {"piccolo-test",new[]{"对正面敌人造成低伤害","对正面敌人造成中伤害，对己方同排队友施加【防御强化】低，持续2回合","损失中生命，对己方生命最低两人释放【护盾】中","【防御强化】中 / 【自然回复】低"}},
    {"krillin-test",new[]{"对正面敌人造成低伤害","对正面敌人造成中伤害","对敌方前排造成高伤害","【速度强化】低"}},
    {"gohan-test",new[]{"对正面敌人造成低伤害","对正面敌人造成中伤害，吸取目标中怒气","对攻击最高敌方造成高伤害","【攻击强化】低 / 己方存在【比克】时，【攻击强化】中"}},
    {"tien-test",new[]{"对正面敌人造成低伤害","对正面敌人造成中伤害","损失低生命，对正面敌方造成超高伤害","【攻击强化】低 / 己方存在【饺子】时，【生命强化】中"}},
    {"yamcha-saiyan-v020",new[]{"对正面敌人造成低伤害","对正面敌人造成中伤害","对正面敌方造成高伤害","己方上阵【Z战士】2名时，【攻击强化】中 / 己方上阵【Z战士】4名时，【防御强化】中，己方上阵【Z战士】5名时，【生命强化】中"}},
    {"chiaotzu-saiyan-v020",new[]{"对正面敌人造成低伤害","对正面敌人造成中伤害","对正面敌人造成中伤害","己方【Z战士】全体【攻击强化】低 & 【防御强化】低"}},
    {"goku-saiyan-kaioken-v020",new[]{"对正面敌人造成低伤害","消耗低生命，对正面敌人造成高伤害","消耗低生命，对正面敌人造成超高伤害","【攻击强化】中 / 【生命偷取】极低"}},
  };
  public static bool Reworked(string id){return id!=null&&DesignSkills.ContainsKey(id);}
  public static string UserSkill(string id,int move){string[] skills;return id!=null&&DesignSkills.TryGetValue(id,out skills)?skills[move]:null;}
 }
}
