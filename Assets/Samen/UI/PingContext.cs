using Samen.Network;
using Samen.Session;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

[InitializeOnLoad]
public class PingContext
{
    static PingContext()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 1 && e.shift && SessionManager.InSessionScene())
        {
            RaycastHit hit;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            bool canDoPing = Physics.Raycast(ray, out hit);
            Vector3 point = hit.point;

            Vector2 mousePos = e.mousePosition;

            GenericMenu menu = new GenericMenu();

            if (canDoPing)
            {
                menu.AddItem(new GUIContent("Ping"), false, () => Samen.UI.Ping.NetworkPingAt(point));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Ping"), false);
            }
                menu.DropDown(new Rect(mousePos, Vector2.zero));

            e.Use();
        }
    }
}
