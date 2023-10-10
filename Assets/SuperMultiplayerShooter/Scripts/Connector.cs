using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PubnubApi;
using PubnubApi.Unity;
using UnityEngine.SceneManagement;
using PubNubUnityShowcase;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;

namespace Visyde
{
    /// <summary>
    /// Connector
    /// - Manages the initial connection, room creation and ongoing communication outside of a game
    /// </summary>
    public class Connector : MonoBehaviour
    {
        public static Connector instance;

        [Header("Settings:")]
        public string gameVersion = "0.1";
        public string gameSceneName = "";
        public int requiredPlayers;
        public string[] maps;

        [Header("Bot Players:")]
        public string[] botPrefixes;        // names for bots

        [Header("Other References:")]
        public CharacterSelector characterSelector;

        public bool tryingToJoinCustom { get; protected set; }
        bool inCustom = true;
        public bool isInCustomGame
        {
            get
            {
                return inCustom && InRoom;
            }
        }
        public bool isMasterClient
        {
            get
            {
                //  True if this instance created the room / game
                return (CurrentRoom != null && userId != null && CurrentRoom.OwnerId == userId);
            }
        }
        //  Return the ID assigned by the game (not the PubNub user ID) for your local player
        public int GetMyId()
        {
            if (CurrentRoom != null)
            {
                for (int i = 0; i < CurrentRoom.PlayerList.Count; i++)
                {
                    if (CurrentRoom.PlayerList[i].IsLocal)
                    {
                        return CurrentRoom.PlayerList[i].ID;
                    }
                }
            }
            return -1;
        }

        //  Return the ID assigned by the game (not the PubNub user ID) of the creator of the game / room
        public int GetMasterId()
        {
            if (CurrentRoom != null)
            {
                for (int i = 0; i < CurrentRoom.PlayerList.Count; i++)
                {
                    if (CurrentRoom.PlayerList[i].IsMasterClient)
                    {
                        return CurrentRoom.PlayerList[i].ID;
                    }
                }
            }
            return -1;
        }

        public int TotalPlayerCount { get; protected set; } // Number of players in a room

        //  PubNub properties
        private PubNubUtilities pubNubUtilities = new PubNubUtilities();
        private static string userId = null;    //  The PubNub user ID for the current instance
        private SampleMainMenu mainMenu;    //  Used to enable the join room button when PubNub is ready
        public PNRoomInfo CurrentRoom = null;
        public bool InRoom { get; set; } = false;
        public List<PNRoomInfo> pubNubRooms { get; set; }   //  List of created rooms system (managed by PubNub presence state)
        public static string PNNickName { get; set; } = "Uninitialized";  //  My nickname, ultimately populated from PubNub App Context
        public static string UserLanguage { get; set; }  // The language code of the user, defaulting to English (en)
        public static bool IsFPSSettingEnabled { get; set; } //Whether or not the setting is enabled for 60 FPS
        private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }
        public PNPlayer LocalPlayer { get; set; }
        private long roomCounter = 0;
        private int GAME_LENGTH_MAKE_ME_CONFIGURABLE = 90;
        //  End PubNub properties

        // Events and Actions
        public delegate void IntEvent(int i);
        public IntEvent onRoomListChange;
        public UnityAction onCreateRoomFailed;
        public UnityAction onJoinRoom;
        public UnityAction onLeaveRoom;
        public UnityAction onDisconnect;
        public event Action<string, string> OnPlayerSelect;
        public delegate void PlayerEvent(PNPlayer player);
        public PlayerEvent onPlayerJoin;
        public PlayerEvent onPlayerLeave;

        // Internal variables:
        private Bot[] curBots;
        private int bnp;
        private bool startCustomGameNow;
        private bool loadNow;                       // if true, the game scene will be loaded.
        private bool loadingInProgress = false;
        private bool isLoadingGameScene;

        //  The local definition of a bot
        public class Bot
        {
            public string name;				// bot name
            public Vector3 scores; 			// x = kills, y = deaths, z = other scores
            public int characterUsing;		// the chosen character of the bot (index only)
            public int hat;
        }

