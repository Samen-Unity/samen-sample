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
        if (inspectors.Length == 0) return;

        var inspector = inspectors[0] as EditorWindow;
        var root = inspector.rootVisualElement;

        // Initialize bigSquare once
        if (bigSquare == null)
        {
            Color bgColor = new Color(0.33f, 0.33f, 0.33f);

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
        }

        // Only create transform editor/container if a GameObject is selected
        if (Selection.activeGameObject != null)
        {
            if (transformEditor == null || transformEditor.target != Selection.activeGameObject.transform)
            {
                if (transformEditor != null)
                {
                    Object.DestroyImmediate(transformEditor);
                    transformEditor = null;
                }

                var transformInspectorType = typeof(Editor).Assembly.GetType("UnityEditor.TransformInspector");
                if (transformInspectorType == null)
                {
                    Debug.LogError("Failed to get TransformInspector type.");
                    return;
                }

                transformEditor = Editor.CreateEditor(Selection.activeGameObject.transform, transformInspectorType);

                transformContainer = new IMGUIContainer(() =>
                {
                    transformEditor.OnInspectorGUI();
                })
                {
                    name = "TransformInspectorContainer"
                };
            }
        }
        else
        {
            // No selection, cleanup
            if (transformEditor != null)
            {
                if (transformEditor != null)
                {
                    Object.DestroyImmediate(transformEditor);
                    transformEditor = null;
                }
                transformEditor = null;
            }
            if (transformContainer != null)
            {
                if(transformContainer.parent == root)
                    root.Remove(transformContainer);
                
                transformContainer = null;
            }
        }

        if (EditorSceneManager.GetActiveScene().name == "Session" && Selection.activeGameObject != null)
        {
            if (root.Q("BigSquare") == null)
                root.Add(bigSquare);

            if (transformContainer != null && root.Q("TransformInspectorContainer") == null)
                root.Add(transformContainer);
        }
        else
        {
            var existingBigSquare = root.Q("BigSquare");
            if (existingBigSquare != null)
                root.Remove(existingBigSquare);

            var existingTransformContainer = root.Q("TransformInspectorContainer");
            if (existingTransformContainer != null)
                root.Remove(existingTransformContainer);
        }
    }
}
