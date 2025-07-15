using Samen.Network;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Samen.UI
{
    public static class Ping
    {
        public static void OnPingPacket(IncomingPacket packet)
        {
            float x = packet.GetFloat(0);
            float y = packet.GetFloat(1);
            float z = packet.GetFloat(2);

            PingAt(new Vector3(x, y, z));
        }

        public static void PingAt(Vector3 position)
        {
            string assetPath = "Assets/Samen/Prefabs/Ping.prefab";

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabAsset == null)
            {
                Debug.LogError("Could not find ping prefab at: " + assetPath);
                return;
            }

            GameObject ping = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            if (ping == null)
            {
                Debug.LogError("Failed to instantiate ping prefab.");
                return;
            }

            ping.transform.position = position;
        }

        public static void NetworkPingAt(Vector3 position)
        {
            Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.Ping)
                .WriteFloat(position.x)
                .WriteFloat(position.y)
                .WriteFloat(position.z));
        }
    }
}
