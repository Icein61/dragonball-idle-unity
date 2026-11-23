using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  Texture2D roleIconAtlas;
  RoleIconAtlas roleIcons;
  readonly TextGenerator roleTextGenerator=new TextGenerator();
  void DrawCharacterRoleNextTo(Text label,Card card){
   var rect=label.rectTransform;
   Rect line=RoleTextInkBounds(label);
   float x=rect.anchoredPosition.x+Mathf.Min(rect.rect.width,Mathf.Ceil(line.xMax))+10;
   DrawCharacterRole(rect.parent,card,x,-rect.anchoredPosition.y-line.yMax,line.height);
  }

  Rect RoleTextInkBounds(Text label){
   var rect=label.rectTransform.rect;
   roleTextGenerator.Populate(label.text,label.GetGenerationSettings(rect.size));
   var vertices=roleTextGenerator.verts;
   float bottom=float.PositiveInfinity,top=float.NegativeInfinity,right=0;
   for(int i=0;i+3<vertices.Count;i+=4){
    var first=vertices[i].position;
    float low=first.y,high=low,end=first.x;
    for(int j=1;j<4;j++){
     var point=vertices[i+j].position;
     low=Mathf.Min(low,point.y);high=Mathf.Max(high,point.y);
     end=Mathf.Max(end,point.x);
    }
    if(high<=low)continue;
    bottom=Mathf.Min(bottom,low);top=Mathf.Max(top,high);right=Mathf.Max(right,end);
   }
   if(top<=bottom)
    return new Rect(0,rect.center.y-label.fontSize*.5f,0,label.fontSize);
   float unit=label.pixelsPerUnit;
   float height=Mathf.Min(label.fontSize,(top-bottom)/unit);
   // Legacy Text quads include font padding above the painted title.
   float center=(top+bottom)/(2*unit)-label.fontSize*.13f;
   return new Rect(0,center-height*.5f,right/unit,height);
  }

  float DrawCharacterRole(Transform parent,Card card,float x,float y,float height){
   if(roleIconAtlas==null){
    roleIconAtlas=Resources.Load<Texture2D>("Interface/Role/V0174/roles");
    var manifest=Resources.Load<TextAsset>("Interface/Role/V0174/regions");
    if(roleIconAtlas==null||manifest==null)throw new System.InvalidOperationException("角色定位图标资源缺失");
    roleIcons=JsonUtility.FromJson<RoleIconAtlas>(manifest.text);
    if(roleIcons.tiles==null||roleIcons.tiles.Length!=3||roleIcons.sourceWidth<=0||roleIcons.sourceHeight<=0)
     throw new System.InvalidOperationException("角色定位图标区域无效");
   }
   int index=card.Type==CharacterType.Output?0:card.Type==CharacterType.Defense?1:2;
   var region=roleIcons.tiles[index];
   float width=region.width*height/region.height;
   var graphic=UNode(parent,"CharacterRoleIcon",x,y,width,height).gameObject.AddComponent<RawImage>();
   graphic.texture=roleIconAtlas;
   graphic.uvRect=new Rect(region.x/roleIcons.sourceWidth,1-(region.y+region.height)/roleIcons.sourceHeight,
    region.width/roleIcons.sourceWidth,region.height/roleIcons.sourceHeight);
   graphic.raycastTarget=false;
   return width;
  }

  [System.Serializable] sealed class RoleIconRegion {public float x,y,width,height;}
  [System.Serializable] sealed class RoleIconAtlas {
   public float sourceWidth,sourceHeight;
   public RoleIconRegion[] tiles;
  }
 }
}
