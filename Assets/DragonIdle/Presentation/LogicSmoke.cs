using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame {
  void AssertSmoke(bool condition,string message){if(!condition)throw new Exception(message);}
  void RunLogicSmoke(string[] args){
   int o=Array.IndexOf(args,"-smokeOut");string dir=o>=0&&o+1<args.Length?args[o+1]:Path.GetDirectoryName(savePath);
   try{
    AssertSmoke(args.Contains("-savePath"),"Smoke requires isolated save path");Directory.CreateDirectory(dir);
    AssertSmoke(Application.productName==Data.GameTitle,"Packaged game title incorrect");
    AssertSmoke(saveError=="","Profile load failed: "+saveError);
    AssertSmoke(Data.Cards.Length==15&&profile.Owned.Count(x=>x)==8,"Catalog initialization failed");
    var art=Resources.Load<Texture2D>("Art/goku-raditz-c-v030");AssertSmoke(art!=null&&art.width==1024&&art.height==1536,"Character sample resource missing or aspect changed");
    foreach(var name in new[]{"piccolo-raditz-c-v030","raditz-c-v030"}){var extra=Resources.Load<Texture2D>("Art/"+name);AssertSmoke(extra!=null&&extra.width==1024&&extra.height==1536,"New art resource missing");}
    storyMode=false;StartBattle();int events=0;while(running&&events++<4000)Advance();
    AssertSmoke(battle.Result==1&&settled,"Smoke battle or reward failed");int wins=profile.Wins;Load();AssertSmoke(profile.Wins==wins,"Save roundtrip failed");
    AssertSmoke(Change(()=>{
     profile.SetMember(1,7);profile.Leader=1;profile.SavePreset(0);profile.SetSkill(0,0,"focus");
     profile.SkillPages[3]=3;profile.UnlockSkill(3);profile.SetSkill(0,1,"guard");profile.Train(0);profile.Equip(profile.Items.Last().id,0,1);
    }),"Loadout save failed");
    Load();AssertSmoke(profile.Team[1]==7&&profile.Leader==1&&profile.Presets[0].cards[1]==Data.Cards[7].Id&&profile.Skill2[0]=="guard"&&profile.Training[0]==1,"Character persistence failed");
    RunChapterSmoke(dir);
    // A v1-shaped JSON fixture exercises Unity serialization, backup and migration together.
    savePath=Path.Combine(dir,"legacy-save.json");
    File.WriteAllText(savePath,"{\"schemaVersion\":1,\"Materials\":481,\"Crystals\":100,\"Gems\":3,\"Unlocked\":1,\"Wins\":2,\"Pulls\":19,\"Leader\":0,\"SelectedStage\":0,\"Training\":[23,0,0,0,0,0,0,0],\"Shards\":[0,0,0,0,0,0,0,9],\"Resonance\":[0,0,0,0,0,0,0,0],\"Policy\":[0,0,0,0,0,0,0,0],\"Owned\":[true,true,true,true,true,true,true,true],\"Team\":[0,1,2,3,4,5,7],\"Skill\":[\"focus\",\"\",\"\",\"\",\"\",\"\",\"\",\"\"],\"Skill2\":[\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\"],\"Items\":[{\"id\":\"legacy-bracer\",\"kind\":0,\"owner\":7,\"slot\":0,\"gem\":true}],\"OfflineBank\":24,\"LastClaimTicks\":638000000000000000,\"FirstMission\":true}");
    Load();AssertSmoke(saveError==""&&profile.schemaVersion==3&&profile.Training[0]==23&&profile.Retired.Single(r=>r.cardId=="trunks-test").shards==9&&profile.Items[0].owner==-1&&profile.Team[6]==6&&profile.Materials==481&&profile.OfflineBank==24&&profile.Pulls==19,"Unity v1 migration failed: "+saveError);
    AssertSmoke(Directory.GetFiles(dir,"legacy-save.json.pre-v041-*.bak").Length==1,"Pre-migration backup missing");
    Save();Load();AssertSmoke(profile.schemaVersion==3&&profile.Owned.Count(x=>x)==8&&profile.Retired.Single(r=>r.cardId=="trunks-test").shards==9,"Migrated save second load failed");
    RunBattleArtSmoke(dir);
    RunGallerySmoke(dir);RunWorkshopSmoke(dir);
    File.WriteAllText(Path.Combine(dir,"smoke-result.json"),"{\"version\":\""+Data.Version+"\",\"battleWon\":true,\"saveRoundTrip\":true,\"legacyMigration\":true,\"characterPersistence\":true,\"chapterFlow\":true,\"v020Migration\":true,\"v040Migration\":true,\"leaderKeywords\":true,\"artResourceLoaded\":true,\"galleryFlow\":true,\"audioSettings\":true,\"visualVerified\":false,\"events\":"+events+"}");
    Debug.Log("DRAGON_IDLE_LOGIC_SMOKE_SUCCESS");Application.Quit(0);
   }catch(Exception ex){File.WriteAllText(Path.Combine(dir,"smoke-failure.txt"),ex.ToString());Debug.LogError(ex);Application.Quit(1);}
  }
 }
}

