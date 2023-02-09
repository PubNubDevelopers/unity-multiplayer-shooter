using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

namespace Visyde
{
    /// <summary>
    /// Connector
    /// - manages the initial connection and matchmaking
    /// </summary>

    public class Connector : MonoBehaviourPunCallbacks
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
        bool inCustom;
        public bool isInCustomGame{
            get{
                return inCustom && PhotonNetwork.InRoom;
            }
        }

        public int selectedMap { get;  protected set; }
        public int totalPlayerCount { get; protected set; }
        public List<RoomInfo> rooms { get; protected set; }
        public bool autoReconnect { get; set; }

        public delegate void IntEvent(int i);
        //public UnityAction onRoomListChange;
        public IntEvent onRoomListChange;
        public UnityAction onCreateRoomFailed;
        public UnityAction onJoinRoom;
        public UnityAction onLeaveRoom;
        public UnityAction onDisconnect;
        public delegate void PlayerEvent(Player player);
        public PlayerEvent onPlayerJoin;
        public PlayerEvent onPlayerLeave;

        // Internal variables:
        Bot[] curBots;
        int bnp;
        bool startCustomGameNow;
        bool loadNow;                       // if true, the game scene will be loaded. Matchmaking will set this to true instantly when enough 
                                            // players are present, custom games on the other hand will require the host to press the "Start" button first.
        bool isLoadingGameScene;

        class Bot
        {
            public string name;				// bot name
            public Vector3 scores; 			// x = kills, y = deaths, z = other scores
            public int characterUsing;		// the chosen character of the bot (index only)
            public int hat;
        }

        void Awake(){
            instance = this;
        }

        void Start()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            loadNow = false;
            rooms = new List<RoomInfo>();
            autoReconnect = true;

