using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonIdle {
 public partial class DemoGame {
  IReadOnlyList<RoundMotionFx.VolleyOrb> BuildVolleyVisuals(RoundMotionPlayback run,Vector2 ground,float scale,Func<RoundAction,Vector2> target,Func<RoundAction,Vector2> targetGround){
   var visible=new List<RoundMotionFx.VolleyOrb>();
   int streams=run.IsRaditzTwin?2:run.VolleyHits.Length;
   for(int i=0;i<streams;i++){
    float launch=run.VolleyLaunch(i);
    float impact=run.VolleyImpact(i);
    float age=run.Time-launch;
    if(age<0||run.Time>impact+185)continue;
    var pose=run.Library.At(run.Clip,run.PoseTimeAt(launch));
    var start=RoundMotionArt.Anchor(pose,pose.beam,ground+(run.IsScatter?run.ShiftAt(launch):Vector2.zero),scale);
    if(run.IsRaditzTwin)start=RoundMotionFx.RaditzHand(start,scale,i);
    var hit=run.VolleyHits[Math.Min(i,run.VolleyHits.Length-1)];
    var end=target(hit);
    if(run.IsRaditzTwin&&i>=run.VolleyHits.Length)end+=new Vector2(0,8);
    visible.Add(new RoundMotionFx.VolleyOrb{Start=start,End=end,Ground=targetGround(hit),Elapsed=age,Flight=impact-launch,
     Miss=hit.Dodged,Scatter=run.IsScatter,Raditz=run.IsRaditzEnergy,TwinRay=run.IsRaditzTwin,
     Enhanced=run.Action.EnhancedBasic,Index=i});
   }
   return visible;
  }
 }
}
