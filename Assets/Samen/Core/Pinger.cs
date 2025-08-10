#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Pinger : MonoBehaviour {


    float timer = 0;
    Transform arrowModel;


    double startTime;
    private void OnEnable()
    {
        startTime = (float)EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
        Debug.LogWarning($"[Ping] Ping at {transform.position}!", this);
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }


    private void OnEditorUpdate()
    {
        if(timer > 10)
        {
            DestroyImmediate(this.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if(arrowModel == null)
            arrowModel = transform.GetChild(0);

        // Move up and down
        timer += Time.deltaTime;
        arrowModel.localPosition = new Vector3(0, BounceFunction((Mathf.Cos(timer * 4) + 1) / 2), 0);


        float scale = 10 - timer;
        scale /= 10;

        arrowModel.localScale = new Vector3(scale, scale, scale);


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