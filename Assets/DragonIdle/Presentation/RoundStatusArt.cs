using System.Collections.Generic;
using UnityEngine;

namespace DragonIdle {
 internal static class RoundStatusArt {
  static readonly Dictionary<RoundStatusIcon,Sprite> sprites=new Dictionary<RoundStatusIcon,Sprite>();
  static readonly Vector2[] Shield={
   new Vector2(16,25),new Vector2(24,22),new Vector2(23,13),new Vector2(16,7),
   new Vector2(9,13),new Vector2(8,22),new Vector2(16,25)
  };

  internal static Sprite Icon(RoundStatusIcon kind){
   if(sprites.TryGetValue(kind,out var saved))return saved;
   const int size=32;
   var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Buff "+kind,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
   var background=new Color32(25,56,55,255);
   Color glyph;
   switch(kind){
    case RoundStatusIcon.DefenseUp: glyph=new Color32(154,249,208,255);break;
    case RoundStatusIcon.DefenseDown: glyph=new Color32(255,168,131,255);break;
    default:glyph=new Color32(169,219,250,255);break;
   }
   var pixels=new Color32[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    var p=new Vector2(x+.5f,y+.5f);
    float rx=Mathf.Max(Mathf.Abs(p.x-16)-10,0),ry=Mathf.Max(Mathf.Abs(p.y-16)-10,0);
    float margin=5.5f-Mathf.Sqrt(rx*rx+ry*ry);
    float inner=GlyphDistance(kind,p);
    var color=Color.Lerp(background,glyph,Mathf.Max(Mathf.Clamp01(2.15f-inner),
     .64f*Mathf.Clamp01(1.5f-margin)));
    color.a=Mathf.Clamp01(margin+.5f);
    pixels[y*size+x]=color;
   }
   texture.SetPixels32(pixels);
   texture.Apply(false,true);
   var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f));
   sprites.Add(kind,sprite);
   return sprite;
  }

  static float GlyphDistance(RoundStatusIcon kind,Vector2 p){
   if(kind==RoundStatusIcon.DodgeUp){
    float sweep=SegmentDistance(p,new Vector2(7,10),new Vector2(18,22));
    return Mathf.Min(sweep,Mathf.Min(SegmentDistance(p,new Vector2(11,10),new Vector2(22,16)),
     SegmentDistance(p,new Vector2(11,6),new Vector2(22,12))));
   }
   float shell=PathDistance(Shield,p);
   float horizontal=SegmentDistance(p,new Vector2(13,16),new Vector2(19,16));
   if(kind==RoundStatusIcon.DefenseUp)
    horizontal=Mathf.Min(horizontal,SegmentDistance(p,new Vector2(16,13),new Vector2(16,19)));
   return Mathf.Min(shell,horizontal);
  }

  static float PathDistance(Vector2[] path,Vector2 point){
   float nearest=float.PositiveInfinity;
   for(int i=1;i<path.Length;i++)nearest=Mathf.Min(nearest,SegmentDistance(point,path[i-1],path[i]));
   return nearest;
  }

  static float SegmentDistance(Vector2 point,Vector2 from,Vector2 to){
   Vector2 edge=to-from;
   return (point-from-edge*Mathf.Clamp01(Vector2.Dot(point-from,edge)/edge.sqrMagnitude)).magnitude;
  }
 }
}
