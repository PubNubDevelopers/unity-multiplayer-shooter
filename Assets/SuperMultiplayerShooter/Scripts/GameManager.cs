using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PubnubApi;
using PubnubApi.Unity;
using PubNubUnityShowcase;
using Newtonsoft.Json;

namespace Visyde
{
    /// <summary>
    /// Game Manager
    /// - Controls the game itself. Provides game settings and serves as the central component by
    /// connecting other components to communicate with each other.
    /// </summary>

    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        //  PubNub Properties
        public Pubnub pubnub = null;
        private PubNubUtilities pubNubUtilities = new PubNubUtilities();
        private SubscribeCallbackListener listener = new SubscribeCallbackListener();
        public readonly Dictionary<string, GameObject> ResourceCache = new Dictionary<string, GameObject>();
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
            get { return (float)(gameStartsIn - epochTime()); }
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

        [System.NonSerialized] public bool gameStarted = false;                         // if the game has started already
        [System.NonSerialized] public string[] playerRankings = new string[0];          // used at the end of the game
        public PlayerInstance[] bots;                                                   // Player instances of bots
        public PlayerInstance[] players;                                                // Player instances of human players (ours is first)
        [System.NonSerialized] public bool isGameOver;                                  // is the game over?
        [System.NonSerialized] public PlayerController ourPlayer;                       // our player's player (the player object itself)
        public List<PlayerController> playerControllers = new List<PlayerController>();	// list of all player controllers currently in the scene
        [System.NonSerialized] public bool dead;
        [System.NonSerialized] public float curRespawnTime;
        public static bool isDraw;                                                      // is game draw?
        bool hasBots;                                                                   // does the game have bots?

        // Local copy of bot stats (so we don't periodically have to download them when we need them):
        string[] bNames = new string[0];		// Bot names
        int[] bScoresKills = new int[PNRoomInfo.MAX_BOTS];
        int[] bScoresDeaths = new int[PNRoomInfo.MAX_BOTS];
        int[] bScoresOther = new int[PNRoomInfo.MAX_BOTS];
        int[] bChars = new int[0];				// Bot's chosen character's index
        int[] bHats = new int[0];               // Bot hats (cosmetics)

        // Used for time syncronization:
        [System.NonSerialized] public double startTime, elapsedTime, remainingTime, gameStartsIn;
        private bool startingCountdownStarted = false;  //  Trigger for Waiting for Players
        private bool doneGameStart = false;

        // For respawning:
        double deathTime;

        // Others:
        List<PNPlayer> playersAll;
        public bool spawnComplete = false;

