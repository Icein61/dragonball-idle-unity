using UnityEngine;

namespace DragonIdle {
 internal sealed class DuelReaction {
  const float PoseBridge = 55;
  readonly RoundMotionArt art;
  RoundMotionArt.Clip clip;
  RoundMotionArt.Pose bridge;
  Vector2 start, settle, impulse;
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
    float p = Mathf.Clamp01(Elapsed / clip.duration);
    // A sharp displacement followed by a softer recovery. Both ends keep continuous positions.
    float kick = p < .32f ? Mathf.SmoothStep(0, 1, p / .32f) : Mathf.SmoothStep(1, 0, (p - .32f) / .68f);
    var position=Vector2.Lerp(start, settle, Mathf.SmoothStep(0, 1, p)) + impulse * kick;
    return new Vector2(Mathf.Clamp(position.x,-12,12),Mathf.Max(-22,position.y));
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

  internal void Hit(string track, int side, bool chained, RoundMotionArt.Pose fromPose = null) {
   Vector2 current = Offset;
   bridge = Pose ?? fromPose;
   bridgeTime = bridge == null ? 0 : PoseBridge;
   float sign = side == 0 ? -1 : 1;
   start = current;
   settle = new Vector2(chained ? sign * 4 : track == "defeat" ? current.x : 0, 0);
   switch (track) {
    case "knockback": impulse = new Vector2(sign * 10, 0); break;
    case "smallFloat": impulse = new Vector2(sign * 4, -9); break;
    case "largeFloat": impulse = new Vector2(sign * 7, -19); break;
    case "knockdown": impulse = new Vector2(sign * 7, 0); break;
    case "counterPrepare":
    case "gohanGuard": impulse = Vector2.zero; break;
    case "defeat": impulse = new Vector2(sign * 3, 0); break;
    default: impulse = new Vector2(sign * 4, 0); break;
   }
   clip = art[track];
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
