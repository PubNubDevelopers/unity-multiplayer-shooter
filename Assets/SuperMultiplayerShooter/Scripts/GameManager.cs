using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//using Photon.Pun;
//using Photon.Realtime;
//using Photon.Pun.UtilityScripts;
using PubnubApi;
using PubnubApi.Unity;
using PubNubUnityShowcase;
using System;
using UnityEngine.Windows;
using Newtonsoft.Json;

namespace Visyde
{
    /// <summary>
    /// Game Manager
    /// - Simply the game manager. The one that controls the game itself. Provides game settings and serves as the central component by
    /// connecting other components to communicate with each other.
    /// </summary>

    public class GameManager : MonoBehaviour //MonoBehaviourPunCallbacks, IInRoomCallbacks, IConnectionCallbacks
    {
        public static GameManager instance;

        //  PubNub Properties
        public Pubnub pubnub = null;
        private PubNubUtilities pubNubUtilities = new PubNubUtilities();
        private SubscribeCallbackListener listener = new SubscribeCallbackListener();
        public readonly Dictionary<string, GameObject> ResourceCache = new Dictionary<string, GameObject>();
        //private bool overallAllPlayersReady = false;
        //  End PubNub properties

        public string playerPrefab;                     // Name of player prefab. The prefab must be in a "Resources" folder.

        [Space]
        public GameMap[] maps;
        [HideInInspector] public int chosenMap = -1;

        [Space]
        [Header("Game Settings:")]
        public bool useMobileControls;              	// if set to true, joysticks and on-screen buttons will be enabled
        public int respawnTime = 5;             		// delay before respawning after death
        public float invulnerabilityDuration = 3;		// how long players stay invulnerable after spawn
        public int preparationTime = 3;                 // the starting countdown before the game starts
        public int gameLength = 120;                    // time in seconds
        public bool showEnemyHealth = true;
        public bool damagePopups = true;
        [System.Serializable]
        public class KillData
        {
            public bool notify = true;
            public string message;
            public int bonusScore;
        }
        public KillData[] multiKillMessages;
        public float multikillDuration = 3;             // multikill reset delay
        public bool allowHurtingSelf;                   // allow grenades and projectiles to hurt their owner
        public bool deadZone;                           // kill players when below the dead zone line
        public float deadZoneOffset;                    // Y position of the dead zone line

        [Space]
        [Header("Others:")]
        public bool doCamShakesOnDamage;            	// allow cam shakes when taking damage
        public float camShakeAmount = 0.3f;
        public float camShakeDuration = 0.1f;

        [Space]
        [Header("References:")]
        public ItemSpawner itemSpawner;
        public ControlsManager controlsManager;
        public UIManager ui;
        public CameraController gameCam;                // The main camera in the game scene (used for the controls)
        public ObjectPooler pooler;

        // if the starting countdown has already begun:
        public bool countdownStarted
        {
            get { return startingCountdownStarted; }
        }
        // the progress of the starting countdown:
        public float countdown
        {
            get { return (float)(gameStartsIn - epochTime() /*Time.timeAsDouble PhotonNetwork.Time*/); }
        }
        // the time elapsed after the starting countdown:
        public float timeElapsed
        {
            get { return (float)elapsedTime; }
        }
        // the remaining time before the game ends:
        public int remainingGameTime
        {
            get { return (int)remainingTime; }
        }
        public GameMap getActiveMap{
            get{
                foreach (GameMap g in maps){
                    if (g.gameObject.activeInHierarchy) return g;
                }

                return null;
            }
        }

        [System.NonSerialized] public bool gameStarted = false;                                                 // if the game has started already
        [System.NonSerialized] public string[] playerRankings = new string[0];       				            // used at the end of the game
        public PlayerInstance[] bots;                                                   // Player instances of bots
        public PlayerInstance[] players;                                               // Player instances of human players (ours is first)
        [System.NonSerialized] public bool isGameOver;                                                          // is the game over?
        [System.NonSerialized] public PlayerController ourPlayer;                                				// our player's player (the player object itself)
        public List<PlayerController> playerControllers = new List<PlayerController>();	// list of all player controllers currently in the scene
        [System.NonSerialized] public bool dead;
        [System.NonSerialized] public float curRespawnTime;
        public static bool isDraw;                                                          				    // is game draw?
        bool hasBots;                                                                       				    // does the game have bots?

        // Local copy of bot stats (so we don't periodically have to download them when we need them):
        string[] bNames = new string[0];		// Bot names
        //Vector3[] bScores = new Vector3[0];		// Bot scores (x = kills, y = deaths, z = other scores)
        int[] bScoresKills = new int[4];
        int[] bScoresDeaths = new int[4];
        int[] bScoresOther = new int[4];
        int[] bChars = new int[0];				// Bot's chosen character's index
        int[] bHats = new int[0];               // Bot hats (cosmetics)

        // Used for time syncronization:
        [System.NonSerialized] public double startTime, elapsedTime, remainingTime, gameStartsIn;
        //  DCC uuu
        private bool startingCountdownStarted = false;  //  Trigger for Waiting for Players
        private bool doneGameStart = false;

        // For respawning:
        double deathTime;

        // Others:
        //Player[] punPlayersAll;
        List<PubNubPlayer> playersAll;
        public bool spawnComplete = false;