        void Awake()
        {
            instance = this;

            // Prepare player instance arrays:
            bots = new PlayerInstance[0];
            players = new PlayerInstance[0];

            // Cache current player list:
            playersAll = Connector.instance.CurrentRoom.PlayerList;

            // Do we have bots in the game? Download bot stats if we have:
            hasBots = ((Connector.instance.CurrentRoom.Bots != null && Connector.instance.CurrentRoom.Bots.Count > 0) ||
                (Connector.instance.CurrentRoom.BotObjects != null && Connector.instance.CurrentRoom.BotObjects.Length > 0));
            if (hasBots)
            {
                bNames = Connector.instance.CurrentRoom.bNames;
                bChars = Connector.instance.CurrentRoom.bChars;
                bHats = Connector.instance.CurrentRoom.bHats;
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
            channels.Add(PubNubUtilities.chanRoomStatus);  
            channels.Add(PubNubUtilities.ToGameChannel(PubNubUtilities.chanItems));
            channels.Add(PubNubUtilities.ToGameChannel(PubNubUtilities.chanItems) + "-pnpres");  //  We are only interested in presence events for this channel
            //  Every player will send their updates on a unique channel, so subscribe to those
            foreach (PlayerInstance playerInstance in players)
            {
                if (!playerInstance.IsMine)
                {
                    channels.Add(PubNubUtilities.ToGameChannel(PubNubUtilities.chanPrefixPlayerActions + playerInstance.playerID));
                    channels.Add(PubNubUtilities.ToGameChannel(PubNubUtilities.chanPrefixPlayerPos + playerInstance.playerID));
                    channels.Add(PubNubUtilities.ToGameChannel(PubNubUtilities.chanPrefixPlayerCursor + playerInstance.playerID));
                }
            }
            //  The master client controls the bots, so if we are not the master, register to receive bot updates
            foreach (PlayerInstance bot in bots)
            {
                if (!Connector.instance.isMasterClient)
                {
                    channels.Add(PubNubUtilities.ToGameChannel(PubNubUtilities.chanPrefixPlayerActions + bot.playerID));
                    channels.Add(PubNubUtilities.ToGameChannel(PubNubUtilities.chanPrefixPlayerPos + bot.playerID));
                    channels.Add(PubNubUtilities.ToGameChannel(PubNubUtilities.chanPrefixPlayerCursor + bot.playerID));
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

        void Start()
        {
            // Setups:
            isDraw = false;
            gameCam.gm = this;

            // Determine the type of controls:
            controlsManager.mobileControls = useMobileControls;

            // Get the chosen map then enable it:
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
            //  Unsubscribe only for this PubNub instance (which is unique per game)
            pubnub.UnsubscribeAll();    
            listener.onMessage -= OnPnMessage;
            listener.onPresence -= OnPnPresence;
        }

        //  The master of the game will wait for all the players to report that they are ready before
        //  telling them all to start the game simultaneously
        void CheckIfAllPlayersReady()
        {
            if (!isGameOver)
            {
                // Check if players are ready:
                if (!startingCountdownStarted)
                {
                    bool allPlayersReady = true;

                    for (int i = 0; i < Connector.instance.CurrentRoom.PlayerList.Count; i++)
                    {
                        if (!Connector.instance.CurrentRoom.PlayerList[i].IsReady)
                        {
                            allPlayersReady = false;
                            break;
                        }
                    }
                    if (allPlayersReady)
                    {
                        //  All players are now ready, tell them to start
                        StartGamePrepare();
                    }
                }
            }
        }

        void Update()
        {
            if (!isGameOver)
            {
                // Start the game when preparation countdown is finished:
                if (startingCountdownStarted)
                {
                    if (elapsedTime >= (gameStartsIn - startTime) && !gameStarted && !doneGameStart)
                    {
                        //  The Game has started
                        if (Connector.instance.isMasterClient)
                        {
                            doneGameStart = true;
                            gameStarted = true;
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
                        deathTime = epochTime() + respawnTime;
                    }
                    curRespawnTime = (float)(deathTime - epochTime());
                    if (curRespawnTime <= 0)
                    {
                        dead = false;
                        deathTime = 0;
                        Spawn(Connector.instance.GetMyId(), true);
                        //  Tell everyone else in the Game to respawn me
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
                    if (Connector.instance.isMasterClient)
                    {
                        // Get player list by order based on scores and also set "draw" to true (the player sorter will set this to false if not draw):
                        isDraw = true;

                        // List of player names for the rankings:
                        PlayerInstance[] ps = SortPlayersByScore();
                        string[] p = new string[ps.Length];
                        for (int i = 0; i < ps.Length; i++)
                        {
                            p[i] = ps[i].PlayerName;
                        }

                        isDraw = ps.Length > 1 && isDraw;

                        // Mark as game over:
                        Dictionary<string, object> h = new Dictionary<string, object>();
                        h.Add("rankings", p);
                        h.Add("draw", isDraw);
                        pubNubUtilities.PubNubSendRoomProperties(pubnub, h);

                        // Hide room from lobby:
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
            elapsedTime = (epochTime() - startTime);
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
                    Spawn(Connector.instance.CurrentRoom.PlayerList[i].ID, false);
                }
            }

            // Spawn the bots if there are any in the game
            if (hasBots)
            {
                for (int i = 0; i < bots.Length; i++)
                {
                    if (Connector.instance.isMasterClient)
                    {
                        //  Spawn a bot instance which we will own and control
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
                //  For dramatic effect, wait a second before reporting we are ready
                Invoke("ReportReady", 1.0f);
            }
        }

        //  Spawns the human player.
        public void Spawn(int playerId, bool isMine)
        {
            Transform spawnPoint = maps[chosenMap].playerSpawnPoints[UnityEngine.Random.Range(0, maps[chosenMap].playerSpawnPoints.Count)];
            if (Connector.instance.CurrentRoom != null)
            {
                GameObject tempOurPlayer = InstantiatePlayer(playerPrefab, spawnPoint.position, Quaternion.identity, false, playerId, -1, isMine, new object[] { 0, DataCarrier.chosenHat });
                if (isMine)
                {
                    ourPlayer = tempOurPlayer.GetComponent<PlayerController>();
                }
            }
        }

        //  Spawns a bot (only works on master client).
        public PlayerController SpawnBot(int bot, int ownerId, bool isMine)
        {
            Transform spawnPoint = maps[chosenMap].playerSpawnPoints[UnityEngine.Random.Range(0, maps[chosenMap].playerSpawnPoints.Count)];
            // Instantiate the bot. Bots are assigned with random hats 
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

        //  Create the player character in the game (either human or bot)
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
            PubNubPlayerProps properties = instance.GetComponent<PubNubPlayerProps>() as PubNubPlayerProps;
            if (properties == null)
            {
                Debug.LogError("Player must have a PubNubPlayer associated with the Prefab");
            }
            else
            {
                properties.IsBot = isBot;
                properties.OwnerId = ownerId;
                properties.IsMine = isMine;
                properties.BotId = botId;
                properties.IsPreview = false;
            }
            instance.SetActive(true);
            return instance;
        }

        public void SomeoneDied(int dying, int killer)
        {
            spawnComplete = true;

            // Add scores (master client only)
            if (Connector.instance.isMasterClient)
            {
                // Kill score to killer:
                if (killer != dying) AddScore(GetPlayerInstance(killer), true, false, 0);

                // and death to dying, and score deduction if suicide:
                AddScore(GetPlayerInstance(dying), false, true, killer == dying? -1 : 0);
            }

            // Display kill feed.
            ui.SomeoneKilledSomeone(GetPlayerInstance(dying), GetPlayerInstance(killer));
        }

        private void ReportReady()
        {
            //  Notify the master instance that we are ready
            Dictionary<string, object> props = new Dictionary<string, object>();
            props.Add("playerReady", Connector.instance.LocalPlayer.UserId);
            pubNubUtilities.PubNubSendRoomProperties(pubnub, props);
        }

        //  Returns the PlayerController of the player with the given name.
        public PlayerController GetPlayerControllerOfPlayer(string name)
        {
            for (int i = 0; i < playerControllers.Count; i++)
            {
                // Check if current item matches a player:
                if (string.CompareOrdinal(playerControllers[i].playerInstance.PlayerName, name) == 0)
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

        //  Player leaderboard sorting:
        IComparer SortPlayers()
        {
            return (IComparer)new PlayerSorter();
        }
        public PlayerInstance[] SortPlayersByScore()
        {
            // Get the full player list:
            PlayerInstance[] allPlayers = GetPlayerList();

            // then sort them out based on scores:
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

            Debug.LogWarning("Msg: Unable to find player instance for id " + playerID);
            return null;
        }

        public PlayerInstance GetPlayerInstance(string playerName)
        {
            // Look in human player list:
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].PlayerName == playerName)
                {
                    return players[i];
                }
            }
            // Look in bots:
            if (hasBots){
                for (int i = 0; i < bots.Length; i++)
                {
                    if (bots[i].PlayerName == playerName)
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

                // then replace the human player list with the full player list array:
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
                    p = new PlayerInstance[Connector.instance.CurrentRoom.BotCount];
                    for (int i = 0; i < p.Length; i++)
                    {
                        // Create a cosmetics instance:
                        Cosmetics cosmetics = new Cosmetics(bHats[i]);

                        // Create this bot's player instance (parameters: player ID, bot's name, not ours, is bot, chosen character, no cosmetic item, kills, deaths, otherScore):
                        p[i] = new PlayerInstance(i + Connector.instance.CurrentRoom.MaxPlayers, bNames[i], false, true, bChars[i], cosmetics, bScoresKills[i], bScoresDeaths[i], bScoresOther[i], null);
                    }
                }
                // otherwise, we can just set the stats directly:
                else
                {
                    for (int i = 0; i < p.Length; i++)
                    {
                        p[i].SetStats(bScoresKills[i], bScoresDeaths[i], bScoresOther[i], false);
                    }
                }
            }
            return p;
        }

        // Generates a PlayerInstance array for human players: 
        PlayerInstance[] GeneratePlayerInstances(bool fresh)
        {
            PlayerInstance[] p = players;
            // If it's the first time generating, player instances should be created first:
            if (players.Length == 0 || fresh)
            {
                p = new PlayerInstance[playersAll.Count];

                for (int i = 0; i < p.Length; i++)
                {
                    // Create a cosmetics instance:
                    int[] c = (int[])playersAll[i].Cosmetics;
                    Cosmetics cosmetics = new Cosmetics(c[0]);

                    // Then create the player instance:
                    p[i] = new PlayerInstance(playersAll[i].ID, playersAll[i].NickName, playersAll[i].IsLocal, false,
                        playersAll[i].Character,
                        cosmetics,
                        (int)playersAll[i].Kills,
                        (int)playersAll[i].Deaths,
                        (int)playersAll[i].OtherScore,
                        playersAll[i]);
                }
            }
            // otherwise, we can just set the stats directly:
            else
            {
                for (int i = 0; i < p.Length; i++)
                {
                    if (i < playersAll.Count - 1)
                    {
                        p[i].SetStats(playersAll[i].Kills, playersAll[i].Deaths, playersAll[i].OtherScore, true);
                    }
                }
            }
            return p;
        }

        //  Set player instance stats. This is only for human players.
        public void SetPlayerInstance(int playerId, int kills, int deaths, int otherScore)
        {
            PlayerInstance p = GetPlayerInstance(playerId);
            if (p != null)
            {
                p.SetStats(kills, deaths, otherScore, false);
            }
        }

        //  Add score to a player.
        public void AddScore(PlayerInstance player, bool kill, bool death, int others)
        {
            player.AddStats(kill ? 1 : 0, death ? 1 : 0, others, true);  // the PlayerInstance will also automatically handle the uploading
        }

        // Upload an updated bot score list to the room properties:
        public void UpdateBotStats()
        {
            Dictionary<string, object> botStats = new Dictionary<string, object>();

            // Get each bot's scores and store them as a Vector3:
            for (int i = 0; i < bots.Length; i++)
            {
                bScoresKills[i] = bots[i].Kills;
                bScoresDeaths[i] = bots[i].Deaths;
                bScoresOther[i] = bots[i].OtherScore;
            }

            botStats.Add("botScoresKills", bScoresKills);
            botStats.Add("botScoresDeaths", bScoresDeaths);
            botStats.Add("botScoresOther", bScoresOther);
            pubNubUtilities.PubNubSendRoomProperties(pubnub, botStats);
        }

        public void DoEmote(int id){
            if (ourPlayer && !ourPlayer.isDead){
                if (pubnub != null)
                    pubNubUtilities.SendEmoji(pubnub, id, ourPlayer.playerInstance.playerID);
            }
        }

        // Calling this will make us disconnect from the current game/room:
        public void QuitMatch()
        {
            SceneManager.LoadScene("MainMenu");
            Connector.instance.OnPlayerLeftRoom(Connector.instance.LocalPlayer.UserId);
            Connector.instance.PubNubRemoveRoom(Connector.instance.LocalPlayer.UserId, false);
            if (Connector.instance.CurrentRoom != null && Connector.instance.CurrentRoom.OwnerId == Connector.instance.LocalPlayer.UserId)
            {
                Connector.instance.LeaveRoom();
            }
        }

        void StartGamePrepare()
        {
            Dictionary<string, object> startGameProps = new Dictionary<string, object>();
            startGameProps.Add("gameStartTime", epochTime());
            startGameProps.Add("gameStartsIn", (epochTime() + preparationTime));
            startGameProps.Add("started", true);
            pubNubUtilities.PubNubSendRoomProperties(pubnub, startGameProps);
        }

        public void OnDisconnected(bool bWasOwner, string playerName)
        {
            if (!isGameOver)
            {
                if (bWasOwner)
                {
                    DataCarrier.message = "The owner of the game (" + playerName + ") disconnected";
                }
                else
                {
                    DataCarrier.message = "" + playerName + " left the game, please start another game";
                }
            }
            SceneManager.LoadScene("MainMenu");
        }

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

        //  Handler for PubNub Messages
        private void OnPnMessage(Pubnub pn, PNMessageResult<object> result)
        {
            if (result != null && result.Channel.Equals(PubNubUtilities.chanRoomStatus))
            {
                //  Messages to update the current room state
                Dictionary<string, object> payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Message.ToString());
                if (payload != null)
                {
                    if (payload.ContainsKey("started"))
                    {
                        gameStarted = (bool)payload["started"];
                    }
                    if (payload.ContainsKey("gameStartsIn"))
                    {
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
                        isDraw = (bool)payload["draw"];
                    }
                    if (payload.ContainsKey("botScoresKills"))
                    {
                        long[] rxBotScoresKills = (payload["botScoresKills"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                        for (int i = 0; i < rxBotScoresKills.Length; i++)
                        {
                            bScoresKills[i] = System.Convert.ToInt32(rxBotScoresKills[i]);
                        }
                        long[] rxBotScoresDeaths = (payload["botScoresDeaths"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                        for (int i = 0; i < (rxBotScoresDeaths).Length; i++)
                        {
                            bScoresDeaths[i] = System.Convert.ToInt32(rxBotScoresDeaths[i]);
                        }
                        long[] rxBotScoresOther = (payload["botScoresOther"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                        for (int i = 0; i < (rxBotScoresOther).Length; i++)
                        {
                            bScoresOther[i] = System.Convert.ToInt32(rxBotScoresOther[i]);
                        }
                        bots = GenerateBotPlayerInstances();
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
                            try
                            {
                                Connector.instance.OnPlayerLeftRoom(Connector.instance.LocalPlayer.UserId);
                                Connector.instance.PubNubRemoveRoom(Connector.instance.LocalPlayer.UserId, false);
                                if (Connector.instance.CurrentRoom != null && Connector.instance.CurrentRoom.OwnerId == Connector.instance.LocalPlayer.UserId)
                                {
                                    Connector.instance.LeaveRoom();
                                }
                                OnDisconnected(bWasOwner, playerName);
                            }
                            catch (System.Exception) { }
                        }
                    }
                    if (payload.ContainsKey("playerReady"))
                    {
                        string playerReadyUserId = (string)payload["playerReady"];
                        for (int i = 0; i < Connector.instance.CurrentRoom.PlayerList.Count; i++)
                        {
                            if (Connector.instance.CurrentRoom.PlayerList[i].UserId.Equals(playerReadyUserId))
                            {
                                Connector.instance.CurrentRoom.PlayerList[i].IsReady = true;
                            }
                        }
                    }
                }
            }
        }

        //  PubNub Presence event handler
        private void OnPnPresence(Pubnub pubnub, PNPresenceEventResult result)
        {
            if (result.Channel.Equals(PubNubUtilities.ToGameChannel(PubNubUtilities.chanItems)))
            {
                if (result.Event.Equals("leave") || result.Event.Equals("timeout"))
                {
                    //  The specified user has left, remove any room they created from the rooms array
                    bool bWasOwner = false;
                    string playerName = "Unknown";
                    for (int i = 0; i < Connector.instance.CurrentRoom.PlayerList.Count; i++)
                    {
                        if (Connector.instance.CurrentRoom.PlayerList[i].UserId.Equals(result.Uuid))
                        {
                            //  Found our player in the player list
                            bWasOwner = Connector.instance.CurrentRoom.PlayerList[i].IsMasterClient;
                            playerName = Connector.instance.CurrentRoom.PlayerList[i].NickName;
                        }
                    }
                    Connector.instance.PubNubRemoveRoom(Connector.instance.LocalPlayer.UserId, false);
                    if (bWasOwner)
                    {
                        //  Clean up after the owner
                        Connector.instance.PubNubRemoveRoom(result.Uuid, true);
                    }
                    Connector.instance.LeaveRoom();
                    OnDisconnected(bWasOwner, playerName);
                }
                else if (result.Event.Equals("join"))
                {
                    //  The specified user has joined.
                    //  No action
                }
            }
        }
    }

    // Player sorter helper:
    public class PlayerSorter : IComparer
    {
        int IComparer.Compare(object a, object b)
        {
            int p1 = (((PlayerInstance)a).Kills) + ((PlayerInstance)a).OtherScore;
            int p2 = (((PlayerInstance)b).Kills) + ((PlayerInstance)b).OtherScore;
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