using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace DragonIdle {
 public partial class DemoGame {
  void RunBattleArtSmoke(string dir){
   foreach(int i in Enumerable.Range(0,Data.Cards.Length)){
    var texture=FighterTexture(i);var regions=Regions(i);
    var portrait=PortraitTexture(i);
    var cardId=Data.Cards[i].Id;
    var expectedSize=cardId=="piccolo-test"?new Vector2Int(1086,1448):new Vector2Int(1024,1536);
    AssertSmoke(portrait!=null&&portrait!=texture&&portrait.width==expectedSize.x&&portrait.height==expectedSize.y,"Portrait missing, resized or using battle atlas: "+cardId);
    AssertSmoke(texture!=null&&texture.width==1536&&texture.height==1024,"Atlas resource missing/rescaled: "+Data.Cards[i].Id);
    AssertSmoke(regions.frames.Length==13&&regions.frames.All(r=>r.w>0&&r.h>0&&r.x>=0&&r.y>=0&&r.x+r.w<=texture.width&&r.y+r.h<=texture.height),"Bad sprite import bounds");
    for(int f=0;f<13;f++){
     var uv=ArtUV(i,f);var mirror=ArtUV(i,f,true);
     AssertSmoke(uv.width>0&&mirror.width==-uv.width&&Mathf.Abs(mirror.x-(uv.x+uv.width))<.00001f,"Pose UV mirroring failed");
    }
    for(int side=0;side<2;side++)for(int slot=0;slot<Data.TeamSlots;slot++)for(int pose=1;pose<=12;pose++){
     var feet=FighterFeet(new Unit{Side=side,Slot=slot});var r=regions.frames[pose];float scale=FighterScale(i)*(slot<3?1:.78f);
     AssertSmoke(feet.x-r.w*scale/2>224&&feet.x+r.w*scale/2<1269&&feet.y-r.h*scale>=280&&feet.y<720,"Fighter pose clipped by arena: "+Data.Cards[i].Id);
    }
   }
   // Exercise the real frame update without automatically fast-forwarding via batch-mode Advance.
   profile=new Profile();storyMode=false;StartBattle();var before=ActionPresentation.Capture(battle.Units);last=battle.Step();action=new ActionPresentation(last,before,battle.Units);
   AssertSmoke(InkChargeSubject(battle.Units)==last.Actor&&Math.Abs(ShownVitals(last.Actor).Rage-before[last.Actor].Rage)<.001,"Battle charge should follow the displayed action snapshot");
   paused=true;UpdateBattlePresentation(.4f);AssertSmoke(action.Elapsed==0,"Paused frame advanced animation");
   paused=false;rate=2;UpdateBattlePresentation(.1f);AssertSmoke(Math.Abs(action.Elapsed-.2)<.0001,"Speed did not reach presentation clock");
   audioSettings.ReducedMotion=true;var pos=FighterFeet(last.Actor);AssertSmoke(AnimatedFeet(last.Actor)==pos,"Reduced motion still lunges");
   audioSettings.ReducedMotion=false;
   foreach(var unit in battle.Units){var feet=FighterFeet(unit);AssertSmoke(feet.x>270&&feet.x<1220&&feet.y>360&&feet.y+36<720,"Fighter overlay outside arena");}
   // A final lethal action must finish before rewards appear, and must settle only once.
   battle.Units.Where(u=>u.Side==1).ToList().ForEach(u=>u.HP=0);
   int wins=profile.Wins;UpdateBattlePresentation(.1f);AssertSmoke(running&&!settled&&profile.Wins==wins,"Rewards displayed before final animation");
   UpdateBattlePresentation(5);AssertSmoke(!running&&settled&&profile.Wins==wins+1,"Final animation did not settle reward");
   UpdateBattlePresentation(5);AssertSmoke(profile.Wins==wins+1,"Reward applied twice");
   StartBattle();AssertSmoke(action==null&&last==null&&running&&!settled,"Replay retained prior effects");
   running=false;battle=null;ClearBattlePresentation();AssertSmoke(action==null&&last==null,"Exit did not clear presentation");rate=1;
   AssertSmoke(silentAudio&&!battleSound.isPlaying&&battleSound.volume==0&&battleClips.Length==4,"Battle smoke produced audio");
   foreach(var clip in battleClips){float[] samples=new float[clip.samples];clip.GetData(samples,0);AssertSmoke(samples.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)&&Math.Abs(v)<=1)&&samples.Any(v=>Math.Abs(v)>.01f),"Battle audio samples invalid");}
   File.WriteAllText(Path.Combine(dir,"battle-art-smoke.json"),"{\"atlases\":9,\"portraits\":9,\"signaturePortraits\":9,\"poses\":108,\"inkCharge\":true,\"frameUpdate\":true,\"pause\":true,\"speed\":true,\"reducedMotion\":true,\"rewardAfterAnimation\":true,\"replayCleanup\":true,\"soundSilent\":true,\"renderVerified\":false,\"listened\":false}");
  }
 }
}
