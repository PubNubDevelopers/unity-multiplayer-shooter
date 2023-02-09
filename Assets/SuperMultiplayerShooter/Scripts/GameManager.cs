using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

namespace Visyde
{
    /// <summary>
    /// Game Manager
    /// - Simply the game manager. The one that controls the game itself. Provides game settings and serves as the central component by
    /// connecting other components to communicate with each other.
    /// </summary>

    public class GameManager : MonoBehaviourPunCallbacks, IInRoomCallbacks, IConnectionCallbacks
    {
        public static GameManager instance;

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
        public bool showEnemyHealth = false;
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
            get { return (float)(gameStartsIn - PhotonNetwork.Time); }
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
        Vector3[] bScores = new Vector3[0];		// Bot scores (x = kills, y = deaths, z = other scores)
        int[] bChars = new int[0];				// Bot's chosen character's index
        int[] bHats = new int[0];               // Bot hats (cosmetics)

        // Used for time syncronization:
        [System.NonSerialized] public double startTime, elapsedTime, remainingTime, gameStartsIn;
        bool startingCountdownStarted, doneGameStart;

        // For respawning:
        double deathTime;

        // Others:
        Player[] punPlayersAll;

        void Awake()
        {
            instance = this;

            // Prepare player instance arrays:
            bots = new PlayerInstance[0];
            players = new PlayerInstance[0];

            // Cache current player list:
            punPlayersAll = PhotonNetwork.PlayerList;

            // Do we have bots in the game? Download bot stats if we have:
            hasBots = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("botNames");
            if (hasBots)
            {
                // Download the stats:
                bNames = (string[])PhotonNetwork.CurrentRoom.CustomProperties["botNames"];
                bScores = (Vector3[])PhotonNetwork.CurrentRoom.CustomProperties["botScores"];
                bChars = (int[])PhotonNetwork.CurrentRoom.CustomProperties["botCharacters"];
                // And their "chosen" cosmetics:
                bHats = (int[])PhotonNetwork.CurrentRoom.CustomProperties["botHats"];

                // ...then generate the player instances:
                bots = GenerateBotPlayerInstances();
            }
            // Generate human player instances:
            players = GeneratePlayerInstances(true);

            // Don't allow the device to sleep while in game:
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
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
            chosenMap = (int)PhotonNetwork.CurrentRoom.CustomProperties["map"];
            for (int i = 0; i < maps.Length; i++)
            {
                maps[i].gameObject.SetActive(chosenMap == i);
            }

            // After loading the scene, we (the local player) are now ready for the game:
            Ready();

            // Start checking if all players are ready:
            InvokeRepeating("CheckIfAllPlayersReady", 1, 0.5f);
        }

