
#if UNITY_EDITOR
using Samen.Session;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[InitializeOnLoad]
public class DisableInspector : Editor
{
    static VisualElement bigSquare;
    static IMGUIContainer transformContainer;
    static Editor transformEditor;

    static DisableInspector()
    {
        EditorApplication.update += UpdateInspectorUI;
    }

    static void UpdateInspectorUI()
    {
        var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        var inspectors = Resources.FindObjectsOfTypeAll(inspectorType);

        for (int i = 0; i < inspectors.Length; i++)
        {
            var inspector = inspectors[i] as EditorWindow;
            var root = inspector.rootVisualElement;

            // Initialize bigSquare once
            if (bigSquare == null)
            {
                Color bgColor = new Color(0.23f, 0.23f, 0.23f);

                bigSquare = new VisualElement();
                bigSquare.style.position = Position.Absolute;
                bigSquare.style.left = 0;
                bigSquare.style.top = 0;
                bigSquare.style.right = 0;
                bigSquare.style.bottom = 0;
                bigSquare.style.backgroundColor = bgColor;
                bigSquare.name = "BigSquare";
                bigSquare.pickingMode = PickingMode.Position;
                bigSquare.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor());

                // Create a Label with some text
                Label textLabel = new Label("Sorry!\n\nThe inspector is disabled during sessions.\nPlease use Prefabs instead.");
                textLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                textLabel.style.color = Color.white;
                textLabel.style.fontSize = 24;
                textLabel.style.position = Position.Absolute;
                textLabel.style.left = 0;
                textLabel.style.top = 0;
                textLabel.style.right = 0;
                textLabel.style.bottom = 0;

                // Add the label to bigSquare
                bigSquare.Add(textLabel);
            }


            if (SessionManager.InSessionScene())
            {
                if (!root.Contains(bigSquare))
                {
                    Debug.Log("[Samen] Locking Inspector.");
                    root.Add(bigSquare);
                }
            }
            else
            {
                if (root.Contains(bigSquare))
                {
                    Debug.Log("[Samen] Unlocking Inspector.");
                    root.Remove(bigSquare);
                }
            }
        }
    }
}
#endif