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
        private static string[] KnownObjectIds;
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
                if (gameObject.GetComponent<SamenNetworkObject>() == null)
                {
                    GameObject.DestroyImmediate(gameObject);
                    EditorUtility.DisplayDialog("Woops!", "Creating objects like that is not (yet) supported.", "Okay");
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
        
        /// <summary>
        /// Gets ran whenever the client discovers a missing object
        /// </summary>
        static void DeletedObjectFound(string id)
        {
            Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.ObjectDestroyed).WriteString(id));
            Debug.Log($"SamenNetworkObject with Id {id} was destroyed or removed from the scene.");
        }
    }
}
