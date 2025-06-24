using Samen;
using UnityEditor;
using UnityEngine;

public class ConnectionUI : EditorWindow
{
    private const string IP_KEY = "SamenConnection_IP";
    private const string PORT_KEY = "SamenConnection_Port";
    private const string USERNAME_KEY = "SamenConnection_Username";

    private string ip = "127.0.0.1";
    private string port = "4041";
    private string username = "Visitor";

    [MenuItem("Window/Samen/Connection")]
    public static void ShowWindow()
    {
        GetWindow<ConnectionUI>("Samen Connection");
    }

    private void OnEnable()
    {
        // Load saved values
        ip = EditorPrefs.GetString(IP_KEY, "127.0.0.1");
        port = EditorPrefs.GetString(PORT_KEY, "4041");
        username = EditorPrefs.GetString(USERNAME_KEY, "Visitor");
    }

    private void OnDisable()
    {
        // Save values when window is closed or Unity closes
        EditorPrefs.SetString(IP_KEY, ip);
        EditorPrefs.SetString(PORT_KEY, port);
        EditorPrefs.SetString(USERNAME_KEY, username);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Information", EditorStyles.boldLabel);

        username = EditorGUILayout.TextField("Username", username);

        EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);

        ip = EditorGUILayout.TextField("IP", ip);
        port = EditorGUILayout.TextField("Port", port);

        if (GUILayout.Button("Connect"))
        {
            Connect();
        }
    }

    private void Connect()
    {
        if (!int.TryParse(port, out int portNum))
        {
            Debug.LogError("Port must be a valid number");
            EditorUtility.DisplayDialog("Connection Failed", "You must enter a valid port.", "Okay.");
           
            return;
        }

        Debug.Log($"Connecting to {ip}:{portNum} as {username}...");

        try
        {
            var connection = new Connection(ip, portNum);
            connection.Connect();

            var packet = new OutgoingPacket(PacketType.Authenticate)
                .WriteString(username);

            Connection.GetConnection().Listen(PacketType.Authenticate, (packet) =>
            {
                Debug.Log("Connected and authenticated!");
            });

            Connection.GetConnection().SendPacket(packet);

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Connection failed: {ex.Message}");
          
            EditorUtility.DisplayDialog("Connection Failed", "Failed to connect to Samen.", "Okay.");
        }
    }
}


enum State
{
    Idle,
    Connecting,
    Connected,
    Failed
}