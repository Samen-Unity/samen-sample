using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SceneIconOverlayDrawer
{
    private static Texture2D _circleTexture;

    static SceneIconOverlayDrawer()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        _circleTexture = CreateCircleTexture();
    }

    static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        if (_circleTexture == null)
            _circleTexture = CreateCircleTexture();

        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (!path.EndsWith(".unity"))
            return;

        if (!SceneStatusUpdater.IsSceneActive(path))
            return;

        Rect iconRect = new Rect(selectionRect.xMax - 14, selectionRect.yMin + 2, 10, 10);
        GUI.DrawTexture(iconRect, _circleTexture);
    }

    static Texture2D CreateCircleTexture()
    {
        int size = 10;
        Texture2D tex = new Texture2D(size, size);
        Color clear = new Color(0, 0, 0, 0);
        Color color = Color.green;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - size / 2f + 0.5f;
                float dy = y - size / 2f + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, dist < size / 2f ? color : clear);
            }
        }

        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.name = "SceneStatusCircle";
        return tex;
    }
}
