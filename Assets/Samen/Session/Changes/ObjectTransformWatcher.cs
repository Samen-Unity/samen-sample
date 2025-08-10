#if UNITY_EDITOR
using Samen;
using Samen.Network;
using Samen.Session;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class ObjectTransformWatcher
{

    static double lastCheckTime = 0;
    static double checkInterval = 0.01;
    private static SamenNetworkObject lastSelected = null;
    static ObjectTransformWatcher()
    {
        timeSinceSceneDisconnect = new System.Diagnostics.Stopwatch();
        timeSinceSceneDisconnect.Start();
        EditorApplication.update += OnEditorTick;
        EditorSceneManager.sceneDirtied += OnSceneDirtied;
    }

    private static void OnSceneDirtied(Scene scene)
    {
        if(SessionManager.InSessionScene())
        {
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void OnEditorTick()
    {
        if (!SessionManager.InSessionScene())
            return;

        UnloadIfDisconnect();

        SamenNetworkObject selected = null;
        if(Selection.activeGameObject != null)
        {
            selected = Selection.activeGameObject.GetComponent<SamenNetworkObject>();
        }

        List<TransformChange> changes = new List<TransformChange>();
        if (EditorApplication.timeSinceStartup - lastCheckTime > checkInterval)
        {
            lastCheckTime = EditorApplication.timeSinceStartup;
            if (lastSelected != null)
                changes.AddRange(lastSelected.GetChanges());
        }

        if (lastSelected != selected)
        {
            if (lastSelected != null)
            {
                changes.AddRange(lastSelected.GetChanges());   
            }

            lastSelected = selected;
        }

        foreach (TransformChange change in changes)
        {

            // Sending any changes to the server
            OutgoingPacket packet = new OutgoingPacket(PacketType.ObjectChange)
                .WriteString(change.objectId)
                .WriteInt((int) change.type)
                .WriteInt((int)change.values.Length);

            foreach(float value in change.values)
            {
                packet.WriteFloat(value);
            }

            Connection.GetConnection().SendPacket(packet);
        }
    }


    private static System.Diagnostics.Stopwatch timeSinceSceneDisconnect;
    public static void UnloadIfDisconnect()
    {
        if (Connection.GetConnection() == null && timeSinceSceneDisconnect.ElapsedMilliseconds > 100)
        {
            timeSinceSceneDisconnect.Restart();
            EditorUtility.DisplayDialog("Connection Lost", "Connection to Samen lost :(", "Close");
            EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), true);

            if (SessionManager.CurrentDataPath != null)
            {
                Scene scene = EditorSceneManager.OpenScene(SessionManager.CurrentDataPath);
                Debug.LogWarning("Falling back to local scene.");

                if(scene == null)
                {
                    var closeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
                return;
            }

            Debug.LogWarning("Loading backup scene");
            // In case we cant find the old scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
#endif