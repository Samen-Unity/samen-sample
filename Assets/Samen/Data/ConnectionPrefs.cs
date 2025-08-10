#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;

namespace Assets.Samen.Data
{
    public static class ConnectionPrefs
    {
        private const string IP_KEY = "SamenConnection_IP";
        private const string PORT_KEY = "SamenConnection_Port";
        private const string USERNAME_KEY = "SamenConnection_Username";

        private static string ip = "127.0.0.1";
        private static string port = "4041";
        private static string username = "Visitor";

        public static string GetLastIp()
        {
            return ip;
        }
        public static string GetLastPort()
        {
            return port;
        }

        public static string GetLastUsername()
        {
            return username;
        }

        public static void Update(string ip, string port, string username)
        {
            ConnectionPrefs.ip = ip;
            ConnectionPrefs.port = port;
            ConnectionPrefs.username = username;

            EditorPrefs.SetString(IP_KEY, ip);
            EditorPrefs.SetString(PORT_KEY, port);
            EditorPrefs.SetString(USERNAME_KEY, username);
        }

        public static void Load()
        {
            ip = EditorPrefs.GetString(IP_KEY, "127.0.0.1");
            port = EditorPrefs.GetString(PORT_KEY, "4041");
            username = EditorPrefs.GetString(USERNAME_KEY, "Visitor");
        }

    }
}
#endif