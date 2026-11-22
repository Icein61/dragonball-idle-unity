using System;
using System.Linq;

namespace DragonIdle {
 [Serializable] public sealed class RoundScene {
  public string id, title, background, bgm;
  public float groundFraming;
 }

 [Serializable] public sealed class RoundDemoSetup {
  public string scene;
  public string[] scenePool, allies, enemies;
  public bool useSavedFormation;
  public double damageScale;
  public float actionGap;

  internal void Validate() {
   if(!(damageScale>=0&&damageScale<=2)||!(actionGap>=0&&actionGap<=2))
    throw new InvalidOperationException("战斗 Demo 伤害和节奏配置无效");
   if(useSavedFormation){
    if(Data.RoundRoster.Count<5)throw new InvalidOperationException("战斗 Demo 需要至少五张上线卡");
    return;
   }
   ValidateSide(allies, RoundBoard.PlayerLimit, true);
   ValidateSide(enemies, RoundBoard.Cells, false);
  }

  static void ValidateSide(string[] positions,int limit,bool unique) {
   if(positions==null||positions.Length!=RoundBoard.Cells)
    throw new InvalidOperationException("战斗 Demo 每方需要九个阵位");
   var cards=positions.Where(id=>!string.IsNullOrEmpty(id)).ToArray();
   if(cards.Length<1||cards.Length>limit||cards.Any(id=>Data.Index(id)<0)||
      unique&&cards.Length!=cards.Distinct().Count())
    throw new InvalidOperationException("战斗 Demo 阵容无效");
  }

  public RoundCombat CreateBattle(RoundDefinitions rules,int seed,Profile profile=null) {
   Validate();
   if(useSavedFormation&&profile==null)throw new ArgumentNullException("profile");
   var formation=useSavedFormation?DrawSavedFormation(profile,seed):this;
   int level=useSavedFormation?RoundProgression.MaxLevel:0;
   return RoundCombat.Demo(Fighters(formation.allies,0,level).Concat(Fighters(formation.enemies,1,level)),
    rules,seed,damageScale==0?1:damageScale);
  }

  internal static RoundDemoSetup DrawSavedFormation(Profile profile,int seed){
   var random=new Random(seed);
   var choices=Data.RoundRoster.ToList();
   var slots=Enumerable.Range(0,RoundBoard.Cells).ToList();
   var enemies=new string[RoundBoard.Cells];
   for(int count=0;count<5;count++){
    int card=random.Next(choices.Count),slot=random.Next(slots.Count);
    enemies[slots[slot]]=choices[card];
    choices.RemoveAt(card);slots.RemoveAt(slot);
   }
   var allies=profile.RoundFormation().Select(index=>index<0?null:Data.Cards[index].Id).ToArray();
   return new RoundDemoSetup{allies=allies,enemies=enemies};
  }

  static System.Collections.Generic.IEnumerable<RoundFighter> Fighters(string[] positions,int side,int level) {
   for(int i=0;i<positions.Length;i++){
    if(string.IsNullOrEmpty(positions[i]))continue;
    var card=Data.Card(positions[i]);
    yield return level>0?RoundProgression.Create(card,level,side,i+1):
     new RoundFighter(card.Id,side,i+1,card.HP,card.Attack,card.Defense,.12);
   }
  }
 }

 [Serializable] public sealed class RoundSceneDefinitions {
  public RoundScene[] scenes;
  public RoundDemoSetup demo;

  public RoundScene Scene(string id){return scenes.Single(scene=>scene.id==id);}

  internal RoundScene PickDemoScene(Random random,string previous){
   if(demo.scenePool==null||demo.scenePool.Length==0)return Scene(demo.scene);
   // Sample from the other maps after the first encounter.
   var choices=demo.scenePool.Length>1?demo.scenePool.Where(id=>id!=previous).ToArray():demo.scenePool;
   return Scene(choices[random.Next(choices.Length)]);
  }

  public void Validate(){
   if(scenes==null||scenes.Length==0||demo==null||scenes.Any(scene=>!ValidScene(scene))||
      scenes.Select(scene=>scene.id).Distinct().Count()!=scenes.Length||
      !scenes.Any(scene=>scene.id==demo.scene))
    throw new InvalidOperationException("战斗场景配置缺失或无效");
   if(demo.scenePool!=null&&(
      demo.scenePool.Distinct().Count()!=demo.scenePool.Length||
      demo.scenePool.Any(id=>!scenes.Any(scene=>scene.id==id&&ValidResource(scene.bgm,"Audio/")))))
    throw new InvalidOperationException("战斗 Demo 随机场景或配乐无效");
   demo.Validate();
  }

  static bool ValidScene(RoundScene scene) {
   return scene!=null&&!string.IsNullOrWhiteSpace(scene.id)&&!string.IsNullOrWhiteSpace(scene.title)&&
    ValidResource(scene.background,"Art/")&&(string.IsNullOrEmpty(scene.bgm)||ValidResource(scene.bgm,"Audio/"))&&
    (scene.groundFraming==0||scene.groundFraming>=.5f&&scene.groundFraming<=1);
  }

  static bool ValidResource(string path,string prefix) {
   return !string.IsNullOrWhiteSpace(path)&&path.StartsWith(prefix,StringComparison.Ordinal)&&
    !path.Contains("..")&&!path.Contains("\\");
  }
 }
}
