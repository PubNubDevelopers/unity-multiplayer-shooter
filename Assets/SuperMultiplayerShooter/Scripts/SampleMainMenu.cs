using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using PubNubAPI;
using Newtonsoft.Json;

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
        public Button findMatchBTN;
        public Button customMatchBTN;
        public GameObject findMatchCancelButtonObj;
        public GameObject findingMatchPanel;
        public GameObject customGameRoomPanel;
        public Text matchmakingPlayerCountText;
        public InputField playerNameInput;
        public GameObject messagePopupObj;
        public Text messagePopupText;
        public GameObject characterSelectionPanel;
        public Image characterIconPresenter;
        public GameObject loadingPanel;
        public Toggle frameRateSetting;

        void Awake(){
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        // Use this for initialization
        void Start()
        {
            //Initialize PubNub Connection
            //Initialize PubNub Configuration
            
            PNConfiguration pnConfiguration = new PNConfiguration();
            pnConfiguration.SubscribeKey = "sub-c-68a629b6-9566-4f83-b74b-000b0fad8a69";
            pnConfiguration.PublishKey = "pub-c-27c9a7a7-bbb0-4210-8dd7-850512753b31";
            pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;
            string uuid = "User#" + Random.Range(0,9999).ToString();
            //Check if the user id already exists on this device. If not, save it.
            if(PlayerPrefs.HasKey("uuid"))
            {
                uuid = PlayerPrefs.GetString("uuid");
            }

            else
            {
                PlayerPrefs.SetString("uuid", uuid);
            }

            pnConfiguration.UserId = uuid;//SystemInfo.deviceUniqueIdentifier; //Guarenteed to be unique for every device. DOES NOT WORK FOR WEBGL BUILDS
            PubNubManager.PubNub = new PubNub(pnConfiguration);

            //Set memberships for existing user. Won't overwrite existing channels even if set multiple times.
            //Needed to invoke SubscribeEventEventArgs.UUIDEventResult calls whenever an object is updated.
            PNMembershipsSet inputMemberships = new PNMembershipsSet();
            inputMemberships.Channel = new PNMembershipsChannel
            {
                ID = "main-menu"
            };

            //Register the user
            PubNubManager.PubNub.SetMemberships().UUID(PubNubManager.PubNub.PNConfig.UserId).Set(new List<PNMembershipsSet> { inputMemberships }).Async((result, status) => {
                if (status.Error)
                {
                    Debug.Log("Error when setting membership: " + status.ErrorData.ToString());
                    //TODO: Retry in case of error.
                }              
            });

            //Pull all player metadata from this PubNub keyset. Save to a cached Dictionary to reference later on.
            PubNubManager.PubNub.GetAllUUIDMetadata().Async((result, status) => {            
                //result.Data is a List<PUUIDMetadataResult>. Details here https://www.pubnub.com/docs/sdks/unity/api-reference/objects#pnuuidmetadataresult
                if (result.Data.Count > 0)
                {
                    //Store players in a cached Dictionary to reference later on.
                    foreach (PNUUIDMetadataResult pnUUIDMetadataResult in result.Data)
                    {
                        PubNubManager.CachedPlayers.Add(pnUUIDMetadataResult.ID, pnUUIDMetadataResult);
                    }                
                }
                //Change playerInput name to be set to username of the user as long as the name was originally set.
                if(PubNubManager.CachedPlayers != null && PubNubManager.CachedPlayers.ContainsKey(PubNubManager.PubNub.PNConfig.UserId))
                {
                    playerNameInput.text = PubNubManager.CachedPlayers[PubNubManager.PubNub.PNConfig.UserId].Name;
                }
                //If current user cannot be found in cached players, then a new user is lgoged in. Set the metadata and add.
                else
                {
                    //Set name as first six characters of UserID for now.
                    PubNubManager.PubNub.SetUUIDMetadata().Name(PubNubManager.PubNub.PNConfig.UserId.Substring(0,6)).UUID(PubNubManager.PubNub.PNConfig.UserId).Async((result, status) =>
                    {
                        if(!status.Error)
                        {
                            PubNubManager.CachedPlayers.Add(result.ID, result);
                            playerNameInput.text = result.ID;
                        }

                        else
                        {
                            //TODO: handle in case of errors.
                        }
                    });
                }
            });

            //Listen for any new incoming messages, presence, object, and state changes.
            PubNubManager.PubNub.SubscribeCallback += (sender, e) => {
                SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;

                /*
                if (mea.MessageResult != null)
                {
                    //Extract any metadata from the message publish.
                    //Used to test in debug console.
                    
                    var metaDataJSON = JsonConvert.SerializeObject(mea.MessageResult.UserMetadata);
                    string username = "";
                    var metaDataDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(metaDataJSON);
                    //If cannot find the metadata, grab first six chars of user id. If the user id is also blank, set back-up as guest.
                    if (metaDataDictionary.TryGetValue("name", out username))
                    {
                        //if(!PubNubManager.CachedPlayers.ContainsKey(mea.MessageResult.IssuingClientId))
                        //{
                            
                            //PNMembershipsSet inputMemberships = new PNMembershipsSet();
                            //inputMemberships.Channel = new PNMembershipsChannel
                            //{
                            //    ID = "menu"
                            //};
                        PubNubManager.PubNub.SetMemberships().UUID(mea.MessageResult.IssuingClientId).Set(new List<PNMembershipsSet> { inputMemberships }).Async((result, status) => {
                        //name has changed. update
                        if (!PubNubManager.CachedPlayers.ContainsKey(mea.MessageResult.IssuingClientId) || !username.Equals(PubNubManager.CachedPlayers[mea.MessageResult.IssuingClientId].Name))
                        {
                            PubNubManager.PubNub.SetUUIDMetadata().Name(username).UUID(mea.MessageResult.IssuingClientId).Async((result, status) =>
                            {
                                //TODO: handle in case of errors.
                                //
                                PubNubManager.CachedPlayers.Add(result.ID, result);
                            });
                        }
                            });
                       //}
                        
                    }
                       
                }
                */

                //Used to catch the online/offline status of friends and users.
                if (mea.PresenceEventResult != null)
                {
                    Debug.Log("In Example, SubscribeCallback in presence" + mea.PresenceEventResult.Channel + mea.PresenceEventResult.Occupancy + mea.PresenceEventResult.Event);
                }

                //Will need to associate users then with a channel and create channel memberships.
                //this way, this event will triggerred, and returns metadata about said users as well.
                //should trigger this event anytime metadata is updated.
                /*
                if(mea.MembershipEventResult != null)
                {
                    Debug.Log(mea.MembershipEventResult.UUID);
                    Debug.Log(mea.MembershipEventResult.Description);
                    Debug.Log(mea.MembershipEventResult.ChannelID);
                    Debug.Log(mea.MembershipEventResult.ObjectsEvent);
                }
                */

                //Whenever metadata is updated (username, etc), update local cached source.
                //Note: Does not trigger for our own updated username.
                if (mea.UUIDEventResult != null)
                {
                    //Player should already exist, unless their UUID was changed.
                    if(PubNubManager.CachedPlayers.ContainsKey(mea.UUIDEventResult.UUID))
                    {
                        //Update cached with new information
                        PubNubManager.CachedPlayers[mea.UUIDEventResult.UUID].Name = mea.UUIDEventResult.Name;
                        PubNubManager.CachedPlayers[mea.UUIDEventResult.UUID].Email = mea.UUIDEventResult.Email;
                        PubNubManager.CachedPlayers[mea.UUIDEventResult.UUID].ExternalID = mea.UUIDEventResult.ExternalID;
                        PubNubManager.CachedPlayers[mea.UUIDEventResult.UUID].ETag = mea.UUIDEventResult.ETag;
                    }

                    //Update user when their UUID has changed.
                    else
                    {
                        //TODO: Implement update when a user has changed their UUID.
                        //Probably not an issue for the demo.
                    }
                }
                if(mea.ChannelEventResult != null)
                {

                }           
            };

            //Subscribe to the lobby chat channel
            PubNubManager.PubNub.Subscribe()
               .Channels(new List<string>(){
                    "main-menu"
               })
               .WithPresence()
               .Execute();

            /*
            if (string.IsNullOrWhiteSpace(playerNameInput.text))
            {
                //Attempte to pull from PlayerPrefs first.
                if (PlayerPrefs.HasKey("name"))
                {
                    playerNameInput.text = PlayerPrefs.GetString("name");
                }
                else
                {
                    playerNameInput.text = "Player" + Random.Range(0, 9999);
                }
            }
           */

            
            
            
            PubNubManager.PubNub.GetMemberships().UUID(PubNubManager.PubNub.PNConfig.UserId).Async((result, status) =>
            {
                if (result.Data != null)
                {
                    var resultData = result.Data as List<PNMemberships>;
                    foreach (PNMemberships m in result.Data)
                    {
                       // Debug.Log(resultData[i].ID);
                    }
                }
            });
            //SetPlayerName();

            // Others:
            frameRateSetting.isOn = Application.targetFrameRate == 60;        
        }

        // Update is called once per frame
        void Update()
        {
            bool connecting = !PhotonNetwork.IsConnectedAndReady || PhotonNetwork.NetworkClientState == ClientState.ConnectedToNameServer || PhotonNetwork.InRoom;

            // Handling texts:
            connectionStatusText.text = connecting ? PhotonNetwork.NetworkClientState == ClientState.ConnectingToGameServer ? "Connecting..." : "Finding network..."
                : "Connected! (" + PhotonNetwork.CloudRegion + ") | Ping: " + PhotonNetwork.GetPing();
            connectionStatusText.color = PhotonNetwork.IsConnectedAndReady ? Color.green : Color.yellow;
            matchmakingPlayerCountText.text = PhotonNetwork.InRoom ? Connector.instance.totalPlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers : "Matchmaking...";

            // Handling buttons:
            customMatchBTN.interactable = !connecting;
            findMatchBTN.interactable = !connecting;
            findMatchCancelButtonObj.SetActive(PhotonNetwork.InRoom);

            // Handling panels:
            customGameRoomPanel.SetActive(Connector.instance.isInCustomGame);
            loadingPanel.SetActive(PhotonNetwork.NetworkClientState == ClientState.ConnectingToGameServer || PhotonNetwork.NetworkClientState == ClientState.DisconnectingFromGameServer);

            // Messages popup system (used for checking if we we're kicked or we quit the match ourself from the last game etc):
            if (DataCarrier.message.Length > 0)
            {
                messagePopupObj.SetActive(true);
                messagePopupText.text = DataCarrier.message;
                DataCarrier.message = "";
            }
        }

        // Changes the player name whenever the user edits the Nickname input (on enter or click out of input field)
        public void SetPlayerName()
        {
            //TODO: Update to remove nickname from this section.
            PhotonNetwork.NickName = playerNameInput.text;
            // Update metadata for current logged in user name.
            PubNubManager.PubNub.SetUUIDMetadata().Name(playerNameInput.text).UUID(PubNubManager.PubNub.PNConfig.UserId).Async((result, status) =>
            {
                //TODO: handle in case of errors.
                //
                var obj = result;
            });
        }

        // Main:
        public void FindMatch(){
            // Enable the "finding match" panel:
            findingMatchPanel.SetActive(true);
            // ...then finally, find a match:
            Connector.instance.FindMatch();
        }

        // Others:
        // *called by the toggle itself in the "On Value Changed" event:
        public void ToggleTargetFps(){
            Application.targetFrameRate = frameRateSetting.isOn? 60 : 30;

            // Display a notif message:
            if (frameRateSetting.isOn){
                DataCarrier.message = "Target frame rate has been set to 60.";
            }
        }
    }
}