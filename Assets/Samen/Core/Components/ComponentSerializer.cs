#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ComponentSerializer
{
    public static Dictionary<string, object> Serialize(Component comp)
    {

        var result = new Dictionary<string, object>();
        var fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            var value = field.GetValue(comp);

            if (value is UnityEngine.Object unityObj)
            {
                GameObject go = null;

                if (unityObj is GameObject gameObj)
                    go = gameObj;
                else if (unityObj is Component compt)
                    go = compt.gameObject;
                SamenNetworkObject compB = go?.GetComponent<SamenNetworkObject>();

                if(compB == null)
                {
                    Debug.LogError("You can only serialize components with a SamenNetworkObject attached.");
                    return null;
                }

                var id = compB.id;
                if (id != null)
                {
                    var refInfo = new ReferenceInfo(id, value.GetType().AssemblyQualifiedName);
                      
                    result[field.Name] = refInfo;
                }
                else
                {
                    result[field.Name] = null;
                }
            }
            else
            {
                result[field.Name] = value;
            }
        }

        return result;
    }

    public static void Apply(Component target, Dictionary<string, object> data)
    {
        var type = target.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (!data.TryGetValue(field.Name, out var value)) continue;

            if (value is Newtonsoft.Json.Linq.JObject jObj && jObj["ID"] != null)
            {
                var refInfo = jObj.ToObject<ReferenceInfo>();
                var go = GetWithId(refInfo.ID);

                if (go != null)
                {
                    UnityEngine.Object refValue = null;

                    if (refInfo.componentType == typeof(GameObject).AssemblyQualifiedName)
                        refValue = go;
                    else
                    {
                        var t = Type.GetType(refInfo.componentType);
                        if (t != null)
                            refValue = go.GetComponent(t);
                    }

                    if (refValue != null)
                        field.SetValue(target, refValue);
                }
            }
            else
            {
                object converted = Convert.ChangeType(value, field.FieldType);
                field.SetValue(target, converted);
            }
        }
    }


    private static GameObject GetWithId(string id)
    {
        return GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None)
            .FirstOrDefault(o => o.id == id)?.gameObject;
    }
}
[Serializable]
public class ReferenceInfo
{
    public string ID { get; set; }
    public string componentType { get; set; }

    public ReferenceInfo(string id, string componentType)
    {
        ID = id;
        this.componentType = componentType;
    }
}
#endif