        void Awake()
        {
            instance = this;
            //  DCC 123
            /*if (Connector.instance.isMasterClient && Connector.instance.CurrentRoom.PlayerList.Count > 1)
            {
                startingCountdownStarted = Connector.instance.atLeastOnePlayerReady;
                Debug.Log("Have set starting countdown started to " + startingCountdownStarted);
            }
            else if (Connector.instance.isMasterClient)
            {
                Debug.Log("Init: " + Connector.instance.CurrentRoom.PlayerList.Count);
                startingCountdownStarted = true;
            }
            */

            // Prepare player instance arrays:
            bots = new PlayerInstance[0];
            players = new PlayerInstance[0];

            // Cache current player list:
            //punPlayersAll = PhotonNetwork.PlayerList;
            playersAll = Connector.instance.CurrentRoom.PlayerList;

            // Do we have bots in the game? Download bot stats if we have:
            //  DCC todo do we need the .Bots variable / property at all??
            //hasBots = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("botNames");
            hasBots = ((Connector.instance.CurrentRoom.Bots != null && Connector.instance.CurrentRoom.Bots.Count > 0) ||
                (Connector.instance.CurrentRoom.BotObjects != null && Connector.instance.CurrentRoom.BotObjects.Length > 0));
            if (hasBots)
            {

                // Download the stats:
                //bNames = (string[])PhotonNetwork.CurrentRoom.CustomProperties["botNames"];
                //bNames = (string[])Connector.instance.CurrentRoom.Bots["botNames"];
                bNames = Connector.instance.CurrentRoom.bNames;
                bChars = Connector.instance.CurrentRoom.bChars;
                bHats = Connector.instance.CurrentRoom.bHats;
                //bScores = (Vector3[])PhotonNetwork.CurrentRoom.CustomProperties["botScores"];
                //bScores = (Vector3[])Connector.instance.CurrentRoom.Bots["botScores"];
                //bScoresKills = (int[])Connector.instance.CurrentRoom.Bots["botScoresKills"];
                //bScoresDeaths = (int[])Connector.instance.CurrentRoom.Bots["botScoresDeaths"];
                //bScoresOther = (int[])Connector.instance.CurrentRoom.Bots["botScoresOther"];
                //bScoresKills = new int[Connector.instance.CurrentRoom.Bots.Count];
                //bScoresDeaths = new int[Connector.instance.CurrentRoom.Bots.Count];
                //bScoresOther = new int[Connector.instance.CurrentRoom.Bots.Count];
                //bChars = (int[])PhotonNetwork.CurrentRoom.CustomProperties["botCharacters"];
                //bChars = (int[])Connector.instance.CurrentRoom.Bots["botCharacters"];
                // And their "chosen" cosmetics:
                //bHats = (int[])PhotonNetwork.CurrentRoom.CustomProperties["botHats"];
                //bHats = (int[])Connector.instance.CurrentRoom.Bots["botHats"];

                // ...then generate the player instances:
                bots = GenerateBotPlayerInstances();
            }
            // Generate human player instances:
            players = GeneratePlayerInstances(true);

            // Don't allow the device to sleep while in game:

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            //  Initialize PubNub and subscribe to the appropriate channels
            pubnub = PNManager.pubnubInstance.InitializePubNub();
            List<string> channels = new List<string>();
            //  Room status updates, such as bot attributes or game started?
            channels.Add(PubNubUtilities.roomStatusChannel);  //  DCC todo add this to PubNubUtilities
            channels.Add(PubNubUtilities.itemChannel);
            channels.Add(PubNubUtilities.itemChannel + "-pnpres");
            //  Every player will send their updates on a unique channel, so subscribe to those
            foreach (PlayerInstance playerInstance in players)
            {
                if (!playerInstance.isMine)
                {
                    channels.Add(PubNubUtilities.playerActionsChannelPrefix + playerInstance.playerID);
                    channels.Add(PubNubUtilities.playerPositionChannelPrefix + playerInstance.playerID);
                    channels.Add(PubNubUtilities.playerCursorChannelPrefix + playerInstance.playerID);
                }
            }
            //  The master client controls the bots, so if we are not the master, register to receive bot updates
            foreach (PlayerInstance bot in bots)
            {
                //if (!PhotonNetwork.IsMasterClient)
                if (!Connector.instance.isMasterClient)
                {
                    channels.Add(PubNubUtilities.playerActionsChannelPrefix + bot.playerID);
                    channels.Add(PubNubUtilities.playerPositionChannelPrefix + bot.playerID);
                    channels.Add(PubNubUtilities.playerCursorChannelPrefix + bot.playerID);
                }
            }
            pubnub.AddListener(listener);
            listener.onMessage += OnPnMessage;
            listener.onPresence += OnPnPresence;
            //Subscribe to the list of Channels
            pubnub.Subscribe<string>()
               .Channels(channels)
               .Execute();
        }

        // Use this for initialization
        void Start()
        {
            // Setups:
            isDraw = false;
            gameCam.gm = this;

            // Determine the type of controls:
            controlsManager.mobileControls = useMobileControls;

            // Get the chosen map then enable it:
            //chosenMap = (int)PhotonNetwork.CurrentRoom.CustomProperties["map"];
            chosenMap = Connector.instance.CurrentRoom.Map;
            for (int i = 0; i < maps.Length; i++)
            {
                maps[i].gameObject.SetActive(chosenMap == i);
            }

            // After loading the scene, we (the local player) are now ready for the game:
            Ready();

            // Start checking if all players are ready:
            if (Connector.instance.isMasterClient)
            {
                Connector.instance.CurrentRoom.PlayerList[Connector.instance.GetMasterId()].IsReady = true;
                InvokeRepeating("CheckIfAllPlayersReady", 1, 0.5f);
            }
        }

        void OnDestroy()
        {
            Debug.Log("Game Manager OnDestroy");
            //  DCC todo Only want to unsubscribe from Game specific channels
            //  DCC todo THIS DOES GET CALLED WHEN THE GAME ENDS SO WE DO WANT TO UNSUBSCRIBE
            //pubnub.SubscribeCallback -= SubscribeCallbackHandler;
            pubnub.UnsubscribeAll();
            listener.onMessage -= OnPnMessage;
            listener.onPresence -= OnPnPresence;
        }

