using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;

namespace Samen.Network
{
    /// <summary>
    /// Checking for network inputs
    /// </summary>
    [InitializeOnLoad]
    public static class ConnectionLoop
    {
        /// <summary>
        /// Function called by Unity
        /// </summary>
        static ConnectionLoop()
        {
            // Check every editor update for incoming packets.
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// Every editor update check for changes
        /// </summary>
        private static void OnEditorUpdate()
        {
            // Make sure that we read any incoming packets, as often as possible
            Connection connection = Connection.GetConnection();
            if (connection != null)
            {
                if (connection.HasConnection() == false)
                {
                    Connection.Disconnect();
                    return;
                }

                connection.ReadPackets();
            }
        }
    }
}
