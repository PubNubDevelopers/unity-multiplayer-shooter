using Photon.Realtime;

namespace Visyde
{
    /// <summary>
    /// Player Instance
    /// - Contains stats and other info about a player in-game
    /// - Every human and bot player has their own player instance
    /// </summary>
    
[System.Serializable]
    public class PlayerInstance
    {
        // Info:
        public int playerID;                            // unique player ID (bots have their own player ID's different from the host's/MasterClient's)
        public string playerName;
        public int character;
        public Player punPlayer { get; protected set; }

        // Cosmetics:
        public Cosmetics cosmeticItems;

        public bool isBot { get; protected set; }       // is this player instance owned by a bot?
        public bool isMine { get; protected set; }      // is this player instance ours?

        // Stats:
        public int kills;
        public int deaths;
        public int otherScore;                          

        public PlayerInstance(int id, string name, bool isMine, bool bot, int character, Cosmetics cosmeticItems, Player thePlayer){
            playerID = id;
            playerName = name;
            isBot = bot;
            punPlayer = thePlayer;
            this.isMine = isMine;
            this.character = character;
            this.cosmeticItems = cosmeticItems;
        }
        public PlayerInstance(int id, string name, bool isMine, bool bot, int character, Cosmetics cosmeticItems, int kills, int deaths, int otherScore, Player thePlayer)
        {
            playerID = id;
            playerName = name;
            isBot = bot;
            punPlayer = thePlayer;
            this.isMine = isMine;
            this.character = character;
            this.cosmeticItems = cosmeticItems;

            // Setting the initial scores:
            this.kills = kills;
            this.deaths = deaths;
            this.otherScore = otherScore;
        }

        // Set stats directly:
        public void SetStats(int kills, int deaths, int otherScore, bool upload){
            this.kills = kills;
            this.deaths = deaths;
            this.otherScore = otherScore;

            // Upload the new stats:
            if (upload) UploadStats();
        }
        // Add to stat:
        public void AddStats(int kills, int deaths, int otherScore, bool upload)
        {
            this.kills += kills;
            this.deaths += deaths;
            this.otherScore += otherScore;

            // Upload the new stats:
            if (upload) UploadStats();
        }

        public void UploadStats(){
            if (!Photon.Pun.PhotonNetwork.IsMasterClient) return;

            // For bots:
            if (isBot){
                GameManager.instance.UpdateBotStats();
            }
            // For human players (update thee Photon Player directly as it will automatically sync across the network):
            else{
                ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                if (kills != 0) h.Add("kills", kills);
                if (deaths != 0) h.Add("deaths", deaths);
                if (otherScore != 0) h.Add("otherScore", otherScore);
                punPlayer.SetCustomProperties(h);
            }
        }
    }
}