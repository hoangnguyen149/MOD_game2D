using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
public static class BuildGame
{
    [MenuItem("Owned Game/Create Bootstrap Scene")]
    public static void Scene(){
        Directory.CreateDirectory("Assets/Scenes");
        if(!File.Exists("Assets/Scenes/Bootstrap.unity")){
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Bootstrap.unity");
        }
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity",true)};
    }
    public static void WindowsRelease(){Build(false);}
    public static void WindowsDevelopment(){Build(true);}
    static void Build(bool dev){
        Scene();PlayerSettings.companyName="OwnedLocal";PlayerSettings.productName="OwnedAdventure";
        // Explicit loopback mock HTTP; GameApi rejects every other destination.
        PlayerSettings.insecureHttpOption=InsecureHttpOption.AlwaysAllowed;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.IL2CPP);
        var output=dev?"../artifacts/Client-Development":"../artifacts/Client";
        Directory.CreateDirectory(output);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/Scenes/Bootstrap.unity"},locationPathName=Path.Combine(output,"OwnedAdventure.exe"),
            target=BuildTarget.StandaloneWindows64,options=dev?BuildOptions.Development:BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        Directory.CreateDirectory(Path.Combine(output,"Data"));
        File.Copy("Assets/StreamingAssets/servers.json",Path.Combine(output,"Data","servers.json"),true);
    }
}
