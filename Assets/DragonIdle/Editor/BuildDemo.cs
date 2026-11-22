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
   const string atlasFolder="Assets/DragonIdle/Resources/Art/V050";
   if(Directory.Exists(atlasFolder)&&Directory.EnumerateFiles(atlasFolder,"*-atlas.png").Any(path=>
      !Data.IsRoundCard(Path.GetFileName(path).Replace("-atlas.png",""))))
    throw new Exception("Use Tools/build-windows.ps1 to exclude offline card assets.");
   if(Directory.Exists("Assets/DragonIdle/Resources/Videos")&&
      Directory.EnumerateFiles("Assets/DragonIdle/Resources/Videos","*.mp4",SearchOption.AllDirectories).Any())
    throw new Exception("Use Tools/build-windows.ps1 when authoring videos are present in Resources.");
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
   var args=Environment.GetCommandLineArgs();
   if(args.Contains("-dragonStagedReferences")){
    var project=new DirectoryInfo(projectRoot);
    if(!project.Name.StartsWith("unity-player-",StringComparison.Ordinal)||project.Parent?.Name!=".validation"||project.Parent.Parent==null)
     throw new Exception("Unexpected staged project path");
    projectRoot=project.Parent.Parent.FullName;
   }
   int folderIndex=Array.IndexOf(args,"-dragonBuildFolder");
   string folder=folderIndex<0?"Windows":folderIndex+1<args.Length?args[folderIndex+1]:"";
   if(!System.Text.RegularExpressions.Regex.IsMatch(folder,@"\AWindows(?:-[A-Za-z0-9._]+)?\z"))
    throw new Exception("Invalid Windows build folder");
   string buildRoot=Path.GetFullPath(Path.Combine(projectRoot,"Builds"));
   string output=Path.GetFullPath(Path.Combine(buildRoot,folder));
   if(Path.GetDirectoryName(output)!=buildRoot||
      Directory.Exists(buildRoot)&&(File.GetAttributes(buildRoot)&FileAttributes.ReparsePoint)!=0||
      Directory.Exists(output)&&(File.GetAttributes(output)&FileAttributes.ReparsePoint)!=0)
    throw new Exception("Unsafe output path");
   if(Directory.Exists(output))Directory.Delete(output,true);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/DragonIdle/Scenes/Demo.unity"},locationPathName=Path.Combine(output,Data.GameTitle+".exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
   Debug.Log("DRAGON_IDLE_BUILD_SUCCESS "+report.summary.totalSize);
  }
 }
}
