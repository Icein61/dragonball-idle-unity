using System;
using System.Collections.Generic;
using UnityEngine;
namespace DragonIdle {
 [Serializable] public class ArtRegion {public int x,y,w,h;}
 [Serializable] public class ArtRegions {public int width,height;public ArtRegion[] frames;}
 public partial class DemoGame {
  readonly Dictionary<string,ArtRegions> artRegions=new Dictionary<string,ArtRegions>();
  ArtRegions Regions(int index){
   string id=Data.Cards[index].Id;
   if(!artRegions.ContainsKey(id)){
    var json=Resources.Load<TextAsset>("Art/V050/"+id+"-frames");
    if(json==null)throw new InvalidOperationException("Missing character regions: "+id);
    artRegions[id]=JsonUtility.FromJson<ArtRegions>(json.text);
   }
   return artRegions[id];
  }
  Rect ArtUV(int index,int region,bool mirror=false){
   var sheet=Regions(index);var r=sheet.frames[region];
   return new Rect((mirror?r.x+r.w:r.x)/(float)sheet.width,1-(r.y+r.h)/(float)sheet.height,(mirror?-r.w:r.w)/(float)sheet.width,r.h/(float)sheet.height);
  }
  float PortraitAspect(int index){var art=PortraitTexture(index);return art==null?2f/3:art.width/(float)art.height;}
  void PaintPortrait(int index,Rect rect){
   var art=PortraitTexture(index);if(art==null)return;
   float w=Mathf.Min(rect.width,rect.height*PortraitAspect(index)),h=w/PortraitAspect(index);
   GUI.DrawTexture(new Rect(rect.x+(rect.width-w)/2,rect.y+(rect.height-h)/2,w,h),art,ScaleMode.ScaleToFit,true);
  }
  float FighterHeight(int index){string id=Data.Cards[index].Id;return id=="gohan-test"||id=="chiaotzu-saiyan-v020"?72:id=="krillin-test"||id=="saibaman-saiyan-v030"?82:id=="nappa-invasion-v030"?108:96;}
  float FighterScale(int index){var sheet=Regions(index);int tallest=0;for(int i=1;i<13;i++)tallest=Math.Max(tallest,sheet.frames[i].h);return Mathf.Min(FighterHeight(index)/sheet.frames[1].h,122f/tallest);}
  void PaintFighter(int index,int pose,Vector2 feet,bool mirror,Color tint,float size=1){
   var art=FighterTexture(index);if(art==null)return;
   var sheet=Regions(index);var r=sheet.frames[1+pose];float scale=FighterScale(index)*size;
   float w=r.w*scale,h=r.h*scale;
   GUI.color=tint;GUI.DrawTextureWithTexCoords(new Rect(feet.x-w/2,feet.y-h,w,h),art,ArtUV(index,1+pose,mirror),true);GUI.color=Color.white;
  }
 }
}
