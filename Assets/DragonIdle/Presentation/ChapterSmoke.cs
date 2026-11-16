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
   // Shape of v0.2 JSON: 20 definitions, no chapter progress.
   profile=new Profile();profile.CardIds=profile.CardIds.Take(20).ToArray();
   profile.Training=profile.Training.Take(20).ToArray();profile.Training[0]=27;
   profile.Shards=profile.Shards.Take(20).ToArray();profile.Shards[13]=9;
   profile.Resonance=profile.Resonance.Take(20).ToArray();profile.Policy=profile.Policy.Take(20).ToArray();
   profile.Owned=profile.Owned.Take(20).ToArray();profile.Owned[13]=true;
   profile.Skill=profile.Skill.Take(20).ToArray();profile.Skill2=profile.Skill2.Take(20).ToArray();
   profile.Team[6]=13;profile.Items[0].owner=13;profile.StoryCleared=null;
   savePath=Path.Combine(dir,"v020-save.json");File.WriteAllText(savePath,JsonUtility.ToJson(profile));
   Load();AssertSmoke(saveError==""&&profile.CardIds.Length==25&&profile.Training[0]==27&&profile.Shards[13]==9&&profile.Team[6]==13&&profile.Items[0].owner==13,"v020 Unity migration failed: "+saveError);
   AssertSmoke(Directory.GetFiles(dir,"v020-save.json.pre-v030-*.bak").Length==1,"v020 backup missing");
   Save();Load();AssertSmoke(profile.StoryCleared.Length==4&&profile.Shards[13]==9&&Directory.GetFiles(dir,"v020-save.json.pre-v030-*.bak").Length==1,"v020 second load duplicates migration");
   storyMode=false;
  }
 }
}
