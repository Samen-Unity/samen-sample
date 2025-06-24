using NUnit.Framework;
using Samen;

using UnityEditor;
using UnityEngine;
using System.Linq;

[InitializeOnLoad]
public static class SessionObjectWatcher
{
    static SessionObjectWatcher()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        SessionManager.OnSessionJoin += RefreshList;
        Connection.OnConnect += UpdateDestroy;
    }

    private static string[] KnownObjectIds;

    private static void UpdateDestroy()
    {
        Connection.GetConnection().Listen(PacketType.ObjectDestroyed, (packet) =>
        {
            SamenNetworkObject destroyedObject = GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
                .Where(obj => obj.id == packet.GetString(0))
                .First();

            for(int i = 0; i < KnownObjectIds.Length; i++)
            {
                if (KnownObjectIds[i] == destroyedObject.id)
                    KnownObjectIds[i] = null;
            }    

            GameObject.DestroyImmediate(destroyedObject.gameObject);
        });
    }

    private static void RefreshList()
    {
        KnownObjectIds = GameObject
            .FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
            .Select(obj => obj.id)
            .ToArray();
    }

    private static void OnHierarchyChanged()
    {
        if (!SessionManager.InSessionScene() || KnownObjectIds == null)
            return;

        var currentIds = GameObject
            .FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
            .Select(obj => obj.id)
            .ToHashSet();

        foreach (string id in KnownObjectIds)
        {
            if (!currentIds.Contains(id) && id != null)
            {
                Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.ObjectDestroyed).WriteString(id));
                Debug.Log($"SamenNetworkObject with Id {id} was destroyed or removed from the scene.");
            }
        }

        KnownObjectIds = currentIds.ToArray();
    }

}
