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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Samen.Session
{
    public class SessionManager
    {
        /// <summary>
        /// Join a session based on a scene's asset path
        /// Removes the old loaded session.
        /// </summary>
        /// <param name="assetPath"></param>
        public static void JoinSession(string assetPath)
        {
            Chat.Clear();
            float totalSteps = 5;

            if (Connection.GetConnection() == null)
            {
                EditorUtility.DisplayDialog("Error", "You are not connected to a Samen Server.", "Okay");
                return;
            }

            OutgoingPacket packet = new OutgoingPacket(PacketType.JoinSession).WriteString(assetPath);
            Connection.GetConnection().SendPacket(packet);

            string sessionPath = "Assets/Samen/Session.unity";
            EditorUtility.DisplayProgressBar("Joining Session...", "Saving scene...", 0);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            if (EditorSceneManager.GetActiveScene().path == sessionPath)
            {
                EditorUtility.DisplayProgressBar("Joining Session...", "Leaving last session...", 1 / totalSteps);
                EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), true);
            }

            EditorUtility.DisplayProgressBar("Joining Session...", "Downloading session...", 2 / totalSteps);

            string sessionData = "";
            var tempListen = Connection.GetConnection().Listen(PacketType.JoinSession, (packet) =>
            {
                sessionData = packet.GetString(0);
            }).Wait(timeout: 100000);


            if (File.Exists(sessionPath))
            {
                EditorUtility.DisplayProgressBar("Joining Session...", "Deleting old session...", 3 / totalSteps);
                File.Delete(sessionPath);
            }

            EditorUtility.DisplayProgressBar("Joining Session...", "Saving session...", 4 / totalSteps);

            File.WriteAllText(sessionPath, sessionData);

            EditorUtility.DisplayProgressBar("Joining Session...", "Opening scene...", 5 / totalSteps);
            EditorSceneManager.OpenScene(sessionPath);

            EditorUtility.DisplayProgressBar("Joining Session...", "Catching up...", 5 / totalSteps);
            Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.RequestSync));


            EditorUtility.ClearProgressBar();
            OnSessionJoin?.Invoke();

            SamenWindow.GetWindow<SamenWindow>();
            CurrentDataPath = assetPath;

            var currentIds = GameObject
            .FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
            .Select(obj => obj.id)
            .ToHashSet();

            SessionHierarchyWatcher.KnownObjectIds = currentIds.ToArray();
        }

        /// <summary>
        /// Create a new session based of a scene's file path
        /// </summary>
        /// <param name="assetPath"></param>
        public static void CreateSession(string assetPath)
        {
            EditorUtility.DisplayProgressBar("Creating Session...", "Preparing scene...", 0f);

            // Get path of selected scene asset
            string path = assetPath;
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity"))
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", "Please select a valid scene asset.", "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            EditorUtility.DisplayProgressBar("Creating Session...", "Adding network components...", 0.3f);

            foreach (var rootGO in scene.GetRootGameObjects())
            {
                AddSamenNetworkObjectRecursively(rootGO);
            }

            EditorUtility.DisplayProgressBar("Creating Session...", "Saving scene...", 0.6f);

            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayProgressBar("Creating Session...", "Reading file data...", 0.8f);

            // Read scene text from disk
            string fileContent = File.ReadAllText(path);

            EditorUtility.DisplayProgressBar("Creating Session...", "Cleaning File ...", 0.85f);
            foreach (GameObject rootGO in scene.GetRootGameObjects())
            {
                RemoveSamenNetworkObjectRecursively(rootGO);
            }

            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayProgressBar("Creating Session...", "Sending file data...", 0.9f);

            OutgoingPacket packet = new OutgoingPacket(PacketType.CreateSession);
            packet.WriteString(path);
            packet.WriteString(fileContent);

            Connection.GetConnection().SendPacket(packet);

            EditorUtility.ClearProgressBar();

            EditorSceneManager.CloseScene(scene, true);
        }

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
                
            }

            return (true, response);
        }

        public static void Export()
        {
            EditorUtility.DisplayProgressBar("Exporting...", "Exporting data...", 0.5f);
            // Clean up our stuff
            foreach (SamenNetworkObject samenNetworkObject in GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None))
            {
                GameObject.DestroyImmediate(samenNetworkObject);
            }

            foreach(DontUpload dontUpdate in GameObject.FindObjectsByType<DontUpload>(FindObjectsSortMode.None))
            {
                GameObject.DestroyImmediate(dontUpdate);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(activeScene);

            byte[] contents = File.ReadAllBytes("Assets/Samen/Session.unity");

            File.WriteAllBytes(SessionManager.CurrentDataPath, contents);
            File.WriteAllBytes("Assets/Samen/Backup", contents); // Just to be sure...

            EditorUtility.ClearProgressBar();

            EditorSceneManager.OpenScene(SessionManager.CurrentDataPath);
        }

        public static void Save()
        {
            Export();
            JoinSession(CurrentDataPath);
        }

        private static void AddSamenNetworkObjectRecursively(GameObject go)
        {
            if (go.GetComponent<SamenNetworkObject>() == null)
            {
                go.AddComponent<SamenNetworkObject>().Create();
            }
            foreach (Transform child in go.transform)
            {
                AddSamenNetworkObjectRecursively(child.gameObject);
            }
        }

        private static void RemoveSamenNetworkObjectRecursively(GameObject go)
        {
            if (go.GetComponent<SamenNetworkObject>() != null)
            {
                GameObject.DestroyImmediate(go.GetComponent<SamenNetworkObject>());
            }

            foreach (Transform child in go.transform)
            {
                RemoveSamenNetworkObjectRecursively(child.gameObject);
            }
        }
    }
}
#endif