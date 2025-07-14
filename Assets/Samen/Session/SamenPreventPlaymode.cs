using Samen;
using Samen.Session;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Script that prevents playmode from being started during a session.
/// </summary>
[InitializeOnLoad]
public class SamenPreventPlaymode
{
    static SamenPreventPlaymode()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Check if we are in a session
        if (!SessionManager.InSessionScene())
            return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorApplication.ExitPlaymode();
            EditorUtility.DisplayDialog("Samen Error", "You can not enter play mode in a Samen Session.", "Okay");
        }
    }
}
