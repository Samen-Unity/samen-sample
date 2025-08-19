#if UNITY_EDITOR
using Assets.Samen.Session.Changes;
using Samen.Network;
using Samen.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Samen.Session
{
    [InitializeOnLoad]
    public class SessionChangePacketListener
    {
        static SessionChangePacketListener()
        {
            Connection.OnConnect += StartListening;
        }

        static void StartListening()
        {
            Connection.GetConnection().Listen(PacketType.ObjectDestroyed, OnObjectDestroyedPacket);
            Connection.GetConnection().Listen(PacketType.ObjectDuplicate, OnObjectDuplicatedPacket);
            Connection.GetConnection().Listen(PacketType.ObjectChange, OnObjectTransformChangePacket);
            Connection.GetConnection().Listen(PacketType.ChatMessage, OnChatMessagePacket);
            Connection.GetConnection().Listen(PacketType.ParentChange, OnParentChangePacket);
            Connection.GetConnection().Listen(PacketType.PrefabCreated, OnPrefabCreatedPacket);
            Connection.GetConnection().Listen(PacketType.Ping, UI.Ping.OnPingPacket);
            Connection.GetConnection().Listen(PacketType.ComponentUpdated, OnComponentUpdated);
        }

        static void OnComponentUpdated(IncomingPacket packet)
        {
            if (!SessionManager.InSessionScene())
                return;

            string objectId = packet.GetString(0);
            string component = packet.GetString(1);
            string json = packet.GetString(2);

            foreach (SamenNetworkObject samenNetworkObject in GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None))
            {
                if (samenNetworkObject.id == objectId)
                {
                    samenNetworkObject.UpdateComponent(component, json);
                }
            }
        }

        static void OnPrefabCreatedPacket(IncomingPacket packet)
        {
            if (!SessionManager.InSessionScene())
                return;

            // Load the prefab asset from the project
            string assetPath = packet.GetString(0);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);


            if (prefabAsset == null)
            {
                Debug.LogError($"Failed to load prefab at path: {assetPath}");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            instance.AddComponent<DontUpload>();

            if (instance == null)
            {
                Debug.LogError("Failed to instantiate prefab.");
                return;
            }

            int idCount = packet.GetInt(1);
            string[] ids = new string[idCount];

            for (int i = 0; i < idCount; i++)
            {
                ids[i] = packet.GetString(2 + i);
            }

            AddObjectIdsToPrefab(instance, ids);

            GameObject.DestroyImmediate(instance.GetComponent<DontUpload>());
        }


        static void AddObjectIdsToPrefab(GameObject prefab, string[] ids)
        {
            if (prefab == null || ids == null || ids.Length == 0)
            {
                Debug.LogError("Invalid prefab or ID list.");
                return;
            }

            // Collect all GameObjects in hierarchy in top-down order
            List<GameObject> allObjects = new List<GameObject>();
            CollectHierarchy(prefab.transform, allObjects);

            if (allObjects.Count != ids.Length)
            {
                Debug.LogError($"ID count mismatch. Found {allObjects.Count} GameObjects, but received {ids.Length} IDs.");
                return;
            }

            for (int i = 0; i < allObjects.Count; i++)
            {
                GameObject obj = allObjects[i];
                SamenNetworkObject netObj = obj.GetComponent<SamenNetworkObject>();

                if (netObj == null)
                {
                    netObj = obj.AddComponent<SamenNetworkObject>();
                }

                netObj.id = ids[i];
            }
        }

        static void CollectHierarchy(Transform root, List<GameObject> result)
        {
            result.Add(root.gameObject);

            for (int i = 0; i < root.childCount; i++)
            {
                CollectHierarchy(root.GetChild(i), result);
            }
        }


        static void OnParentChangePacket(IncomingPacket packet)
        {
            if (!SessionManager.InSessionScene())
                return;


            SamenNetworkObject childObject = GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
                    .Where(obj => obj.id == packet.GetString(0))
                    .First();

            string parentId = packet.GetString(1);

            if(parentId == "none")
            {
                childObject.transform.parent = null;
            }
            else
            {
                SamenNetworkObject parentObject = GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
                    .Where(obj => obj.id == parentId)
                    .First();

                childObject.transform.parent = parentObject.transform;
            }
        }

        static void OnChatMessagePacket(IncomingPacket packet)
        {
            if (!SessionManager.InSessionScene())
                return;

            Chat.AddMessage(new ChatMessage(packet.GetString(0), packet.GetString(1)));
        }

        static void OnObjectTransformChangePacket(IncomingPacket packet)
        {
            if (!SessionManager.InSessionScene())
                return;

            // This is us getting a change from the server
            float[] values = new float[packet.GetInt(2)];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = packet.GetFloat(i + 3);
            }

            TransformChange change = new TransformChange(packet.GetString(0), (TransformChangeType) packet.GetInt(1), values);

            foreach (SamenNetworkObject samenNetworkObject in GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None))
            {
                if (samenNetworkObject.id == change.objectId)
                {
                    samenNetworkObject.ApplyChange(change);
                }
            }
        }

        static void OnObjectDestroyedPacket(IncomingPacket packet)
        {
            if (!SessionManager.InSessionScene())
                return;

            SamenNetworkObject destroyedObject = GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
                    .Where(obj => obj.id == packet.GetString(0))
                    .FirstOrDefault();

            if (destroyedObject == null)
                return;

            for (int i = 0; i < SessionHierarchyWatcher.KnownObjectIds.Length; i++)
            {
                if (SessionHierarchyWatcher.KnownObjectIds[i] == destroyedObject.id)
                    SessionHierarchyWatcher.KnownObjectIds[i] = null;
            }

            // Destroy the object to sync back with the server
            GameObject.DestroyImmediate(destroyedObject.gameObject);
        }

        static void OnObjectDuplicatedPacket(IncomingPacket packet)
        {
            if (!SessionManager.InSessionScene())
                return;

            SamenNetworkObject duplicatedObject = GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
                .Where(obj => obj.id == packet.GetString(0))
                .First();

            GameObject createdObject = GameObject.Instantiate(duplicatedObject.gameObject);
            createdObject.GetComponent<SamenNetworkObject>().id = packet.GetString(1);
        }
    }
}
#endif