using UnityEditor;
using UnityEngine;
namespace DragonIdle.Editor {
 public sealed class ShellArtImport:AssetPostprocessor {
  void OnPreprocessTexture(){
   if(!assetPath.Contains("/Interface/V070/"))return;
   var t=(TextureImporter)assetImporter;bool scene=assetPath.EndsWith("-scene.png");
   t.textureType=scene?TextureImporterType.Default:TextureImporterType.Sprite;
   if(!scene){t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
    t.spriteBorder=assetPath.Contains("button-")?new Vector4(30,12,30,12):Vector4.zero;}
   t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
   t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;t.isReadable=false;
  }
 }
}
