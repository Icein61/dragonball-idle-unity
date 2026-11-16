using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame {
  void RunChapterSmoke(string dir){
   savePath=Path.Combine(dir,"chapter-save.json");profile=new Profile();storyMode=true;Save();
   for(int stage=0;stage<4;stage++){
    int s=stage;AssertSmoke(Change(()=>profile.StorySelected=s),"Story selection save failed");
    StartBattle();int events=0;while(running&&events++<4000)Advance();
    AssertSmoke(battle.Result==1&&settled&&profile.StoryCleared[s],"Chapter clear failed "+stage);
    int crystals=profile.Crystals;Load();
    AssertSmoke(profile.Crystals==crystals&&profile.StoryCleared[s]&&profile.StorySelected==s,"Chapter persistence failed");
    if(stage==0)AssertSmoke(Change(()=>profile.SetMember(6,Data.Index("piccolo-raditz-v030"))),"Story character equip failed");
   }
   int raditz=Data.Index("raditz-arrival-v030");int before=profile.Shards[raditz];
   AssertSmoke(profile.Owned[raditz],"Boss reward missing");StartBattle();int replay=0;while(running&&replay++<4000)Advance();
   AssertSmoke(battle.Result==1&&profile.Shards[raditz]==before,"Boss replay duplicated guaranteed card");
   foreach(int count in new[]{20,25}){
    profile=new Profile{schemaVersion=2};profile.CardIds=Data.PreviousCardIds.Take(count).ToArray();
    profile.Training=new int[count];profile.Training[0]=27;profile.Shards=new int[count];profile.Shards[13]=9;
    profile.Resonance=new int[count];profile.Policy=new int[count];
    profile.Owned=Enumerable.Range(0,count).Select(i=>i<7||i==8||i==9||i==10||i==13).ToArray();
    profile.Skill=Enumerable.Repeat("",count).ToArray();profile.Skill2=Enumerable.Repeat("",count).ToArray();
    profile.Team=new[]{0,1,2,3,4,5,13};profile.Items[0].owner=13;profile.Items[0].gem=true;
    profile.Presets.Add(new TeamPreset{name="旧阵容",leader=6,cards=profile.Team.Select(i=>profile.CardIds[i]).ToArray()});
    profile.Pulls=19;profile.SaiyanPulls=39;if(count==20)profile.StoryCleared=null;
    savePath=Path.Combine(dir,count==20?"v020-save.json":"v040-save.json");File.WriteAllText(savePath,JsonUtility.ToJson(profile));
    Load();AssertSmoke(saveError==""&&profile.CardIds.Length==15&&profile.Training[0]==27&&profile.Retired.Single(r=>r.cardId=="goku-namek-ssj-v020").shards==9&&profile.Team[6]==6&&profile.Items[0].owner==-1&&profile.Items[0].gem,"Previous Unity migration failed: "+saveError);
    AssertSmoke(profile.Presets[0].cards.All(id=>Data.Index(id)>=0)&&profile.Presets[0].leader==6&&profile.Pulls==19&&profile.SaiyanPulls==39,"Preset or economy migration failed");
    string pattern=Path.GetFileName(savePath)+".pre-v041-*.bak";
    AssertSmoke(Directory.GetFiles(dir,pattern).Length==1,"Previous backup missing");
    Save();Load();AssertSmoke(profile.StoryCleared.Length==4&&profile.Retired.Count==10&&profile.Retired.Single(r=>r.cardId=="goku-namek-ssj-v020").shards==9&&Directory.GetFiles(dir,pattern).Length==1,"Second load duplicates migration");
   }
   storyMode=false;
  }
 }
}