        void CheckIfAllPlayersReady()
        {
            if (!isGameOver)
            {
                // Check if players are ready:
                if (!startingCountdownStarted)
                {
                    bool allPlayersReady = true;

                    for (int i = 0; i < punPlayersAll.Length; i++)
                    {
                        // If a player hasn't yet finished loading, don't start:
                        if (punPlayersAll[i].GetScore() == -1)
                        {
                            allPlayersReady = false;
                        }
                    }
                    // Start the preparation countdown when all players are done loading:
                    if (allPlayersReady && PhotonNetwork.IsMasterClient)
                    {
                        StartGamePrepare();
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!isGameOver)
            {
                // Start the game when preparation countdown is finished:
                if (startingCountdownStarted)
                {
                    if (elapsedTime >= (gameStartsIn - startTime) && !gameStarted && !doneGameStart)
                    {
                        // GAME HAS STARTED!
                        if (PhotonNetwork.IsMasterClient)
                        {
                            doneGameStart = true;
                            ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                            h["started"] = true;
                            PhotonNetwork.CurrentRoom.SetCustomProperties(h);
                            StartGameTimer();
                        }

                        CancelInvoke("CheckIfAllPlayersReady");
                    }
                }

                // Respawning:
                if (dead)
                {
                    if (deathTime == 0)
                    {
                        deathTime = PhotonNetwork.Time + respawnTime;
                    }
                    curRespawnTime = (float)(deathTime - PhotonNetwork.Time);
                    if (curRespawnTime <= 0)
                    {
                        dead = false;
                        deathTime = 0;
                        Spawn();
                    }
                }

                // Calculating the elapsed and remaining time:
                CheckTime();

                // Finish game when elapsed time is greater than or equal to game length:
                if (elapsedTime + 1 >= gameLength && gameStarted && !isGameOver)
                {
                    // Post the player rankings:
                    if (PhotonNetwork.IsMasterClient)
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
                        ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                        h.Add("rankings", p);
                        h.Add("draw", isDraw);
                        PhotonNetwork.CurrentRoom.SetCustomProperties(h);

                        // Hide room from lobby:
                        PhotonNetwork.CurrentRoom.IsVisible = false;
                    }
                }

                // Check if game is over:
                if (playerRankings.Length > 0){
                    isGameOver = true;
                }
            }
        }

        void CheckTime(){
            elapsedTime = (PhotonNetwork.Time - startTime);
            remainingTime = gameLength - (elapsedTime % gameLength);
        }

        // Called when we enter the game world:
        void Ready()
        {
            // Set our score to 0 on start (this is not the player's actual score, this is only used to determine if we're ready or not, 0 = ready, -1 = not):
            PhotonNetwork.LocalPlayer.SetScore(0);

            // Spawn our player:
            Spawn();

            // ... and the bots if we have some and if we are the master client:
            if (hasBots && PhotonNetwork.IsMasterClient)
            {
                for (int i = 0; i < bots.Length; i++)
                {
                    SpawnBot(i + PhotonNetwork.CurrentRoom.MaxPlayers);    // parameter is the bot ID
                }
            }
        }

        /// <summary>
        /// Spawns the player.
        /// </summary>
        public void Spawn()
        {
            Transform spawnPoint = maps[chosenMap].playerSpawnPoints[UnityEngine.Random.Range(0, maps[chosenMap].playerSpawnPoints.Count)];
            // There are 2 values in the player's instatiation data. The first one is reserved and only used if the player is a bot, while the 
            // second is for the cosmetics (in this case we only have 1 which is for the chosen hat, but you can add as many as your game needs):
            ourPlayer = PhotonNetwork.Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity, 0, new object[] { 0, DataCarrier.chosenHat }).GetComponent<PlayerController>();
        }

        /// <summary>
        /// Spawns a bot (only works on master client).
        /// </summary>
        public void SpawnBot(int bot)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Transform spawnPoint = maps[chosenMap].playerSpawnPoints[UnityEngine.Random.Range(0, maps[chosenMap].playerSpawnPoints.Count)];
            // Instantiate the bot. Bots are assigned with random hats (second value of the instantiation data):
            PlayerController botP = PhotonNetwork.InstantiateSceneObject(playerPrefab, spawnPoint.position, Quaternion.identity, 0, new object[] { bot }).GetComponent<PlayerController>();
        }

