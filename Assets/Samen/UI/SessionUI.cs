using Samen;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionUI : EditorWindow
{
    [MenuItem("Window/Samen/Session")]
    public static void ShowWindow()
    {
        GetWindow<ConnectionUI>("Samen Session");
    }

    private void OnGUI()
    {
        if(!SessionManager.InSessionScene())
        {
            EditorGUILayout.LabelField("You are not in a session.", EditorStyles.boldLabel);
            return;
        }

        if(GUILayout.Button("Export & Override"))
        {
            EditorUtility.DisplayProgressBar("Exporting...", "Exporting data...", 0.5f);
            // Clean up our stuff
            foreach (SamenNetworkObject samenNetworkObject in GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None))
            {
                DestroyImmediate(samenNetworkObject);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(activeScene);

            byte[] contents = File.ReadAllBytes("Assets/Samen/Session.unity");

            File.WriteAllBytes(SessionManager.CurrentDataPath, contents);
            File.WriteAllBytes("Assets/Samen/Backup", contents); // Just to be sure...

            EditorUtility.ClearProgressBar();

            EditorSceneManager.OpenScene(SessionManager.CurrentDataPath);
        }

    }
}
