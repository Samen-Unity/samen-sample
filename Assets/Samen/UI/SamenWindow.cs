#if UNITY_EDITOR
using Assets.Samen.Data;
using Samen.Network;
using Samen.Session;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Samen.UI
{
    public class SamenWindow : EditorWindow
    {


        [MenuItem("Window/Samen", priority = -100000)]
        public static void ShowWindow()
        {
            GetWindow<SamenWindow>("Samen");
        }

        string ipField;
        string portField;
        string usernameField;
        string passwordField;

        bool remember;

        private void OnEnable()
        {
            // Load saved values
            ConnectionPrefs.Load();

            ipField = ConnectionPrefs.GetLastIp();
            portField = ConnectionPrefs.GetLastPort();
            usernameField = ConnectionPrefs.GetLastUsername();
            remember  = ConnectionPrefs.GetRemember();
        }

        private void OnDisable()
        {
            // Save values when window is closed or Unity closes
            ConnectionPrefs.Update(ipField, portField, usernameField, passwordField, remember);
        }

        private void OnGUI()
        {
            if (Connection.GetConnection() == null || Connection.connectionState == ConnectionState.Disconnected)
            {
                LoginPage();
            }
            else if (SessionManager.InSessionScene())
            {
                SessionPage();
            }
            else
            {
                ConnectedPage();
            }
        }

        private void LoginPage()
        {
            EditorGUILayout.LabelField("Connect with Samen", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            usernameField = EditorGUILayout.TextField(new GUIContent("Username", "Enter your display name"), usernameField);
            passwordField = EditorGUILayout.TextField(new GUIContent("Password", "Enter your password"), passwordField);

            ipField = EditorGUILayout.TextField(new GUIContent("IP Address", "Enter the Samen Host IP"), ipField);
            portField = EditorGUILayout.TextField(new GUIContent("Port", "Enter the connection port"), portField);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Connect", GUILayout.Height(25)))
            {
                Connection.Connect(ipField, portField, usernameField, passwordField);
            }

            remember = GUILayout.Toggle(remember, "Automatically Relog");
        }

        private void SessionPage()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Current Session", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Active Scene Path:", EditorStyles.label);
            EditorGUILayout.SelectableLabel(SessionManager.CurrentDataPath, EditorStyles.textField, GUILayout.Height(20));

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Save", GUILayout.Height(25)))
            {
                SessionManager.Save();
                EditorUtility.DisplayDialog("Save Complete", "Your scene has been saved!", "Okay!");
            }

            if (GUILayout.Button("Save & Exit", GUILayout.Height(25)))
            {
                SessionManager.Export();
                EditorUtility.DisplayDialog("Export Complete", "Scene data has been exported successfully.", "Okay!");
            }

            EditorGUILayout.EndVertical();

            Chat.CreateChatUI();
        }

        private void ConnectedPage()
        {
            GUILayout.Space(10);

            var style = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            GUILayout.Label("You are connected to Samen.\nJoin a session to start!", style);

            if(GUILayout.Button("Log out"))
            {
                ConnectionPrefs.SetRemember(false);
                Connection.Disconnect();
            }
        }
    }
}

enum State
{
    Idle,
    Connecting,
    Connected,
    Failed
}
#endif