        void CheckIfAllPlayersReady()
        {
            if (!isGameOver)
            {
                // Check if players are ready:
                if (!startingCountdownStarted)
                {
                    bool allPlayersReady = true;

                    //for (int i = 0; i < punPlayersAll.Length; i++)
                    //for (int i = 0; i < playersAll.Count; i++)
                    Debug.Log("There are this many people in the game: " + Connector.instance.CurrentRoom.PlayerList.Count);
                    for (int i = 0; i < Connector.instance.CurrentRoom.PlayerList.Count; i++)
                    {
                        Debug.Log("Player " + Connector.instance.CurrentRoom.PlayerList[i].NickName + " is ready? " + Connector.instance.CurrentRoom.PlayerList[i].IsReady);
                        if (!Connector.instance.CurrentRoom.PlayerList[i].IsReady)
                        {
                            allPlayersReady = false;
                            Debug.Log("Player " + Connector.instance.CurrentRoom.PlayerList[i].NickName + " is not ready");
                            break;
                        }
                        // If a player hasn't yet finished loading, don't start:
                        //if (punPlayersAll[i].GetScore() == -1)
                        //if (playersAll[i].GetScore() == -1)
                        //{
                        //    Debug.Log("Setting all Players ready to false because " + playersAll[i].NickName);
                        //    allPlayersReady = false;
                        //}
                    }
                    Debug.Log("All players Ready? " + allPlayersReady);
                    if (allPlayersReady)
                    {
                        //  All players are now ready, tell them to start
                        StartGamePrepare();
                    }

                    // Start the preparation countdown when all players are done loading:
                    //  DCC 123
                    //if ((allPlayersReady) && Connector.instance.isMasterClient)//PhotonNetwork.IsMasterClient)
                    //{
                    //    Debug.Log("Init: All players are ready");
                    //    //  DCC uuu
                    //    //  StartGamePrepare();
                    //    this.overallAllPlayersReady = true;
                    //}
                    //else
                    //{
                    //    Debug.Log("Init: All players are NOT ready");
                    //}
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log("Starting Countdown Started: " + startingCountdownStarted);
            if (!isGameOver)
            {
                // Start the game when preparation countdown is finished:
                if (startingCountdownStarted)
                {
                    if (elapsedTime >= (gameStartsIn - startTime) && !gameStarted && !doneGameStart)
                    {
                        // GAME HAS STARTED!
                        //if (PhotonNetwork.IsMasterClient)
                        //  DCC 123
                        //  DCC uuu
                        if (Connector.instance.isMasterClient)
                        {
                            doneGameStart = true;
                            gameStarted = true;
                            //  DCC todo do this properly
                            //pnSendGameState("started", true);
                            //  DCC todo.  If the non-master users are not listening for this, we can probably make a direct call?
                            //  DCC 123 commented this
                            //  DCC uuu
                            //pubNubUtilities.PubNubSendRoomProperties(pubnub, "started", true);
                            //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                            //h["started"] = true;
                            //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
                            //StartGameTimer();
                            CancelInvoke("CheckIfAllPlayersReady");
                        }
                        else
                        {
                            doneGameStart = true;
                            gameStarted = true;
                        }

                    }
                }

                // Respawning:
                if (dead)
                {
                    if (deathTime == 0)
                    {
                        //deathTime = PhotonNetwork.Time + respawnTime;
                        deathTime = /*Time.timeAsDouble*/ epochTime() + respawnTime;
                    }
                    //curRespawnTime = (float)(deathTime - PhotonNetwork.Time);
                    curRespawnTime = (float)(deathTime - epochTime() /*Time.timeAsDouble*/);
                    if (curRespawnTime <= 0)
                    {
                        dead = false;
                        deathTime = 0;
                        Spawn(Connector.instance.GetMyId(), true);
                        //  Tell everyone else in the Game to respawn
                        Dictionary<string, object> props = new Dictionary<string, object>();
                        props.Add("respawn", Connector.instance.GetMyId());
                        pubNubUtilities.PubNubSendRoomProperties(pubnub, props);
                    }
                }

                // Calculating the elapsed and remaining time:
                CheckTime();

                // Finish game when elapsed time is greater than or equal to game length:
                if (elapsedTime + 1 >= gameLength && gameStarted && !isGameOver)
                {
                    // Post the player rankings:
                    //if (PhotonNetwork.IsMasterClient)
                    if (Connector.instance.isMasterClient)
                    {
                        // Get player list by order based on scores and also set "draw" to true (the player sorter will set this to false if not draw):
                        isDraw = true;

                        // List of player names for the rankings:
                        PlayerInstance[] ps = SortPlayersByScore();
                        string[] p = new string[ps.Length];
                        for (int i = 0; i < ps.Length; i++)
                        {
                            p[i] = ps[i].playerName;
                        }

                        isDraw = ps.Length > 1 && isDraw;

                        // Mark as game over:
                        //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                        Dictionary<string, object> h = new Dictionary<string, object>();
                        h.Add("rankings", p);
                        h.Add("draw", isDraw);
                        //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
                        pubNubUtilities.PubNubSendRoomProperties(pubnub, h);

                        // Hide room from lobby:
                        //PhotonNetwork.CurrentRoom.IsVisible = false;
                        Connector.instance.PubNubRemoveRoom(Connector.instance.LocalPlayer.UserId, true);
                    }
                }

                // Check if game is over:
                if (playerRankings.Length > 0){
                    isGameOver = true;
                }
            }
        }

        void CheckTime(){
            //elapsedTime = (PhotonNetwork.Time - startTime);
            elapsedTime = (/*Time.timeAsDouble*/ epochTime() - startTime);
            //remainingTime = gameLength - (elapsedTime % gameLength);
            remainingTime = gameLength - elapsedTime;
        }

        // Called when we enter the game world:
        void Ready()
        {
            // Spawn our player:
            Spawn(Connector.instance.GetMyId(), true);
            for (int i = 0; i < Connector.instance.CurrentRoom.PlayerList.Count; i++)
            {
                if (!Connector.instance.CurrentRoom.PlayerList[i].IsLocal)
                {
                    //  We have other real players in this game, spawn in their players
                    //  DCC todo, probably makes sense to spawn in players in response to a message from them containing their location
                    //  DCC todo TEST ONLY
                    Spawn(Connector.instance.CurrentRoom.PlayerList[i].ID, false);
                }
            }

            //  DCC todo also spawn the other players in the current room (they should already have up to date information)

            // ... and the bots if we have some and if we are the master client:
            //if (hasBots && PhotonNetwork.IsMasterClient)
            //if (hasBots && Connector.instance.isMasterClient)
            if (hasBots)
            {
                for (int i = 0; i < bots.Length; i++)
                {
                    //SpawnBot(i + PhotonNetwork.CurrentRoom.MaxPlayers);    // parameter is the bot ID
                    if (Connector.instance.isMasterClient)
                    {
                        //  Spawn a bot instance which we will own and control
                        //  DCC xxx
                        SpawnBot(i + Connector.instance.CurrentRoom.MaxPlayers, Connector.instance.LocalPlayer.ID, true);    // parameter is the bot ID
                    }
                    else
                    {
                        //  Spawn a bot instance which will be controlled by the master
                        SpawnBot(i + Connector.instance.CurrentRoom.MaxPlayers, Connector.instance.GetMasterId(), false);    
                    }
                }
            }

            if (!Connector.instance.isMasterClient)
            {
                //  If we have started and we are not the master, the master must have
                //  started the game for us
                //  DCC 123
                //gameStarted = true;
                //Debug.Log("Setting starting countdown started to true");
                //  DCC uuu
                //startingCountdownStarted = true;
                //startTime = epochTime();
                //gameStartsIn = (/*Time.timeAsDouble*/ epochTime() + preparationTime);

                //  Notify the master we are ready by setting our Score to 0
                //  DCC todo I don't think this is necessary since I assume that the game is ready to start straight away
                Dictionary<string, object> p = new Dictionary<string, object>();
                p.Add("playerStats", "stats");
                p.Add("score", 0);
                p.Add("userId", Connector.instance.LocalPlayer.UserId);

                // Debug.Log("Init: Sending message to master to say I am ready");
                pubNubUtilities.PubNubSendRoomProperties(pubnub, p);
                //  DCC todo I should be able to get rid of the whole SetScore property since I have moved to a different way of detecting if the player is ready
                Connector.instance.LocalPlayer.SetScore(0);


                Invoke("ReportReady", 1.0f);

                //Dictionary<string, object> metaData = new Dictionary<string, object>();
                //metaData["isReady"] = true;
                //string[] channels = new string[] { PubNubUtilities.gameLobbyChannel };
                //pubnub.SetPresenceState()
                //    .Channels(channels)
                //    .Uuid(Connector.instance.LocalPlayer.UserId)
                //    .State(metaData)
                //    .ExecuteAsync();
      
            }
            else
            {
                //  DCC todo this is a hack to get the master to load if there are two real players
                //  DCC 123
                //startingCountdownStarted = true;
                //  DCC 123
                //startTime = epochTime();
                //gameStartsIn = (/*Time.timeAsDouble*/ epochTime() + preparationTime);

                // Set our score to 0 on start (this is not the player's actual score, this is only used to determine if we're ready or not, 0 = ready, -1 = not):
                //  DCC todo check this works
                //Connector.instance.LocalPlayer.SetScore(0);

                
            }


        }

        /// <summary>
        /// Spawns the player.
        /// </summary>
        public void Spawn(int playerId, bool isMine)
        {
            Transform spawnPoint = maps[chosenMap].playerSpawnPoints[UnityEngine.Random.Range(0, maps[chosenMap].playerSpawnPoints.Count)];
            // There are 2 values in the player's instatiation data. The first one is reserved and only used if the player is a bot, while the 
            // second is for the cosmetics (in this case we only have 1 which is for the chosen hat, but you can add as many as your game needs):
            //  DCC todo spawn the player
            //ourPlayer = PhotonNetwork.Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity, 0, new object[] { 0, DataCarrier.chosenHat }).GetComponent<PlayerController>();
            if (Connector.instance.CurrentRoom != null)
            {
                //  DCC todo At some point we will need to spawn the other human players, presumably after being told to do so by a PN message.  The PlayerID to spawn (below set to the LocalPlayer.ID) will need to be part of that message
                GameObject tempOurPlayer = InstantiatePlayer(playerPrefab, spawnPoint.position, Quaternion.identity, false, playerId, -1, isMine, new object[] { 0, DataCarrier.chosenHat });
                if (isMine)
                {
                    ourPlayer = tempOurPlayer.GetComponent<PlayerController>();
                }
            }
        }


        /// <summary>
        /// Spawns a bot (only works on master client).
        /// </summary>
        public PlayerController SpawnBot(int bot, int ownerId, bool isMine)
        {
            //if (!PhotonNetwork.IsMasterClient) return;
            //if (!Connector.instance.isMasterClient) return null;

            Transform spawnPoint = maps[chosenMap].playerSpawnPoints[UnityEngine.Random.Range(0, maps[chosenMap].playerSpawnPoints.Count)];
            // Instantiate the bot. Bots are assigned with random hats (second value of the instantiation data):
            //  DCC todo spawn a bot
            //PlayerController botP = PhotonNetwork.InstantiateSceneObject(playerPrefab, spawnPoint.position, Quaternion.identity, 0, new object[] { bot }).GetComponent<PlayerController>();
            if (Connector.instance.CurrentRoom != null)
            {
                GameObject tempBot = InstantiatePlayer(playerPrefab, spawnPoint.position, Quaternion.identity, true, ownerId, bot, isMine, new object[] { 0, DataCarrier.chosenHat });
                PlayerController createdBot = tempBot.GetComponent<PlayerController>();
                return createdBot;
            }
            else {
                return null;
            }
        }

        //  DCC todo need to specify which hat is given to the character
        public GameObject InstantiatePlayer(string prefabId, Vector3 position, Quaternion rotation, bool isBot, int ownerId, int botId, bool isMine, object[] data)
        {
            GameObject res = null;
            bool cached = this.ResourceCache.TryGetValue(prefabId, out res);
            if (!cached)
            {
                res = Resources.Load<GameObject>(prefabId);
                if (res == null)
                    Debug.LogError("DefaultPool failed to load " + prefabId + ", did you add it to a Resources folder?");
                else
                    this.ResourceCache.Add(prefabId, res);
            }

            if (res.activeSelf)
                res.SetActive(false);
            
            GameObject instance = GameObject.Instantiate(res, position, rotation) as GameObject;
            //CosmeticsManager cosmetics = instance.GetComponent<CosmeticsManager>() as CosmeticsManager;
            //cosmetics.chosenHat = 0;
            PubNubPlayerProps properties = instance.GetComponent<PubNubPlayerProps>() as PubNubPlayerProps;
            if (properties == null)
            {
                Debug.LogError("Player must have a PubNubPlayer associated with the Prefab");
            }
            else
            {
                properties.isBot = isBot;
                properties.ownerId = ownerId;
                properties.IsMine = isMine;
                properties.botId = botId;
                properties.preview = false;
                //properties.itemIndex = itemIndex;
                //properties.spawnPointIndex = spawnPointIndex;
                //properties.index = index;
            }
            instance.SetActive(true);
            return instance;
        }

        public void SomeoneDied(int dying, int killer)
        {
            spawnComplete = true;

            // Add scores (master client only)
            if (Connector.instance.isMasterClient /*PhotonNetwork.IsMasterClient*/)
            {
                // Kill score to killer:
                if (killer != dying) AddScore(GetPlayerInstance(killer), true, false, 0);

                // ... and death to dying, and score deduction if suicide:
                AddScore(GetPlayerInstance(dying), false, true, killer == dying? -1 : 0);
            }

            // Display kill feed.
            ui.SomeoneKilledSomeone(GetPlayerInstance(dying), GetPlayerInstance(killer));
        }

        private void ReportReady()
        {
            Debug.Log("Notifying master that I am ready");
            //  Notify the master instance that we are ready
            Dictionary<string, object> props = new Dictionary<string, object>();
            props.Add("playerReady", Connector.instance.LocalPlayer.UserId);
            pubNubUtilities.PubNubSendRoomProperties(pubnub, props);
        }

        /// <summary>
        /// Returns the PlayerController of the player with the given name.
        /// </summary>
        public PlayerController GetPlayerControllerOfPlayer(string name)
        {
            for (int i = 0; i < playerControllers.Count; i++)
            {
                // Check if current item matches a player:
                if (string.CompareOrdinal(playerControllers[i].playerInstance.playerName, name) == 0)
                {
                    return playerControllers[i];
                }
            }

            return null;
        }
        public PlayerController GetPlayerControllerOfPlayer(PlayerInstance player)
        {
            for (int i = 0; i < playerControllers.Count; i++)
            {
                // Check if current item matches a player:
                if (playerControllers[i].playerInstance == player)
                {
                    return playerControllers[i];
                }
            }

            return null;
        }

        public void RemovePlayerController(int PlayerID)
        {
            for (int i = 0; i < playerControllers.Count; i++)
            {
                if (playerControllers[i].playerInstance.playerID == PlayerID)
                {
                    playerControllers.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Checks how many players are still in the game. If there's only 1 left, the game will end.
        /// </summary>
        /*
         * DCC: Changed the logic so when one player leaves the game ends
        public void CheckPlayersLeft()
        {
            if (GetPlayerList().Length <= 1 && PhotonNetwork.CurrentRoom.MaxPlayers > 1)
            {
                print("GAME OVER!");
                //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                Dictionary<string, object> h = new Dictionary<string, object>();
                double skip = 0;
                h["gameStartTime"] = skip;
                //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
                pubNubUtilities.PubNubSendRoomProperties(pubnub, h);
            }
        }
        */

        // Player leaderboard sorting:
        IComparer SortPlayers()
        {
            return (IComparer)new PlayerSorter();
        }
        public PlayerInstance[] SortPlayersByScore()
        {
            // Get the full player list:
            PlayerInstance[] allPlayers = GetPlayerList();

            // ...then sort them out based on scores:
            System.Array.Sort(allPlayers, SortPlayers());
            return allPlayers;
        }

        public PlayerInstance GetPlayerInstance(int playerID)
        {
            // Look in human player list:
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].playerID == playerID)
                {
                    return players[i];
                }
            }
            
            // Look in bots:
            if (hasBots)
            {
                for (int i = 0; i < bots.Length; i++)
                {
                    if (bots[i].playerID == playerID)
                    {
                        return bots[i];
                    }
                }
            }
            Debug.Log("Msg: Unable to find player instance for id " + playerID);

            return null;
        }
        public PlayerInstance GetPlayerInstance(string playerName)
        {
            // Look in human player list:
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].playerName == playerName)
                {
                    return players[i];
                }
            }
            // Look in bots:
            if (hasBots){
                for (int i = 0; i < bots.Length; i++)
                {
                    if (bots[i].playerName == playerName)
                    {
                        return bots[i];
                    }
                }
            }

