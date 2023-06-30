using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Chat;
using Photon.Pun;
using System;
using Newtonsoft.Json;

namespace Visyde
{
    /// <summary>
    /// Chat System (Lobby in main menu only)
    /// - manages the chat system itself as well as the chat's UI in one script
    /// </summary>

    public class ChatSystem : MonoBehaviour
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
        public Dropdown languageOptions;

        // Internals:
        VerticalLayoutGroup vlg;
       // private PubNub _pubnub;
 
        private string _lobbySubscribe = "chat.translate."; //Wildcard subscribe to listen for all channels
        private string _lobbyPublish = "chat.translate."; //Publish channel will include
        private string targetLanguage = "en"; //Changes based on when users select a different value in the drop-down list.

        // Use this for initialization. Will initiate at main menu since the manager that controls this window is attached to the MainMenu.Managers.
        void Start()
        {
            vlg = messageDisplay.transform.parent.GetComponent<VerticalLayoutGroup>();
           // _pubnub = PubNubManager.Instance.InitializePubNub();
            languageOptions.onValueChanged.AddListener(delegate {
                OnLanguageChange(languageOptions);
            });
        }

        void OnEnable()
        {
            Connector.instance.onJoinRoom += OnJoinedRoom;
            Connector.instance.onLeaveRoom += OnLeftRoom;
        }
        void OnDisable()
        {
            Connector.instance.onJoinRoom -= OnJoinedRoom;
            Connector.instance.onLeaveRoom -= OnLeftRoom;
        }

        // Update is called once per frame
        void Update()
        {
            //Can always check to see if pubnub connection still active.
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendChatMessage();
            }
        }

        /// <summary>
        /// Publishes the chat message.
        /// </summary>
        public void SendChatMessage()
        {
            if (!string.IsNullOrEmpty(inputField.text))
            {
                MessageModeration filter = new MessageModeration();
                filter.text = inputField.text;
                filter.source = "en";
                filter.target = targetLanguage;           
               /* _pubnub.Publish()
                    .Channel(_lobbyPublish)
                    .Message(filter)
                    .Async((result, status) => {
                        if (status.Error)
                        {
                            Debug.Log(status.Error);
                            Debug.Log(status.ErrorData.Info);
                        }              
                    });*/
                //clear input field.
                inputField.text = string.Empty;
            }
        }
        public void SendSystemChatMessage(string message, bool negative)
        {
            //DisplayChat(message, "", false, true, negative);
        }

        /// <summary>
        /// Displays the chat mesage
        /// </summary>
        /// <param name="message">The message from the user</param>
        /// <param name="from">The user who the sent the message</param>
        /// <param name="ours">Is the message sent from client</param>
        /// <param name="systemMessage">Message sent by systen.</param>
        /// <param name="negative">Color scheme</param>
        void DisplayChat(string message, string from, bool ours, bool systemMessage, bool negative)
        {

            string finalMessage = "";

            if (systemMessage)
            {
                finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(negative ? negativeNotifColor : positiveNotifColor) + ">" + message + "</color>";
            }
            else
            {
                finalMessage = "\n" + "<color=#" + ColorUtility.ToHtmlStringRGBA(ours ? ourColor : othersColor) + ">" + from + "</color>: <color=" + ColorUtility.ToHtmlStringRGBA(ours ? ourChatColor : othersChatColor) + ">" + message + "</color>";
            }
            messageDisplay.text += finalMessage;

            // Canvas refresh:
            Canvas.ForceUpdateCanvases();
            vlg.enabled = false;
            vlg.enabled = true;
        }

        /// <summary>
        /// Photon event triggerred when a user joins a room.
        /// </summary>
        void OnJoinedRoom()
        {
            _lobbyPublish += PhotonNetwork.CurrentRoom.Name; //Guarenteed to be unique, set by server.
            _lobbySubscribe += PhotonNetwork.CurrentRoom.Name;
            loadingIndicator.SetActive(false);
            messageDisplay.text = "";
           
            //Listen for any new incoming messages
           /* _pubnub.SubscribeCallback += (sender, e) => {
                SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;
                if (mea.MessageResult != null)
                {
                    string message = ParseMessage(mea.MessageResult.Payload);
                    GetUsername(mea.MessageResult, message);
                }
            };

            //Subscribe to the lobby chat channel
            _pubnub.Subscribe()
               .Channels(new List<string>(){
                    _lobbySubscribe
               })
               .Execute();*/
        }  
        /*
        /// <summary>
        /// Obtains the username and displays the chat.
        /// </summary>
        /// <param name="message"></param>
        private void GetUsername(PNMessageResult payload, string message)
        {
            string username = "";
            if (PubNubManager.Instance.CachedPlayers.ContainsKey(payload.IssuingClientId))
            {
                username = PubNubManager.Instance.CachedPlayers[payload.IssuingClientId].Name;
            }

            //Check in case the username is null. Set to back-up of UUID just in case.
            if(string.IsNullOrWhiteSpace(username))
            {
                username = payload.IssuingClientId;
            }
         
            //Display the chat pulled.
            DisplayChat(message, username, PubNubManager.Instance.UserId.Equals(payload.IssuingClientId), false, false);
        }*/

        /// <summary>
        /// Parses the payload for translating the message.
        /// If the payload does contain profanity, simply prints the entire message as asterisks.
        /// You can, however, isolate the profanity and replace only the words containing profanity with asterisks.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        private string ParseMessage(object payload)
        {
            object message = "";
            var metaDataJSON = JsonConvert.SerializeObject(payload);

            //Extracting text from the payload. If it cannot be found, simply print a temp message.
            var messageDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(metaDataJSON);
            if (messageDictionary == null || !messageDictionary.TryGetValue("text", out message))
            {
                message = "<...>";
            }
            return message.ToString();
        }

        /// <summary>
        /// Called when the user changes the language in the drop-down.
        /// </summary>
        private void OnLanguageChange(Dropdown change)
        {

            switch (change.value)
            {
                case 0:
                    targetLanguage = "en";
                     break;
                case 1:
                    targetLanguage = "es";
                    break;
                case 2:
                    targetLanguage = "fr";
                    break;
                case 3:
                    targetLanguage = "ja";
                    break;
                default:
                    targetLanguage = "en";
                    break;
            }
        }

        void OnLeftRoom()
        {
            /*
            //Unsubscrube from lobby chat once player leaves the room.
            _pubnub.Unsubscribe()
                .Channels(new List<string>()
                {
                    _lobbySubscribe
                })
                .Async((result, status) =>
                {
                    if (status.Error)
                    {
                        Debug.Log(string.Format("Unsubscribe Error: {0} {1} {2}", status.StatusCode, status.ErrorData, status.Category));
                    }
                    else
                    {
                        Debug.Log(string.Format("DateTime {0}, In Unsubscribe, result: {1}", DateTime.UtcNow, result.Message));
                    }
                });
            */
        }

        //Photon functions. Will be removed as more functionality is integrated.
        public void OnChatStateChange(ChatState state) { }
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
        public void OnPrivateMessage(string sender, object message, string channelName) { }    
        public void OnUserUnsubscribed(string channel, string user) { }
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