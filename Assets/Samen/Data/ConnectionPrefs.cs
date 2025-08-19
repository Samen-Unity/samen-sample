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
        private const string REMEMBER_KEY = "SamenConnection_Remember";
        private const string PASSWORD_KEY = "SamenConnection_Password";

        private static string ip = "127.0.0.1";
        private static string port = "4041";
        private static string username = "Visitor";
        private static bool remember = false;
        private static string password = "Password";

        public static bool GetRemember()
        {
            return remember;
        }
        public static string GetLastIp()
        {
            return ip;
        }
        public static string GetLastPassword()
        {
            return password;
        }

        public static string GetLastPort()
        {
            return port;
        }


        public static void SetRemember(bool remember)
        {
            ConnectionPrefs.remember = remember;
        }

        public static string GetLastUsername()
        {
            return username;
        }

        public static void Update(string ip, string port, string username, string password, bool remember)
        {
            ConnectionPrefs.ip = ip;
            ConnectionPrefs.port = port;
            ConnectionPrefs.username = username;
            ConnectionPrefs.remember = remember;
            ConnectionPrefs.password = password;

            EditorPrefs.SetString(IP_KEY, ip);
            EditorPrefs.SetString(PORT_KEY, port);
            EditorPrefs.SetString(USERNAME_KEY, username);
            EditorPrefs.SetBool(REMEMBER_KEY, remember);
            EditorPrefs.SetString(PASSWORD_KEY, password);
        }

        public static void Load()
        {
            ip = EditorPrefs.GetString(IP_KEY, "127.0.0.1");
            port = EditorPrefs.GetString(PORT_KEY, "4041");
            username = EditorPrefs.GetString(USERNAME_KEY, "Visitor");
            remember = EditorPrefs.GetBool(REMEMBER_KEY, false);
            password = EditorPrefs.GetString(PASSWORD_KEY, "Password");
        }

    }
}
#endif