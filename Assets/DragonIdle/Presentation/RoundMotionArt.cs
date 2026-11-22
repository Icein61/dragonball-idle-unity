using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 internal sealed class RoundMotionArt {
  internal const string CardId = "goku-test",VegetaCardId="vegeta-test",PiccoloCardId="piccolo-test",GohanCardId="gohan-test",KrillinCardId="krillin-test",RaditzCardId=Data.RaditzId,YamchaCardId=Data.YamchaId,TienCardId=Data.TienId;
  internal const string OpeningSkill = "修炼的成果";
  const string Folder = "Art/V0161/Goku/";
  const string Icon = "Art/V0131/Goku/passive-training";

  [Serializable] internal sealed class Pose {
   public string image;
   public int[] crop, pivot, hit, beam;
   public float zoom;
   [NonSerialized] public Texture2D Texture;
  }

  [Serializable] internal sealed class Cue {
   public int at, drawing;
   public string kind;
  }

  [Serializable] internal sealed class RootKey {
   public int at;
   public float x;
  }

  [Serializable] internal sealed class Beam {
   public int gather, compress, push, fire, flight, cut, extinguish;
  }

  [Serializable] internal sealed class Clip {
   public string id, title, fx;
   public int duration, impactAt, haloFrom, haloTo, returnAt, launchAt, depthAlignAt;
   public int runUntil,hopDrawing,approachDrawing,reactionHoldAt;
   public int[] runFrames;
   public bool fullStride;
   public float returnHop;
   public Cue[] poses, impacts;
   public RootKey[] root;
   public Beam beam;

   internal int KeyAt(float time) {
    int key = poses.Length - 1;
    while (key > 0 && time < poses[key].at) key--;
    return key;
   }

   internal Vector2 PlanarShift(float time,Vector2 travel,bool locomotion=false) {
    float depth=0;
    if(beam!=null&&beam.gather>0){
     depth=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,beam.gather,time))*
      (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(beam.cut,duration,time)));
    }else if(travel.y!=0&&impacts.Length>0&&returnAt>0){
     float first=impacts[0].at;
     float alignment=depthAlignAt>0?depthAlignAt:first;
     depth=time<first?Mathf.SmoothStep(0,1,Mathf.InverseLerp(40,alignment,time)):
      1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(returnAt,root[root.Length-1].at,time));
    }
    float hop=locomotion&&returnHop>0?Mathf.Sin(Mathf.PI*ReturnPhase(time))*returnHop:0;
    return new Vector2(Travel(time)*travel.x,depth*travel.y-hop);
   }

   internal float ReturnPhase(float time) {
    return returnAt>0&&root.Length>0?Mathf.InverseLerp(returnAt,root[root.Length-1].at,time):0;
   }

   internal float Travel(float time) {
    if (root.Length == 0) return 0;
    for (int i = 1; i < root.Length; i++) {
     if (time >= root[i].at) continue;
     var from = root[i - 1];
     var to = root[i];
     return Mathf.SmoothStep(from.x, to.x, Mathf.InverseLerp(from.at, to.at, time));
    }
    return root[root.Length - 1].x;
   }
  }

  [Serializable] sealed class Manifest {
   public Pose[] atlas;
   public Clip[] clips;
  }

  readonly Manifest data;
  readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
  internal Pose[] Atlas => data.atlas;
  internal Clip this[string id] => clips[id];

  RoundMotionArt(Manifest manifest,string folder) {
   data = manifest;
   var textures = new Dictionary<string, Texture2D>();
   foreach (var pose in data.atlas) {
    if (!textures.TryGetValue(pose.image, out var texture)) {
     texture = Resources.Load<Texture2D>(pose.image.StartsWith("Art/",StringComparison.Ordinal)?pose.image:folder+pose.image);
     textures.Add(pose.image, texture);
    }
    var area = pose.crop;
    if (texture == null || area.Length != 4 || area[0] < 0 || area[1] < 0 ||
        area[2] <= 0 || area[3] <= 0 || area[0] + area[2] > texture.width ||
        area[1] + area[3] > texture.height)
     throw new InvalidOperationException("Invalid motion atlas region: " + pose.image);
    pose.Texture = texture;
   }
   foreach (var clip in data.clips) {
    if(clip.runUntil>0&&(clip.runFrames==null||clip.runFrames.Length<2||clip.impacts.Length==0||
       clip.runUntil>clip.impacts[0].at))
     throw new InvalidOperationException("Run must resolve before first impact: "+clip.id);
    if(clip.id=="ultimate"||(clip.beam!=null&&clip.beam.gather>0))ValidateBeam(clip);
    clips.Add(clip.id,clip);
   }
  }

  void ValidateBeam(Clip clip) {
   var beam=clip.beam;
   if(beam==null||beam.gather<=0||beam.gather>=beam.compress||beam.compress>=beam.push||
      beam.push>=beam.fire||beam.flight<=0||beam.fire+beam.flight!=clip.impactAt||
      clip.impactAt>=beam.cut||beam.cut>=beam.extinguish||beam.extinguish>clip.duration)
    throw new InvalidOperationException("Invalid beam timing: "+clip.id);
   for(int key=clip.KeyAt(beam.gather);key<=clip.KeyAt(beam.fire);key++){
    var anchor=data.atlas[clip.poses[key].drawing].beam;
    if(anchor==null||anchor.Length!=2)throw new InvalidOperationException("Missing beam anchor: "+clip.poses[key].at);
   }
  }

  internal static RoundMotionArt Load(string folder=Folder,string resource="Motion") {
   var manifest = Resources.Load<TextAsset>(folder + resource);
   if (manifest == null) throw new InvalidOperationException("Missing motion manifest: "+folder+resource);
   return new RoundMotionArt(JsonUtility.FromJson<Manifest>(manifest.text),folder);
  }

  internal static Texture2D LoadOpeningIcon() => Resources.Load<Texture2D>(Icon);

  internal Pose At(Clip clip, float time) => data.atlas[clip.poses[clip.KeyAt(time)].drawing];

  internal static void Draw(RawImage image, Pose pose, Vector2 ground, float scale, bool mirror) {
   var rect = pose.crop;
   var t = pose.Texture;
   scale *= pose.zoom;
   float left = (mirror ? pose.pivot[0] - rect[0] - rect[2] : rect[0] - pose.pivot[0]) * scale;
   image.rectTransform.anchoredPosition = new Vector2(ground.x + left, -ground.y + (pose.pivot[1] - rect[1]) * scale);
   image.rectTransform.sizeDelta = new Vector2(rect[2] * scale, rect[3] * scale);
   image.texture = t;
   // Unity texture UVs start at the bottom. Mirroring reverses U within the same crop.
   image.uvRect = new Rect((rect[0] + (mirror ? rect[2] : 0)) / (float)t.width,
    1 - (rect[1] + rect[3]) / (float)t.height, (mirror ? -1 : 1) * rect[2] / (float)t.width,
    rect[3] / (float)t.height);
  }

  internal static Vector2 Anchor(Pose pose, int[] point, Vector2 ground, float scale) {
   return ground + new Vector2(point[0] - pose.pivot[0], point[1] - pose.pivot[1]) * (scale * pose.zoom);
  }
 }
}
