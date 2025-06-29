using Samen;
using System;
using System.Collections.Generic;
using UnityEngine;


// Any object in a session has this
[ExecuteInEditMode]
public class SamenNetworkObject : MonoBehaviour
{

    private void Start()
    {
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

                Debug.Log("Duplication send, new is");

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
    public void ApplyChange(SessionChange sessionChange)
    {
        switch (sessionChange.type)
        {
            case SessionChangeType.Position:
                transform.localPosition = new Vector3(sessionChange.values[0], sessionChange.values[1], sessionChange.values[2]);
                break;

            case SessionChangeType.Scale:
                transform.localScale = new Vector3(sessionChange.values[0], sessionChange.values[1], sessionChange.values[2]);
                break;

            case SessionChangeType.Rotation:
                transform.localRotation = new Quaternion(sessionChange.values[0], sessionChange.values[1], sessionChange.values[2], sessionChange.values[3]);
                break;

            default:
                Debug.LogWarning("Unsupported SessionChangeType: " + sessionChange.type);
                break;
        }

        CacheValues();
    }

    /// <summary>
    /// Returns a list of any changes if they are required to be send.  
    /// </summary>
    /// <returns></returns>
    public List<SessionChange> GetChanges()
    {
        List<SessionChange> changes = new List<SessionChange>();

        if (transform.localPosition != cachedPosition)
        {
            cachedPosition = transform.localPosition;
            changes.Add(new SessionChange(id, SessionChangeType.Position, new float[] { cachedPosition.x, cachedPosition.y, cachedPosition.z }));
        }

        if (transform.localScale != cachedScale)
        {
            cachedScale = transform.localScale;
            changes.Add(new SessionChange(id, SessionChangeType.Scale, new float[] { cachedScale.x, cachedScale.y, cachedScale.z }));
        }

        if (transform.localRotation != cachedRotation)
        {
            cachedRotation = transform.localRotation;
            changes.Add(new SessionChange(id, SessionChangeType.Rotation, new float[] { cachedRotation.x, cachedRotation.y, cachedRotation.z, cachedRotation.w }));
        }

        return changes;
    }

}
