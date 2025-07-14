using Samen.Network;
using Samen.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Samen.Session
{
    [InitializeOnLoad]
    public class SessionChangePacketListener
    {
        private static string[] KnownObjectIds;
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
                    .First();

            for (int i = 0; i < KnownObjectIds.Length; i++)
            {
                if (KnownObjectIds[i] == destroyedObject.id)
                    KnownObjectIds[i] = null;
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