        public void SomeoneDied(int dying, int killer)
        {
            // Add scores (master client only)
            if (PhotonNetwork.IsMasterClient)
            {
                // Kill score to killer:
                if (killer != dying) AddScore(GetPlayerInstance(killer), true, false, 0);

                // ... and death to dying, and score deduction if suicide:
                AddScore(GetPlayerInstance(dying), false, true, killer == dying? -1 : 0);
            }

            // Display kill feed.
            ui.SomeoneKilledSomeone(GetPlayerInstance(dying), GetPlayerInstance(killer));
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

        /// <summary>
        /// Checks how many players are still in the game. If there's only 1 left, the game will end.
        /// </summary>
        public void CheckPlayersLeft()
        {
            if (GetPlayerList().Length <= 1 && PhotonNetwork.CurrentRoom.MaxPlayers > 1)
            {
                print("GAME OVER!");
                ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                double skip = 0;
                h["gameStartTime"] = skip;
                PhotonNetwork.CurrentRoom.SetCustomProperties(h);
            }
        }

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
                    p = new PlayerInstance[bNames.Length];
                    for (int i = 0; i < p.Length; i++)
                    {
                        // Create a cosmetics instance:
                        Cosmetics cosmetics = new Cosmetics(bHats[i]);

                        // Create this bot's player instance (parameters: player ID, bot's name, not ours, is bot, chosen character, no cosmetic item, kills, deaths, otherScore):
                        p[i] = new PlayerInstance(i + PhotonNetwork.CurrentRoom.MaxPlayers, bNames[i], false, true, bChars[i], cosmetics, Mathf.RoundToInt(bScores[i].x), Mathf.RoundToInt(bScores[i].y), Mathf.RoundToInt(bScores[i].z), null);
                    }
                }
                // ...otherwise, we can just set the stats directly:
                else
                {
                    for (int i = 0; i < p.Length; i++)
                    {
                        p[i].SetStats(Mathf.RoundToInt(bScores[i].x), Mathf.RoundToInt(bScores[i].y), Mathf.RoundToInt(bScores[i].z), false);
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
                p = new PlayerInstance[punPlayersAll.Length];

                for (int i = 0; i < p.Length; i++)
                {
                    // Create a cosmetics instance:
                    int[] c = (int[])punPlayersAll[i].CustomProperties["cosmetics"];
                    Cosmetics cosmetics = new Cosmetics(c[0]);

                    // Then create the player instance:
                    p[i] = new PlayerInstance(punPlayersAll[i].ActorNumber, punPlayersAll[i].NickName, punPlayersAll[i].IsLocal, false, 
                        (int)punPlayersAll[i].CustomProperties["character"], 
                        cosmetics,
                        (int)punPlayersAll[i].CustomProperties["kills"],
                        (int)punPlayersAll[i].CustomProperties["deaths"],
                        (int)punPlayersAll[i].CustomProperties["otherScore"],
                        punPlayersAll[i]);
                }
            }
            // ...otherwise, we can just set the stats directly:
            else
            {
                for (int i = 0; i < p.Length; i++)
                {
                    if (i < punPlayersAll.Length - 1)
                    {
                        p[i].SetStats((int)punPlayersAll[i].CustomProperties["kills"], (int)punPlayersAll[i].CustomProperties["deaths"], (int)punPlayersAll[i].CustomProperties["otherScore"], true);
                    }
                }
            }
            return p;
        }

        /// <summary>
        /// Set player instance stats. This is only for human players.
        /// </summary>
        public void SetPlayerInstance(Player forPlayer)
        {
            PlayerInstance p = GetPlayerInstance(forPlayer.NickName);
            p.SetStats((int)forPlayer.CustomProperties["kills"], (int)forPlayer.CustomProperties["deaths"], (int)forPlayer.CustomProperties["otherScore"], false);
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
            ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();

            // Get each bot's scores and store them as a Vector3:
            bScores = new Vector3[bots.Length];
            for (int i = 0; i < bots.Length; i++)
            {
                bScores[i] = new Vector3((int)bots[i].kills, (int)bots[i].deaths, (int)bots[i].otherScore);
            }

            h.Add("botScores", bScores);
            PhotonNetwork.CurrentRoom.SetCustomProperties(h);
        }

        // Others:
        public void DoEmote(int id){
            if (ourPlayer && !ourPlayer.isDead){
                ourPlayer.photonView.RPC("Emote", RpcTarget.All, id);
            }
        }

        // Calling this will make us disconnect from the current game/room:
        public void QuitMatch()
        {
            PhotonNetwork.LeaveRoom();
        }

#region Timer Sync
        void StartGameTimer()
        {
            ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
            h["gameStartTime"] = PhotonNetwork.Time;
            PhotonNetwork.CurrentRoom.SetCustomProperties(h);
        }
        void StartGamePrepare()
        {
            ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
            h["gameStartsIn"] = PhotonNetwork.Time + preparationTime;
            PhotonNetwork.CurrentRoom.SetCustomProperties(h);
        }
#endregion

#region Photon calls
        public override void OnLeftRoom()
        {
            DataCarrier.message = "";
            DataCarrier.LoadScene("MainMenu");
        }
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
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
            punPlayersAll = PhotonNetwork.PlayerList;

            players = GeneratePlayerInstances(false);
        }
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            punPlayersAll = PhotonNetwork.PlayerList;

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