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
   }
   int before=profile.Items.Count(item=>item.locked);
   AssertSmoke(before==1,"Boss reward missing");StartBattle();int replay=0;while(running&&replay++<4000)Advance();
   AssertSmoke(battle.Result==1&&profile.Items.Count(item=>item.locked)==before,"Boss replay duplicated guaranteed item");
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
    Load();AssertSmoke(saveError==""&&profile.CardIds.Length==9&&profile.Training[0]==27&&profile.Retired.Single(r=>r.cardId=="goku-namek-ssj-v020").shards==9&&profile.Team[6]==6&&profile.Items[0].owner==-1&&profile.Items[0].gem,"Previous Unity migration failed: "+saveError);
    AssertSmoke(profile.Presets[0].cards.All(id=>Data.Index(id)>=0)&&profile.Presets[0].leader==6&&profile.Pulls==19&&profile.SaiyanPulls==39,"Preset or economy migration failed");
    string pattern=Path.GetFileName(savePath)+".pre-v041-*.bak";
    AssertSmoke(Directory.GetFiles(dir,pattern).Length==1,"Previous backup missing");
    Save();Load();AssertSmoke(profile.StoryCleared.Length==4&&profile.Retired.Count==count-9&&profile.Retired.Single(r=>r.cardId=="goku-namek-ssj-v020").shards==9&&Directory.GetFiles(dir,pattern).Length==1,"Second load duplicates migration");
   }
   // v0.7.1 used schema 3 and included six cards after the retained roster.
   var oldIds=Data.Cards.Select(card=>card.Id).Concat(new[]{"goku-saiyan-guard-v020","piccolo-raditz-v030","raditz-arrival-v030","nappa-invasion-v030","yajirobe-saiyan-v030","saibaman-saiyan-v030"}).ToArray();
   profile=new Profile{CardIds=oldIds};
   profile.Training=new int[oldIds.Length];profile.Shards=new int[oldIds.Length];profile.Shards[9]=6;
   profile.Resonance=new int[oldIds.Length];profile.Policy=new int[oldIds.Length];
   profile.Owned=oldIds.Select((id,i)=>i<8||i==9).ToArray();
   profile.Skill=oldIds.Select(id=>"").ToArray();profile.Skill2=oldIds.Select(id=>"").ToArray();
   profile.Team[6]=9;profile.Items[0].owner=9;profile.Items[0].gem=true;
   profile.Presets.Add(new TeamPreset{name="旧阵容",leader=6,cards=profile.Team.Select(i=>oldIds[i]).ToArray()});
   savePath=Path.Combine(dir,"v071-save.json");File.WriteAllText(savePath,JsonUtility.ToJson(profile));
   Load();AssertSmoke(saveError==""&&profile.CardIds.Length==9&&profile.Team[6]==6&&profile.Items[0].owner==-1&&profile.Items[0].gem
    &&profile.Presets[0].cards.All(id=>Data.Index(id)>=0)&&profile.Presets[0].leader==6
    &&profile.Retired.Single(record=>record.cardId==oldIds[9]).shards==6,"Schema-3 roster migration failed");
   AssertSmoke(Directory.GetFiles(dir,"v071-save.json.pre-v080-*.bak").Length==1,"Schema-3 backup missing");
   Save();Load();AssertSmoke(!profile.NeedsRosterMigration&&profile.Retired.Count==6,"Schema-3 migration repeated");
   storyMode=false;
  }
 }
}
