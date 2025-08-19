#if UNITY_EDITOR
using Samen.Network;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ConnectionLostNotification
{
    // Set your target scene name here (case-sensitive)
    private const string targetSceneName = "Connection Lost";

    static ConnectionLostNotification()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (SceneManager.GetActiveScene().name != targetSceneName)
            return;

        Handles.BeginGUI();

        Vector2 size = sceneView.position.size;

        // Background box centered
        Color prevColor = GUI.color;
        float boxWidth = 300;
        float boxHeight = 50;
        float boxX = (size.x - boxWidth) / 2;
        float boxY = (size.y - boxHeight) / 2;
        GUI.color = new Color(1, 0, 0, 1f);
        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), GUIContent.none);
        GUI.color = prevColor;

        // Text style
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = Color.white;
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        string title = "Connection Lost";
        string subtitle = "Please reconnect in the 'Samen' tab";

        if(Connection.GetConnection() != null)
        {
            title = "Connection Regained!";
            subtitle = "Join a session to continue...";
        }


        Rect rect = new Rect(boxX, boxY, boxWidth, boxHeight);
        GUI.Label(rect, title, style);

        Rect rect2 = new Rect(boxX - 150, boxY + 100, boxWidth + 300, boxHeight);
        GUI.Label(rect2, subtitle, style);

        Handles.EndGUI();
    }

}
#endif