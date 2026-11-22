using UnityEditor;
namespace DragonIdle.Editor {
 public class CharacterArtImport:AssetPostprocessor {
  public override uint GetVersion(){return 2;}
  void OnPreprocessTexture(){
   bool roundGoku=(assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V013",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0142/Goku/",System.StringComparison.Ordinal))&&assetPath.Contains("/Goku/");
   bool roundVegeta=assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0140/Vegeta/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0141/Vegeta/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0142/Vegeta/",System.StringComparison.Ordinal);
   bool recent=assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0143/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0144/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0145/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0145/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0150/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0151/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0152/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0153/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0154/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0161/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0162/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0172/Yamcha/",System.StringComparison.Ordinal)||
    assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0173/Tien/",System.StringComparison.Ordinal);
   bool battlefield=assetPath.StartsWith("Assets/DragonIdle/Resources/Art/V0141/Battle/",System.StringComparison.Ordinal);
   if(!recent&&!battlefield&&!roundGoku&&!roundVegeta&&!assetPath.Contains("/Art/V050/")&&!assetPath.Contains("/Art/V051/")&&!assetPath.Contains("/Art/CharaOriImg/"))return;
   var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;
   t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=UnityEngine.TextureWrapMode.Clamp;t.filterMode=UnityEngine.FilterMode.Bilinear;
   t.maxTextureSize=roundVegeta||recent?4096:2048;t.textureCompression=battlefield?TextureImporterCompression.CompressedHQ:TextureImporterCompression.Uncompressed;t.isReadable=false;
  }
 }
}
