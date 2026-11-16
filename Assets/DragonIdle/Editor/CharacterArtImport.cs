using UnityEditor;
namespace DragonIdle.Editor {
 public class CharacterArtImport:AssetPostprocessor {
  void OnPreprocessTexture(){
   if(!assetPath.Contains("/Art/V050/")&&!assetPath.Contains("/Art/V051/"))return;
   var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;
   t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=UnityEngine.TextureWrapMode.Clamp;t.filterMode=UnityEngine.FilterMode.Bilinear;
   t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;t.isReadable=false;
  }
 }
}