            return null;
        }

        // Get player list (humans + bots):
        public PlayerInstance[] GetPlayerList()
        {
            // Get the (human) player list:
            PlayerInstance[] tempP = players;

            // If we have bots, include them to the player list:
            if (hasBots)
            {
                // Merge the human list and bot list into one array:
                PlayerInstance[] p = new PlayerInstance[tempP.Length + bots.Length];
                tempP.CopyTo(p, 0);
                bots.CopyTo(p, tempP.Length);

                // ...then replace the human player list with the full player list array:
                tempP = p;
            }
            return tempP;
        }

        // Generates a PlayerInstance array for bots: 
        PlayerInstance[] GenerateBotPlayerInstances()
        {
            PlayerInstance[] p = bots;

            if (hasBots)
            {
                // If it's the first time generating, player instances should be created first:
                if (bots.Length == 0)
                {
                    //p = new PlayerInstance[bNames.Length];
                    p = new PlayerInstance[Connector.instance.CurrentRoom.botCount];
                    for (int i = 0; i < p.Length; i++)
                    {
                        // Create a cosmetics instance:
                        Cosmetics cosmetics = new Cosmetics(bHats[i]);

                        // Create this bot's player instance (parameters: player ID, bot's name, not ours, is bot, chosen character, no cosmetic item, kills, deaths, otherScore):
                        //p[i] = new PlayerInstance(i + PhotonNetwork.CurrentRoom.MaxPlayers, bNames[i], false, true, bChars[i], cosmetics, Mathf.RoundToInt(bScores[i].x), Mathf.RoundToInt(bScores[i].y), Mathf.RoundToInt(bScores[i].z), null);
                        //p[i] = new PlayerInstance(i + Connector.instance.CurrentRoom.MaxPlayers, bNames[i], false, true, bChars[i], cosmetics, Mathf.RoundToInt(bScores[i].x), Mathf.RoundToInt(bScores[i].y), Mathf.RoundToInt(bScores[i].z), null);
                        p[i] = new PlayerInstance(i + Connector.instance.CurrentRoom.MaxPlayers, bNames[i], false, true, bChars[i], cosmetics, bScoresKills[i], bScoresDeaths[i], bScoresOther[i], null);
                    }
                }
                // ...otherwise, we can just set the stats directly:
                else
                {
                    for (int i = 0; i < p.Length; i++)
                    {
                        //p[i].SetStats(Mathf.RoundToInt(bScores[i].x), Mathf.RoundToInt(bScores[i].y), Mathf.RoundToInt(bScores[i].z), false);
                        p[i].SetStats(bScoresKills[i], bScoresDeaths[i], bScoresOther[i], false);
                    }
                }
            }
            return p;
        }
        public void SetBotPlayerInstance(string botName, int kills, int deaths, int otherScore){

        } 
        // Generates a PlayerInstance array for human players: 
        PlayerInstance[] GeneratePlayerInstances(bool fresh)
        {
            PlayerInstance[] p = players;
            // If it's the first time generating, player instances should be created first:
            if (players.Length == 0 || fresh)
            {
                //p = new PlayerInstance[punPlayersAll.Length];
                p = new PlayerInstance[playersAll.Count];

                for (int i = 0; i < p.Length; i++)
                {
                    // Create a cosmetics instance:
                    //int[] c = (int[])punPlayersAll[i].CustomProperties["cosmetics"];
                    int[] c = (int[])playersAll[i].Cosmetics;
                    Cosmetics cosmetics = new Cosmetics(c[0]);

                    // Then create the player instance:
                    /*p[i] = new PlayerInstance(punPlayersAll[i].ActorNumber, punPlayersAll[i].NickName, punPlayersAll[i].IsLocal, false,
                        (int)punPlayersAll[i].CustomProperties["character"],
                        cosmetics,
                        (int)punPlayersAll[i].CustomProperties["kills"],
                        (int)punPlayersAll[i].CustomProperties["deaths"],
                        (int)punPlayersAll[i].CustomProperties["otherScore"],
                        punPlayersAll[i]);*/
                    p[i] = new PlayerInstance(playersAll[i].ID, playersAll[i].NickName, playersAll[i].IsLocal, false,
                        playersAll[i].Character,
                        cosmetics,
                        (int)playersAll[i].Kills,
                        (int)playersAll[i].Deaths,
                        (int)playersAll[i].OtherScore,
                        playersAll[i]);
                }
            }
            // ...otherwise, we can just set the stats directly:
            else
            {
                for (int i = 0; i < p.Length; i++)
                {
                    //if (i < punPlayersAll.Length - 1)
                    if (i < playersAll.Count - 1)
                    {
                        //p[i].SetStats((int)punPlayersAll[i].CustomProperties["kills"], (int)punPlayersAll[i].CustomProperties["deaths"], (int)punPlayersAll[i].CustomProperties["otherScore"], true);
                        p[i].SetStats(playersAll[i].Kills, playersAll[i].Deaths, playersAll[i].OtherScore, true);
                    }
                }
            }
            return p;
        }

