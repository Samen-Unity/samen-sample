#if UNITY_EDITOR
using Samen;
using Samen.Network;
using Samen.Session;
using System.IO;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;
using UnityEngine.SceneManagement;
public class SceneContextMenu
{

    /// <summary>
    /// Check if the Create Session option should appear
    /// </summary>
    /// <returns></returns>
    [MenuItem("Assets/Samen/Create and Join Session", true, 200000)]
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
    [MenuItem("Assets/Samen/Create and Join Session", priority = 200000)]
    private static void CreateSessionOption()
    {
        string selectedAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        SessionManager.CreateSession(selectedAssetPath);
        SessionManager.JoinSession(selectedAssetPath);
    }

    /// <summary>
    /// Join Session Option
    /// </summary>
    [MenuItem("Assets/Samen/Join Session", priority = 200000)]
    private static void JoinSessionOption()
    {
        string selectedAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        SessionManager.JoinSession(selectedAssetPath);
    }
}
#endif