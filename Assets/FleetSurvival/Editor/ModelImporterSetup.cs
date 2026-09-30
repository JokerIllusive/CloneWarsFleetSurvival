using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace FleetSurvival.Editor
{
    public static class ModelImporterSetup
    {
        static AddAndRemoveRequest request;
        static double deadline;
        public static void Install()
        {
            request=Client.AddAndRemove(new[]{"com.unity.cloud.gltfast@6.18.0"},new string[0]);
            deadline=EditorApplication.timeSinceStartup+300;
            EditorApplication.update+=Poll;
        }
        static void Poll()
        {
            if(request==null) return;
            if(!request.IsCompleted)
            {
                if(EditorApplication.timeSinceStartup<deadline) return;
                Debug.LogError("glTF importer installation timed out."); EditorApplication.Exit(2); return;
            }
            EditorApplication.update-=Poll;
            if(request.Status==StatusCode.Success) { Debug.Log("GLTF_IMPORTER_READY"); EditorApplication.Exit(0); }
            else { Debug.LogError(request.Error.message); EditorApplication.Exit(1); }
        }
    }
}