        /// <summary>
        /// Set player instance stats. This is only for human players.
        /// </summary>
        public void SetPlayerInstance(int playerId /*PubNubPlayer forPlayer*/, int kills, int deaths, int otherScore)
        {
            //PlayerInstance p = GetPlayerInstance(forPlayer.NickName);
            PlayerInstance p = GetPlayerInstance(playerId);
            //p.SetStats((int)forPlayer.CustomProperties["kills"], (int)forPlayer.CustomProperties["deaths"], (int)forPlayer.CustomProperties["otherScore"], false);
            if (p != null)
            {
                p.SetStats(kills, deaths, otherScore, false);
            }
        }

        /// <summary>
        /// Add score to a player.
        /// </summary>
        public void AddScore(PlayerInstance player, bool kill, bool death, int others)
        {
            player.AddStats(kill ? 1 : 0, death ? 1 : 0, others, true);  // the PlayerInstance will also automatically handle the uploading
        }

        // Upload an updated bot score list to the room properties:
        public void UpdateBotStats()
        {
            //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
            Dictionary<string, object> h = new Dictionary<string, object>();

            // Get each bot's scores and store them as a Vector3:
            //bScores = new Vector3[bots.Length];
            //bScoresKills = new int[4];
            //bScoresDeaths = new int[4];
            //bScoresOther = new int[4];
            for (int i = 0; i < bots.Length; i++)
            {
                //bScores[i] = new Vector3((int)bots[i].kills, (int)bots[i].deaths, (int)bots[i].otherScore);
                bScoresKills[i] = bots[i].kills;
                bScoresDeaths[i] = bots[i].deaths;
                bScoresOther[i] = bots[i].otherScore;
            }

            //h.Add("botScores", bScores);
            h.Add("botScoresKills", bScoresKills);
            h.Add("botScoresDeaths", bScoresDeaths);
            h.Add("botScoresOther", bScoresOther);
            //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
            pubNubUtilities.PubNubSendRoomProperties(pubnub, h);
        }

