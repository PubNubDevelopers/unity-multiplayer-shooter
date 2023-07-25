using System.Collections.Generic;
using UnityEngine;

namespace PubNubUnityShowcase
{
    /// <summary>
    /// PNPlayer
    /// - Defines a player, either a member of the lobby or an active player in a game
    /// </summary>
    ///
    public class PNPlayer
    {
        public PNPlayer(string uuid, string nickname, bool isLocal, bool isMasterClient, int character, int chosenHat)
        {
            UserId = uuid;
            NickName = nickname;
            IsLocal = isLocal;
            IsMasterClient = isMasterClient;
            Cosmetics = new int[2];
            Cosmetics[0] = chosenHat;
            Character = character;
            IsReady = false;
        }
        public string UserId { get; }   //  PubNub UserId for this player
        public string NickName { get; } //  PubNub name (as defined in App Context)
        public bool IsLocal { get; }    //  Local players are controlled by the current instance (either you are controlling the character, or the character is a bot controlled by the master client)
        public bool IsMasterClient { get; } //  The client who creates the game (room)
        public bool IsReady { get; set; }   //  The player is instantiated and ready
        public int[] Cosmetics { get; set; }    //  Only one 'cosmetic' is available, in [0], which is the hat
        public int Character { get; set; }  //  Which game character (blue, green or red)
        public int Kills { get; set; }      //  Player score
        public int Deaths { get; set; }     //  Player score
        public int OtherScore { get; set; } //  Player score
        public int ID { get; set; } //  ID assigned by the game, not the PubNub UserID
        public void SetProperties(Dictionary<string, object> props)
        {
            //  This logic populates the attributes of a player at any point, including prior to spawning
            //  they are set / sent by the room master and received by all active player instances (not just locally controlled ones)
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
            }
            if (props.ContainsKey("cosmetics"))
            {
                long[] cosmeticsPayload = (props["cosmetics"] as Newtonsoft.Json.Linq.JArray).ToObject<long[]>();
                Cosmetics = System.Array.ConvertAll<long, int>(cosmeticsPayload, System.Convert.ToInt32);
            }
        }
    }
}
