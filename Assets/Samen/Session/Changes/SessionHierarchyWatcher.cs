#if UNITY_EDITOR
using Samen.Network;
using Samen.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Samen.Session.Changes
{
    [InitializeOnLoad]
    public static class SessionHierarchyWatcher
    {
        public static string[] KnownObjectIds;
        static SessionHierarchyWatcher()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        static void OnHierarchyChanged()
        {
            if (!SessionManager.InSessionScene() || KnownObjectIds == null)
                return;

            // Prevent objects from being created directly now
            foreach (GameObject gameObject in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (gameObject.GetComponent<SamenNetworkObject>() == null && gameObject.GetComponent<DontUpload>() == null)
                {
                    AddPrefab(gameObject);
                }
            }

            var currentIds = GameObject
                .FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
                .Select(obj => obj.id)
                .ToHashSet();

            foreach (string id in KnownObjectIds)
            {
                // The object was deleted on our end!
                if (!currentIds.Contains(id) && id != null)
                {
                    DeletedObjectFound(id);
                }
            }

            KnownObjectIds = currentIds.ToArray();
        }
        
        static void AddPrefab(GameObject gameObject)
        {
            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);

            if (prefab == null)
            {
                GameObject.DestroyImmediate(gameObject);
                EditorUtility.DisplayDialog("Sorry!", "You can not make new objects in a session. You can only add existing prefabs.", "Okay!");
                return;
            }

            if (!PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                return;

            string path = AssetDatabase.GetAssetPath(prefab);
            List<string> ids = CreateObjectIds(gameObject, new List<string>());

            OutgoingPacket packet = new OutgoingPacket(PacketType.PrefabCreated);

            packet.WriteString(path); // Asset path;
            packet.WriteInt(ids.Count);

            for(int i = 0; i < ids.Count; i++)
            {
                packet.WriteString(ids[i]);
            }

            Connection.GetConnection().SendPacket(packet);
        }

        private static List<string> CreateObjectIds(GameObject gameObject, List<string> current)
        {
            if(gameObject.GetComponent<SamenNetworkObject>() == null)
            {
                SamenNetworkObject networkObject = gameObject.AddComponent<SamenNetworkObject>();
                networkObject.Create();
                current.Add(networkObject.id);
            }

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject g = gameObject.transform.GetChild(i).gameObject;
                CreateObjectIds(g, current);
            }

            return current;
        }


        /// <summary>
        /// Gets ran whenever the client discovers a missing object
        /// </summary>
        static void DeletedObjectFound(string id)
        {
            Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.ObjectDestroyed).WriteString(id));
            
        }
    }
}
#endif