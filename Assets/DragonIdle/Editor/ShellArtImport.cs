using UnityEditor;
using UnityEngine;
namespace DragonIdle.Editor {
 public sealed class ShellArtImport:AssetPostprocessor {
  void OnPreprocessTexture(){
   if(!assetPath.Contains("/Interface/Rarity/"))return;
   var t=(TextureImporter)assetImporter;
   t.textureType=TextureImporterType.Sprite;
   t.spriteImportMode=SpriteImportMode.Single;
   t.spritePixelsPerUnit=100;
   t.spriteBorder=Vector4.zero;
   t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
   t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;t.isReadable=false;
  }
 }
}
