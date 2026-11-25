using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DragonIdle {
 public partial class DemoGame {
  void RunProfileSmoke(string[] args){
   string mode="";
   try{
    int index=Array.IndexOf(args,"-profileSmoke");
    mode=index+1<args.Length?args[index+1]:"";
    var formation=Enumerable.Repeat(-1,RoundBoard.Cells).ToArray();
    formation[7]=Data.Index(Data.RoundRoster[0]);
    switch(mode){
     case "create":
      AssertSmoke(saveError==""&&File.Exists(savePath)&&!File.Exists(savePath+".bak")&&profile.LastClaimTicks>0&&
       !profile.RoundFormation().SequenceEqual(formation),"First launch did not create the default profile");
      string fresh=File.ReadAllText(savePath);
      AssertSmoke(Change(()=>StoreProfileSmokeFormation(formation)),"Formation update failed");
      AssertSmoke(File.ReadAllText(savePath+".bak")==fresh&&!File.Exists(savePath+".tmp")&&
       profileStore.Read().RoundFormation().SequenceEqual(formation),"First persisted formation or backup differs");
      break;
     case "reload":
      AssertSmoke(saveError==""&&profile.RoundFormation().SequenceEqual(formation),"Saved formation did not survive relaunch");
      string persisted=File.ReadAllText(savePath),backup=File.ReadAllText(savePath+".bak");
      AssertSmoke(Directory.Exists(savePath+".tmp"),"Missing write-failure fixture");
      int[] changed=Enumerable.Repeat(-1,RoundBoard.Cells).ToArray();
      changed[0]=formation[7];
      AssertSmoke(!Change(()=>StoreProfileSmokeFormation(changed))&&notice.StartsWith("保存失败，本次操作已撤回：")&&
       profile.RoundFormation().SequenceEqual(formation)&&File.ReadAllText(savePath)==persisted&&
       File.ReadAllText(savePath+".bak")==backup,"Write failure did not preserve the profile, backup and in-memory formation");
      break;
     case "corrupt":
      const string invalid="{ invalid JSON";
      AssertSmoke(saveError.StartsWith("存档读取失败，原文件保留。请检查备份：")&&profile.LastClaimTicks>0&&
       File.ReadAllText(savePath)==invalid,"Corrupt profile did not preserve its original bytes and signal an error");
      AssertSmoke(!Change(()=>StoreProfileSmokeFormation(formation))&&File.ReadAllText(savePath)==invalid&&
       !File.Exists(savePath+".tmp"),"Corrupt-profile fallback allowed an overwrite");
      break;
     case "blocked-create":
      AssertSmoke(saveError.StartsWith("存档读取失败，原文件保留。请检查备份：")&&profile.LastClaimTicks>0&&
       !File.Exists(savePath)&&!File.Exists(savePath+".bak"),"Failed first write did not preserve the empty save directory");
      AssertSmoke(!Change(()=>StoreProfileSmokeFormation(formation))&&!File.Exists(savePath),
       "Fallback following a failed first write allowed a second write");
      break;
     default: throw new InvalidOperationException("Unknown profile smoke mode");
    }
    Debug.Log("PROFILE_SMOKE_PASS "+mode);
    Application.Quit(0);
   }catch(Exception ex){
    Debug.LogError("PROFILE_SMOKE_FAIL "+mode+": "+ex);
    Application.Quit(1);
   }
  }

  void StoreProfileSmokeFormation(int[] formation){
   if(!profile.TrySaveRoundFormation(formation))throw new InvalidOperationException("Profile smoke formation is invalid");
  }
 }
}
