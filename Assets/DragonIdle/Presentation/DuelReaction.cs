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
  float duration,releasePoseTime;
  float SettleAt=>bridgeTime+(duration-bridgeTime)*.57f;
  float PoseTime {
   get {
    float at=Mathf.InverseLerp(bridgeTime,duration,Elapsed)*(clip.duration-1);
    return chained&&clip.holdChainedPose?Mathf.Min(at,clip.reactionHoldAt):at;
   }
  }
  internal float ChainRemaining=>chained?Mathf.Max(0,duration-Elapsed):0;
  internal Vector2 ContactOffset=>chained?settle:Offset;
  internal static float FixedDuration(string track,float authored){
   switch(track){
    case "standing":return 370;
    case "smallFloat":return 530;
    case "largeFloat":return 640;
    case "knockback":return 580;
    case "knockdown":return 620;
    default:return authored;
   }
  }
  internal float Elapsed { get; private set; }
  internal string Track => clip?.id;
  internal bool Active => clip != null && (chained || Elapsed < duration);
  internal bool Defeated => Track == "defeat";

  internal DuelReaction(RoundMotionArt library) { art = library; }

  internal Vector2 Offset {
   get {
    if (clip == null) return Vector2.zero;
    if(releaseAt>=0)return Vector2.Lerp(releaseOffset,Vector2.zero,Mathf.SmoothStep(0,1,Mathf.InverseLerp(releaseAt,duration,Elapsed)));
    if(Elapsed<travelUntil){
     float progress=Mathf.InverseLerp(travelFrom,travelUntil,Elapsed);
     float eased=travelFrom>0?progress*(2-progress):Mathf.SmoothStep(0,1,progress);
     return Vector2.Lerp(start,settle,eased);
    }
    if(chained||Defeated)return settle;
    return Vector2.Lerp(settle,Vector2.zero,Mathf.SmoothStep(0,1,Mathf.InverseLerp(SettleAt,duration,Elapsed)));
   }
  }

  internal RoundMotionArt.Pose Pose {
   get {
    if (clip==null||!Active&&!Defeated)return null;
    if(bridge!=null&&Elapsed<bridgeTime)return bridge;
    float at=releaseAt>=0?Mathf.Lerp(releasePoseTime,clip.duration-1,Mathf.InverseLerp(releaseAt,duration,Elapsed)):PoseTime;
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
   duration=FixedDuration(track,clip.duration);
   if(chained&&track=="standing")duration=530;
   travelUntil=chained?duration:Mathf.Min(SettleAt,track=="largeFloat"?220:164);
   this.chained=chained;
   releaseAt=-1;
   Elapsed = 0;
  }

  internal void ReleaseChain() {
   if(!chained)return;
   releaseOffset=Offset;
   releasePoseTime=PoseTime;
   releaseAt=Elapsed;
   duration=releaseAt+180;
   chained=false;
  }

  internal void Advance(float milliseconds) {
   if (clip != null) Elapsed = Mathf.Min(duration,Elapsed + milliseconds);
  }

  internal void Finish() {
   if (clip != null) Elapsed = duration;
  }
 }
}
