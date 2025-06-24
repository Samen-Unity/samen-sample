using Samen;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class SessionWatcher
{

    static double lastCheckTime = 0;
    static double checkInterval = 0.01;
    private static SamenNetworkObject lastSelected = null;

    static SessionWatcher()
    {
        EditorApplication.update += OnEditorTick;
        Connection.OnConnect += RegisterList;
    }


    private static void RegisterList()
    {
        Connection.GetConnection().Listen(PacketType.ObjectChange, (packet) =>
        {
            // This is us getting a change from the server
            float[] values = new float[packet.GetInt(2)];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = packet.GetFloat(i + 3);
            }

            SessionChange change = new SessionChange(packet.GetString(0), (SessionChangeType)packet.GetInt(1), values);

            foreach (SamenNetworkObject samenNetworkObject in GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None))
            {
                if (samenNetworkObject.id == change.objectId)
                {
                    samenNetworkObject.ApplyChange(change);
                }
            }

        });
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

        List<SessionChange> changes = new List<SessionChange>();
        if (EditorApplication.timeSinceStartup - lastCheckTime > checkInterval)
        {
            lastCheckTime = EditorApplication.timeSinceStartup;
            if (lastSelected != null)
                changes.AddRange(lastSelected.GetChanges());
        }

        // Do a last minute test!
        if (lastSelected != selected)
        {
            if (lastSelected != null)
            {
                changes.AddRange(lastSelected.GetChanges());   
            }

            lastSelected = selected;
        }

        foreach (SessionChange change in changes)
        {

            // Sending any changes to the interwebs
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

    public static void UnloadIfDisconnect()
    {
        if (Connection.GetConnection() == null)
        {
            EditorUtility.DisplayDialog("Connection Lost", "Connection to Samen lost :(", "Close");

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}