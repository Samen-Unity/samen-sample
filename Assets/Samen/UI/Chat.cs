using Samen.Network;
using Samen.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Samen.UI
{
    public class Chat
    {
        private static List<ChatMessage> chatMessages;

        public static void Clear()
        {
            chatMessages = new List<ChatMessage>();
            scrollPos = 0;
        }


        private static float scrollPos = 0;

        static string currentMessage;
        public static void CreateChatUI()
        {
            EditorGUILayout.Space(25);
            EditorGUILayout.LabelField("Chat", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(new Vector2(0, scrollPos), GUILayout.Height(200)).y;

            foreach (ChatMessage message in chatMessages)
            {
                EditorGUILayout.BeginVertical("helpbox");

                EditorGUILayout.LabelField(message.GetAuthor(), EditorStyles.boldLabel);
                EditorGUILayout.LabelField(message.GetContent());

                EditorGUILayout.EndVertical();

                GUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();


            currentMessage = EditorGUILayout.TextArea(currentMessage);
            if(GUILayout.Button("Send", GUILayout.Height(25)))
            {
                Connection.GetConnection().SendPacket(new OutgoingPacket(PacketType.ChatMessage)
                    .WriteString(currentMessage));

                currentMessage = "";
            }
;        }

        public static void AddMessage(ChatMessage message)
        {
            scrollPos = 1000;
            chatMessages.Add(message);
        }
    }

    public class ChatMessage
    {
        private string content;
        private string author;

        public string GetContent()
        {
            return content;
        }

        public string GetAuthor()
        {
            return author;
        }

        public ChatMessage(string author, string content)
        {
            this.author = author;
            this.content = content;
        }
    }
}
