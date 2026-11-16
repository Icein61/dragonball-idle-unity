using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DragonIdle.Editor {
 public static class BuildDemo {
  [MenuItem("龙珠爆裂激斗/Build Windows Demo")]
  public static void Build(){
   Directory.CreateDirectory("Assets/DragonIdle/Scenes");
   if(File.Exists("Assets/DragonIdle/Scenes/Demo.unity"))EditorSceneManager.OpenScene("Assets/DragonIdle/Scenes/Demo.unity");
   else {
    var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    var camera=new GameObject("Camera").AddComponent<Camera>();camera.backgroundColor=new Color(.03f,.05f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;
    new GameObject("Dragon Idle").AddComponent<DemoGame>();
    EditorSceneManager.SaveScene(scene,"Assets/DragonIdle/Scenes/Demo.unity");
   }
   PlayerSettings.companyName="Icein61";PlayerSettings.productName=Data.GameTitle;PlayerSettings.bundleVersion=Data.Version;
   string iconPath="Assets/DragonIdle/Branding/goku-kame-island-icon-v052-reference-v2.png";
   AssetDatabase.ImportAsset(iconPath,ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(iconPath);
   if(importer==null)throw new Exception("Game icon missing");
   importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
   var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
   if(icon==null||icon.width!=1024||icon.height!=1024)throw new Exception("Expected 1024-square game icon");
   foreach(var group in new[]{BuildTargetGroup.Unknown,BuildTargetGroup.Standalone}){
    var sizes=PlayerSettings.GetIconSizesForTargetGroup(group);
    if(sizes.Length==0)throw new Exception("No application icon slots for "+group);
    PlayerSettings.SetIconsForTargetGroup(group,sizes.Select(size=>icon).ToArray());
    if(!PlayerSettings.GetIconsForTargetGroup(group).All(texture=>texture==icon))throw new Exception("Game icon binding failed");
   }
   PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
   PlayerSettings.runInBackground=true;PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/DragonIdle/Scenes/Demo.unity",true)};
   string projectRoot=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
   string output=Path.GetFullPath(Path.Combine(projectRoot,"Builds","Windows"));
   if(output!=Path.Combine(projectRoot,"Builds","Windows"))throw new Exception("Unsafe output path");
   if(Directory.Exists(output))Directory.Delete(output,true);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/DragonIdle/Scenes/Demo.unity"},locationPathName="Builds/Windows/"+Data.GameTitle+".exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
   Debug.Log("DRAGON_IDLE_BUILD_SUCCESS "+report.summary.totalSize);
  }
 }
}
