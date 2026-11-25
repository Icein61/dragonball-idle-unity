using System;
using System.IO;
using UnityEngine;

namespace DragonIdle {
 internal sealed class ProfileStore {
  internal struct OpenResult {
   internal readonly Profile Profile;
   internal readonly bool RosterMigration,FormationMigration;

   internal OpenResult(Profile profile,bool rosterMigration=false,bool formationMigration=false){
    Profile=profile;
    RosterMigration=rosterMigration;
    FormationMigration=formationMigration;
   }
  }

  internal string FilePath {get;set;}
  internal string SaveError {get;set;}="";

  internal OpenResult Open(){
   try{
    if(!File.Exists(FilePath)){
     var fresh=NewProfile();
     Write(fresh);
     return new OpenResult(fresh);
    }

    var profile=Read();
    bool rosterMigration=profile.NeedsRosterMigration,formationMigration=profile.NeedsFormationMigration;
    if(rosterMigration||formationMigration)
     Backup(profile.schemaVersion<3?".pre-v041-":rosterMigration?".pre-v080-":".pre-v0120-");
    bool roundMigration=profile.NeedsRoundRosterMigration;
    if(roundMigration)Backup(".pre-v0144-");

    profile.Normalize();
    profile.CreateTeam();
    if(roundMigration){
     profile.MigrateRoundRoster();
     Write(profile);
    }
    return new OpenResult(profile,rosterMigration,formationMigration);
   }catch(Exception ex){
    var fallback=NewProfile();
    SaveError="存档读取失败，原文件保留。请检查备份："+ex.Message;
    return new OpenResult(fallback);
   }
  }

  internal Profile Read(){
   var profile=JsonUtility.FromJson<Profile>(File.ReadAllText(FilePath));
   if(profile==null)throw new InvalidOperationException("空存档");
   return profile;
  }

  internal void Write(Profile profile){
   if(SaveError!="")throw new IOException(SaveError);
   profile.Validate();
   Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
   string temporary=FilePath+".tmp";
   File.WriteAllText(temporary,JsonUtility.ToJson(profile,true));
   if(File.Exists(FilePath))File.Replace(temporary,FilePath,FilePath+".bak");
   else File.Move(temporary,FilePath);
  }

  void Backup(string reason){
   File.Copy(FilePath,FilePath+reason+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".bak",false);
  }

  static Profile NewProfile(){
   return new Profile{LastClaimTicks=DateTime.UtcNow.Ticks};
  }
 }
}
