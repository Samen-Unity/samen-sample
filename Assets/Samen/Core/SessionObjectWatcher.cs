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
        Connection.OnConnect += UpdateListeners;
    }

    private static string[] KnownObjectIds;

    private static void UpdateListeners()
    {
        // Check if any destroy packets where recieved.
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

            // Destroy the object to sync back with the server
            GameObject.DestroyImmediate(destroyedObject.gameObject);
        });

        Connection.GetConnection().Listen(PacketType.ObjectDuplicate, (packet) =>
        {
            SamenNetworkObject duplicatedObject = GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
                .Where(obj => obj.id == packet.GetString(0))
                .First();

            GameObject createdObject = GameObject.Instantiate(duplicatedObject.gameObject);
            createdObject.GetComponent<SamenNetworkObject>().id = packet.GetString(1);
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

            // The object was deleted on our end!
            if (!currentIds.Contains(id) && id != null)
            {
                // Send a packet to destroy the object server side!
                Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.ObjectDestroyed).WriteString(id));
                Debug.Log($"SamenNetworkObject with Id {id} was destroyed or removed from the scene.");
            }
        }

        KnownObjectIds = currentIds.ToArray();
    }

}
