using Samen.Network;
using Samen.Session;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SceneStatusUpdater
{
    private static Dictionary<string, bool> sceneStatus = new Dictionary<string, bool>();
    private static double nextUpdateTime = 0;
    private const float updateInterval = 5f;

    static SceneStatusUpdater()
    {
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (EditorApplication.timeSinceStartup < nextUpdateTime)
            return;

        nextUpdateTime = EditorApplication.timeSinceStartup + updateInterval;

        string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            try
            {
                bool condition = CheckMyCondition(path);
                if (!sceneStatus.ContainsKey(path) || sceneStatus[path] != condition)
                {
                    sceneStatus[path] = condition;
                    EditorApplication.RepaintProjectWindow();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error checking scene status for {path}: {ex}");
            }
        }
    }

    private static bool CheckMyCondition(string path)
    {
        return Connection.GetConnection() != null && SessionManager.HasActiveSession(path).result;
    }

    public static bool IsSceneActive(string path)
    {
        return sceneStatus.TryGetValue(path, out bool active) && active;
    }
}