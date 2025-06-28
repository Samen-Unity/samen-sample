using UnityEngine;

/// <summary>
/// A change, with its arguments
/// </summary>
public class SessionChange
{
    /// <summary>
    /// The id of the object that was changed
    /// </summary>
    public string objectId;

    /// <summary>
    /// The type that was changed
    /// </summary>
    public SessionChangeType type;

    /// <summary>
    /// The new values
    /// </summary>
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
