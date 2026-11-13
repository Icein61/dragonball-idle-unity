using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DragonIdle.Editor {
 public static class BuildDemo {
  [MenuItem("Dragon Idle/Build Windows Demo")]
  public static void Build(){
   Directory.CreateDirectory("Assets/DragonIdle/Scenes");
   if(File.Exists("Assets/DragonIdle/Scenes/Demo.unity"))EditorSceneManager.OpenScene("Assets/DragonIdle/Scenes/Demo.unity");
   else {
    var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    var camera=new GameObject("Camera").AddComponent<Camera>();camera.backgroundColor=new Color(.03f,.05f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;
    new GameObject("Dragon Idle").AddComponent<DemoGame>();
    EditorSceneManager.SaveScene(scene,"Assets/DragonIdle/Scenes/Demo.unity");
   }
   PlayerSettings.companyName="Icein61";PlayerSettings.productName="Dragon Idle Demo";PlayerSettings.bundleVersion=Data.Version;
   PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
   PlayerSettings.runInBackground=true;PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/DragonIdle/Scenes/Demo.unity",true)};
   string projectRoot=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
   string output=Path.GetFullPath(Path.Combine(projectRoot,"Builds","Windows"));
   if(output!=Path.Combine(projectRoot,"Builds","Windows"))throw new Exception("Unsafe output path");
   if(Directory.Exists(output))Directory.Delete(output,true);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/DragonIdle/Scenes/Demo.unity"},locationPathName="Builds/Windows/DragonIdle.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
   Debug.Log("DRAGON_IDLE_BUILD_SUCCESS "+report.summary.totalSize);
  }
 }
}
