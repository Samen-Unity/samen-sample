#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting.Antlr3.Runtime;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public static class ComponentSerializer
{
    public static string Serialize(Component comp)
    {
        string json = EditorJsonUtility.ToJson(comp, false);
       
        JObject root = JObject.Parse(json);
        FindAndAddSamenReferences(root, comp);
        string newJson = root.ToString(Newtonsoft.Json.Formatting.None);

        return newJson;
    }

    private static void FindAndAddSamenReferences(JToken token, Component comp)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            foreach (var property in obj.Properties())
            {
                if (property.Name == "instanceID" && property.Value.Type == JTokenType.Integer)
                {
                    var fieldName = property.Parent.Path.Split('.').Last(); 

                    var fieldInfo = comp.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    UnityEngine.Object reference = null;

                    if (fieldInfo != null)
                    {
                        var value = fieldInfo.GetValue(comp);
                        if (value is UnityEngine.Object unityObj)
                            reference = unityObj;
                    }
                    else
                    {
                        var propInfo = comp.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (propInfo != null)
                        {
                            var value = propInfo.GetValue(comp);
                            if (value is UnityEngine.Object unityObj)
                                reference = unityObj;
                        }
                    }

                    string permaId = null;
                    string permaType = null;

                    if(reference == null)
                    {
                        permaId = "Null";
                        permaType = "Null";
                        Debug.LogWarning("Value is null!");
                    }
                    else if (reference is GameObject gameObject)
                    {
                        permaId = gameObject.GetComponent<SamenNetworkObject>()?.id;
                        permaType = "GameObject";
                    }
                    else if (reference is Component component)
                    {
                        permaId = component.gameObject.GetComponent<SamenNetworkObject>()?.id;
                        permaType = component.GetType().AssemblyQualifiedName;
                    }

                    property.Parent.Replace(new JObject
                    {
                        ["permaId"] = permaId ?? "(missing SamenNetworkObject)",
                        ["permaType"] = permaType ?? (reference != null ? reference.GetType().Name : "null")
                    });
                }
                else
                {
                    FindAndAddSamenReferences(property.Value, comp);
                }
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                FindAndAddSamenReferences(item, comp);
            }
        }
    }

    public static void FindAndRemoveSamenReferences(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;

            if (obj.TryGetValue("permaId", out var permaIdToken) &&
                obj.TryGetValue("permaType", out var permaTypeToken))
            {
                string id = permaIdToken.ToString();
                string typeStr = permaTypeToken.ToString();
                int instanceId = -1;

                var owner = GetOwnerFromSamenId(id);

                if (owner == null)
                {
                    Debug.LogWarning($"No GameObject found for SamenID '{id}'");
                }
                else if (typeStr == "GameObject")
                {
                    instanceId = owner.GetInstanceID();
                }
                else
                {
                    Type compType = Type.GetType(typeStr);
                    if (compType == null)
                    {
                        Debug.LogWarning($"Type '{typeStr}' not found.");
                    }
                    else
                    {
                        var component = owner.GetComponent(compType);
                        if (component == null)
                        {
                            Debug.LogWarning($"Component '{typeStr}' not found on GameObject '{owner.name}'");
                        }
                        else
                        {
                            instanceId = component.GetInstanceID();
                        }
                    }
                }

                var parentProp = obj.Parent as JProperty;
                if (parentProp != null)
                {
                    parentProp.Value = new JObject
                    {
                        ["instanceID"] = instanceId
                    };
                }

                return;
            }

            foreach (var property in obj.Properties().ToList())
            {
                FindAndRemoveSamenReferences(property.Value);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                FindAndRemoveSamenReferences(item);
            }
        }
    }


    public static SamenNetworkObject GetOwnerFromSamenId(string samenId)
    {
        var allNetworkObjects = GameObject.FindObjectsByType<SamenNetworkObject>(FindObjectsSortMode.None);

        foreach (var networkObject in allNetworkObjects)
        {
            if (networkObject.id == samenId)
            {
                return networkObject;
            }
        }

        return null;
    }
    public static void Apply(Component target, string json)
    {
        JObject root = JObject.Parse(json);

        JObject cleaned = (JObject)root.DeepClone();
        RemoveObjRef(cleaned);
        string fixedJson = cleaned.ToString(Newtonsoft.Json.Formatting.None);
        EditorJsonUtility.FromJsonOverwrite(fixedJson, target);

        var so = new SerializedObject(target);
        var iterator = so.GetIterator();

        while (iterator.NextVisible(true))
        {
            if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.name != "m_Script")
            {
                var path = iterator.propertyPath;
                UnityEngine.Object refObj = GetReferenceFromPath(root, path);
                iterator.objectReferenceValue = refObj;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
    }

    private static UnityEngine.Object GetReferenceFromPath(JToken root, string propertyPath)
    {
        var tokens = propertyPath.Split('.');
        JToken current = root["MonoBehaviour"];

        foreach (var token in tokens)
        {
            if (current == null) return null;

            current = current[token];
        }

        if (current is JObject refObj &&
            refObj.TryGetValue("permaId", out var permaIdToken) &&
            refObj.TryGetValue("permaType", out var permaTypeToken))
        {
            string permaId = permaIdToken.ToString();
            string permaType = permaTypeToken.ToString();

            var owner = GetOwnerFromSamenId(permaId);
            if (owner == null) return null;

            if (permaType == "GameObject") return owner.gameObject;

            var type = Type.GetType(permaType);
            if (type == null) return null;

            return owner.GetComponent(type);
        }

        return null;
    }


    private static void RemoveObjRef(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            var keysToRemove = new List<string>();

            foreach (var property in obj.Properties().ToList())
            {
                if (property.Value.Type == JTokenType.Object)
                {
                    var valObj = (JObject)property.Value;
                    if (valObj.ContainsKey("permaId") && valObj.ContainsKey("permaType"))
                    {
                        keysToRemove.Add(property.Name);
                        continue;
                    }
                }
                RemoveObjRef(property.Value);
            }

            foreach (var key in keysToRemove)
            {
                obj.Remove(key);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                RemoveObjRef(item);
            }
        }
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
