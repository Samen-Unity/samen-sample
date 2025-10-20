#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A temporary object to move the cursor up and down.
/// </summary>
[ExecuteInEditMode]
public class Pinger : MonoBehaviour {

    float timer = 0;
    Transform arrowModel;
    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        Debug.LogWarning($"[Ping] Ping at {transform.position}!", this.transform);
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        // Remove after a while
        if(timer > 10)
        {
            DestroyImmediate(this.gameObject);
        }
    }
    private void OnDrawGizmos()
    {
        // Get the arrow model
        if(arrowModel == null)
            arrowModel = transform.GetChild(0);

        // Move up and down
        timer += Time.deltaTime;

        // Move the arrow button up and down following the bounce function
        arrowModel.localPosition = new Vector3(0, BounceFunction((Mathf.Cos(timer * 4) + 1) / 2), 0);

        // Slowly make the object smaller
        float scale = 10 - timer;
        scale /= 10;

        arrowModel.localScale = new Vector3(scale, scale, scale);
        
        // Draw a little sphere that can go through objects.
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }

    private float BounceFunction(float x)
    {
        return x < 0.5
           ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * x, 2))) / 2
           : (Mathf.Sqrt(1 - Mathf.Pow(-2 * x + 2, 2)) + 1) / 2;
    }
}
#endif