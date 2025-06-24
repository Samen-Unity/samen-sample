using Samen;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SamenPreventPlaymode
{
    static SamenPreventPlaymode()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionManager.InSessionScene())
            return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorApplication.ExitPlaymode();
            EditorUtility.DisplayDialog("Samen Error", "You can not enter play mode in a Samen Session.", "Okay");
        }
    }
}
