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

public static class ComponentSerializer
{
    public static string Serialize(Component comp)
    {
        string json = EditorJsonUtility.ToJson(comp, false);
        JObject root = JObject.Parse(json);
        FindAndAddSamenReferences(root);
        string newJson = root.ToString(Newtonsoft.Json.Formatting.None);
        return newJson;
    }

    private static void FindAndAddSamenReferences(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            foreach (var property in obj.Properties())
            {
                if (property.Name == "instanceID" && property.Value.Type == JTokenType.Integer)
                {
                    int oldValue = (int)property.Value;

                    UnityEngine.Object reference = EditorUtility.InstanceIDToObject(oldValue);

                    string permaId = null;
                    string permaType = null;
                    if (reference is GameObject gameObject)
                    {
                        permaId = gameObject.GetComponent<SamenNetworkObject>().id;
                        permaType = "GameObject";
                    }
                    else if (reference is Component component)
                    {
                        permaId = component.gameObject.GetComponent<SamenNetworkObject>().id;
                        permaType = component.GetType().AssemblyQualifiedName;
                    }

                    property.Parent.Replace(new JObject
                    {
                        ["permaId"] = permaId ?? "(missing SamenNetworkObject)",
                        ["permaType"] = permaType ?? reference.GetType().Name
                    });
                }
                else
                {
                    FindAndAddSamenReferences(property.Value);
                }
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                FindAndAddSamenReferences(item);
            }
        }
    }

    public static void FindAndRemoveSamenReferences(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            var properties = obj.Properties().ToList();

            foreach (var property in properties)
            {
                if (property.Name == "instanceID" && property.Value.Type == JTokenType.Object)
                {
                    var instanceObj = (JObject)property.Value;
                    var permaIdToken = instanceObj["permaId"];
                    var permaTypeToken = instanceObj["permaType"];

                    if (permaIdToken == null || permaTypeToken == null)
                    {
                        continue;
                    }

                    string typeStr = permaTypeToken.ToString();
                    string id = permaIdToken.ToString();

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
                                Debug.LogWarning($"Component '{typeStr}' not found on GameObject '{owner.name}'.");
                            }
                            else
                            {
                                instanceId = component.GetInstanceID();
                            }
                        }
                    }

                    property.Value = new JValue(instanceId);
                }
                else
                {
                    FindAndRemoveSamenReferences(property.Value);
                }
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

        FindAndRemoveSamenReferences(root);

        string fixedJson = root.ToString(Newtonsoft.Json.Formatting.None);
        EditorJsonUtility.FromJsonOverwrite(fixedJson, target);
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
