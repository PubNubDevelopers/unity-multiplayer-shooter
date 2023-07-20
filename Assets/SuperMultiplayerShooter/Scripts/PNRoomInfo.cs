using System.Collections.Generic;
using Visyde;

/// <summary>
/// PNRoomInfo
/// - Defines a room in the lobby and during a game that players are part of and can chat with eachother
/// </summary>
///
namespace PubNubUnityShowcase
{
    public class PNRoomInfo
    {
        public static int MAX_BOTS = 4; //  Done for simplicity
        public PNRoomInfo(string ownerId, string name, int map, int maxPlayers, bool allowBots, long id)
        {
            OwnerId = ownerId;  //  The PubNub ID of the user who created the game
            Name = name;        //  Typically the same as the nickname of the user who created the game
            Map = map;
            MaxPlayers = maxPlayers;
            AllowBots = allowBots;
            this.PlayerList = new List<PNPlayer>();
            ID = id;
            IsOpen = true;
        }
        //  Bots and bot objects define the same thing, keps separate for legacy reasons
        public Dictionary<string, object> Bots = null;  
        public Connector.Bot[] BotObjects = null;
        public List<PNPlayer> PlayerList;   //  List of players currently in this room
        public int BotCount { get; set; }
        public string[] bNames = new string[MAX_BOTS];
        public int[] bChars = new int[MAX_BOTS];
        public int[] bHats = new int[MAX_BOTS];
        public bool IsOpen { get; set; }
        public string OwnerId { get; }  //  PubNub UserID of the creator of the room
        public string Name { get; }    //  Nickname of the creator of the room
        public int Map { get; }
        public int PlayerCount { get { return PlayerList.Count; }}
        public int MaxPlayers { get;  } = 0;
        public bool AllowBots { get; } = false;
        //  Assign a room ID rather than rely on the owner's ID as the room ID.  Avoids the issue where the owner's ID might be too long for a channel name.  This should not happen with the current channel names.
        public long ID { get; } 
        public void AddBots(Dictionary<string, object> bots)
        {
            this.Bots = bots;
        }
        public void AddBotObjects(Connector.Bot[] bots)
        {
            this.BotObjects = bots;
        }

        //  Rather than the master notify all players of assigned IDs, have IDs assigned in a deterministic
        //  fashion, based on the player UserId, so all players know eachother's ID without communication
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

        //  Whether or not a player with the specific PubNub UserID is in the room / game
        public bool ContainsPlayer(string UserId)
        {
            for (int i = 0; i < PlayerList.Count; i++)
            {
                if (PlayerList[i].UserId == UserId) return true;
            }
            return false;
        }
    }
}
