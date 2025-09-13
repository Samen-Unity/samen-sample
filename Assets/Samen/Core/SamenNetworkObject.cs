#if UNITY_EDITOR
using Newtonsoft.Json;
using Samen;
using Samen.Network;
using Samen.Session;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Unity.Burst.Intrinsics.X86.Avx;


// Any object in a session has this
[ExecuteAlways]
public class SamenNetworkObject : MonoBehaviour
{

    private void Start()
    {
        if (!SessionManager.InSessionScene())
        {
            EditorUtility.DisplayDialog("Nope!", "You can not add this component to an object.", "OK!");
            DestroyImmediate(this);
        }

        SamenNetworkObject[] existingObjects = GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None); 
        foreach (SamenNetworkObject samenNetworkObject in existingObjects)
        {
            if (samenNetworkObject == this)
                continue;

            if(samenNetworkObject.id == this.id)
            {
                // Create a new ID for ourselfs
                this.id = null;
                Create();


                Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.ObjectDuplicate)
                    .WriteString(samenNetworkObject.id) // The ID of the object duplicated
                    .WriteString(this.id) // Our new id
                    );

            }
        }
    }

    public string id;
    // Serialized Values
    private Vector3 cachedPosition;
    private Vector3 cachedScale;
    private Quaternion cachedRotation;
    private void CacheValues()
    {
        cachedPosition = transform.localPosition;
        cachedScale = transform.localScale;
        cachedRotation = transform.localRotation;
    }

    /// <summary>
    /// Internal use only.
    /// </summary>
    public void Create()
    {
        if(id == null)
        {
            id = Guid.NewGuid().ToString();
        }

        CacheValues();
    }

    /// <summary>
    /// Apply a change comming from the server
    /// </summary>
    /// <param name="sessionChange"></param>
    public void ApplyChange(TransformChange sessionChange)
    {
        switch (sessionChange.type)
        {
            case TransformChangeType.Position:
                transform.localPosition = new Vector3(sessionChange.values[0], sessionChange.values[1], sessionChange.values[2]);
                break;

            case TransformChangeType.Scale:
                transform.localScale = new Vector3(sessionChange.values[0], sessionChange.values[1], sessionChange.values[2]);
                break;

            case TransformChangeType.Rotation:
                transform.localRotation = new Quaternion(sessionChange.values[0], sessionChange.values[1], sessionChange.values[2], sessionChange.values[3]);
                break;

            default:
                Debug.LogWarning("Unsupported SessionChangeType: " + sessionChange.type);
                break;
        }

        CacheValues();
    }

    public void OnComponentAdded(Component component)
    {
        OutgoingPacket packet = new OutgoingPacket(PacketType.CompomentAdded);
        packet.WriteString(id);
        packet.WriteString(component.GetType().FullName);

        Connection.GetConnection().SendPacket(packet);
    }

    public void OnComponentRemoved(Component component)
    {
        OutgoingPacket packet = new OutgoingPacket(PacketType.ComponentRemoved);
        packet.WriteString(id);
        packet.WriteString(component.GetType().FullName);

        Connection.GetConnection().SendPacket(packet);
    }

    /// <summary>
    /// Returns a list of any changes if they are required to be send.  
    /// </summary>
    /// <returns></returns>
    public List<TransformChange> GetChanges()
    {
        List<TransformChange> changes = new List<TransformChange>();

        if (transform.localPosition != cachedPosition)
        {
            cachedPosition = transform.localPosition;
            changes.Add(new TransformChange(id, TransformChangeType.Position, new float[] { cachedPosition.x, cachedPosition.y, cachedPosition.z }));
        }

        if (transform.localScale != cachedScale)
        {
            cachedScale = transform.localScale;
            changes.Add(new TransformChange(id, TransformChangeType.Scale, new float[] { cachedScale.x, cachedScale.y, cachedScale.z }));
        }

        if (transform.localRotation != cachedRotation)
        {
            cachedRotation = transform.localRotation;
            changes.Add(new TransformChange(id, TransformChangeType.Rotation, new float[] { cachedRotation.x, cachedRotation.y, cachedRotation.z, cachedRotation.w }));
        }

        return changes;
    }

    public void OnTransformParentChanged()
    {
        Debug.Log("Parent Changed!");


        string parentId = "none";
        if (transform.parent != null)
        {
            SamenNetworkObject parent = transform.parent.GetComponent<SamenNetworkObject>();
            parentId = parent.id;
        }

        Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.ParentChange)
            .WriteString(id)
            .WriteString(parentId));
    }

    public static Type GetTypeByName(string typeName)
    {
        // Try default first (works for some types)
        var type = Type.GetType(typeName);
        if (type != null)
            return type;

        // Search all loaded assemblies (including UnityEngine.dll)
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = asm.GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }
}

[CustomEditor(typeof(SamenNetworkObject), true)]
[CanEditMultipleObjects]
public class SamenNetworkObjectInspector : Editor
{
    private const string targetSceneName = "Session"; // Replace with your actual scene name

    public override VisualElement CreateInspectorGUI()
    {
        if (SceneManager.GetActiveScene().name != targetSceneName)
        {
            return base.CreateInspectorGUI();
        }

        // Otherwise, render ONLY the custom UI
        var root = new VisualElement();
        root.style.paddingTop = 6;
        root.style.paddingLeft = 10;

        // Title
        var title = new Label("Synced with Samen Server.");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 8;
        title.style.marginTop = 8;
        title.style.fontSize = 18;
        root.Add(title);

        // Warning
        var disclaimer = new Label("Please do not remove this component.");
        disclaimer.style.unityFontStyleAndWeight = FontStyle.Bold;
        disclaimer.style.marginBottom = 8;
        disclaimer.style.marginTop = 8;
        disclaimer.style.fontSize = 14;
        root.Add(disclaimer);

        return root;
    }
}
#endif