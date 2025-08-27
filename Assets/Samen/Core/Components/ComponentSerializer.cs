#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

public static class ComponentSerializer
{
    public static Dictionary<string, object> Serialize(Component comp)
    {
        string json = EditorJsonUtility.ToJson(comp, true);
        JObject parsed = JObject.Parse(json);
        var result = new Dictionary<string, object>();

        foreach (var prop in parsed.Properties())
        {
            string name = prop.Name;
            JToken value = prop.Value;

            var field = comp.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                UnityEngine.Object objRef = field.GetValue(comp) as UnityEngine.Object;

                if (objRef == null)
                {
                    result[name] = null;
                    continue;
                }

                GameObject go = objRef switch
                {
                    GameObject goRef => goRef,
                    Component compRef => compRef.gameObject,
                    _ => null
                };

                var netObj = go?.GetComponent<SamenNetworkObject>();
                if (netObj == null)
                {
                    Debug.LogError($"Cannot serialize '{name}'. No SamenNetworkObject on {go?.name}!");
                    return null;
                }

                var refInfo = new ReferenceInfo(netObj.id, objRef.GetType().AssemblyQualifiedName);
                result[name] = refInfo;
            }
            else
            {
                result[name] = value.ToObject<object>();
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

            if (value is JObject jObj && jObj["ID"] != null)
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
                try
                {
                    object converted = Convert.ChangeType(value, field.FieldType);
                    field.SetValue(target, converted);
                }
                catch
                {
                    Debug.LogWarning($"Failed to set field {field.Name} on {target.name}");
                }
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