        // Others:
        public void DoEmote(int id){
            if (ourPlayer && !ourPlayer.isDead){
                if (pubnub != null)
                    pubNubUtilities.SendEmoji(pubnub, id, ourPlayer.playerInstance.playerID);
            }
        }

        // Calling this will make us disconnect from the current game/room:
        public void QuitMatch()
        {
            //  DCC todo
            //PhotonNetwork.LeaveRoom();
            //DataCarrier.message = "You Quit the Game";
            SceneManager.LoadScene("MainMenu");
            Connector.instance.OnPlayerLeftRoom(Connector.instance.LocalPlayer.UserId);
            Connector.instance.PubNubRemoveRoom(Connector.instance.LocalPlayer.UserId, false);
            if (Connector.instance.CurrentRoom != null && Connector.instance.CurrentRoom.OwnerId == Connector.instance.LocalPlayer.UserId)
            {
                Connector.instance.LeaveRoom();
            }
        }

#region Timer Sync
        /*
        void StartGameTimer()
        {
            //  DCC todo
            //pnSendGameState();
            pubNubUtilities.PubNubSendRoomProperties(pubnub, "gameStartTime", epochTime() );

            //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
            //h["gameStastarrtTime"] = PhotonNetwork.Time;
            //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
        }
    */
        void StartGamePrepare()
        {
            Debug.Log("Init: Calling StartGamePrepare");
            //  DCC todo only call this if we are the master?
            //pnSendGameState("gameStartsIn", (Time.timeAsDouble + preparationTime));
            Dictionary<string, object> props = new Dictionary<string, object>();
            props.Add("gameStartTime", epochTime());
            props.Add("gameStartsIn", (epochTime() + preparationTime));
            props.Add("started", true);
            pubNubUtilities.PubNubSendRoomProperties(pubnub, props);
            //ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
            //h["gameStartsIn"] = PhotonNetwork.Time + preparationTime;
            //PhotonNetwork.CurrentRoom.SetCustomProperties(h);
        }
#endregion

#region Photon calls
        /*
        public override void OnLeftRoom()
        {
            DataCarrier.message = "";
            DataCarrier.LoadScene("MainMenu");
        }
        */

        /*
        public void pnSendGameState(string property, double value)
        {
            if (property.Equals("gameStartsIn"))
            {
                Dictionary<string, object> payload = new Dictionary<string, object>();
                payload["gameStartsIn"] = value;

            }
            else if (property.Equals("gameStartTime"))
            {
                startTime = value;
                CheckTime();
            }
        }
        public void pnSendGameState(string property, bool value)
        {
            if (property.Equals("started"))
            {
                gameStarted = value;
            }
        }
        */

        /*
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            //  DCC update game (room) state via PubNub
            Debug.Log("Game Manager: Room properties update");
            if (propertiesThatChanged.ContainsKey("started")){
                gameStarted = (bool)PhotonNetwork.CurrentRoom.CustomProperties["started"];
            }
            if (propertiesThatChanged.ContainsKey("gameStartsIn"))
            {
                gameStartsIn = (double)propertiesThatChanged["gameStartsIn"];
                startingCountdownStarted = true;
            } 
            // Game timer:
            if (propertiesThatChanged.ContainsKey("gameStartTime"))
            {
                startTime = (double)propertiesThatChanged["gameStartTime"];
                CheckTime();
            }
            // Check if game is over:
            if (propertiesThatChanged.ContainsKey("rankings"))
            {
                playerRankings = (string[])propertiesThatChanged["rankings"];
                isDraw = (bool)propertiesThatChanged["draw"];
            }

            // Update our copy of bot stats if the online version changed:
            if (propertiesThatChanged.ContainsKey("botNames"))
            {
                bNames = (string[])PhotonNetwork.CurrentRoom.CustomProperties["botNames"];
                bots = GenerateBotPlayerInstances();
            }
            if (propertiesThatChanged.ContainsKey("botScores"))
            {
                bScores = (Vector3[])PhotonNetwork.CurrentRoom.CustomProperties["botScores"];
                bots = GenerateBotPlayerInstances();
            }
            if (propertiesThatChanged.ContainsKey("botCharacters"))
            {
                bChars = (int[])PhotonNetwork.CurrentRoom.CustomProperties["botCharacters"];
                bots = GenerateBotPlayerInstances();
            }
        }
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            SetPlayerInstance(targetPlayer);
            ui.UpdateBoards();
        }
        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            // Game timer:
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("gameStartTime"))
            {
                StartGameTimer();
            }
        }
        public override void OnPlayerEnteredRoom(Player newPlayer){
            //punPlayersAll = PhotonNetwork.PlayerList;
            playersAll = Connector.instance.CurrentRoom.PlayerList;

            players = GeneratePlayerInstances(false);
        }
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            //punPlayersAll = PhotonNetwork.PlayerList;
            playersAll = Connector.instance.CurrentRoom.PlayerList;

            players = GeneratePlayerInstances(true);

            // Display a message when someone disconnects/left the game/room:
            ui.DisplayMessage(otherPlayer.NickName + " left the match", UIManager.MessageType.LeftTheGame);

            // Refresh bot list (only if we have bots):
            if (hasBots)
            {
                bots = GenerateBotPlayerInstances();
            }

            // Other refreshes:
            ui.UpdateBoards();
            CheckPlayersLeft();
        }
        public override void OnDisconnected(DisconnectCause cause)
        {
            DataCarrier.message = "You've been disconnected from the game!";
            DataCarrier.LoadScene("MainMenu");
        }
        */

