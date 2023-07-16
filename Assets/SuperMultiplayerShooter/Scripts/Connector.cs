using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
//using Photon.Pun;
//using Photon.Realtime;
//using Photon.Pun.UtilityScripts;
//using PubNubAPI;
using PubnubApi;
using PubnubApi.Unity;
using UnityEngine.SceneManagement;
using PubNubUnityShowcase;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Newtonsoft.Json;
using System.Linq;
using System.Xml;
using Photon.Pun;

namespace Visyde
{
    /// <summary>
    /// Connector
    /// - manages the initial connection and matchmaking
    /// </summary>
    ///
    
    public class PubNubRoomInfo
    {
        public static int MAX_BOTS = 4;
        public PubNubRoomInfo(string ownerId, string name, int map, int maxPlayers, bool allowBots) {
            this.ownerId = ownerId;
            this.name = name;
            this.map = map;
            this.maxPlayers = maxPlayers;
            this.allowBots = allowBots;
            //this.botNames = new List<string>();
            this.PlayerList = new List<PubNubPlayer>();
        }
        protected bool isOpen = true;
        protected string ownerId;   //  PubNub UserID of the creator of the room
        protected string name;      //  Nickname of the creator of the room
        protected int map;
        //protected int playerCount = 0;
        protected int maxPlayers = 0;
        protected bool allowBots = false;
        public Dictionary<string, object> Bots = null;
        //public List<string> botNames;
        public Connector.Bot[] BotObjects = null;
        public List<PubNubPlayer> PlayerList;
        public int botCount;
        public string[] bNames = new string[MAX_BOTS];
        public int[] bChars = new int[MAX_BOTS];
        public int[] bHats = new int[MAX_BOTS];

        public bool IsOpen
        {
            get
            {
                return this.isOpen;
            }
            set
            {
                this.isOpen = IsOpen;
            }
        }
        public string OwnerId
        {
            get
            {
                return this.ownerId;
            }
        }
        public string Name
        {
            get
            {
                return this.name;
            }
        }
        public int Map
        {
            get
            {
                return this.map;
            }
        }
        public int PlayerCount
        {
            get
            {
                return PlayerList.Count;
                //return this.playerCount;
            }
        }
        public int MaxPlayers
        {
            get
            {
                return this.maxPlayers;
            }
        }
        public bool AllowBots
        {
            get
            {
                return this.allowBots;
            }
        }
        //public List<string> BotNames
        //{
        //    get
        //    {
        //        return this.botNames;
        //    }
        //}
        public void AddBots(Dictionary<string, object> bots)
        {
            this.Bots = bots;
        }
        public void AddBotObjects(Connector.Bot[] bots)
        {
            this.BotObjects = bots;
        }

        public void SortPlayerListAndAssignIds()
        {
            PlayerList.Sort((x, y) => x.UserId.CompareTo(y.UserId));
            for (int i = 0; i < PlayerList.Count; i++)
            {
                PlayerList[i].ID = i;
                if (PlayerList[i].IsLocal)
                {
                    Connector.instance.LocalPlayer.ID = i;
                }
            }
        }
    }

