using Samen;
using System.IO;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;
using UnityEngine.SceneManagement;
public class SceneSettings
{

    /// <summary>
    /// Check if the Create Session option should appear
    /// </summary>
    /// <returns></returns>
    [MenuItem("Assets/Samen/Create Session", true, 200000)]
    private static bool ShouldShowCreateSessionOption()
    {
        if (Connection.GetConnection() == null)
        {
            return false;
        }

        Object selected = Selection.activeObject;

        if (selected == null)
            return false;

        string path = AssetDatabase.GetAssetPath(selected);

        (bool success, bool result) result = SessionManager.HasActiveSession(path);

        if (!result.success)
        {
            return false;
        }

        return Path.GetExtension(path) == ".unity" && !result.result;
    }

    /// <summary>
    /// Check if the join session button should appear
    /// </summary>
    /// <returns></returns>
    [MenuItem("Assets/Samen/Join Session", true, 200000)]
    private static bool ShouldShowJoinSessionOption()
    {
        if (Connection.GetConnection() == null)
        {
            return false;
        }

        Object selected = Selection.activeObject;

        if (selected == null)
            return false;

        string path = AssetDatabase.GetAssetPath(selected);

        (bool success, bool result) result = SessionManager.HasActiveSession(path);

        if (!result.success)
        {
            return false;
        }

        return Path.GetExtension(path) == ".unity" && result.result;
    }


    /// <summary>
    /// Create Session option
    /// </summary>
    [MenuItem("Assets/Samen/Create Session", priority = 200000)]
    private static void CreateSessionOption()
    {
        EditorUtility.DisplayProgressBar("Creating Session...", "Preparing scene...", 0f);

        // Get path of selected scene asset
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity"))
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Error", "Please select a valid scene asset.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        EditorUtility.DisplayProgressBar("Creating Session...", "Adding network components...", 0.3f);

        foreach (var rootGO in scene.GetRootGameObjects())
        {
            AddSamenNetworkObjectRecursively(rootGO);
        }

        EditorUtility.DisplayProgressBar("Creating Session...", "Saving scene...", 0.6f);

        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayProgressBar("Creating Session...", "Reading file data...", 0.8f);

        // Read scene text from disk
        string fileContent = File.ReadAllText(path);

        EditorUtility.DisplayProgressBar("Creating Session...", "Cleaning File ...", 0.85f);
        foreach (GameObject rootGO in scene.GetRootGameObjects())
        {
            RemoveSamenNetworkObjectRecursively(rootGO);
        }

        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayProgressBar("Creating Session...", "Sending file data...", 0.9f);

        OutgoingPacket packet = new OutgoingPacket(PacketType.CreateSession);
        packet.WriteString(path);
        packet.WriteString(fileContent);

        Connection.GetConnection().SendPacket(packet);

        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog("Session Created!", $"Your session for {path} has been created!", "Okay!");

        EditorSceneManager.CloseScene(scene, true);
    }

    private static void AddSamenNetworkObjectRecursively(GameObject go)
    {
        if (go.GetComponent<SamenNetworkObject>() == null)
        {
            go.AddComponent<SamenNetworkObject>().Create();
        }
        foreach (Transform child in go.transform)
        {
            AddSamenNetworkObjectRecursively(child.gameObject);
        }
    }

    private static void RemoveSamenNetworkObjectRecursively(GameObject go)
    {
        if (go.GetComponent<SamenNetworkObject>() != null)
        {
            GameObject.DestroyImmediate(go.GetComponent<SamenNetworkObject>());
        }

        foreach (Transform child in go.transform)
        {
            RemoveSamenNetworkObjectRecursively(child.gameObject);
        }
    }


    /// <summary>
    /// Join Session Option
    /// </summary>
    [MenuItem("Assets/Samen/Join Session", priority = 200000)]
    private static void JoinSessionOption()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        float totalSteps = 5;

        if (Connection.GetConnection() == null)
        {
            EditorUtility.DisplayDialog("Error", "You are not connected to a Samen Server.", "Okay");
            return;
        }

        OutgoingPacket packet = new OutgoingPacket(PacketType.JoinSession).WriteString(path);
        Connection.GetConnection().SendPacket(packet);

        string sessionPath = "Assets/Samen/Session.unity";
        EditorUtility.DisplayProgressBar("Joining Session...", "Saving scene...", 0);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        if (EditorSceneManager.GetActiveScene().path == sessionPath)
        {
            EditorUtility.DisplayProgressBar("Joining Session...", "Leaving last session...", 1 / totalSteps);
            EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), true);
        }

        EditorUtility.DisplayProgressBar("Joining Session...", "Downloading session...", 2 / totalSteps);

        string sessionData = "";
        var tempListen = Connection.GetConnection().Listen(PacketType.JoinSession, (packet) =>
        {
            sessionData = packet.GetString(0);
        }).Wait(timeout: 100000);


        if (File.Exists(sessionPath))
        {
            EditorUtility.DisplayProgressBar("Joining Session...", "Deleting old session...", 3 / totalSteps);
            File.Delete(sessionPath);
        }

        EditorUtility.DisplayProgressBar("Joining Session...", "Saving session...", 4 / totalSteps);

        File.WriteAllText(sessionPath, sessionData);

        EditorUtility.DisplayProgressBar("Joining Session...", "Opening scene...", 5 / totalSteps);
        EditorSceneManager.OpenScene(sessionPath);

        EditorUtility.DisplayProgressBar("Joining Session...", "Catching up...", 5 / totalSteps);
        Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.RequestSync));


        EditorUtility.ClearProgressBar();
        SessionManager.OnSessionJoin?.Invoke();

        SessionUI.GetWindow<SessionUI>();
        SessionManager.CurrentDataPath = path;


    }
}
