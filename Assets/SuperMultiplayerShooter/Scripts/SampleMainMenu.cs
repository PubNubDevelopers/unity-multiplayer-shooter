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
using PubNubUnityShowcase;
using System.Runtime.ConstrainedExecution;
using Unity.VisualScripting;
using System;

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
        public Button shopButton;
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
        public Button nameChangeBtn;
        public Button chatBtn;
        public Button friendsBtn;
        public Button settingsBtn;
        public Text coinsText;
        public NotificationPopup notificationPopup;

        private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }
        public event Action<string, string> OnCurrencyUpdate;


        void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        // Use this for initialization
        async void Start()
        {
            //Add Listeners
            PNManager.pubnubInstance.onPubNubMessage += OnPnMessage;
            PNManager.pubnubInstance.onPubNubPresence += OnPnPresence;
            PNManager.pubnubInstance.onPubNubObject += OnPnObject;
            PNManager.pubnubInstance.onPubNubReady += OnPnReady;
            await PNManager.pubnubInstance.InitializePubNub();
        }

        async void OnPnReady()
        {
            Invoke("GetActiveGlobalPlayers", 2.0f); //  Give the system time to settle down before calling global player count
            await PNManager.pubnubInstance.GetAllUserMetadata(); //Loading Player Cache.
            customMatchBTN.interactable = true;
            customizeCharacterButton.interactable = true;
            shopButton.interactable = true;
            chatBtn.interactable = true;
            friendsBtn.interactable = true;
            settingsBtn.interactable = true;
            playerNameInput.interactable = true;
            nameChangeBtn.interactable = true;
            nameChangeBtn.onClick.AddListener(async () => await SetPlayerName());

            //Subscribe to trigger events whenever a new dropdown option is added.
            Connector.instance.OnCurrencyUpdated += UpdateCurrency;
            MainMenuSetup();
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
        /// Event listener to handle PubNub Message events
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="result"></param>
        private void OnPnMessage(PNMessageResult<object> result)
        {
            //Ignore messages from self (they are not translated / ran through profanity filter).
            if (result != null)
            {
                // Illuminate - Handle New Price Changes.
                // Workaround until Object Event Listeners are Working
                if (result.Channel.StartsWith("illuminate"))
                {
                    string message = "";
                    if(result.Channel.Equals("illuminate.itemshop"))
                    {
                        // The metadata has already been updated. Simply fetch the updated data and display new shop items.
                        Connector.instance.LoadShopData();
                        message = "Item" + result.Message.ToString() + "was just discounted in the shop!\r\nGo check it out!";
                    }
                    
                    else if (result.Channel.Equals("illuminate.discountcodes"))
                    {
                        // Force a reload on user discount codes as well
                        Connector.instance.LoadDiscountCodes();
                        message = "You've received the following discount code:\r\n" + result.Message.ToString();

                    }

                    notificationPopup.ShowPopup(message);
                }
                // Illuminate - Handle Receiving New Discount Code
            }
        }

        /// Listen for status updates to update metadata cache
        /// </summary>
        /// <param name="result"></param>
        private async void OnPnPresence(PNPresenceEventResult result)
        {
            // Determine if a player comes online that has yet to be cached.
            if (result != null && result.Uuid != null && result.Event.Equals("join")
                && result.Channel.Equals(PubNubUtilities.chanGlobal)
                && !PNManager.pubnubInstance.CachedPlayers.ContainsKey(result.Uuid))
            {
                //If not, obtain and cache the user.
                await PNManager.pubnubInstance.GetUserMetadata(result.Uuid);
            }

            if (result != null && result.Channel.Equals(PubNubUtilities.chanGlobal))
            {
                //Inform the total number of global players.
                UpdateGlobalPlayers(result.Occupancy);
            }
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
                    if (result.Event.Equals("set"))
                    {
                        if (PNManager.pubnubInstance.CachedPlayers.ContainsKey(result.UuidMetadata.Uuid))
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

                else if(result.Type.Equals("channel"))
                {
                    // Setup Shop Items
                    ShopItemData shopItem = new ShopItemData
                    {
                        id = result.ChannelMetadata.Custom["id"].ToString(),
                        description = result.ChannelMetadata.Custom["description"].ToString(),
                        category = result.ChannelMetadata.Custom["category"].ToString(),
                        currency_type = result.ChannelMetadata.Custom["currency_type"].ToString(),
                        price = Convert.ToInt32(result.ChannelMetadata.Custom["price"]),
                        quantity_given = Convert.ToInt32(result.ChannelMetadata.Custom["quantity_given"]),
                        discounted = Convert.ToBoolean(result.ChannelMetadata.Custom["discounted"]),
                        discount_codes = JsonConvert.DeserializeObject<List<string>>(result.ChannelMetadata.Custom["discount_codes"].ToString())
                    };

                    // Step 1: Find the index of the item with the matching id
                    int index = Connector.instance.ShopItemDataList.FindIndex(item => item.id == shopItem.id);

                    if (index != -1)
                    {
                        // Step 2: Item exists, so update it
                        Connector.instance.ShopItemDataList[index] = shopItem;
                    }
                    else
                    {
                        // Item not found, you might want to add it to the list instead
                        Connector.instance.ShopItemDataList.Add(shopItem);
                    }
                }
            }
        }

        // Update is called once per frame.
        // Note: Ensuring each Gameobject is not null due to the connection not being destroyed when
        // transitioning scenes.
        void Update()
        {
            // Handling panels:
            if (customGameRoomPanel != null)
            {
                customGameRoomPanel.SetActive(Connector.instance.isInCustomGame);

            }

            // Messages popup system (used for checking if we we're kicked or we quit the match ourself from the last game etc):
            if (messagePopupObj != null && DataCarrier.message.Length > 0)
            {
                messagePopupObj.SetActive(true);
                messagePopupText.text = DataCarrier.message;
                DataCarrier.message = "";
            }

            if (chat != null)
            {
                //Listen for whenever user opens the chat window.
                if (chat.activeSelf == false && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                {
                    //Don't allow interaction with chat if windows
                    if (!characterSelectionPanel.activeSelf && !lobbyBrowserPanel.activeSelf && !settingsPanel.activeSelf && !cosmeticsPanel.activeSelf)
                    {
                        //opens the chat window.
                        chat.SetActive(true);
                    }
                }
            }

            if (lobbyBrowserPanel != null && customGameRoomPanel != null)
            {
                //Don't allow chatting while searching for games.
                if (lobbyBrowserPanel.activeSelf && !customGameRoomPanel.activeSelf)
                {
                    chat.SetActive(false);
                }

                else
                {
                    chat.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Gets all the active global players in the game
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetActiveGlobalPlayers()
        {
            //  Determine who is present based on who is subscribed to the global channel.
            //  Called when we first launch to determine the game state
            PNResult<PNHereNowResult> herenowResponse = await pubnub.HereNow()
                .Channels(new string[]
                {
                    PubNubUtilities.chanGlobal
                })
                .ExecuteAsync();
            PNHereNowResult hereNowResult = herenowResponse.Result;
            PNStatus status = herenowResponse.Status;
            if (status != null && status.Error)
            {
                Debug.Log($"Error calling PubNub HereNow ({PubNubUtilities.GetCurrentMethodName()}): {status.ErrorData.Information}");
            }
            else
            {
                UpdateGlobalPlayers(hereNowResult.TotalOccupancy);
            }
            return true;
        }

        /// <summary>
        /// Updates the player's currency after successfully playing the game.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateCurrency()
        {
            //  Determine who is present based on who is subscribed to the global channel.
            //  Called when we first launch to determine the game state
            PNResult<PNHereNowResult> herenowResponse = await pubnub.HereNow()
                .Channels(new string[]
                {
                    PubNubUtilities.chanGlobal
                })
                .ExecuteAsync();
            PNHereNowResult hereNowResult = herenowResponse.Result;
            PNStatus status = herenowResponse.Status;
            if (status != null && status.Error)
            {
                Debug.Log($"Error calling PubNub HereNow ({PubNubUtilities.GetCurrentMethodName()}): {status.ErrorData.Information}");
            }
            else
            {
                UpdateGlobalPlayers(hereNowResult.TotalOccupancy);
            }
            return true;
        }

        /// <summary>
        /// Called whenever the scene or game ends. Unsubscribe event listeners.
        /// </summary>
        void OnDestroy()
        {
            PNManager.pubnubInstance.onPubNubMessage -= OnPnMessage;
            PNManager.pubnubInstance.onPubNubPresence -= OnPnPresence;
            PNManager.pubnubInstance.onPubNubObject -= OnPnObject;
            PNManager.pubnubInstance.onPubNubReady -= OnPnReady;

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
            if (PNManager.pubnubInstance.CachedPlayers.Count > 0 && PNManager.pubnubInstance.CachedPlayers.ContainsKey(pubnub.GetCurrentUserId()))
            {
                playerNameInput.text = Connector.PNNickName = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name;
                //  Populate the available hat inventory and other settings, read from PubNub App Context
                Dictionary<string, object> customData = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom;
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
                        PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom = customData;
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

                    //Set the Selected Hat
                    if (customData.ContainsKey("chosen_hat"))
                    {
                        int result;
                        if (Int32.TryParse(customData["chosen_hat"].ToString(), out result))
                        {
                            DataCarrier.chosenHat = result;
                        }
                    }

                    //Legacy Default situations
                    else
                    {
                        customData.Add("chosen_hat", DataCarrier.chosenHat);
                        PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom = customData;
                    }

                    //Set the Selected Avatar
                    if (customData.ContainsKey("chosen_character"))
                    {
                        int result;
                        if (Int32.TryParse(customData["chosen_character"].ToString(), out result))
                        {
                            DataCarrier.chosenCharacter = result;
                        }
                    }

                    //Legacy Default situations
                    else
                    {
                        customData.Add("chosen_character", DataCarrier.chosenCharacter);
                        PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom = customData;
                    }

                    // Add Coins (earned in-game currency) for the player
                    if(customData.ContainsKey("coins"))
                    {
                        if (Int32.TryParse(customData["coins"].ToString(), out int result))
                        {
                            // Check to see if the player has played a game. Update coins if they don't match.
                            if(DataCarrier.coins > 0 && DataCarrier.coins > result)
                            {
                                //Update coins.
                                Connector.instance.CurrencyUpdated("coins", DataCarrier.coins);
                            }

                            else
                            {
                                DataCarrier.coins = result;
                            }
                        }                     
                    }
                    
                    // Legacy Situations: All players should have at least 0 coins.
                    else
                    {
                        customData.Add("coins", "0");
                        PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom = customData;
                    }
                                  
                    // Displays the player's currency fields
                    DisplayCurrency();
                    //Update the sprite image
                    characterIconPresenter.sprite = DataCarrier.characters[DataCarrier.chosenCharacter].icon;
                }
            }
        }

        public void OnUserNameChanged()
        {
            //Only enable name change button interactivity if there's changes to be made
            if (Connector.PNNickName.Equals(playerNameInput.text))
            {
                nameChangeBtn.interactable = false;
            }

            else
            {
                nameChangeBtn.interactable = true;
            }
        }

        // Changes the player name
        public async Task<bool> SetPlayerName()
        {
            await PNManager.pubnubInstance.UpdateUserMetadata(pubnub.GetCurrentUserId(), playerNameInput.text, PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom);
            Connector.PNNickName = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name = playerNameInput.text;
            nameChangeBtn.interactable = false;
            return true;
        }

        /// <summary>
        /// Displays the currency updates in the UI
        /// </summary>
        public void DisplayCurrency()
        {
            coinsText.text = DataCarrier.coins.ToString();
        }

        /// <summary>
        /// Updates the player's metadata with the currency.
        /// </summary>
        /// <param name="currencyKey"></param>
        private async void UpdateCurrency(string currencyKey, int value)
        {
            
            Dictionary<string, object> customData = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom;

            // Add Coins (earned in-game currency) for the player
            if (customData.ContainsKey(currencyKey))
            {
                customData[currencyKey] = value;                 
            }
              
            await PNManager.pubnubInstance.UpdateUserMetadata(pubnub.GetCurrentUserId(), Connector.PNNickName, customData);
            DisplayCurrency();
        }
    }
}