    public class PubNubPlayer
    {
        //  dcc todo tidy up all these accessors
        public PubNubPlayer(string uuid, string nickname, bool isLocal, bool isMasterClient, int character, int chosenHat)
        {
            this.UserId = uuid;
            this.NickName = nickname;
            this.IsLocal = isLocal;
            this.IsMasterClient = isMasterClient;
            //  DCC todo set these properties as appropriate
            this.Cosmetics = new int[2];
            this.Cosmetics[0] = chosenHat;
            this.Character = character;
            this.IsReady = false;
        }
        public string UserId;
        public string NickName;
        public bool IsLocal;
        public bool IsMasterClient;
        private int score = -1;
        public void SetScore(int score)
        {
            this.score = score;
        }
        public bool IsReady {  get; set; }
        public int GetScore() { return this.score; }
        public int[] Cosmetics;
        //  DCC todo make these variables either private or (if not appropriate) Read only
        //  DCC todo these need to be populated and handled
        public int Character;
        public int Kills;
        public int Deaths;
        public int OtherScore;
        public int ID; //  Needs to match for all distributed instances of this player, lexagraphically sorted by userId
        public void SetProperties(Dictionary<string, object> props)
        {
            //  This logic populates the attributes of a player prior to it being spawned (they are set / sent by the room master)
            //  DCC todo This is untested
            //  DCC todo I added true || here because I wanted to send a slave the character and hat for the master 
            if (true || !IsMasterClient)
            {
                Debug.Log("Init: Recieved SetProperties call NOT in Master");

                if (props.ContainsKey("kills"))
                {
                    Kills = System.Convert.ToInt32(props["kills"]);
                }
                if (props.ContainsKey("deaths"))
                {
                    Deaths = System.Convert.ToInt32(props["deaths"]);
                }
                if (props.ContainsKey("otherScore"))
                {
                    OtherScore = System.Convert.ToInt32(props["otherScore"]);
                }
                if (props.ContainsKey("character"))
                {
                    Character = System.Convert.ToInt32(props["character"]);
                    Debug.Log("Set Character for user " + UserId + "to " + Character);
                }
                if (props.ContainsKey("cosmetics"))
                {
                    //Cosmetics = System.Array.ConvertAll<long, int>((long[])props["cosmetics"], System.Convert.ToInt32);
                    long[] cosmeticsPayload = (props["cosmetics"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                    Cosmetics = System.Array.ConvertAll<long, int>(cosmeticsPayload, System.Convert.ToInt32);

                    Debug.Log("Received Cosmetics Array: " + Cosmetics);
                    //long[] cosmeticsPayload = (long[])props["cosmetics"];
                    //Cosmetics = new int[cosmeticsPayload.Length];
                    //for (int i = 0; i < cosmeticsPayload.Length; i++)
                    //{
                    //    Cosmetics[i] = System.Convert.ToInt32(cosmeticsPayload[i]);
                    //}
                }
                if (props.ContainsKey("score"))
                {
                    Debug.Log("Setting Score to " + System.Convert.ToInt32(props["score"]) + " for player " + NickName);
                    SetScore(System.Convert.ToInt32(props["score"]));
                }
            }
            /*
            else
            {
                //  Is master client.  //  DON'T NEED THIS I DON'T THINK
                //  DCC 123
                Debug.Log("Init: Received SetProperties call in Master");
                if (props.ContainsKey("kills"))
                {
                    Debug.Log("Temp: Received Kills");
                }
                if (props.ContainsKey("deaths"))
                {
                    Debug.Log("Temp: Received deaths");
                }
                if (props.ContainsKey("otherScore"))
                {
                    Debug.Log("Temp: Received otherScore");
                }
                if (props.ContainsKey("character"))
                {
                    Debug.Log("Temp: Received character");
                }
                if (props.ContainsKey("cosmetics"))
                {
                    Debug.Log("Temp: Received cosmetics");
                }
                if (props.ContainsKey("score"))
                {
                    Debug.Log("Setting Score to " + System.Convert.ToInt32(props["score"]) + " for player " + NickName);
                    SetScore(System.Convert.ToInt32(props["score"]));
                }
            }
            */
        }
    }

    public class Connector : MonoBehaviour //MonoBehaviourPunCallbacks
    {
        public static Connector instance;

        [Header("Settings:")]
        public string gameVersion = "0.1";
        public string gameSceneName = "";
        public int requiredPlayers;
        public string[] maps;

        [Header("Bot Players:")]
        [Tooltip("This is only for matchmaking.")] public bool createBots;
        public float startCreatingBotsAfter;		// (Only if `createBots` is enabled) after this delay, the game will start on generating bots to fill up the room.
        public float minBotCreationTime;			// minimum bot join/creation delay
        public float maxBotCreationTime;			// maximum bot join/creation delay
        public string[] botPrefixes;                // names for bots

        [Header("Other References:")]
        public CharacterSelector characterSelector;

        public bool tryingToJoinCustom { get; protected set; }
        bool inCustom = true;
        public bool isInCustomGame {
            get {
                return inCustom && inRoom; // PhotonNetwork.InRoom;
            }
        }
        public bool isMasterClient {
            get
            {
                //  DCC todo test this and implement it
                return (CurrentRoom != null && userId != null && CurrentRoom.OwnerId == userId);
            }
        }

        public int GetMyId()
        {
            if (CurrentRoom != null)
            {
                for (int i = 0; i < CurrentRoom.PlayerList.Count; i++)
                {
                    if (CurrentRoom.PlayerList[i].IsLocal )
                    {
                        return CurrentRoom.PlayerList[i].ID;
                    }
                }
            }
            return -1;
        }

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

        public int selectedMap { get;  protected set; }
        public int totalPlayerCount { get; protected set; }
        //public List<RoomInfo> rooms { get; protected set; }
        //  PubNub properties
        private Pubnub pubnub = null;
        private PubNubUtilities pubNubUtilities = new PubNubUtilities();
        private SubscribeCallbackListener listener = new SubscribeCallbackListener();
        private static string userId = null;
        public bool atLeastOnePlayerReady = false;
        public PubNubRoomInfo CurrentRoom = null;
        public bool inRoom = false;
        public List<PubNubRoomInfo> pubNubRooms { get; protected set; }
        public static string pnNickName = "Uninitialized";
        /*
        {
            get
            {
                //  DCC todo Name is available from PubNubManager.Instance.CachedPlayers[subscribeEventEventArgs.PresenceEventResult.UUID].Name;
                //  DCC todo use Oliver's method he will provide
                //return pnManager.GetUserNickname();
                Debug.Log("NICKNAME: " + PNManager.pubnubInstance.GetUserNickname());
                //return PhotonNetwork.NickName;
                return PNManager.pubnubInstance.GetUserNickname();
            }
        }
    */
        public Pubnub GetPubNubObject() { return pubnub; }
        public PubNubPlayer LocalPlayer { get; set; }
        //  End PubNub properties

        public bool autoReconnect { get; set; }

        public delegate void IntEvent(int i);
        //public UnityAction onRoomListChange;
        public IntEvent onRoomListChange;
        public UnityAction onCreateRoomFailed;
        public UnityAction onJoinRoom;
        public UnityAction onLeaveRoom;
        public UnityAction onDisconnect;
        public delegate void PlayerEvent(PubNubPlayer player);
        public PlayerEvent onPlayerJoin;
        public PlayerEvent onPlayerLeave;
        public delegate void PNMessageEvent(PNMessageResult<object> message);
        public PNMessageEvent onLobbyChatMessage;

        // Internal variables:
        Bot[] curBots;
        int bnp;
        bool startCustomGameNow;
        bool loadNow;                       // if true, the game scene will be loaded. Matchmaking will set this to true instantly when enough 
                                            // players are present, custom games on the other hand will require the host to press the "Start" button first.
        bool isLoadingGameScene;

        public class Bot
        {
            public string name;				// bot name
            public Vector3 scores; 			// x = kills, y = deaths, z = other scores
            public int characterUsing;		// the chosen character of the bot (index only)
            public int hat;
        }

        void Awake(){
            instance = this;
        }

        private void OnDestroy()
        {
            //  DCC todo is there a way in the new Unity SDK to execute an unsubscribe synchronously?  Maybe need to run the logic in onApplicationQuit() instead

            Debug.Log("OnDestroy::Unsubscribe");

            //This never gets called because we are already in the process of shutting down
            listener.onMessage -= OnPnMessage;
            listener.onPresence -= OnPnPresence;
            pubnub.Unsubscribe<string>()
                .Channels(new string[] { PubNubUtilities.gameLobbyChannel, PubNubUtilities.gameLobbyRoomsWildcardRoot + "*" })
                .Execute();
            Debug.Log("OnDestroy::Unsubscribe::Done");

            //pubnub.Unsubscribe().Channels(new List<string>() { "game", "rooms.*" }).Async((result, status) =>
            //{
            //    if (status.Error)
            //    {
            //        Debug.Log("Error unsubscribing: " + status.ErrorData.Info);
            //    }
            //});
        }

        async void Start()
        {
            //  DCC Photon Initialization
            /*
            PhotonNetwork.AutomaticallySyncScene = true;
            loadNow = false;
            rooms = new List<RoomInfo>();
            autoReconnect = true;

            // Do connection loop:
            StartCoroutine(Reconnection());
            */
            //  DCC End Photon initialization

            //  PubNub initialization
            pnNickName = await PNManager.pubnubInstance.GetUserNickname();
            loadNow = false;
            userId = PlayerPrefs.GetString("uuid");
            pubNubRooms = new List<PubNubRoomInfo>();
            //pubnub = PubNubManager.Instance.InitializePubNub();
            Debug.Log("temp: Initializing PubNub");
            pubnub = PNManager.pubnubInstance.InitializePubNub();
            pubnub.AddListener(listener);
            listener.onMessage += OnPnMessage;
            listener.onPresence += OnPnPresence;
            pubnub.Subscribe<string>()
                .Channels(new List<string>() { 
                    PubNubUtilities.lobbyChatWildcardRoot + "*",
                    PubNubUtilities.gameLobbyChannel, 
                    PubNubUtilities.gameLobbyChannel + "-pnpres",
                    PubNubUtilities.gameLobbyRoomsWildcardRoot + "*", 
                    PubNubUtilities.roomStatusChannel })
                //.WithPresence()
                .Execute();
            //pubnub.Subscribe().Channels(new List<string>() { "game", "rooms.*", PubNubUtilities.roomStatusChannel }).WithPresence().Execute();
            //pubnub.SubscribeCallback += SubscribeCallbackHandler;
            PubNubGetRooms();
            //  End PubNub initialization
        }

        // DCC Photon Initialization
        /*
        // This will automatically connect the client to the server every 2 seconds if not connected:
        IEnumerator Reconnection(){
            while(autoReconnect){
                yield return new WaitForSeconds(2f);

                if (!PhotonNetwork.IsConnected || PhotonNetwork.NetworkClientState == ClientState.ConnectingToMasterServer){
                    PhotonNetwork.ConnectUsingSettings();
                    PhotonNetwork.GameVersion = gameVersion;
                }
            }
        }
        */
        // DCC End Photon Initialization

        // Update is called once per frame
        void Update()
        {
            // Room managing:
            //if (PhotonNetwork.InRoom && !isLoadingGameScene)
            if (inRoom && !isLoadingGameScene)
            {
                // Set the variable "loadNow" to true if the room is already full:
                //if (totalPlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers && ((isInCustomGame && startCustomGameNow) || !isInCustomGame))
                if (totalPlayerCount >= CurrentRoom.MaxPlayers && ((isInCustomGame && startCustomGameNow) || !isInCustomGame))
                {
                    loadNow = true;
                }

                // Go to the game scene if the variable "loadNow" is true:
                if (loadNow){
                    //if (PhotonNetwork.IsMasterClient)
                    if (isMasterClient)
                    {
                        Debug.Log("Init: Telling clients to load the game");
                        //PhotonNetwork.CurrentRoom.IsOpen = false;
                        CurrentRoom.IsOpen = false;
                        CurrentRoom.SortPlayerListAndAssignIds();
                        //  DCC todo?  Notify other participants that the game is open?
                        //PhotonNetwork.LoadLevel(gameSceneName);
                        SynchronizePlayerCharacteristics();
                        SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
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
                        //  Set up the initial bot properties for each non-master instance
                        loadProps.Add("botNames", bn);
                        loadProps.Add("botCharacters", bc);
                        loadProps.Add("botHats", bHats);
                        pubNubUtilities.PubNubSendRoomProperties(pubnub, loadProps);
                        //  DCC todo?  Notify other participants that the game has started
                        loadNow = false;
                        isLoadingGameScene = true;
                    }
                }
            }
        }

        private void SynchronizePlayerCharacteristics()
        {
            //  Called by master.  Notify all other human players of eachother's characteristics
            for (int i = 0; i < CurrentRoom.PlayerList.Count; i++)
            {
                Dictionary<string, object> playerProps = new Dictionary<string, object>();
                //if (!CurrentRoom.PlayerList[i].IsMasterClient)
                //{
                    playerProps.Add("playerStats", "stats");
                    playerProps.Add("userId", CurrentRoom.PlayerList[i].UserId);
                    playerProps.Add("character", CurrentRoom.PlayerList[i].Character);
                    int[] cosmetics = new int[1];   // You can have as many items as you want, but in our case we only need 1 and that's for the hat
                    cosmetics[0] = CurrentRoom.PlayerList[i].Cosmetics[0];
                    playerProps.Add("cosmetics", cosmetics);
                    Debug.Log("Sending Player Characteristics for User Id " + CurrentRoom.PlayerList[i].UserId);
                    pubNubUtilities.PubNubSendRoomProperties(pubnub, playerProps);
                //}
            }
        }


        // Matchmaking:
        public void FindMatch()
        {
            //  DCC todo Remove FindMatch
            /*
            ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
            h.Add("isInMatchmaking", true);
            PhotonNetwork.JoinRandomRoom(h, 0);
            */
        }
        public void CancelMatchmaking()
        {
            //  DCC todo Remove CancelMatchmaking
            /*
            PhotonNetwork.LeaveRoom();
            // Clear bots list:
            curBots = new Bot[0];
            */
        }

        // Custom Game:
        public void JoinCustomGame(PubNubRoomInfo room){
            tryingToJoinCustom = true;
            CurrentRoom = room;
            if (isMasterClient)
            {
                //  Assume we were able to join the room
                PubNubPlayer self = new PubNubPlayer(userId, pnNickName, true, true, DataCarrier.chosenCharacter, DataCarrier.chosenHat);
                self.SetScore(0);
                //  DCC todo these should be the same object
                LocalPlayer = self;
                CurrentRoom.PlayerList.Add(self);
                OnJoinedRoom();

                //  DCC todo add anyone else who is in the room (for 3 or 4 human players).  This MIGHT just work.  Test it.
            }
            else
            {
                //  Send a message to the host that we want to join their game
                PNJoinCustomRoom(pubnub, "rooms." + room.OwnerId, userId, pnNickName, room.OwnerId);
                //  Join the game on our local instance
                PubNubPlayer self = new PubNubPlayer(userId, pnNickName, true, false, DataCarrier.chosenCharacter, DataCarrier.chosenHat);
                self.SetScore(0);
                //  DCC todo these should be the same object
                LocalPlayer = self;
                CurrentRoom.PlayerList.Add(self);
                OnJoinedRoom();
            }
            //  DCC todo delete this
            //PhotonNetwork.JoinRoom(room.Name);
        }
        public async void CreateCustomGame(int selectedMap, int maxPlayers, bool allowBots)
        {

            if (pubnub != null)
            {
                //  The newly created game state is stored in PubNub User State (part of the Presence system)
                //  This state is cleared if a user disconnects
                Dictionary<string, object> metaData = new Dictionary<string, object>();
                metaData["name"] = pnNickName;  
                metaData["visible"] = 1;    //  If the user leaves the room then we can toggle the room's visibility
                metaData["ownerId"] = userId;
                metaData["maxPlayers"] = maxPlayers;
                metaData["started"] = 0;
                metaData["map"] = selectedMap;
                //metaData["playerCount"] = 1;
                metaData["customAllowBots"] = allowBots ? 1 : 0;
                string channelName = PubNubUtilities.gameLobbyChannel;
                Debug.Log("temp: Creating game with name " + pnNickName);
                PNResult<PNSetStateResult> setPresenceStateResponse = await pubnub.SetPresenceState()
                    .Channels(new string[] { channelName })
                    .Uuid(userId)
                    .State(metaData)
                    .ExecuteAsync();
                if (setPresenceStateResponse.Status.Error)
                {
                    Debug.Log("Error setting PubNub Presence State (CreateCustomGame): " + setPresenceStateResponse.Status.ErrorData.Information);
                }

                //pubnub.SetPresenceState().Channels(new List<string>() { channelName }).UUID(userId).State(metaData).Async((result, status) =>
                //{
                //    if (status.Error)
                //    {
                //        Debug.Log("Error setting PubNub Presence State (CreateCustomGame): " + status.ErrorData.Info);
                //    }
                //});
                PubNubRoomInfo roomInfo = new PubNubRoomInfo(userId, pnNickName, selectedMap, maxPlayers, allowBots);
                CurrentRoom = roomInfo;
                PubNubAddRoom(roomInfo);
                JoinCustomGame(roomInfo);
            }
           
            /*
            if (PhotonNetwork.IsConnectedAndReady)
            {
                ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                h.Add("started", false);
                h.Add("map", selectedMap);
                h.Add("customAllowBots", allowBots);
                h.Add("isInMatchmaking", false);

                PhotonNetwork.CreateRoom(PhotonNetwork.NickName, new RoomOptions()
                {
                    MaxPlayers = (byte)maxPlayers,
                    IsVisible = true,
                    CleanupCacheOnLeave = true,
                    EmptyRoomTtl = 0,
                    //PlayerTtl = 10000,
                    CustomRoomProperties = h,
                    CustomRoomPropertiesForLobby = new string[] { "map", "isInMatchmaking" }
                });
            }
            */
        }
        public void StartCustomGame(){
            // Start creating bots (if bots are allowed) as this will fill out the empty players:
            if (inCustom && !loadNow)
            {
                // Create the bots if allowed:
                //if ((bool)PhotonNetwork.CurrentRoom.CustomProperties["customAllowBots"])
                if (CurrentRoom.AllowBots)
                {
                    // Clear the bots array first:
                    curBots = new Bot[0];
                    // Generate a number to be attached to the bot names:
                    bnp = Random.Range(0, 9999);
                    int numCreatedBots = 0;
                    //int max = PhotonNetwork.CurrentRoom.MaxPlayers - totalPlayerCount;
                    int max = CurrentRoom.MaxPlayers - totalPlayerCount;
                    while (numCreatedBots < max)
                    {
                        CreateABot();
                        numCreatedBots++;
                    }
                    CurrentRoom.botCount = max;
                    for (int i = 0; i < CurrentRoom.botCount; i++)
                    {
                        CurrentRoom.bNames[i] = curBots[i].name;
                        CurrentRoom.bChars[i] = curBots[i].characterUsing;
                        CurrentRoom.bHats[i] = curBots[i].hat;
                    }
                }

                startCustomGameNow = true;
            }
        }

        // Bot Creation:
        /*
        void StartCreatingBots()
        {
            // Generate a number to be attached to the bot names:
            bnp = Random.Range(0, 9999);
            Invoke("CreateABot", Random.Range(minBotCreationTime, maxBotCreationTime));
        }
        */

        //  DCC TODO this is ONLY CALLED IN THE MASTER (SINCE IT ASSIGNS THE RANDOM NAMES)
        void CreateABot()
        {
            //if (PhotonNetwork.InRoom)
            if (inRoom)
            {
                // Add a new bot to the bots array:
                Bot[] b = new Bot[curBots.Length + 1];
                for (int i = 0; i < curBots.Length; i++)
                {
                    b[i] = curBots[i];
                }
                b[b.Length - 1] = new Bot();

                // Setup the new bot (set the name and the character chosen):
                b[b.Length - 1].name = botPrefixes[Random.Range(0, botPrefixes.Length)] + bnp;
                b[b.Length - 1].characterUsing = Random.Range(0, characterSelector.characters.Length);
                // And choose a random hat, or none:
                b[b.Length - 1].hat = Random.Range(-1, ItemDatabase.instance.hats.Length);
                bnp += 1;   // make next bot name unique
                
                // Now replace the old bot array with the new one:
                curBots = b;

                // ...and upload the new bot array to the room properties:
                //  DCC changed this to a dictionary
                //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                Dictionary<string, object> h = new Dictionary<string, object>();

                string[] bn = new string[b.Length];
                //Vector3[] bs = new Vector3[b.Length];
                int[] bsKills = new int[b.Length];
                int[] bsDeaths = new int[b.Length];
                int[] bsOther = new int[b.Length];
                int[] bc = new int[b.Length];
                int[] bHats = new int[b.Length];
                for (int i = 0; i < b.Length; i++)
                {
                    bn[i] = b[i].name;
                    //bs[i] = b[i].scores;
                    bsKills[i] = System.Convert.ToInt32(b[i].scores.x);
                    bsDeaths[i] = System.Convert.ToInt32(b[i].scores.y);
                    bsOther[i] = System.Convert.ToInt32(b[i].scores.z);
                    bc[i] = b[i].characterUsing;
                    bHats[i] = b[i].hat;
                }
                bn[bn.Length - 1] = b[b.Length - 1].name;
                //bs[bs.Length - 1] = b[b.Length - 1].scores;
                bsKills[bsKills.Length - 1] = System.Convert.ToInt32(b[b.Length - 1].scores.x);
                bsDeaths[bsDeaths.Length - 1] = System.Convert.ToInt32(b[b.Length - 1].scores.y);
                bsOther[bsOther.Length - 1] = System.Convert.ToInt32(b[b.Length - 1].scores.z);
                bc[bc.Length - 1] = b[b.Length - 1].characterUsing;
                bHats[bc.Length - 1] = b[b.Length - 1].hat;

                h.Add("botNames", bn);
                //h.Add("botScores", bs);
                h.Add("botScoresKills", bsKills);
                h.Add("botScoresDeaths", bsDeaths);
                h.Add("botScoresOther", bsOther);
                h.Add("botCharacters", bc);
                h.Add("botHats", bHats);
                //  DCC todo send Room Properties
                //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
                //  DCC todo THINK I CAN DELETE THIS SINCE I TRANSFER THE NAMES AND CHARACTERS AND HATS ANOTHER WAY
                //  DCC - not so sure, adding this back in since sometimes the wrong number of bots were being created
                pubNubUtilities.PubNubSendRoomProperties(pubnub, h);

                //  Add the properties to the current room
                CurrentRoom.AddBots(h);
                UpdatePlayerCount();


                /*
                if (!isInCustomGame)
                {
                    // Continue adding another bot after a random delay (to give human players enough time to join, and also to simulate realism):
                    Invoke("CreateABot", Random.Range(minBotCreationTime, maxBotCreationTime));
                }
                */
                
            }
        }


        
        Bot[] GetBotList()
        {
            Bot[] list = new Bot[0];

            // Download the bots list if we already have one:
            //if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("botNames"))
            //{
            //string[] bn = (string[])PhotonNetwork.CurrentRoom.CustomProperties["botNames"];
            string[] bn = CurrentRoom.bNames;
            //Vector3[] bs = (Vector3[])PhotonNetwork.CurrentRoom.CustomProperties["botScores"];
            Vector3[] bs = new Vector3[PubNubRoomInfo.MAX_BOTS];
            for(int i = 0; i < bs.Length; i++)
            {
                bs[i] = new Vector3(0, 0, 0);
            }
            //int[] bc = (int[])PhotonNetwork.CurrentRoom.CustomProperties["botCharacters"];
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
            //int players = PhotonNetwork.CurrentRoom.PlayerCount;
            int players = CurrentRoom.PlayerCount;

            // ...then check if there are bots in the room:
            if (CurrentRoom.Bots != null && CurrentRoom.Bots.Count > 0)
            {
                //  DCC todo this GetBotList logic hasn't been implemented
                // If there are, set the bots list from the server:
                //if (!PhotonNetwork.IsMasterClient) curBots = GetBotList();
                //if (!Connector.instance.isMasterClient) curBots = GetBotList();

                // ... and get the number of bots and add it to the total player count:
                //players += curBots.Length;
                players += CurrentRoom.Bots.Count;
            }

            // Set the total player count:
            totalPlayerCount = players;
        }

        // PHOTON:
        /*
        public override void OnConnectedToMaster()
        {
            if (PhotonNetwork.IsConnectedAndReady) PhotonNetwork.JoinLobby();
        }
        public override void OnDisconnected(DisconnectCause cause)
        {
            isLoadingGameScene = false;

            // Events:
            onDisconnect();
        }
        */
        

        public /* override */ void OnPlayerEnteredRoom(PubNubPlayer player)
        {
            // When a player connects, update the player count:
            UpdatePlayerCount();

            // Events:
            try
            {
                onPlayerJoin(player);
            }
            catch (System.Exception) { }
        }
        public /* override */ void OnPlayerLeftRoom(string uuid /*PubNubPlayer player*/)
        {
            if (CurrentRoom == null || CurrentRoom.PlayerList == null) return;

            if (isInCustomGame)
            {
                //  A player left and we are currently in a game
                //  DCC todo                
                Dictionary<string, object> props = new Dictionary<string, object>();
                props.Add("playerLeft", LocalPlayer.ID);
                props.Add("playerUserId", LocalPlayer.UserId);
                props.Add("playerName", LocalPlayer.NickName);
                bool isGameOwner = CurrentRoom.OwnerId == LocalPlayer.UserId;
                props.Add("wasGameOwner", (isGameOwner ? 1 : 0));
                pubNubUtilities.PubNubSendRoomProperties(pubnub, props);
                
            }

            PubNubPlayer player = null;

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

            /*
            //  DCC Commenting out matchmaking
            // Also, if a player disconnects while matchmaking and they happen to be the master client, a new master client will be assigned.
            // We could be the new master client so check if we are. If we are, continue adding bots (if bots are allowed):
            if (PhotonNetwork.IsMasterClient && createBots && !isInCustomGame)
            {
                // Get the existing bot list made by the last master client, or make a new one if none:
                curBots = GetBotList();
                // Start creating bots after a delay:
                Invoke("StartCreatingBots", curBots.Length > 0 ? Random.Range(minBotCreationTime, maxBotCreationTime) : startCreatingBotsAfter);
            }
            */

            // Events:
            try
            {
                onPlayerLeave(player);
            } catch (System.Exception) { }
        }

        /*
         *  Replaced by invoking onRoomListChange directly (which is what the LobbyBrowser registers for)
        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            // List only custom rooms:
            rooms = new List<RoomInfo>();
            List<RoomInfo> r = roomList;
            for (int i = 0; i < r.Count; i++)
            {
                if (r[i].CustomProperties.ContainsKey("isInMatchmaking"))
                {
                    if ((bool)r[i].CustomProperties["isInMatchmaking"] == false)
                    {
                        rooms.Add(r[i]);
                    }
                }
            }

            // Do events:
            onRoomListChange(r.Count);
        }
        */


        //  DCC todo listen for the room update and update the player count
        /*
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            
            // A bot might have joined, so update the total player count:
            UpdatePlayerCount();
            
        }
        public override void OnCreateRoomFailed(short returnCode, string message){

            tryingToJoinCustom = false;

            // Events:
            onCreateRoomFailed();
        }
        */
        /*
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            //  DCC Removed logic related to finding a game
            /* 
            print("<color=red>" + message + "</color>");
            // Create a new room if we failed to find one:
            if (PhotonNetwork.IsConnectedAndReady)
            {
                // Prepare the room properties:
                ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                h.Add("started", false);
                h.Add("map", Random.Range(0, maps.Length));
                h.Add("isInMatchmaking", true);

                // Then create the room, with the prepared room properties in the RoomOptions argument:
                PhotonNetwork.CreateRoom(null, new RoomOptions()
                {
                    MaxPlayers = (byte)requiredPlayers,
                    CleanupCacheOnLeave = true,
                    IsVisible = true,
                    CustomRoomProperties = h,
                    CustomRoomPropertiesForLobby = new string[] { "map", "isInMatchmaking" }
                });
            }
            
        }
        */
        /*
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            DataCarrier.message = message;
            if (!tryingToJoinCustom) PhotonNetwork.JoinRandomRoom();
        }
        */
        public /*override*/ void OnJoinedRoom()
        {
            Debug.Log("temp: OnJoinedRoom");
            inRoom = true;
            //  DCC todo update channel metadata?

            //  DCC todo replace or update all this logic
            tryingToJoinCustom = false;

            // Know if the room we joined in is a custom game or not:
            inCustom = true;// !(bool)PhotonNetwork.CurrentRoom.CustomProperties["isInMatchmaking"];

            // This is only used to check if we've loaded the game and ready. This sets to 0 after loading the game scene:
            //PhotonNetwork.LocalPlayer.SetScore(-1); // -1 = not ready, 0 = ready
            //LocalPlayer.SetScore(-1);

            // Setup scores (these are the actual player scores):
            //ExitGames.Client.Photon.Hashtable p = new ExitGames.Client.Photon.Hashtable();
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

            // Apply:
            //  DCC todo apply these to the local player properly
            //PhotonNetwork.LocalPlayer.SetCustomProperties(p);
            pubNubUtilities.PubNubSendRoomProperties(pubnub, p);

            // Let's update the total player count (for local reference):
            UpdatePlayerCount();

            // (MATCHMAKING ONLY) Start creating bots (if bots are allowed):
            /*
            if (PhotonNetwork.IsMasterClient && createBots && !isInCustomGame)
            {
                // Clear the bots array first:
                curBots = new Bot[0];
                // then start creating new ones:
                Invoke("StartCreatingBots", startCreatingBotsAfter);
            }
            */

            // Events:
            onJoinRoom();
        }

        public void LeaveRoom()
        {
            inRoom = false;
            OnLeftRoom();
        }

        public /*override*/ void OnLeftRoom(){
            tryingToJoinCustom = false;
            isLoadingGameScene = false;

            //  DCC todo should we unsubscribe from the 'game' channel here?

            //  Remove any room we might have previously created
            PubNubRemoveRoom(userId, true);

            //  Send a message to the host that we want to leave their game
            if (userId != CurrentRoom.OwnerId)
            {
                PNLeaveCustomRoom(pubnub, "rooms." + CurrentRoom.OwnerId, userId, CurrentRoom.OwnerId);
                OnPlayerLeftRoom(userId);
            }

            // Events:
            try { onLeaveRoom(); } catch (System.Exception) { }
        }

        private async void PubNubGetRooms()
        {
            //  Determine who is present based on who is subscribed to the lobby chat channel
            PNResult<PNHereNowResult> herenowResponse = await pubnub.HereNow()
                .Channels(new string[]
                {
                    PubNubUtilities.gameLobbyChannel
                })
                .IncludeUUIDs(true)
                .ExecuteAsync();
            PNHereNowResult hereNowResult = herenowResponse.Result;
            PNStatus status = herenowResponse.Status;

            if(status.Error)
            {
                Debug.Log("Error calling HereNow: " + status.ErrorData.Information);
            }
            else
            {
                Debug.Log("HereNow: Result.Channels length is " + hereNowResult.Channels.Count);
                foreach(KeyValuePair<string, PNHereNowChannelData> kvp in hereNowResult.Channels)
                {
                    PNHereNowChannelData hereNowChannelData = kvp.Value as PNHereNowChannelData;
                    if (kvp.Value != null)
                    {
                        Debug.Log("HereNow: Found HereNow channel data for channel " + hereNowChannelData.ChannelName + " with occupancy " + hereNowChannelData.Occupancy);
                        List<PNHereNowOccupantData> hereNowOccupantData = hereNowChannelData.Occupants as List<PNHereNowOccupantData>;
                        if (hereNowOccupantData != null)
                        {
                            foreach (PNHereNowOccupantData pnHereNowOccupantData in hereNowOccupantData)
                            {
                                Debug.Log("HereNow: Calling User is Online for User ID " + pnHereNowOccupantData.Uuid + " with state " + pnHereNowOccupantData.State);
                                UserIsOnlineOrStateChange(pnHereNowOccupantData.Uuid);
                            }
                        }
                    }
                }
            }


            //pubnub.HereNow().Channels(new List<string>() { "game" }).IncludeUUIDs(true).Async((result, status) =>
            //{
            //    if (status.Error)
            //    {
            //        Debug.Log("Error calling HereNow: " + status.ErrorData.Info);
            //    }
            //    else
            //    {
            //        Debug.Log("HereNow: Result.Channels length is " + result.Channels.Count);
            //        foreach (KeyValuePair<string, PNHereNowChannelData> kvp in result.Channels)
            //        {
            //            PNHereNowChannelData hereNowChannelData = kvp.Value as PNHereNowChannelData;
            //            if (kvp.Value != null)
            //            {
            //                Debug.Log("HereNow: Found HereNow channel data for channel " + hereNowChannelData.ChannelName + " with occupancy " + hereNowChannelData.Occupancy);
            //                List<PNHereNowOccupantData> hereNowOccupantData = hereNowChannelData.Occupants as List<PNHereNowOccupantData>;
            //                if (hereNowOccupantData != null)
            //                {
            //                    foreach(PNHereNowOccupantData pnHereNowOccupantData in hereNowOccupantData)
            //                    {
            //                        Debug.Log("HereNow: Calling User is Online for User ID " + pnHereNowOccupantData.UUID + " with state " + pnHereNowOccupantData.State);
            //                        UserIsOnlineOrStateChange(pnHereNowOccupantData.UUID);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //});
        }

        private async void UserIsOnlineOrStateChange(string uuid)
        {
            //  A user has come online.  If they have an active room which they have created
            //  then add it to our rooms list
            Debug.Log("temp: User is online or state change " + uuid);
            if (uuid == userId)
            {
                //  This is our own online state change
                //return;
            }

            //string channelName = "rooms." + uuid;
            string channelName = PubNubUtilities.gameLobbyChannel;

            //  DCC xxz
            //pubnub.GetPresenceState()
            //    .Channels(new string[] { channelName })
            //    .Uuid(uuid)
            //    .Execute(new PNGetStateResultExt((result, status) => {
            //        Debug.Log("temp: Synchronous response from GetState");
            //    }));
                    
            PNResult<PNGetStateResult> getStateResponse = await pubnub.GetPresenceState()
                .Channels(new string[] { channelName })
                .Uuid(uuid)
                .ExecuteAsync();
            if (getStateResponse.Status.Error)
            {
                Debug.Log("Error retrieving PubNub Presence State (UserIsOnlineOrStateChange): " + getStateResponse.Status.ErrorData.Information);
            }
            else
            {
                Debug.Log("temp: Response from getState " + getStateResponse.Status.StatusCode);
                //  There is a previously created room associated with this user
                Dictionary<string, object> userState = getStateResponse.Result.StateByUUID;
                //if (!userState.ContainsKey("visible"))
                //    userState = (Dictionary<string, object>)userState[channelName];
                //int visible = System.Convert.ToInt32(userState["visible"]);

                //for (int i = 0; i < userState.Keys.Count; i++)
                //{
                //    Debug.Log(userState.ElementAt(i));
                //    //  DCC To Do read in the created rooms
                //}
                foreach (KeyValuePair<string, object> entry in userState)
                {
                    //Debug.Log("Presence State: " + entry.Key + ", " + entry.Value.ToString());
                }
                if (userState.Count > 0)
                {
                    Debug.Log("temp: Userstate count is " + userState.Count);
                    if (userState.ContainsKey("visible") && System.Convert.ToInt32(userState["visible"]) == 0)
                    {
                        Debug.Log("temp: Visible was set to 0, leaving room");
                        //  User has created a room, then left, so we should no longer show that room
                        PubNubRemoveRoom(uuid, false);
                        //  If they were the owner of the room, we should leave it.
                        //  If they were not the owner, we should just remove them
                        if (CurrentRoom != null && CurrentRoom.OwnerId == uuid)
                        {
                            LeaveRoom();
                        }
                    }
                    else
                    {
                        Debug.Log("temp: Adding room, retrieving properties");
                        string name = (string)userState["name"];
                        int map = System.Convert.ToInt32(userState["map"]);
                        //int playerCount = System.Convert.ToInt32(userState["playerCount"]);
                        int maxPlayers = System.Convert.ToInt32(userState["maxPlayers"]);
                        bool allowBots = (System.Convert.ToInt32(userState["customAllowBots"]) == 1);
                        PubNubRoomInfo roomInfo = new PubNubRoomInfo(uuid, name, selectedMap, maxPlayers, allowBots);
                        PubNubPlayer owner = new PubNubPlayer(uuid, name, false, true, DataCarrier.chosenCharacter, DataCarrier.chosenHat);
                        roomInfo.PlayerList.Add(owner);
                        PubNubAddRoom(roomInfo);
                    }
                }
                else
                {
                    Debug.Log("temp: User State did not contain any data");
                }



                /*
                if (visible == 0)
                {
                    //  User has created a room, then left, so we should no longer show that room
                    PubNubRemoveRoom(uuid, false);
                    //  If they were the owner of the room, we should leave it.
                    //  If they were not the owner, we should just remove them
                    if (CurrentRoom.OwnerId == uuid)
                    {
                        LeaveRoom();
                    }
                }
                else
                {
                    string name = (string)userState["name"];
                    int map = System.Convert.ToInt32(userState["map"]);
                    //int playerCount = System.Convert.ToInt32(userState["playerCount"]);
                    int maxPlayers = System.Convert.ToInt32(userState["maxPlayers"]);
                    bool allowBots = (System.Convert.ToInt32(userState["customAllowBots"]) == 1);
                    PubNubRoomInfo roomInfo = new PubNubRoomInfo(uuid, name, selectedMap, maxPlayers, allowBots);
                    PubNubPlayer owner = new PubNubPlayer(uuid, name, false, true, DataCarrier.chosenCharacter, DataCarrier.chosenHat);
                    roomInfo.PlayerList.Add(owner);
                    PubNubAddRoom(roomInfo);
                }
                */
            }

            /*
            pubnub.GetPresenceState().Channels(new List<string>() { channelName }).UUID(uuid).Async((result, status) =>
            {
                if (status.Error)
                {
                    Debug.Log("Error retrieving PubNub Presence State (UserIsOnlineOrStateChange): " + status.ErrorData.Info);
                }
                else if (result != null && result.StateByChannels != null && result.StateByChannels.Count > 0)
                {
                    //  There is a previously created room associated with this user
                    Dictionary<string, object> userState = result.StateByChannels;
                    if (!userState.ContainsKey("visible"))
                        userState = (Dictionary<string, object>)userState[channelName];
                    int visible = System.Convert.ToInt32(userState["visible"]);
                    if (visible == 0)
                    {
                        //  User has created a room, then left, so we should no longer show that room
                        PubNubRemoveRoom(uuid, false);
                        //  If they were the owner of the room, we should leave it.
                        //  If they were not the owner, we should just remove them
                        if (CurrentRoom.OwnerId == uuid)
                        {
                            LeaveRoom();
                        }
                    }
                    else
                    {
                        string name = (string)userState["name"];
                        int map = System.Convert.ToInt32(userState["map"]);
                        //int playerCount = System.Convert.ToInt32(userState["playerCount"]);
                        int maxPlayers = System.Convert.ToInt32(userState["maxPlayers"]);
                        bool allowBots = (System.Convert.ToInt32(userState["customAllowBots"]) == 1);
                        PubNubRoomInfo roomInfo = new PubNubRoomInfo(uuid, name, selectedMap, maxPlayers, allowBots);
                        PubNubPlayer owner = new PubNubPlayer(uuid, name, false, true, DataCarrier.chosenCharacter, DataCarrier.chosenHat);
                        roomInfo.PlayerList.Add(owner);
                        PubNubAddRoom(roomInfo);
                    }
                }
                else
                {
                    Debug.Log("Could not find Presence State information: " + uuid);
                }
            });
            */

        }

        public void UserIsOffline(string uuid)
        {
            Debug.Log("temp: User went offline" + uuid);
            OnPlayerLeftRoom(uuid);
            //  Remove the room, if it exists
            PubNubRemoveRoom(uuid, false);
            if (CurrentRoom != null && CurrentRoom.OwnerId == uuid)
            {
                Debug.Log("temp: User " + uuid + " is leaving the room");
                LeaveRoom();
            }
        }

        private void PubNubAddRoom(PubNubRoomInfo roomInfo)
        {
            //  Only add the room if one does not already exist
            bool addRoom = true;
            foreach (PubNubRoomInfo room in pubNubRooms)
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
                Debug.Log("Removing Room (setting visibility to 0) " + uuid);
                Dictionary<string, object> metaData = new Dictionary<string, object>();
                metaData["visible"] = 0;
                //List<string> channels = new List<string>() { "game" };
                string[] channels = new string[] { PubNubUtilities.gameLobbyChannel };
                PNResult<PNSetStateResult> setPresenceResponse = await pubnub.SetPresenceState()
                    .Channels(channels)
                    .Uuid(userId)
                    .State(metaData)
                    .ExecuteAsync();
                if (setPresenceResponse.Status.Error)
                {
                    Debug.Log("Error setting PubNub Presence State (PubNubRemoveRoom): " + setPresenceResponse.Status.ErrorData.Information);
                }

                //pubnub.SetPresenceState().Channels(channels).Uuid(userId).State(metaData).Async((result, status) =>
                //{
                //    if (status.Error)
                //    {
                //        Debug.Log("Error setting PubNub Presence State (PubNubRemoveRoom): " + status.ErrorData.Info);
                //    }
                //});
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

        //private void SubscribeCallbackHandler(object sender, System.EventArgs e)
        private void OnPnMessage(Pubnub pn, PNMessageResult<object> result)
        {
            //Debug.Log("temp: Received message on " + result.Channel + ", message is " + result.Message);
            //SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;

            if (result.Message != null)
            {
                //Debug.Log("temp: Message was not null");
                //  Messages (Publish)
                //if (mea.MessageResult.Subscription.Equals(PubNubUtilities.roomStatusChannel))
                //  Notify chat
                try
                {
                    onLobbyChatMessage(result);
                }
                catch (System.Exception) { }
                if (result.Channel.Equals(PubNubUtilities.roomStatusChannel))
                {
                    if (CurrentRoom == null)
                    {
                        Debug.Log("Not in an active game");
                        return;
                    }

                    Dictionary<string, object> payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Message.ToString());

                    //if (mea.MessageResult.Payload is Dictionary<string, object>)
                    if (payload is Dictionary<string, object>)
                    {
                        Debug.Log("Init: Recieved message in Connector Subscribe handler");
                        //Dictionary<string, object> payload = (Dictionary<string, object>)mea.MessageResult.Payload;
                        //Dictionary<string, object> payload = (Dictionary<string, object>)payloadCheck;
                        if (payload.ContainsKey("gameSceneLoaded"))
                        {
                            Debug.Log("Init: Received Game Scheme Loaded");
                            Debug.Log("Current Room Name: " + CurrentRoom.Name + ", loading room is named " + payload["currentRoomName"]);

                            if (!CurrentRoom.Name.Equals(payload["currentRoomName"])) return;   //  Check the game being loaded is intended for us
                            
                            if (!isMasterClient)
                            {
                                //  Received details about the bots in the game from the master instance
                                int botCount = System.Convert.ToInt32(payload["botCount"]);
                                CurrentRoom.botCount = botCount;
                                if (botCount > 0)
                                {
                                    //string[] rxBotNames = (string[])payload["botNames"];
                                    string[] rxBotNames = (payload["botNames"] as Newtonsoft.Json.Linq.JArray).ToObject<string[]>();
                                    //long[] rxBotCharacters = (long[])payload["botCharacters"];
                                    long[] rxBotCharacters = (payload["botCharacters"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                                    //long[] rxBotHats = (long[])payload["botHats"];
                                    long[] rxBotHats = (payload["botHats"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                                    for (int i = 0; i < botCount; i++)
                                    {
                                        CurrentRoom.bNames[i] = rxBotNames[i];
                                        CurrentRoom.bChars[i] = System.Convert.ToInt32(rxBotCharacters[i]);
                                        CurrentRoom.bHats[i] = System.Convert.ToInt32(rxBotHats[i]);
                                    }
                                    //  Create local instances of the bots which will be controlled by the master client
                                    //Bot[] b = new Bot[botCount];
                                    //CurrentRoom.AddBots(b);
                                    //curBots = new Bot[0];
                                    curBots = GetBotList();
                                    CurrentRoom.AddBotObjects(curBots);
                                    for (int i = 0; i < botCount; i++)
                                    {
                                        //CreateABot(true);
                                    }
                                }

                                UpdatePlayerCount();
                                CurrentRoom.SortPlayerListAndAssignIds();
                                SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
                                //  DCC todo?  Notify other participants that the game has started
                                loadNow = false;
                                isLoadingGameScene = true;
                            }

                        }
                        if (payload.ContainsKey("playerStats"))
                        {
                            Debug.Log("Init: Received Player Stats");
                            //  DCC 123
                            //GameManager.instance.startingCountdownStarted = true;
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
                            if (CurrentRoom == null || CurrentRoom.PlayerList == null) return;
                            Debug.Log("There IS a current room and a Player List in that room");
                            for (int i = 0; i < CurrentRoom.PlayerList.Count; i++)
                            {
                                if (userId != null && userId.Equals(CurrentRoom.PlayerList[i].UserId))
                                {
                                    Debug.Log("Found Room.  Calling set properties for user ID " + userId);
                                    CurrentRoom.PlayerList[i].SetProperties(payload);
                                }
                                else if (playerId != -1 && playerId == CurrentRoom.PlayerList[i].ID)
                                {
                                    Debug.Log("Found Room.  Calling set properties.");
                                    CurrentRoom.PlayerList[i].SetProperties(payload);
                                }
                            }
                        }
                    }
                }
                else if (result.Channel.StartsWith(PubNubUtilities.gameLobbyRoomsWildcardRoot))
                {
                    //  Not the Room status Channel
                    object[] payloadCheck = JsonConvert.DeserializeObject<object[]>(result.Message.ToString());
                    //(mea.MessageResult.Payload is string[])
                    bool isStringArray = payloadCheck.OfType<string>().Any();
                    if (!isStringArray)
                    {
                        Debug.LogWarning("Unexpected Message type recieved on " + result.Channel);
                    }
                    //string[] payload = (string[])mea.MessageResult.Payload;
                    string[] payload = JsonConvert.DeserializeObject<string[]>(result.Message.ToString());
                    string requestorId = payload[1];
                    if (requestorId == userId)
                    {
                        return; //  Ignore messages we sent ourself
                    }
                    if (payload[0].Equals("JOIN_CUSTOM_ROOM"))
                    {
                        string requestorNickname = payload[2];
                        int requestedCharacter = System.Int32.Parse(payload[3]);
                        int requestedHat = System.Int32.Parse(payload[4]);
                        string roomOwnerId = payload[5];
                        Debug.Log("Received Remote join request from " + requestorId);
                        PubNubPlayer remotePlayer = new PubNubPlayer(requestorId, requestorNickname, false, false, requestedCharacter, requestedHat);
                        //  Consider whether the player entered the current room
                        
                        //if (CurrentRoom != null && CurrentRoom.PlayerList != null)
                        //    CurrentRoom.PlayerList.Add(remotePlayer);
                        //  Consider the case where we are not in the room
                        foreach (PubNubRoomInfo room in pubNubRooms)
                        {
                            if (room.OwnerId.Equals(roomOwnerId))
                            {
                                room.PlayerList.Add(remotePlayer);
                                break;
                            }
                        }
                        OnPlayerEnteredRoom(remotePlayer);
                        onRoomListChange(pubNubRooms.Count);
                    }
                    else if (payload[0].Equals("LEAVE_CUSTOM_ROOM"))
                    {
                        Debug.Log("Received Remote leave request from " + requestorId);
                        string roomOwnerId = payload[2];
                        OnPlayerLeftRoom(requestorId);
                        for (int i = 0; i < pubNubRooms.Count; i++)
                        {
                            if (pubNubRooms[i].OwnerId.Equals(roomOwnerId))
                            {
                                for (int j = 0; j < pubNubRooms[i].PlayerList.Count; j++)
                                {
                                    Debug.Log("Player Count: " + pubNubRooms[i].PlayerList.Count);
                                    if (pubNubRooms[i].PlayerList[j].UserId.Equals(requestorId))
                                    {
                                        pubNubRooms[i].PlayerList.RemoveAt(j);
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log("Comparing " + pubNubRooms[i].PlayerList[j].UserId + " with " + requestorId + " but did not match");
                                    }
                                }
                            }
                        }
                        onRoomListChange(pubNubRooms.Count);
                    }
                }
            }
        }

        private void OnPnPresence(Pubnub pubnub, PNPresenceEventResult result)
        {
            if (result.Channel.Equals(PubNubUtilities.gameLobbyChannel))
            {
                //else if (mea.PresenceEventResult != null)
                //{
                //if (mea.PresenceEventResult.Event.Equals("leave") || mea.PresenceEventResult.Event.Equals("timeout"))
                if (result.Event.Equals("leave") || result.Event.Equals("timeout"))
                {
                    Debug.Log("Presence: " + result.Event + " for channel " + result.Channel  + " for user " + result.Uuid);
                    //  The specified user has left, remove any room they created from the rooms array
                    //UserIsOffline(mea.PresenceEventResult.UUID);
                    UserIsOffline(result.Uuid);
                }
                //else if (mea.PresenceEventResult.Event.Equals("join"))
                else if (result.Event.Equals("join"))
                {
                    Debug.Log("Presence: Join for channel " + result.Channel + " for user " + result.Uuid);
                    //  The specified user has joined.  If they have created a room then add it to our room list
                    //UserIsOnlineOrStateChange(mea.PresenceEventResult.UUID);
                    UserIsOnlineOrStateChange(result.Uuid);
                }
                //else if (mea.PresenceEventResult.Event.Equals("state-change"))
                else if (result.Event.Equals("state-change"))
                {
                    Debug.Log("Presence: State Change for channel " + result.Channel + " for user " + result.Uuid);
                    //  The specified user has created or deleted a room
                    //UserIsOnlineOrStateChange(mea.PresenceEventResult.UUID);
                    UserIsOnlineOrStateChange(result.Uuid);
                }
                //}
            }

        }

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
                Debug.Log("Error sending PubNub Message (Join Custom Room " + channel + "): " + publishResponse.Status.ErrorData.Information);
            }
            //pubnub.Publish().Message(joinCustomRoomMsg).Channel(channel).Async((result, status) =>
            //{
            //    if (status.Error)
            //    {
            //        Debug.Log("Error sending PubNub Message (Join Custom Room " + channel + ")");
            //    }
            //});
        }

        private async void PNLeaveCustomRoom(Pubnub pubnub, string channel, string myUserId, string ownerId)
        {
            string[] leaveCustomRoomMsg = new string[3];
            leaveCustomRoomMsg[0] = "LEAVE_CUSTOM_ROOM";
            leaveCustomRoomMsg[1] = myUserId;
            leaveCustomRoomMsg[2] = ownerId;
            Debug.Log("Publishing leave request on " + channel);
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                .Channel(channel)
                .Message(leaveCustomRoomMsg)
                .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log("Error sending PubNub Message (Leave Custom Room " + channel + "): " + publishResponse.Status.ErrorData.Information);
            }

            //pubnub.Publish().Message(leaveCustomRoomMsg).Channel(channel).Async((result, status) =>
            //{
            //    if (status.Error)
            //    {
            //        Debug.Log("Error sending PubNub Message (Leave Custom Room " + channel + ")");
            //    }
            //});
        }

    }
}
