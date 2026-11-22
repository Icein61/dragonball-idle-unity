using UnityEngine;
using UnityEngine.UI;

namespace DragonIdle {
 public partial class DemoGame {
  Texture2D roleIconAtlas;
  RoleIconAtlas roleIcons;
  void DrawCharacterRole(Transform parent,Card card,float x,float y,float size){
   if(roleIconAtlas==null){
    roleIconAtlas=Resources.Load<Texture2D>("Interface/Role/V0173/roles");
    var manifest=Resources.Load<TextAsset>("Interface/Role/V0173/regions");
    if(roleIconAtlas==null||manifest==null)throw new System.InvalidOperationException("角色定位图标资源缺失");
    roleIcons=JsonUtility.FromJson<RoleIconAtlas>(manifest.text);
    if(roleIcons.tiles==null||roleIcons.tiles.Length!=3||roleIcons.sourceWidth<=0||roleIcons.sourceHeight<=0)
     throw new System.InvalidOperationException("角色定位图标区域无效");
   }
   int index=card.Type==CharacterType.Output?0:card.Type==CharacterType.Defense?1:2;
   var region=roleIcons.tiles[index];
   float scale=size/Mathf.Max(region.width,region.height);
   float width=region.width*scale,height=region.height*scale;
   var graphic=UNode(parent,"CharacterRoleIcon",x+(size-width)/2,y+(size-height)/2,width,height).gameObject.AddComponent<RawImage>();
   graphic.texture=roleIconAtlas;
   graphic.uvRect=new Rect(region.x/roleIcons.sourceWidth,1-(region.y+region.height)/roleIcons.sourceHeight,
    region.width/roleIcons.sourceWidth,region.height/roleIcons.sourceHeight);
   graphic.raycastTarget=false;
  }

  [System.Serializable] sealed class RoleIconRegion {public float x,y,width,height;}
  [System.Serializable] sealed class RoleIconAtlas {
   public float sourceWidth,sourceHeight;
   public RoleIconRegion[] tiles;
  }
 }
}
