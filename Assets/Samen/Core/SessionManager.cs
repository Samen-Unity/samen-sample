using Samen;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Samen
{
    public class SessionManager
    {
        public static string CurrentDataPath;

        /// <summary>
        /// Returns true if the editor has the session scene loaded, false if otherwise.
        /// </summary>
        /// <returns></returns>
        public static bool InSessionScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.name == "Session";
        }

        public static Action OnSessionJoin;

        /// <summary>
        /// Blocks the thread until a response is provided.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static (bool success, bool result) HasActiveSession(string path)
        {
            Connection connection = Connection.GetConnection();
            if (connection == null)
            {
                Debug.LogWarning("Calling HasActiveSession while client is not connected will result in inaccurate information.");
                return (false, false);
            }

            connection.SendPacket(new OutgoingPacket(PacketType.SessionExists).WriteString(path));


            bool response = false;

            bool timeout = connection.Listen(PacketType.SessionExists, (packet) =>
            {
                response = packet.GetBool(0);
            }).Wait();

            if (timeout)
            {
                Debug.LogWarning("Communications timed out!");
            }

            Debug.Log("Response was " + response);
            return (true, response);
        }
    }
}