            // Do connection loop:
            StartCoroutine(Reconnection());
        }

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

        // Update is called once per frame
        void Update()
        {
            // Room managing:
            if (PhotonNetwork.InRoom && !isLoadingGameScene)
            {
                // Set the variable "loadNow" to true if the room is already full:
                if (totalPlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers && ((isInCustomGame && startCustomGameNow) || !isInCustomGame))
                {
                    loadNow = true;
                }

                // Go to the game scene if the variable "loadNow" is true:
                if (loadNow){
                    if (PhotonNetwork.IsMasterClient)
                    {
                        PhotonNetwork.CurrentRoom.IsOpen = false;
                        PhotonNetwork.LoadLevel(gameSceneName);
                        loadNow = false;
                        isLoadingGameScene = true;
                    }
                }
            }
        }

        // Matchmaking:
        public void FindMatch()
        {
            ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
            h.Add("isInMatchmaking", true);
            PhotonNetwork.JoinRandomRoom(h, 0);
        }
        public void CancelMatchmaking()
        {
            PhotonNetwork.LeaveRoom();
            // Clear bots list:
            curBots = new Bot[0];
        }

        // Custom Game:
        public void JoinCustomGame(RoomInfo room){
            tryingToJoinCustom = true;
            PhotonNetwork.JoinRoom(room.Name);
        }
        public void CreateCustomGame(int selectedMap, int maxPlayers, bool allowBots)
        {
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
        }
        public void StartCustomGame(){
            // Start creating bots (if bots are allowed) as this will fill out the empty players:
            if (inCustom && !loadNow)
            {
                // Create the bots if allowed:
                if ((bool)PhotonNetwork.CurrentRoom.CustomProperties["customAllowBots"])
                {
                    // Clear the bots array first:
                    curBots = new Bot[0];
                    // Generate a number to be attached to the bot names:
                    bnp = Random.Range(0, 9999);
                    int numCreatedBots = 0;
                    int max = PhotonNetwork.CurrentRoom.MaxPlayers - totalPlayerCount;
                    while (numCreatedBots < max)
                    {
                        CreateABot();
                        numCreatedBots++;
                    }
                }

                startCustomGameNow = true;
            }
        }

        // Bot Creation:
        void StartCreatingBots()
        {
            // Generate a number to be attached to the bot names:
            bnp = Random.Range(0, 9999);
            Invoke("CreateABot", Random.Range(minBotCreationTime, maxBotCreationTime));
        }
        void CreateABot()
        {
            if (PhotonNetwork.InRoom)
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
                ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();

                string[] bn = new string[b.Length];
                Vector3[] bs = new Vector3[b.Length];
                int[] bc = new int[b.Length];
                int[] bHats = new int[b.Length];
                for (int i = 0; i < b.Length; i++)
                {
                    bn[i] = b[i].name;
                    bs[i] = b[i].scores;
                    bc[i] = b[i].characterUsing;
                    bHats[i] = b[i].hat;
                }
                bn[bn.Length - 1] = b[b.Length - 1].name;
                bs[bs.Length - 1] = b[b.Length - 1].scores;
                bc[bc.Length - 1] = b[b.Length - 1].characterUsing;
                bHats[bc.Length - 1] = b[b.Length - 1].hat;

                h.Add("botNames", bn);
                h.Add("botScores", bs);
                h.Add("botCharacters", bc);
                h.Add("botHats", bHats);
                PhotonNetwork.CurrentRoom.SetCustomProperties(h);

                if (!isInCustomGame)
                {
                    // Continue adding another bot after a random delay (to give human players enough time to join, and also to simulate realism):
                    Invoke("CreateABot", Random.Range(minBotCreationTime, maxBotCreationTime));
                }
            }
        }

        Bot[] GetBotList()
        {
            Bot[] list = new Bot[0];

            // Download the bots list if we already have one:
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("botNames"))
            {
                string[] bn = (string[])PhotonNetwork.CurrentRoom.CustomProperties["botNames"];
                Vector3[] bs = (Vector3[])PhotonNetwork.CurrentRoom.CustomProperties["botScores"];
                int[] bc = (int[])PhotonNetwork.CurrentRoom.CustomProperties["botCharacters"];

                list = new Bot[bn.Length];
                for (int i = 0; i < list.Length; i++)
                {
                    list[i] = new Bot();
                    list[i].name = bn[i];
                    list[i].scores = bs[i];
                    list[i].characterUsing = bc[i];
                }
            }
            return list;
        }

        void UpdatePlayerCount()
        {
            // Get the "Real" player count:
            int players = PhotonNetwork.CurrentRoom.PlayerCount;

            // ...then check if there are bots in the room:
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("botNames"))
            {
                // If there are, set the bots list from the server:
                if (!PhotonNetwork.IsMasterClient) curBots = GetBotList();

                // ... and get the number of bots and add it to the total player count:
                players += curBots.Length;
            }

            // Set the total player count:
            totalPlayerCount = players;
        }

        // PHOTON:
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

        public override void OnPlayerEnteredRoom(Player player)
        {
            // When a player connects, update the player count:
            UpdatePlayerCount();

            // Events:
            onPlayerJoin(player);
        }
        public override void OnPlayerLeftRoom(Player player)
        {
            // When a player disconnects, update the player count:
            UpdatePlayerCount();

            // Also, if a player disconnects while matchmaking and they happen to be the master client, a new master client will be assigned.
            // We could be the new master client so check if we are. If we are, continue adding bots (if bots are allowed):
            if (PhotonNetwork.IsMasterClient && createBots && !isInCustomGame)
            {
                // Get the existing bot list made by the last master client, or make a new one if none:
                curBots = GetBotList();
                // Start creating bots after a delay:
                Invoke("StartCreatingBots", curBots.Length > 0 ? Random.Range(minBotCreationTime, maxBotCreationTime) : startCreatingBotsAfter);
            }

            // Events:
            onPlayerLeave(player);
        }

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
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
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
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            DataCarrier.message = message;
            if (!tryingToJoinCustom) PhotonNetwork.JoinRandomRoom();
        }
        public override void OnJoinedRoom()
        {
            tryingToJoinCustom = false;

            // Know if the room we joined in is a custom game or not:
            inCustom = !(bool)PhotonNetwork.CurrentRoom.CustomProperties["isInMatchmaking"];

            // This is only used to check if we've loaded the game and ready. This sets to 0 after loading the game scene:
            PhotonNetwork.LocalPlayer.SetScore(-1); // -1 = not ready, 0 = ready

            // Setup scores (these are the actual player scores):
            ExitGames.Client.Photon.Hashtable p = new ExitGames.Client.Photon.Hashtable();
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
            PhotonNetwork.LocalPlayer.SetCustomProperties(p);

            // Let's update the total player count (for local reference):
            UpdatePlayerCount();

            // (MATCHMAKING ONLY) Start creating bots (if bots are allowed):
            if (PhotonNetwork.IsMasterClient && createBots && !isInCustomGame)
            {
                // Clear the bots array first:
                curBots = new Bot[0];
                // then start creating new ones:
                Invoke("StartCreatingBots", startCreatingBotsAfter);
            }

            // Events:
            onJoinRoom();
        }
        public override void OnLeftRoom(){
            tryingToJoinCustom = false;
            isLoadingGameScene = false;

            // Events:
            onLeaveRoom();
        }
    }
}