        void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            PNManager.pubnubInstance.onPubNubMessage -= OnPnMessage;
            PNManager.pubnubInstance.onPubNubPresence -= OnPnPresence;
            PNManager.pubnubInstance.onPubNubReady -= OnPnReady;
        }

        void Start()
        {
            //  PubNub initialization
            PNManager.pubnubInstance.onPubNubMessage += OnPnMessage;
            PNManager.pubnubInstance.onPubNubPresence += OnPnPresence;
            //  If PubNub is already initialized, then call OnPnReady() directly, else wait for the callback
            if (PNManager.pubnubInstance.pubnub != null)
            {
                OnPnReady();
            }
            else
            {
                PNManager.pubnubInstance.onPubNubReady += OnPnReady;    //  It will take some finite time to PubNub to initialize, during which we register for this ready event
            }
            loadNow = false;
            isLoadingGameScene = false;
            pubNubRooms = new List<PNRoomInfo>();
            userId = PlayerPrefs.GetString("uuid"); //  Stored in local storage when PNManager is instantiated
            UserLanguage = GetUserLanguage();
            IsFPSSettingEnabled = GetFPSSetting();
        }

        async void OnPnReady()
        {
            PNNickName = await PNManager.pubnubInstance.GetUserNickname();
            await PubNubGetRooms();
        }

        async void Update()
        {
            // Room managing:
            if (InRoom && !isLoadingGameScene)
            {
                // Set the variable "loadNow" to true if the room is already full:
                if (TotalPlayerCount >= CurrentRoom.MaxPlayers && ((isInCustomGame && startCustomGameNow) || !isInCustomGame))
                {
                    loadNow = true;
                }

                if (loadNow)
                {
                    if (isMasterClient && !loadingInProgress)
                    {
                        loadingInProgress = true;
                        //PNManager.pubnubInstance.onPubNubPresence -= OnPnPresence;
                        //  If we are the master client, load the game scene for ourselves and notify all other
                        //  participants in the game to load the game scene themselves (everyone is responsible
                        //  for spawning their own instances of all players in their own scenes)
                        CurrentRoom.IsOpen = false;
                        CurrentRoom.SortPlayerListAndAssignIds();
                        await SynchronizePlayerCharacteristics(); //  Exchange info about human players
                        await PubNubGameInProgress();
                        isLoadingGameScene = true;
                        SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);

                        //  Notify other participants to load their own scene and provide the required info to allow
                        //  this, e.g. which bots are present and their attributes.  Player info is sent separately elsewhere
                        Dictionary<string, object> loadProps = new Dictionary<string, object>();
                        loadProps.Add("gameSceneLoaded", gameSceneName);
                        loadProps.Add("currentRoomName", CurrentRoom.Name);
                        loadProps.Add("botCount", curBots.Length);
                        string[] bn = new string[curBots.Length];
                        int[] bc = new int[curBots.Length];
                        int[] bHats = new int[curBots.Length];
                        for (int i = 0; i < curBots.Length; i++)
                        {
                            bn[i] = curBots[i].name;
                            bc[i] = curBots[i].characterUsing;
                            bHats[i] = curBots[i].hat;
                        }
                        loadProps.Add("botNames", bn);
                        loadProps.Add("botCharacters", bc);
                        loadProps.Add("botHats", bHats);
                        loadProps.Add("roomOwnerId", CurrentRoom.OwnerId);
                        pubNubUtilities.PubNubSendRoomProperties(pubnub, loadProps);
                        loadNow = false;
                        loadingInProgress = false;
                    }
                }
            }
        }


        private async Task SynchronizePlayerCharacteristics()
        {
            //  Called by master.  Notify all other human players of eachother's characteristics
            for (int i = 0; i < CurrentRoom.PlayerList.Count; i++)
            {
                Dictionary<string, object> playerProps = new Dictionary<string, object>();
                playerProps.Add("playerStats", "stats");
                playerProps.Add("userId", CurrentRoom.PlayerList[i].UserId);
                playerProps.Add("character", CurrentRoom.PlayerList[i].Character);
                int[] cosmetics = new int[1];   // You can have as many items as you want, but in our case we only need 1 and that's for the hat
                cosmetics[0] = CurrentRoom.PlayerList[i].Cosmetics[0];
                playerProps.Add("cosmetics", cosmetics);
                playerProps.Add("roomOwnerId", CurrentRoom.OwnerId);
                await pubNubUtilities.PubNubSendRoomProperties(pubnub, playerProps);
            }
        }

        //  Join a specified room / game
        public void JoinCustomGame(PNRoomInfo room)
        {
            tryingToJoinCustom = true;
            CurrentRoom = room;
            if (isMasterClient)
            {
                PNPlayer self = new PNPlayer(userId, PNNickName, true, true, DataCarrier.chosenCharacter, DataCarrier.chosenHat);
                LocalPlayer = self;
                CurrentRoom.PlayerList.Add(self);
                OnJoinedRoom(CurrentRoom.OwnerId);
            }
            else
            {
                //  Send a message to the host that we want to join their game
                PNJoinCustomRoom(pubnub, "rooms." + room.OwnerId, userId, PNNickName, room.OwnerId);
            }
        }

        //  Create a room / game (by definition, the creator is the master)
        //  Implementation note: Rooms are defined using PubNub's presence system -
        //  Each player (UUID) is able to create a room and when they do so, all other players are notified
        //  that the channel's presence state has changed (through the state-change event).  Other players can then join
        //  a room by sending a PubNub message to the room owner, or are free to create their own rooms which others can join.
        //  The current implementation allows each player to create up to 1 room at a time, but this is not a limitation
        //  of PubNub, it is a limitation of this game's implementation.
        //  One advangate of using the presence system to define rooms is you also get notified when a player's presence
        //  changes (e.g. they go offline) on the same channel but there are other ways to implement a lobby and
        //  room system with PubNub.  Alternatively you could use PubNub's message history
        //  to store a created room, then others could read a channel's history to decide which room to join.
        public async Task<bool> CreateCustomGame(int selectedMap, int maxPlayers, bool allowBots, int gameLength)
        {
            if (pubnub != null)
            {
                //  The newly created game state is stored in PubNub User State (part of the Presence system)
                //  This state is cleared if a user disconnects
                Dictionary<string, object> metaData = new Dictionary<string, object>();
                metaData["name"] = PNNickName;
                metaData["visible"] = 1;    //  If the user leaves the room then we can toggle the room's visibility
                metaData["inProgress"] = 0; //  Set to 1 when the game starts
                metaData["ownerId"] = userId;
                metaData["maxPlayers"] = maxPlayers;
                metaData["started"] = 0;
                metaData["map"] = selectedMap;
                metaData["customAllowBots"] = allowBots ? 1 : 0;
                metaData["gameLength"] = gameLength * 60; //Match Duration will be set in seconds
                string channelName = PubNubUtilities.chanGlobal;
                PNResult<PNSetStateResult> setPresenceStateResponse = await pubnub.SetPresenceState()
                    .Channels(new string[] { channelName })
                    .Uuid(userId)
                    .State(metaData)
                    .ExecuteAsync();
                if (setPresenceStateResponse != null && setPresenceStateResponse.Status.Error)
                {
                    Debug.Log($"Error setting PubNub Presence State ({PubNubUtilities.GetCurrentMethodName()}): {setPresenceStateResponse.Status.ErrorData.Information}");
                }
            }
            return true;
        }

        public void StartCustomGame()
        {
            // Start creating bots (if bots are allowed) as this will fill out the empty players:
            if (inCustom && !loadNow)
            {
                // Create the bots if allowed:
                if (CurrentRoom.AllowBots)
                {
                    // Clear the bots array first:
                    curBots = new Bot[0];
                    // Generate a number to be attached to the bot names:
                    bnp = UnityEngine.Random.Range(0, 9999);
                    int numCreatedBots = 0;
                    int max = CurrentRoom.MaxPlayers - TotalPlayerCount;
                    while (numCreatedBots < max)
                    {
                        CreateABot();
                        numCreatedBots++;
                    }
                    CurrentRoom.BotCount = max;
                    for (int i = 0; i < CurrentRoom.BotCount; i++)
                    {
                        CurrentRoom.bNames[i] = curBots[i].name;
                        CurrentRoom.bChars[i] = curBots[i].characterUsing;
                        CurrentRoom.bHats[i] = curBots[i].hat;
                    }
                }
                startCustomGameNow = true;
            }
        }

        void CreateABot()
        {
            if (InRoom)
            {
                // Add a new bot to the bots array:
                Bot[] b = new Bot[curBots.Length + 1];
                for (int i = 0; i < curBots.Length; i++)
                {
                    b[i] = curBots[i];
                }
                b[b.Length - 1] = new Bot();

                // Setup the new bot (set the name and the character chosen):
                b[b.Length - 1].name = botPrefixes[UnityEngine.Random.Range(0, botPrefixes.Length)] + bnp;
                b[b.Length - 1].characterUsing = UnityEngine.Random.Range(0, characterSelector.characters.Length);
                // And choose a random hat, or none:
                b[b.Length - 1].hat = UnityEngine.Random.Range(-1, ItemDatabase.instance.hats.Length);
                bnp += 1;   // make next bot name unique

                // Now replace the old bot array with the new one:
                curBots = b;

                // ...and upload the new bot array to the room properties:
                Dictionary<string, object> h = new Dictionary<string, object>();

                string[] bn = new string[b.Length];
                int[] bsKills = new int[b.Length];
                int[] bsDeaths = new int[b.Length];
                int[] bsOther = new int[b.Length];
                int[] bc = new int[b.Length];
                int[] bHats = new int[b.Length];
                for (int i = 0; i < b.Length; i++)
                {
                    bn[i] = b[i].name;
                    bsKills[i] = System.Convert.ToInt32(b[i].scores.x);
                    bsDeaths[i] = System.Convert.ToInt32(b[i].scores.y);
                    bsOther[i] = System.Convert.ToInt32(b[i].scores.z);
                    bc[i] = b[i].characterUsing;
                    bHats[i] = b[i].hat;
                }
                bn[bn.Length - 1] = b[b.Length - 1].name;
                bsKills[bsKills.Length - 1] = System.Convert.ToInt32(b[b.Length - 1].scores.x);
                bsDeaths[bsDeaths.Length - 1] = System.Convert.ToInt32(b[b.Length - 1].scores.y);
                bsOther[bsOther.Length - 1] = System.Convert.ToInt32(b[b.Length - 1].scores.z);
                bc[bc.Length - 1] = b[b.Length - 1].characterUsing;
                bHats[bc.Length - 1] = b[b.Length - 1].hat;

                h.Add("botNames", bn);
                h.Add("botScoresKills", bsKills);
                h.Add("botScoresDeaths", bsDeaths);
                h.Add("botScoresOther", bsOther);
                h.Add("botCharacters", bc);
                h.Add("botHats", bHats);
                h.Add("roomOwnerId", CurrentRoom.OwnerId);
                pubNubUtilities.PubNubSendRoomProperties(pubnub, h);

                //  Add the properties to the current room
                CurrentRoom.AddBots(h);
                UpdatePlayerCount();
            }
        }

        Bot[] GetBotList()
        {
            Bot[] list = new Bot[0];

            string[] bn = CurrentRoom.bNames;
            Vector3[] bs = new Vector3[PNRoomInfo.MAX_BOTS];
            for (int i = 0; i < bs.Length; i++)
            {
                bs[i] = new Vector3(0, 0, 0);
            }
            int[] bc = CurrentRoom.bChars;
            int[] bHats = CurrentRoom.bHats;
            list = new Bot[bn.Length];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = new Bot();
                list[i].name = bn[i];
                list[i].scores = bs[i];
                list[i].characterUsing = bc[i];
                list[i].hat = bHats[i];
            }
            return list;
        }

        void UpdatePlayerCount()
        {
            if (CurrentRoom == null) return;
            // Get the "Real" player count:
            int players = CurrentRoom.PlayerCount;

            // ...then check if there are bots in the room:
            if (CurrentRoom.Bots != null && CurrentRoom.Bots.Count > 0)
            {
                // ... and get the number of bots and add it to the total player count:
                players += CurrentRoom.Bots.Count;
            }

            // Set the total player count:
            TotalPlayerCount = players;
        }

        public void OnPlayerEnteredRoom(PNPlayer player)
        {
            // When a player connects, update the player count:
            UpdatePlayerCount();

            try { onPlayerJoin(player); }
            catch (System.Exception) { }
        }

        public void OnPlayerLeftRoom(string uuid)
        {
            if (CurrentRoom == null || CurrentRoom.PlayerList == null) return;

            if (isInCustomGame)
            {
                //  A player left and we are currently in a game
                Dictionary<string, object> props = new Dictionary<string, object>();
                props.Add("playerLeft", LocalPlayer.ID);
                props.Add("playerUserId", LocalPlayer.UserId);
                props.Add("playerName", LocalPlayer.NickName);
                bool isGameOwner = CurrentRoom.OwnerId == LocalPlayer.UserId;
                props.Add("wasGameOwner", (isGameOwner ? 1 : 0));
                props.Add("roomOwnerId", CurrentRoom.OwnerId);
                pubNubUtilities.PubNubSendRoomProperties(pubnub, props);
            }

            PNPlayer player = null;

            for (int i = 0; i < CurrentRoom.PlayerList.Count; i++)
            {
                if (CurrentRoom.PlayerList[i].UserId == uuid)
                {
                    player = CurrentRoom.PlayerList[i];
                    CurrentRoom.PlayerList.RemoveAt(i);
                    break;
                }
            }

            // When a player disconnects, update the player count:
            UpdatePlayerCount();

            try { onPlayerLeave(player); }
            catch (System.Exception) { }
        }

        public void OnJoinedRoom(string roomOwnerId)
        {
            InRoom = true;
            tryingToJoinCustom = false;

            // Know if the room we joined in is a custom game or not:
            inCustom = true;

            // Setup scores (these are the actual player scores):
            Dictionary<string, object> p = new Dictionary<string, object>();
            p.Add("playerStats", "stats");
            p.Add("userId", userId);
            p.Add("kills", 0);
            p.Add("deaths", 0);
            p.Add("otherScore", 0);
            // Also set the chosen character:
            p.Add("character", DataCarrier.chosenCharacter);
            // ...and the cosmetics:
            int[] cosmetics = new int[1];   // You can have as many items as you want, but in our case we only need 1 and that's for the hat
            cosmetics[0] = DataCarrier.chosenHat;
            /* // Sample usage:
            cosmetics[1] = DataCarrier.chosenBackpack;
            cosmetics[2] = DataCarrier.chosenShoes; */
            p.Add("cosmetics", cosmetics);
            p.Add("roomOwnerId", roomOwnerId);

            pubNubUtilities.PubNubSendRoomProperties(pubnub, p);

            // Let's update the total player count (for local reference):
            UpdatePlayerCount();

            try { onJoinRoom(); } catch (System.Exception) { }
        }

        public void LeaveRoom()
        {
            InRoom = false;
            OnLeftRoom();
            CurrentRoom = null;
        }

        public void OnLeftRoom()
        {
            tryingToJoinCustom = false;
            isLoadingGameScene = false;

            //  All the leaving room logic is hanled by PubNub presence state
            //  and the state change handler, so just initiate a state change here.
            PNLeaveCustomRoom(pubnub, "rooms." + CurrentRoom.OwnerId, userId, CurrentRoom.OwnerId);
        }

        public async Task<bool> PubNubGetRooms()
        {
            //  Determine who is present based on who is subscribed to the lobby chat channel
            //  Called when we first launch to determine the game state
            PNResult<PNHereNowResult> herenowResponse = await pubnub.HereNow()
                .Channels(new string[]
                {
                    PubNubUtilities.chanGlobal
                })
                .IncludeUUIDs(true)
                .ExecuteAsync();
            PNHereNowResult hereNowResult = herenowResponse.Result;
            PNStatus status = herenowResponse.Status;

            if (status != null && status.Error)
            {
                Debug.Log($"Error calling PubNub HereNow ({PubNubUtilities.GetCurrentMethodName()}): {status.ErrorData.Information}");
            }
            else
            {
                foreach (KeyValuePair<string, PNHereNowChannelData> kvp in hereNowResult.Channels)
                {
                    PNHereNowChannelData hereNowChannelData = kvp.Value as PNHereNowChannelData;
                    if (kvp.Value != null)
                    {
                        List<PNHereNowOccupantData> hereNowOccupantData = hereNowChannelData.Occupants as List<PNHereNowOccupantData>;
                        if (hereNowOccupantData != null)
                        {
                            foreach (PNHereNowOccupantData pnHereNowOccupantData in hereNowOccupantData)
                            {
                                await UserIsOnlineOrStateChange(pnHereNowOccupantData.Uuid);
                            }
                        }
                    }
                }
                await PopulateRoomMembers();
            }
            return true;
        }

        public async Task<bool> PopulateRoomMembers()
        {
            //  Send a message to each room and the owner will reply with the current room occupants
            //  Called when we first launch, after we know what rooms exist
            foreach (PNRoomInfo room in pubNubRooms)
            {
                string[] getRoomInfo = new string[3];
                getRoomInfo[0] = "GET_ROOM_MEMBERS";
                getRoomInfo[1] = userId;
                getRoomInfo[2] = room.OwnerId; //  The owner of the room we want to get the information about
                string channel = "rooms." + room.OwnerId;
                PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                                .Channel(channel)
                                .Message(getRoomInfo)
                                .ExecuteAsync();
                if (publishResponse.Status.Error)
                {
                    Debug.Log($"Error sending PubNub Message ({PubNubUtilities.GetCurrentMethodName()}): {publishResponse.Status.ErrorData.Information}");
                }
            }
            return true;
        }

        private async Task<bool> UserIsOnlineOrStateChange(string uuid)
        {
            //  A user has come online.  If they have an active room which they have created
            //  then add it to our rooms list
            string channelName = PubNubUtilities.chanGlobal;

            PNResult<PNGetStateResult> getStateResponse = await pubnub.GetPresenceState()
                .Channels(new string[] { channelName })
                .Uuid(uuid)
                .ExecuteAsync();
            if (getStateResponse.Status.Error)
            {
                Debug.Log($"Error retrieving PubNub Presence State ({PubNubUtilities.GetCurrentMethodName()}): {getStateResponse.Status.ErrorData.Information}");
            }
            else
            {
                //  There is a previously created room associated with this user
                Dictionary<string, object> userState = getStateResponse.Result.StateByUUID;
                if (userState.Count > 0)
                {
                    if (userState.ContainsKey("visible") && System.Convert.ToInt32(userState["visible"]) == 0)
                    {
                        //  User has created a room, then left, so we should no longer show that room
                        PubNubRemoveRoom(uuid, false);
                        //  If they were the owner of the room, we should leave it.
                        if (CurrentRoom != null && CurrentRoom.OwnerId == uuid)
                        {
                            LeaveRoom();
                        }
                    }
                    else if (userState.ContainsKey("name"))
                    {
                        if (isLoadingGameScene) return true;    //  Don't join a game when already playing a game

                        string name = (string)userState["name"];
                        int map = System.Convert.ToInt32(userState["map"]);
                        int maxPlayers = System.Convert.ToInt32(userState["maxPlayers"]);
                        bool allowBots = (System.Convert.ToInt32(userState["customAllowBots"]) == 1);
                        GAME_LENGTH_MAKE_ME_CONFIGURABLE = System.Convert.ToInt32(userState["gameLength"]);
                        string ownerId = (string)userState["ownerId"];
                        bool inProgress = System.Convert.ToInt32(userState["inProgress"]) == 1;
                        PNRoomInfo roomInfo = new PNRoomInfo(uuid, name, map, maxPlayers, allowBots, roomCounter, GAME_LENGTH_MAKE_ME_CONFIGURABLE);
                        roomInfo.IsOpen = !inProgress;
                        roomCounter++;
                        PNPlayer owner = new PNPlayer(uuid, name, false, true, DataCarrier.chosenCharacter, DataCarrier.chosenHat);
                        if (userId.Equals(ownerId))
                        {
                            //  We created the room, so join it.
                            CurrentRoom = roomInfo;
                            JoinCustomGame(roomInfo);
                        }
                        else
                        {
                            roomInfo.PlayerList.Add(owner);
                        }

                        PubNubAddRoom(roomInfo);
                    }
                }
            }
            return true;
        }

        //  Called of a PubNub presence event fires indicating that the specified user has gone offline
        public void UserIsOffline(string uuid)
        {
            OnPlayerLeftRoom(uuid);

            //  Remove the room, if it exists
            PubNubRemoveRoom(uuid, false);
            if (CurrentRoom != null && CurrentRoom.OwnerId == uuid)
            {
                LeaveRoom();
            }
        }

        //  Create a room / lobby 
        private void PubNubAddRoom(PNRoomInfo roomInfo)
        {
            //  Only add the room if one does not already exist
            bool addRoom = true;
            foreach (PNRoomInfo room in pubNubRooms)
            {
                if (room.OwnerId == roomInfo.OwnerId)
                {
                    addRoom = false;
                    break;
                }
            }
            if (addRoom)
            {
                if (pubNubRooms != null)
                {
                    pubNubRooms.Add(roomInfo);
                    try { onRoomListChange(pubNubRooms.Count); } catch (System.Exception) { }
                }
            }
        }

        public async void PubNubRemoveRoom(string uuid, bool notifyOthers)
        {
            if (notifyOthers)
            {
                Dictionary<string, object> metaData = new Dictionary<string, object>();
                metaData["visible"] = 0;
                string[] channels = new string[] { PubNubUtilities.chanGlobal };
                PNResult<PNSetStateResult> setPresenceResponse = await pubnub.SetPresenceState()
                    .Channels(channels)
                    .Uuid(userId)
                    .State(metaData)
                    .ExecuteAsync();
                if (setPresenceResponse.Status.Error)
                {
                    Debug.Log($"Error setting PubNub Presence State ({PubNubUtilities.GetCurrentMethodName()}): {setPresenceResponse.Status.ErrorData.Information}");
                }
            }

            for (int i = 0; i < pubNubRooms.Count; i++)
            {
                if (pubNubRooms[i].OwnerId == uuid)
                {
                    if (pubNubRooms != null)
                    {
                        pubNubRooms.RemoveAt(i);
                        try { onRoomListChange(pubNubRooms.Count); } catch (System.Exception) { }
                        break;
                    }
                }
            }
        }

        //  Called by the master instance to indicate that their controlled game is in progress
        private async Task PubNubGameInProgress()
        {
            Dictionary<string, object> metaData = new Dictionary<string, object>();
            metaData["inProgress"] = 0;
            string[] channels = new string[] { PubNubUtilities.chanGlobal };
            PNResult<PNSetStateResult> setPresenceResponse = await pubnub.SetPresenceState()
                .Channels(channels)
                .Uuid(userId)
                .State(metaData)
                .ExecuteAsync();
            if (setPresenceResponse.Status.Error)
            {
                Debug.Log($"Error setting PubNub Presence State ({PubNubUtilities.GetCurrentMethodName()}): {setPresenceResponse.Status.ErrorData.Information}");
            }
        }

        //  Handler for PubNub Message event
        private async void OnPnMessage(PNMessageResult<object> result)
        {
            if (result.Message != null)
            {
                if (result.Channel.Equals(PubNubUtilities.chanRoomStatus))
                {
                    if (CurrentRoom == null)
                    {
                        return;
                    }

                    Dictionary<string, object> payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Message.ToString());

                    if (payload is Dictionary<string, object>)
                    {
                        //  The master instance has told us to start our game
                        if (payload.ContainsKey("gameSceneLoaded"))
                        {
                            if (!CurrentRoom.OwnerId.Equals((string)payload["roomOwnerId"])) return;    //  Check the game being loaded is intended for us

                            if (!isMasterClient)
                            {
                                //  Received details about the bots in the game from the master instance
                                int botCount = System.Convert.ToInt32(payload["botCount"]);
                                CurrentRoom.BotCount = botCount;
                                if (botCount > 0)
                                {
                                    string[] rxBotNames = (payload["botNames"] as Newtonsoft.Json.Linq.JArray).ToObject<string[]>();
                                    long[] rxBotCharacters = (payload["botCharacters"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                                    long[] rxBotHats = (payload["botHats"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                                    for (int i = 0; i < botCount; i++)
                                    {
                                        CurrentRoom.bNames[i] = rxBotNames[i];
                                        CurrentRoom.bChars[i] = System.Convert.ToInt32(rxBotCharacters[i]);
                                        CurrentRoom.bHats[i] = System.Convert.ToInt32(rxBotHats[i]);
                                    }
                                    curBots = GetBotList();
                                    CurrentRoom.AddBotObjects(curBots);
                                }

                                UpdatePlayerCount();
                                CurrentRoom.SortPlayerListAndAssignIds();
                                SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
                                loadNow = false;
                                isLoadingGameScene = true;
                            }
                        }
                        if (payload.ContainsKey("playerStats"))
                        {
                            string userId = null;
                            int playerId = -1;
                            if (payload.ContainsKey("userId"))
                            {
                                userId = (string)payload["userId"];
                            }
                            if (payload.ContainsKey("playerId"))
                            {
                                playerId = System.Convert.ToInt32(payload["playerId"]);
                            }
                            string roomOwnerId = (string)payload["roomOwnerId"];

                            if (CurrentRoom == null || CurrentRoom.PlayerList == null ||
                                CurrentRoom.OwnerId != roomOwnerId) return;

                            for (int i = 0; i < CurrentRoom.PlayerList.Count; i++)
                            {
                                if (userId != null && userId.Equals(CurrentRoom.PlayerList[i].UserId))
                                {
                                    CurrentRoom.PlayerList[i].SetProperties(payload);
                                }
                                else if (playerId != -1 && playerId == CurrentRoom.PlayerList[i].ID)
                                {
                                    CurrentRoom.PlayerList[i].SetProperties(payload);
                                }
                            }
                        }
                    }
                }
                else if (result.Channel.StartsWith(PubNubUtilities.chanPrefixLobbyRooms))
                {
                    object[] payloadCheck = JsonConvert.DeserializeObject<object[]>(result.Message.ToString());
                    bool isStringArray = payloadCheck.OfType<string>().Any();
                    if (!isStringArray) { return; }
                    string[] payload = JsonConvert.DeserializeObject<string[]>(result.Message.ToString());
                    string requestorId = payload[1];
                    if (payload[0].Equals("JOIN_CUSTOM_ROOM"))
                    {
                        //  Someone wants to join a room in a lobby
                        string requestorNickname = payload[2];
                        int requestedCharacter = System.Int32.Parse(payload[3]);
                        int requestedHat = System.Int32.Parse(payload[4]);
                        string roomOwnerId = payload[5];
                        PNPlayer newPlayer = new PNPlayer(requestorId, requestorNickname, (requestorId.Equals(userId)), (requestorId.Equals(roomOwnerId)), requestedCharacter, requestedHat);

                        //  Consider whether the player entered the current room
                        if (requestorId == userId)
                        {
                            LocalPlayer = newPlayer;
                            OnJoinedRoom(roomOwnerId);
                        }

                        //  Consider the case where we are not in the room
                        foreach (PNRoomInfo room in pubNubRooms)
                        {
                            if (room.OwnerId.Equals(roomOwnerId))
                            {
                                room.PlayerList.Add(newPlayer);
                                if (room.OwnerId == CurrentRoom.OwnerId)
                                {
                                    //  Would be better to have CurrentRoom be a pointer into the pubNubRooms array!
                                    CurrentRoom = room;
                                }
                                break;
                            }
                        }
                        OnPlayerEnteredRoom(newPlayer);
                        try { onRoomListChange(pubNubRooms.Count); } catch (System.Exception) { }
                    }
                    else if (payload[0].Equals("LEAVE_CUSTOM_ROOM"))
                    {
                        //  Someone wants to leave a room in a lobby
                        string roomOwnerId = payload[2];

                        if (requestorId.Equals(roomOwnerId) && requestorId.Equals(userId))
                        {
                            //  Room owner removing their own room
                            PubNubRemoveRoom(roomOwnerId, true);
                        }
                        else
                        {
                            OnPlayerLeftRoom(requestorId);
                            for (int i = 0; i < pubNubRooms.Count; i++)
                            {
                                if (pubNubRooms[i].OwnerId.Equals(roomOwnerId))
                                {
                                    for (int j = 0; j < pubNubRooms[i].PlayerList.Count; j++)
                                    {
                                        if (pubNubRooms[i].PlayerList[j].UserId.Equals(requestorId))
                                        {
                                            pubNubRooms[i].PlayerList.RemoveAt(j);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        try { onRoomListChange(pubNubRooms.Count); } catch (System.Exception) { }
                        if (requestorId == roomOwnerId)
                        {
                            try { onLeaveRoom(); } catch (System.Exception) { }
                        }
                    }
                    else if (payload[0].Equals("GET_ROOM_MEMBERS"))
                    {
                        //  If someone joins and there are already room members, they can ask the room's master
                        //  who those members are (they can retrieve the rooms but not the room members from PN HereNow)
                        string roomOwnerId = payload[2];
                        for (int i = 0; i < pubNubRooms.Count; i++)
                        {
                            //  If it is a room that I own
                            if (roomOwnerId == userId && pubNubRooms[i].OwnerId.Equals(roomOwnerId))
                            {
                                for (int j = 0; j < pubNubRooms[i].PlayerList.Count; j++)
                                {
                                    string[] rxRoomMemberPayload = new string[7];
                                    rxRoomMemberPayload[0] = "RECEIVE_ROOM_MEMBER";
                                    rxRoomMemberPayload[1] = pubNubRooms[i].PlayerList[j].UserId;
                                    rxRoomMemberPayload[2] = pubNubRooms[i].PlayerList[j].NickName;
                                    rxRoomMemberPayload[3] = "" + pubNubRooms[i].PlayerList[j].Character;
                                    rxRoomMemberPayload[4] = "" + pubNubRooms[i].PlayerList[j].Cosmetics[0];
                                    rxRoomMemberPayload[5] = pubNubRooms[i].OwnerId;
                                    rxRoomMemberPayload[6] = requestorId;
                                    PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                                        .Channel("rooms." + roomOwnerId)
                                        .Message(rxRoomMemberPayload)
                                        .ExecuteAsync();
                                    if (publishResponse.Status.Error)
                                    {
                                        Debug.Log($"Error sending PubNub Message ({PubNubUtilities.GetCurrentMethodName()}): {publishResponse.Status.ErrorData.Information}");
                                    }
                                }
                            }
                        }
                    }
                    else if (payload[0].Equals("RECEIVE_ROOM_MEMBER"))
                    {
                        //  Response from GET_ROOM_MEMBERS, sent from the master to the instance who asked the original question.
                        string requestorNickname = payload[2];
                        int requestedCharacter = System.Int32.Parse(payload[3]);
                        int requestedHat = System.Int32.Parse(payload[4]);
                        string roomOwnerId = payload[5];
                        string recipientId = payload[6];
                        if (userId == recipientId)
                        {
                            PNPlayer remotePlayer = new PNPlayer(requestorId, requestorNickname, false, false, requestedCharacter, requestedHat);
                            //  Consider whether the player entered the current room
                            foreach (PNRoomInfo room in pubNubRooms)
                            {
                                if (room.OwnerId.Equals(roomOwnerId) && !room.OwnerId.Equals(requestorId))
                                {
                                    if (!RoomContains(room, remotePlayer))
                                    {
                                        room.PlayerList.Add(remotePlayer);
                                    }
                                    break;
                                }
                            }
                            try { onRoomListChange(pubNubRooms.Count); } catch (System.Exception) { }
                        }
                    }
                }
            }
        }

        private bool RoomContains(PNRoomInfo room, PNPlayer remotePlayer)
        {
            bool ret = false;
            if (room != null)
            {
                for (int j = 0; j < room.PlayerList.Count; j++)
                {
                    if (room.PlayerList[j].UserId.Equals(remotePlayer.UserId))
                    {
                        ret = true;
                        break;
                    }
                }
            }
            return ret;
        }

        //  Handler for PubNub Presence events
        private async void OnPnPresence(PNPresenceEventResult result)
        {
            if (result.Channel.Equals(PubNubUtilities.chanGlobal))
            {
                if (result.Event.Equals("leave") || result.Event.Equals("timeout"))
                {
                    //  The specified user has left, remove any room they created from the rooms array
                    UserIsOffline(result.Uuid);
                }
                else if (result.Event.Equals("join"))
                {
                    //  The specified user has joined.  If they have created a room then add it to our room list
                    await UserIsOnlineOrStateChange(result.Uuid);
                }
                else if (result.Event.Equals("state-change"))
                {
                    //  The specified user has created or deleted a room
                    await UserIsOnlineOrStateChange(result.Uuid);
                }
            }
        }

        //  User wants to join a rom.  Notify everyone by sending a PubNub message
        private async void PNJoinCustomRoom(Pubnub pubnub, string channel, string myUserId, string myNickname, string ownerId)
        {
            string[] joinCustomRoomMsg = new string[6];
            joinCustomRoomMsg[0] = "JOIN_CUSTOM_ROOM";
            joinCustomRoomMsg[1] = myUserId;
            joinCustomRoomMsg[2] = myNickname;
            joinCustomRoomMsg[3] = "" + DataCarrier.chosenCharacter;
            joinCustomRoomMsg[4] = "" + DataCarrier.chosenHat;
            joinCustomRoomMsg[5] = ownerId; //  The owner of the room we want to join
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                .Channel(channel)
                .Message(joinCustomRoomMsg)
                .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log($"Error sending PubNub Message ({PubNubUtilities.GetCurrentMethodName()}): {publishResponse.Status.ErrorData.Information}");
            }
        }

        //  User wants to leave a room.  Notify everyone by sending a PubNub message
        private async void PNLeaveCustomRoom(Pubnub pubnub, string channel, string myUserId, string ownerId)
        {
            string[] leaveCustomRoomMsg = new string[3];
            leaveCustomRoomMsg[0] = "LEAVE_CUSTOM_ROOM";
            leaveCustomRoomMsg[1] = myUserId;
            leaveCustomRoomMsg[2] = ownerId;
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                .Channel(channel)
                .Message(leaveCustomRoomMsg)
                .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log($"Error sending PubNub Message ({PubNubUtilities.GetCurrentMethodName()}): {publishResponse.Status.ErrorData.Information}");
            }
        }

        //  Whether the specified playerID (User ID) is present in the specified room
        public bool RoomContainsPlayerId(PNRoomInfo room, string playerId)
        {
            bool ret = false;
            if (room != null)
            {
                for (int j = 0; j < room.PlayerList.Count; j++)
                {
                    if (room.PlayerList[j].UserId.Equals(playerId))
                    {
                        ret = true;
                        break;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Update the calling class when selecting a player in the list.
        /// </summary>
        /// <param name="action">The action that is occurring (adding a friend, creating private message option, etc)</param>

        /// <param name="id"></param>
        public void PlayerSelected(string action, string id)
        {
            OnPlayerSelect?.Invoke(action, id);
        }

        /// <summary>
        /// Returns the language of the user
        /// </summary>
        /// <returns></returns>
        public string GetUserLanguage()
        {
            string localeCode = "";

            if (PNManager.pubnubInstance.CachedPlayers.ContainsKey(userId)
                && PNManager.pubnubInstance.CachedPlayers[userId].Custom != null
                && PNManager.pubnubInstance.CachedPlayers[userId].Custom.ContainsKey("language"))
            {
                localeCode = PNManager.pubnubInstance.CachedPlayers[userId].Custom["language"].ToString();
                LocalizationSettings.SelectedLocale = Locale.CreateLocale(localeCode);
            }

            // For legacy players who have not set their language. Defaults to English.
            else
            {
                localeCode = LocalizationSettings.SelectedLocale.Identifier.Code;
            }

            return localeCode;
        }

        public bool GetFPSSetting()
        {
            bool setting = false;
            if (PNManager.pubnubInstance.CachedPlayers.ContainsKey(userId)
                && PNManager.pubnubInstance.CachedPlayers[userId].Custom != null
                && PNManager.pubnubInstance.CachedPlayers[userId].Custom.ContainsKey("60fps"))
            {
                bool.TryParse(PNManager.pubnubInstance.CachedPlayers[userId].Custom["60fps"].ToString(), out setting);
            }

            return setting;
        }

        //  When the player is first created, they are assigned some random hats 
        public List<int> GenerateRandomHats()
        {
            System.Random rnd = new System.Random();
            List<int> myHats = Enumerable.Range(0, 7).OrderBy(x => rnd.Next()).Take(4).ToList();
            return myHats;
        }

        //  Update the player hat inventory (shown on the customize screen)
        public void UpdateAvailableHats(List<int> availableHats)
        {
            SampleInventory.instance.availableHats.Clear();
            foreach (int hat in availableHats)
            {
                SampleInventory.instance.availableHats.Add(hat);
            }
        }

        // Test case
        public async Task<bool> RemoveUserIDMetdata(string id)
        {
            // Remove Metadata for UUID set in the pubnub instance
            PNResult<PNRemoveUuidMetadataResult> removeUuidMetadataResponse = await pubnub.RemoveUuidMetadata()
                .Uuid(id)
                .ExecuteAsync();
            PNRemoveUuidMetadataResult removeUuidMetadataResult = removeUuidMetadataResponse.Result;
            PNStatus status = removeUuidMetadataResponse.Status;

            return true;
        }
    }
}