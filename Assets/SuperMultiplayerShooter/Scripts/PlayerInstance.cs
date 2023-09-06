using System.Collections.Generic;
using PubnubApi;
using PubNubUnityShowcase;

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
        public string PlayerName { get; }
        public int Character { get; }
        public PNPlayer player { get; protected set; }
        private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }

        // Cosmetics:
        public Cosmetics cosmeticItems;

        public bool IsBot { get; protected set; }       // is this player instance owned by a bot?
        public bool IsMine { get; protected set; }      // is this player instance ours?

        // Stats:
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int OtherScore { get; set; }                          

        public PlayerInstance(int id, string name, bool isMine, bool bot, int character, Cosmetics cosmeticItems, PNPlayer thePlayer){
            playerID = id;
            PlayerName = name;
            IsBot = bot;
            player = thePlayer;
            IsMine = isMine;
            Character = character;
            this.cosmeticItems = cosmeticItems;
        }
        public PlayerInstance(int id, string name, bool isMine, bool bot, int character, Cosmetics cosmeticItems, int kills, int deaths, int otherScore, PNPlayer thePlayer)
        {
            playerID = id;
            PlayerName = name;
            IsBot = bot;
            player = thePlayer;
            IsMine = isMine;
            this.Character = character;
            this.cosmeticItems = cosmeticItems;

            // Setting the initial scores:
            Kills = kills;
            Deaths = deaths;
            OtherScore = otherScore;
        }

        // Set stats directly:
        public void SetStats(int kills, int deaths, int otherScore, bool upload){
            Kills = kills;
            Deaths = deaths;
            OtherScore = otherScore;

            // Upload the new stats:
            if (upload) UploadStats();
        }
        // Add to stat:
        public void AddStats(int kills, int deaths, int otherScore, bool upload)
        {
            Kills += kills;
            Deaths += deaths;
            OtherScore += otherScore;

            // Upload the new stats:
            if (upload) UploadStats();
        }

        public void UploadStats(){
            if (!Connector.instance.isMasterClient) return;

            // For bots:
            if (IsBot){
                GameManager.instance.UpdateBotStats();
            }
            else
            {
                Dictionary<string, object> playerProps = new Dictionary<string, object>();
                playerProps.Add("playerStats", "stats");
                playerProps.Add("playerId", playerID);
                playerProps.Add("roomOwnerId", Connector.instance.CurrentRoom.OwnerId);
                if (Kills != 0) playerProps.Add("kills", Kills);
                if (Deaths != 0) playerProps.Add("deaths", Deaths);
                if (OtherScore != 0) playerProps.Add("otherScore", OtherScore);
                new PubNubUtilities().PubNubSendRoomProperties(pubnub, playerProps);
            }
        }
    }
}