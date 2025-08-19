#if UNITY_EDITOR
using Assets.Samen.Data;
using Samen.Network;
using UnityEngine;

[ExecuteInEditMode]
public class Reconnect : MonoBehaviour
{
    public void Start()
    {
        if(ConnectionPrefs.GetRemember())
        {
            Connection.Connect(ConnectionPrefs.GetLastIp(), ConnectionPrefs.GetLastPort(), ConnectionPrefs.GetLastUsername(), ConnectionPrefs.GetLastPassword());
        }
    }
}
#endif