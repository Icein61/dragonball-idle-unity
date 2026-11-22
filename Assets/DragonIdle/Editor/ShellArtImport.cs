using UnityEditor;
using UnityEngine;
namespace DragonIdle.Editor {
 public sealed class ShellArtImport:AssetPostprocessor {
  void OnPreprocessTexture(){
   bool badge=assetPath.Contains("/Interface/Rarity/");
   bool backdrop=assetPath.Contains("/Interface/Gallery/Rarity/");
   bool role=assetPath.Contains("/Interface/Role/");
   if(!badge&&!backdrop&&!role)return;
   var t=(TextureImporter)assetImporter;
   t.textureType=badge?TextureImporterType.Sprite:TextureImporterType.Default;
   if(badge){
    t.spriteImportMode=SpriteImportMode.Single;
    t.spritePixelsPerUnit=100;t.spriteBorder=Vector4.zero;
   }
   t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
   t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=badge||role?2048:512;t.textureCompression=TextureImporterCompression.Uncompressed;t.isReadable=false;
  }
 }
}
