#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// A change, with its arguments
/// </summary>
public class TransformChange
{
    /// <summary>
    /// The id of the object that was changed
    /// </summary>
    public string objectId;

    /// <summary>
    /// The type that was changed
    /// </summary>
    public TransformChangeType type;

    /// <summary>
    /// The new values
    /// </summary>
    public float[] values;

    public TransformChange(string objectId, TransformChangeType type, float[] values)
    {
        this.objectId = objectId;
        this.type = type;
        this.values = values;
    }
}

public enum TransformChangeType
{
    Position,
    Rotation,
    Scale
}
#endif