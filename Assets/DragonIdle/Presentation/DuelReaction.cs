using UnityEngine;

namespace DragonIdle {
 internal sealed class DuelReaction {
  const float PoseBridge=55,LaunchBridge=24,SmallLift=25,LargeLift=51,KnockbackDistance=60;
  readonly RoundMotionArt art;
  RoundMotionArt.Clip clip;
  RoundMotionArt.Pose bridge;
  Vector2 start,settle;
  float travelFrom,travelUntil;
  float bridgeTime, releaseAt=-1;
  Vector2 releaseOffset;
  bool chained;
  float EndOfHold=>Mathf.Min(clip.duration-1,bridgeTime+(clip.reactionHoldAt>0?clip.reactionHoldAt:clip.duration*.42f));
  internal float Elapsed { get; private set; }
  internal string Track => clip?.id;
  internal bool Active => clip != null && Elapsed < clip.duration;
  internal bool Defeated => Track == "defeat";

  internal DuelReaction(RoundMotionArt library) { art = library; }

  internal Vector2 Offset {
   get {
    if (clip == null) return Vector2.zero;
    if(releaseAt>=0)return Vector2.Lerp(releaseOffset,Vector2.zero,Mathf.SmoothStep(0,1,Mathf.InverseLerp(releaseAt,clip.duration,Elapsed)));
    if(Elapsed<travelUntil){
     float progress=Mathf.InverseLerp(travelFrom,travelUntil,Elapsed);
     float eased=travelFrom>0?progress*(2-progress):Mathf.SmoothStep(0,1,progress);
     return Vector2.Lerp(start,settle,eased);
    }
    if(chained||Defeated)return settle;
    return Vector2.Lerp(settle,Vector2.zero,Mathf.SmoothStep(0,1,Mathf.InverseLerp(EndOfHold,clip.duration,Elapsed)));
   }
  }

  internal RoundMotionArt.Pose Pose {
   get {
    if (clip==null||!Active&&!Defeated)return null;
    if(bridge!=null&&Elapsed<bridgeTime)return bridge;
    float at=Mathf.Max(0,Elapsed-bridgeTime);
    return art.At(clip,at);
   }
  }

  internal void Hit(string track,int side,bool chained,RoundMotionArt.Pose fromPose=null,float awayFromSource=0){
   Vector2 current = Offset;
   bridge = Pose ?? fromPose;
   bool launches=track=="smallFloat"||track=="largeFloat";
   bridgeTime=bridge==null?0:launches?LaunchBridge:PoseBridge;
   travelFrom=launches?bridgeTime:0;
   float sign=awayFromSource==0?(side==0?-1:1):Mathf.Sign(awayFromSource);
   start=current;
   clip=art[track];
   switch(track){
    case "knockback": settle=new Vector2(sign*KnockbackDistance,0); break;
    case "smallFloat": settle=new Vector2(0,-SmallLift); break;
    case "largeFloat": settle=new Vector2(0,-LargeLift); break;
    case "knockdown":
    case "defeat": settle=new Vector2(current.x,0); break;
    case "counterPrepare":
    case "gohanGuard": settle=current; break;
    default: settle=new Vector2(current.x+sign*4,current.y); break;
   }
   travelUntil=Mathf.Min(EndOfHold,track=="largeFloat"?220:164);
   this.chained=chained;
   releaseAt=-1;
   Elapsed = 0;
  }

  internal void ReleaseChain() {
   if(!chained)return;
   releaseOffset=Offset;
   releaseAt=Elapsed;
   chained=false;
  }

  internal void Advance(float milliseconds) {
   if (clip != null) Elapsed = Mathf.Min(chained?EndOfHold:clip.duration, Elapsed + milliseconds);
  }

  internal void Finish() {
   if (clip != null) Elapsed = chained?EndOfHold:clip.duration;
  }
 }
}
