using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PubnubApi;
using PubnubApi.Unity;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

namespace Visyde
{
    /// <summary>
    /// Sample Main Menu
    /// - A sample script that handles the main menu UI.
    /// </summary>

    public class SampleMainMenu : MonoBehaviour
    {
        [Header("Main:")]
        public Text connectionStatusText;
        public Button customMatchBTN;
        public GameObject customGameRoomPanel;
        public Button customizeCharacterButton;
        public InputField playerNameInput;
        public GameObject messagePopupObj;
        public Text messagePopupText;
        public GameObject characterSelectionPanel;
        public Image characterIconPresenter;
        public GameObject loadingPanel;
        public Text totalCountPlayers;
        public GameObject chat;
        public GameObject lobbyBrowserPanel;
        public GameObject settingsPanel;
        public GameObject cosmeticsPanel;

        void Awake(){
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        // Use this for initialization
        void Start()
        {
            //Add Listeners
            Connector.instance.OnGlobalPlayerCountUpdate += UpdateGlobalPlayers;
            Connector.instance.OnConnectorReady += ConnectorReady;
            Connector.instance.onPubNubObject += OnPnObject;
        }

        /// <summary>
        /// Watch for any presence event changes by subscribing to Connector
        /// </summary>
        /// <param name="count">The global number of players in the game</param>
        private void UpdateGlobalPlayers(int count)
        {
            totalCountPlayers.text = count.ToString();
        }

        /// <summary>
        /// Watch for when the connector is ready.
        /// </summary>
        private void ConnectorReady()
        {
            //Sets up the Main Menu based on player cached information.
            MainMenuSetup();
        }
    
        /// <summary>
        /// Event listener to handle PubNub Object events
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="result"></param>
        private void OnPnObject(PNObjectEventResult result)
        {
            //Catch other player updates (when their name changes, etc)
            if (result != null)
            {
                //User Metadata Update from another player, such as a name change. Update chached player
                if (result.Type.Equals("uuid"))
                {
                    UserMetadata meta = new UserMetadata
                    {
                        Uuid = result.UuidMetadata.Uuid,
                        Name = result.UuidMetadata.Name,
                        Email = result.UuidMetadata.Email,
                        ExternalId = result.UuidMetadata.ExternalId,
                        ProfileUrl = result.UuidMetadata.ProfileUrl,
                        Custom = result.UuidMetadata.Custom,
                        Updated = result.UuidMetadata.Updated
                    };

                    // Add new or update existing player
                    if(result.Event.Equals("set"))
                    {
                        if(PNManager.pubnubInstance.CachedPlayers.ContainsKey(result.UuidMetadata.Uuid))
                        {
                            PNManager.pubnubInstance.CachedPlayers[result.UuidMetadata.Uuid] = meta;
                        }

                        else
                        {
                            PNManager.pubnubInstance.CachedPlayers.Add(result.UuidMetadata.Uuid, meta);
                        }
                    }

                    // Remove player from cache
                    else if (result.Event.Equals("delete") && PNManager.pubnubInstance.CachedPlayers.ContainsKey(result.UuidMetadata.Uuid))
                    {
                        PNManager.pubnubInstance.CachedPlayers.Remove(result.UuidMetadata.Uuid);
                    }
                }            
            }
        }
      
        // Update is called once per frame
        void Update()
        {
            // Handling panels:
            customGameRoomPanel.SetActive(Connector.instance.isInCustomGame);

            // Messages popup system (used for checking if we we're kicked or we quit the match ourself from the last game etc):
            if (DataCarrier.message.Length > 0)
            {
                messagePopupObj.SetActive(true);
                messagePopupText.text = DataCarrier.message;
                DataCarrier.message = "";
            }

            //Listen for whenever user opens the chat window.
            if (chat.activeSelf == false && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                //Don't allow interaction with chat if windows
                if(!characterSelectionPanel.activeSelf && !lobbyBrowserPanel.activeSelf && !settingsPanel.activeSelf && !cosmeticsPanel.activeSelf)
                {
                    //opens the chat window.
                    chat.SetActive(true);
                }           
            }
        }

        /// <summary>
        /// Called whenever the scene or game ends. Unsubscribe event listeners.
        /// </summary>
        void OnDestroy()
        {
            Connector.instance.OnGlobalPlayerCountUpdate -= UpdateGlobalPlayers;
            Connector.instance.OnConnectorReady -= ConnectorReady;
            Connector.instance.onPubNubObject -= OnPnObject;

            //Clear out the cached players when changing scenes. The list needs to be updated when returning to the scene in case
            //there are new players.
            PNManager.pubnubInstance.CachedPlayers.Clear();
        }

        /// <summary>
        /// Extracts metadata for active user and performs menu setup.
        /// </summary>
        private void MainMenuSetup()
        {          
            //Change playerInput name to be set to username of the user as long as the name was originally set.
            if (PNManager.pubnubInstance.CachedPlayers.Count > 0 && PNManager.pubnubInstance.CachedPlayers.ContainsKey(Connector.instance.GetPubNubObject().GetCurrentUserId()))
            {
                playerNameInput.text = Connector.PNNickName = PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Name;
                //  Populate the available hat inventory and other settings, read from PubNub App Context
                Dictionary<string, object> customData = PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Custom;
                if (customData != null)
                {
                    List<int> availableHats = new List<int>();
                    if (customData.ContainsKey("hats"))
                    {
                        availableHats = JsonConvert.DeserializeObject<List<int>>(customData["hats"].ToString());
                    }

                    // Users should always have hats should never be null - catch legacy user situations.
                    else
                    {
                        availableHats = Connector.instance.GenerateRandomHats();
                        customData.Add("hats", availableHats);
                        PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Custom = customData;
                    }

                    Connector.instance.UpdateAvailableHats(availableHats);

                    if (customData.ContainsKey("language"))
                    {
                        Connector.UserLanguage = customData["language"].ToString();
                    }

                    if (customData.ContainsKey("60fps"))
                    {
                        bool result = false;
                        bool.TryParse(customData["60fps"].ToString(), out result);
                        Connector.IsFPSSettingEnabled = result;
                    }
                }
            }                        
        }
      
        // Changes the player name whenever the user edits the Nickname input (on enter or click out of input field)
        public async void SetPlayerName()
        {
            await PNManager.pubnubInstance.UpdateUserMetadata(Connector.instance.GetPubNubObject().GetCurrentUserId(), playerNameInput.text, PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Custom);
            Connector.PNNickName = PNManager.pubnubInstance.CachedPlayers[Connector.instance.GetPubNubObject().GetCurrentUserId()].Name = playerNameInput.text;     
        }
    }
}