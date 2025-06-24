using UnityEngine;

public class SessionChange
{
    public string objectId;
    public SessionChangeType type;
    public float[] values;

    public SessionChange(string objectId, SessionChangeType type, float[] values)
    {
        this.objectId = objectId;
        this.type = type;
        this.values = values;
    }
}

public enum SessionChangeType
{
    Position,
    Rotation,
    Scale
}
