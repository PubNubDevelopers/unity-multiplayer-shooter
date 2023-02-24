using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Chat;
using Photon.Pun;
using Photon.Realtime;

namespace Visyde
{
    /// <summary>
    /// Chat System (Lobby in main menu only)
    /// - manages the chat system itself as well as the chat's UI in one script
    /// </summary>

    public class ChatSystem : MonoBehaviour, IChatClientListener
    {
        [Header("Settings:")]
        public Color ourColor;
        public Color othersColor;
        public Color ourChatColor;
        public Color othersChatColor;
        public Color positiveNotifColor;
        public Color negativeNotifColor;
        [Header("References:")]
        public Text messageDisplay;
        public InputField inputField;
        public Button sendButton;
        public GameObject loadingIndicator;

        ChatClient chatClient;
        public bool HasChatAppID{
			get{
                return !string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat);
            }
		}

        // Internals:
        VerticalLayoutGroup vlg;

        // Use this for initialization
        void Start()
        {
            vlg = messageDisplay.transform.parent.GetComponent<VerticalLayoutGroup>();
        }

        void OnEnable(){
            Connector.instance.onJoinRoom += OnJoinedRoom;
            Connector.instance.onLeaveRoom += OnLeftRoom;
        }
        void OnDisable(){
            Connector.instance.onJoinRoom -= OnJoinedRoom;
            Connector.instance.onLeaveRoom -= OnLeftRoom;
        }

        // Update is called once per frame
        void Update()
        {
            if (chatClient != null)
            {
                chatClient.Service();

                // Functionalities:
                if (chatClient.CanChat)
                {
                    sendButton.interactable = true;
                    loadingIndicator.SetActive(false);

                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        SendChatMessage();
                    }
                }
                else
                {
                    sendButton.interactable = false;
                    loadingIndicator.SetActive(true);
                }
            }
        }

        public void ConnectToChat(){
            if (HasChatAppID)
            {
                // Only connect if we have a chat ID specified in the PhotonServerSettings:
                chatClient = new ChatClient(this);
                chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, Connector.instance.gameVersion, new Photon.Chat.AuthenticationValues(PhotonNetwork.NickName));
            }
        }
        public void SendChatMessage(){
            if (!string.IsNullOrEmpty(inputField.text))
            {
                chatClient.PublishMessage(PhotonNetwork.CurrentRoom.Name, inputField.text);
                inputField.text = string.Empty;
            }
        }
        public void SendSystemChatMessage(string message, bool negative){
            DisplayChat(message, "", false, true, negative);
        }
        void DisplayChat(string message, string from, bool ours, bool systemMessage, bool negative){

            string finalMessage = "";
            
            if (systemMessage){
                finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(negative ? negativeNotifColor : positiveNotifColor) + ">" + message + "</color>";
            }
            else{
                finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(ours ? ourColor : othersColor) + ">" + from + "</color>: <color=" + ColorUtility.ToHtmlStringRGBA(ours ? ourChatColor : othersChatColor) + ">" + message + "</color>";
            }
            messageDisplay.text += finalMessage;

            // Canvas refresh:
            Canvas.ForceUpdateCanvases();
            vlg.enabled = false;
            vlg.enabled = true;
        }

        // Photon stuff:
        void OnJoinedRoom()
        {
            messageDisplay.text = "";
            // Connect to the chat server when we join a room:
            ConnectToChat();
        }
        void OnLeftRoom()
        {
            // Disconnect from the chat server when we leave a room:
            if (chatClient != null) chatClient.Disconnect();
        }
        public void OnChatStateChange(ChatState state) { }
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message){}
        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            // Get the name of the sender:
            int last = senders.Length - 1;
            // Display the message:
            DisplayChat (messages[last].ToString(), senders[last], senders[last] == PhotonNetwork.NickName, false, false);
        }
        public void OnPrivateMessage(string sender, object message, string channelName) { }
        public void OnUserSubscribed(string channel, string user) { }
        public void OnUserUnsubscribed(string channel, string user) { }
        public void OnConnected()
        {
            chatClient.Subscribe(new string[] { PhotonNetwork.CurrentRoom.Name }, 5); // subscribe to the chat channel once connected to the chat server
        }
        public void OnDisconnected() { }
        public void OnSubscribed(string[] channels, bool[] results) { }
        public void OnUnsubscribed(string[] channels) { }
        public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
        {
            if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
            {
                UnityEngine.Debug.LogError(message);
            }
            else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
            {
                UnityEngine.Debug.LogWarning(message);
            }
            else
            {
                UnityEngine.Debug.Log(message);
            }
        }
    }
}