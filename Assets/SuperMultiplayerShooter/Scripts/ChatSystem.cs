using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using PubnubApi;
using PubNubUnityShowcase;
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
       // public Text messageDisplay;
       // public InputField inputField;
       // public Button sendButton;
       // public GameObject loadingIndicator;
        public Dropdown languageOptions;

        // Internals:
        VerticalLayoutGroup vlg;
        private string _lobbyPublish = "chat.translate."; //Publish channel will include
        private string targetLanguage = "en"; //Changes based on when users select a different value in the drop-down list.

        // Use this for initialization. Will initiate at main menu since the manager that controls this window is attached to the MainMenu.Managers.
        void Start()
        {
            //vlg = messageDisplay.transform.parent.GetComponent<VerticalLayoutGroup>();
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


        /// <summary>
        /// Called whenever the scene or game ends. Unsbscribe from event listeners.
        /// </summary>
        private void OnDestroy()
        {
        }

      
        //  Event triggerred when a user joins a room.
        void OnJoinedRoom()
        {
         
            //_lobbyPublish = PubNubUtilities.chanPrefixLobbyChat + Connector.instance.CurrentRoom.OwnerId;
           
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
            //Remove Lobby option from dropdown.
            //print message in console indicating you left the lobby
            //change option to ALL as default
        }
    }
}