        public void OnDisconnected(bool bWasOwner, string playerName)
        {
            if (bWasOwner)
            {
                DataCarrier.message = "The owner of the game (" + playerName + ") disconnected";
            }
            else
            {
                DataCarrier.message = "" + playerName + " left the game, please start another game";
            }
            SceneManager.LoadScene("MainMenu");
        }

        #endregion

        void OnDrawGizmos()
        {
            // Dead zone:
            if (deadZone)
            {
                Gizmos.color = new Color(1, 0, 0, 0.5f);
                Gizmos.DrawCube(new Vector3(0, deadZoneOffset - 50, 0), new Vector3(1000, 100, 0));
            }
        }

        private int epochTime()
        {
            //  Returns seconds since 2020
            System.DateTime epochStart = new System.DateTime(2020, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
            return cur_time;
        }

        public bool isBot(int playerId)
        {
            return playerId >= players.Length;
        }

        //  DCC todo extend this to the other properties and tidy it
        //private void SubscribeCallbackHandler(object sender, System.EventArgs e)
        private void OnPnMessage(Pubnub pn, PNMessageResult<object> result)
        {
            //SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;
            //if (mea.MessageResult != null && mea.MessageResult.Subscription.Equals(PubNubUtilities.roomStatusChannel))
            if (result != null && result.Channel.Equals(PubNubUtilities.roomStatusChannel))
            {
                //  Messages to update the current room state
                Dictionary<string, object> payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Message.ToString());
                //if (mea.MessageResult.Payload is Dictionary<string, object>)
                if (payload != null)
                {
                    //Dictionary<string, object> payload = (Dictionary<string, object>)mea.MessageResult.Payload;
                    if (payload.ContainsKey("started"))
                    {
                        Debug.Log("Init: Starting game in slave");
                        gameStarted = (bool)payload["started"];
                    }
                    if (payload.ContainsKey("gameStartsIn"))
                    {
                        Debug.Log("Recevied Game Starts In");
                        gameStartsIn = System.Convert.ToSingle(payload["gameStartsIn"]);
                        startingCountdownStarted = true;
                    }
                    if (payload.ContainsKey("gameStartTime"))
                    {
                        startTime = System.Convert.ToSingle(payload["gameStartTime"]);
                        CheckTime();
                        
                    }
                    if (payload.ContainsKey("rankings"))
                    {
                        playerRankings = (payload["rankings"] as Newtonsoft.Json.Linq.JArray).ToObject<string[]>();
                        //playerRankings = (string[])payload["rankings"];
                        isDraw = (bool)payload["draw"];
                    }
                    /*
                    if (payload.ContainsKey("botNames"))
                    {
                        bNames = (string[])payload["botNames"];
                        bots = GenerateBotPlayerInstances();
                        //  DCC todo Instantiate bots in this room if we are not the master
                    }
                    if (payload.ContainsKey("botScores"))
                    {
                        bScores = (Vector3[])payload["botScores"];
                        bots = GenerateBotPlayerInstances();

                    }*/
                    if (payload.ContainsKey("botScoresKills"))
                    {
                        //bScores = (Vector3[])payload["botScores"];
                        //long[] rxBotScoresKills = (long[])payload["botScoresKills"];
                        long[] rxBotScoresKills = (payload["botScoresKills"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                        //bScoresKills = new int[rxBotScoresKills.Length];
                        for (int i = 0; i < rxBotScoresKills.Length; i++)
                        {
                            bScoresKills[i] = System.Convert.ToInt32(rxBotScoresKills[i]);
                        }
                        //long[] rxBotScoresDeaths = (long[])payload["botScoresDeaths"];
                        long[] rxBotScoresDeaths = (payload["botScoresDeaths"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                        //bScoresDeaths = new int[rxBotScoresDeaths.Length];
                        for (int i = 0; i < (rxBotScoresDeaths).Length; i++)
                        {
                            bScoresDeaths[i] = System.Convert.ToInt32(rxBotScoresDeaths[i]);
                        }
                        //long[] rxBotScoresOther = (long[])payload["botScoresOther"];
                        long[] rxBotScoresOther = (payload["botScoresOther"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                        //bScoresOther = new int[rxBotScoresOther.Length];
                        for (int i = 0; i < (rxBotScoresOther).Length; i++)
                        {
                            bScoresOther[i] = System.Convert.ToInt32(rxBotScoresOther[i]);
                        }
                        //bScoresKills = Array.ConvertAll<long, int>((long[])payload["botScoresKills"], Convert.ToInt32);
                        //bScoresDeaths = Array.ConvertAll<long, int>((long[])payload["botScoresDeaths"], Convert.ToInt32);
                        //bScoresOther = Array.ConvertAll<long, int>((long[])payload["botScoresOther"], Convert.ToInt32);
                        bots = GenerateBotPlayerInstances();
                    }
                    if (payload.ContainsKey("botCharacters"))
                    {
                        //  DCC todo this cast is not valid??
                        //bChars = Array.ConvertAll<long, int>((long[])payload["botCharacters"], Convert.ToInt32);
                        //bots = GenerateBotPlayerInstances();
                    }
                    if (payload.ContainsKey("playerStats"))
                    {
                        int kills = 0;
                        int deaths = 0;
                        int otherScore = 0;
                        if (payload.ContainsKey("kills")) kills = System.Convert.ToInt32(payload["kills"]);
                        if (payload.ContainsKey("deaths")) deaths = System.Convert.ToInt32(payload["deaths"]);
                        if (payload.ContainsKey("otherScore")) otherScore = System.Convert.ToInt32(payload["otherScore"]);
                        int playerId = -1;
                        if (payload.ContainsKey("playerId"))
                        {
                            playerId = System.Convert.ToInt32(payload["playerId"]);
                        }
                        else if (payload.ContainsKey("userId"))
                        {
                            string userId = (string)payload["userId"];
                            for (int i = 0; i < Connector.instance.CurrentRoom.PlayerList.Count; i++)
                            {
                                if (userId != null && userId.Equals(Connector.instance.CurrentRoom.PlayerList[i].UserId))
                                {
                                    playerId = Connector.instance.CurrentRoom.PlayerList[i].ID;
                                }
                            }
                        }
                        if (payload.ContainsKey("score"))
                        {
                            //  DCC 123
                            for (int i = 0; i < Connector.instance.CurrentRoom.PlayerList.Count; i++)
                            {
                                if (playerId != -1 && Connector.instance.CurrentRoom.PlayerList[i].ID == playerId)
                                {
                                    Debug.Log("Init: Setting Score for " + Connector.instance.CurrentRoom.PlayerList[i].NickName);
                                    Connector.instance.CurrentRoom.PlayerList[i].SetScore(System.Convert.ToInt32(payload["score"]));
                                }
                            }
                        }
                        SetPlayerInstance(playerId, kills, deaths, otherScore);
                        ui.UpdateBoards();
                    }
                    if (payload.ContainsKey("respawn"))
                    {
                        int playerId = System.Convert.ToInt32(payload["respawn"]);
                        if (playerId != Connector.instance.LocalPlayer.ID)
                        {
                            //  Respawn the remote player asking to be respawned
                            Spawn(playerId, false);
                        }
                    }
                    if (payload.ContainsKey("playerLeft"))
                    {
                        if (!isGameOver)
                        {
                            int playerId = System.Convert.ToInt32(payload["playerLeft"]);
                            string playerUserId = (string)payload["playerUserId"];
                            int wasOwner = System.Convert.ToInt32(payload["wasGameOwner"]);
                            string playerName = (string)payload["playerName"];
                            bool bWasOwner = (wasOwner == 1);
                            //  DCC todo this code is duplicated elsewhere, I can be more efficient here
                            try
                            {
                                Connector.instance.OnPlayerLeftRoom(Connector.instance.LocalPlayer.UserId);
                                Connector.instance.PubNubRemoveRoom(Connector.instance.LocalPlayer.UserId, false);
                                if (Connector.instance.CurrentRoom != null && Connector.instance.CurrentRoom.OwnerId == Connector.instance.LocalPlayer.UserId)
                                {
                                    Connector.instance.LeaveRoom();
                                }
                                //Connector.instance.UserIsOffline(playerUserId);
                                Debug.Log("Calling OnDisconnected from PlayerLeft");
                                OnDisconnected(bWasOwner, playerName);
                            }
                            catch (System.Exception) { }
                        }
                    }
                    if (payload.ContainsKey("playerReady"))
                    {
                        string playerReadyUserId = (string)payload["playerReady"];
                        Debug.Log("Got a message that Player with gm " + playerReadyUserId);
                        for (int i = 0; i < Connector.instance.CurrentRoom.PlayerList.Count; i++)
                        {
                            Debug.Log("Considering " + Connector.instance.CurrentRoom.PlayerList[i].UserId);
                            if (Connector.instance.CurrentRoom.PlayerList[i].UserId.Equals(playerReadyUserId))
                            {
                                Debug.Log("Setting ready to true for nickname " + Connector.instance.CurrentRoom.PlayerList[i].NickName);
                                Connector.instance.CurrentRoom.PlayerList[i].IsReady = true;
                            }
                            else
                            {
                                Debug.Log("Not our Player REady User ID");
                            }
                        }
                    }

                }
            }
        }


        private void OnPnPresence(Pubnub pubnub, PNPresenceEventResult result)
        {
            //else if (mea.PresenceEventResult != null)
            //{
            //  Detect when a remote player has unintentionally left the game, through presence timeout (e.g. connection lost)
            //if (mea.PresenceEventResult.Event.Equals("leave") || mea.PresenceEventResult.Event.Equals("timeout"))
            if (result.Channel.Equals(PubNubUtilities.itemChannel))
            {
                if (result.Event.Equals("leave") || result.Event.Equals("timeout"))
                {
                    //  The specified user has left, remove any room they created from the rooms array
                    //  DCC uuu
                    if (true || !isGameOver)
                    {
                        Debug.Log("Game: Presence: User has Left");
                        bool bWasOwner = false;
                        string playerName = "Unknown";
                        for (int i = 0; i < Connector.instance.CurrentRoom.PlayerList.Count; i++)
                        {
                            //if (Connector.instance.CurrentRoom.PlayerList[i].UserId.Equals(mea.PresenceEventResult.UUID))
                            if (Connector.instance.CurrentRoom.PlayerList[i].UserId.Equals(result.Uuid))
                            {
                                //  Found our player in the player list
                                bWasOwner = Connector.instance.CurrentRoom.PlayerList[i].IsMasterClient;
                                playerName = Connector.instance.CurrentRoom.PlayerList[i].NickName;
                            }
                        }
                        //Connector.instance.OnPlayerLeftRoom(Connector.instance.LocalPlayer.UserId);
                        Connector.instance.PubNubRemoveRoom(Connector.instance.LocalPlayer.UserId, false);
                        if (bWasOwner)
                        {
                            //  Clean up after the owner
                            //Connector.instance.PubNubRemoveRoom(mea.PresenceEventResult.UUID, true);
                            Connector.instance.PubNubRemoveRoom(result.Uuid, true);
                        }
                        //  DCC uuu
                        Connector.instance.LeaveRoom();
                        //Connector.instance.UserIsOffline(playerUserId);
                        Debug.Log("Calling OnDisconnected from presence event");
                        OnDisconnected(bWasOwner, playerName);
                    }
                }
                //else if (mea.PresenceEventResult.Event.Equals("join"))
                else if (result.Event.Equals("join"))
                {
                    //  The specified user has joined.  If they have created a room then add it to our room list
                    //UserIsOnlineOrStateChange(mea.PresenceEventResult.UUID);
                    Debug.Log("Game: Presence: User has Joined");
                }
                //}
            }
        }

    }


    // Player sorter helper:
    public class PlayerSorter : IComparer
    {
        int IComparer.Compare(object a, object b)
        {
            int p1 = (((PlayerInstance)a).kills) + ((PlayerInstance)a).otherScore;
            int p2 = (((PlayerInstance)b).kills) + ((PlayerInstance)b).otherScore;
            if (p1 == p2)
            {
                return 0;
            }
            else
            {
                GameManager.isDraw = false;  // game isn't a draw if a player as a different score

                if (p1 > p2)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }
    }
}