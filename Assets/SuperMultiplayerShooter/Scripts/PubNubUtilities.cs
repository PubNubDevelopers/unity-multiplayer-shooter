using UnityEngine;
using PubnubApi;
using PubnubApi.Unity;
using System.Collections.Generic;
using Visyde;

/// <summary>
/// PubNubUtilities
/// - Helper functions to facilitiate easier connection to PubNub.
/// </summary>

namespace PubNubUnityShowcase
{
    public static class MessageConstants
    {
        public static int idMsgEmoji            = 0;
        static public int idMsgShoot            = 1;
        static public int idMsgMelee            = 2;
        static public int idMsgPosition         = 3;
        static public int idMsgCursor           = 4;
        static public int idMsgSpawnPowerUp     = 6;
        static public int idMsgPickedUpPowerUp  = 7;
        static public int idMsgReceivePowerUp   = 8;
        static public int idMsgUpdateOthers     = 9;
        static public int idMsgSpawnWeapon      = 10;
        static public int idMsgGrabWeapon       = 11;
        static public int idMsgPickedUpWeapon   = 12;
        static public int idMsgApplyDamage      = 13;
        static public int idMsgForceDead        = 14;
        static public int idMsgTriggerDeadZone  = 15;
    }

    public class PubNubUtilities
    {
        //  CHANNELS
        //  --  Channels specific to a game --
        //  For updates where we do not need to receive our own values, for example
        //  sending our position to others, there is 1 channel per player, 1 to 4.
        static public string chanPrefixPlayerActions = "player_action_";       //  E.g. shooting
        static public string chanPrefixPlayerPos = "player_position_";     //  position, velocity
        static public string chanPrefixPlayerCursor = "player_cursor_";       //  cursor
        static public string chanItems = "item_update";
        static public string chanRoomStatus = "currentRoomStatus";             //  E.g. game starting or scores update
        //  --  Channels global to all games / lobbies  --
        static public string chanPrefixLobbyRooms = "rooms.";
        static public string chanGlobal = "global";
        static public string chanChatLobby = "chat.lobby.";
        static public string chanChatTranslate = "translate.";
        static public string chanChat = "chat.";
        static public string chanLeaderboardPub = "score.leaderboard";
        static public string chanLeaderboardSub = "leaderboard_scores";
        //  --  Channels specific for chat messages
        static public string chanChatAll = "chat.all";
        static public string chanFriendList = "friends-";
        static public string chanFriendChat = "presence-";
        static public string chanPrivateChat = "chat.private.*";

        //  Some values, such as player position or cursor location will not change
        //  between update intervals, only send out date if it changes.
        private bool cachedPosition = false;
        private Vector2 cachePosition;
        private Vector2 cacheVelocity;
        private Vector3 cacheMousePos;
        private bool cachedCursor = false;
        private bool cacheMoving;
        private bool cacheIsFalling;
        private float cacheXInput;

        public readonly Dictionary<string, GameObject> ResourceCache = new Dictionary<string, GameObject>();

        public PubNubUtilities()
        {
        }

        public static bool IsMasterClient
        {
            get
            {
                return Visyde.Connector.instance.isMasterClient;
            }
        }

        //  Send an emoji to others via Signal
        public void SendEmoji(Pubnub pubnub, int emojiId, int playerID)
        {
            int[] emojiMsg = new int[3];
            emojiMsg[0] = playerID;
            emojiMsg[1] = MessageConstants.idMsgEmoji;
            emojiMsg[2] = emojiId;
            string channelName = ToGameChannel(chanPrefixPlayerActions + playerID);
            //  Note: int[] gets serialized to long[] by PubNub
            pubnub.Signal().Message(emojiMsg).Channel(channelName).Execute((result, status) =>
            {
                if (status.Error)
                {
                    Debug.Log("Error sending PubNub Signal (Emoji): " + status.ErrorData.Information);
                }
            });
        }

        //  Notify others we are shooting via Signal
        public void Shoot(Pubnub pubnub, int playerID, Vector3 mousePos)
        {
            float[] shootData = new float[5];
            shootData[0] = playerID;   
            shootData[1] = MessageConstants.idMsgShoot;
            shootData[2] = mousePos.x;
            shootData[3] = mousePos.y;
            shootData[4] = mousePos.z;
            string channelName = ToGameChannel(chanPrefixPlayerActions + playerID);
            pubnub.Signal().Message(shootData).Channel(channelName).Execute((result, status) =>
            {
                if (status.Error)
                {
                    Debug.Log("Error sending PubNub Signal (Shoot): " + status.ErrorData.Information);
                }
            });
        }

        //  Notify others we have our knife out and are shaking it around, through Signal
        public void MeleeAttack(Pubnub pubnub, int playerID)
        {
            int[] meleeMsg = new int[2];
            meleeMsg[0] = playerID;
            meleeMsg[1] = MessageConstants.idMsgMelee;
            string channelName = ToGameChannel(chanPrefixPlayerActions + playerID);
            pubnub.Signal().Message(meleeMsg).Channel(channelName).Execute((result, status) =>
            {
                if (status.Error)
                {
                    Debug.Log("Error sending PubNub Signal (Melee): " + status.ErrorData.Information);
                }
            });
        }

        //  Notify others we have hurt them, through Publish message
        public async void ApplyDamage(Pubnub pubnub, int playerID, int fromPlayer, int value, bool gun)
        {
            int[] applyDamageMsg = new int[5];
            applyDamageMsg[0] = playerID;
            applyDamageMsg[1] = MessageConstants.idMsgApplyDamage;
            applyDamageMsg[2] = fromPlayer;
            applyDamageMsg[3] = value;
            applyDamageMsg[4] = gun ? 1 : 0;
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                         .Channel(ToGameChannel(chanItems))
                         .Message(applyDamageMsg)
                         .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log($"Error sending (Apply Damange): " + publishResponse.Status.ErrorData.Information);
            }
        }

        //  Tell the recipient they are dead, to ensure game state consistency across all players.
        public async void ForceDead(Pubnub pubnub, int playerID)
        {
            int[] forceDeadMsg = new int[2];
            forceDeadMsg[0] = playerID;
            forceDeadMsg[1] = MessageConstants.idMsgForceDead;
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                         .Channel(ToGameChannel(chanItems))
                         .Message(forceDeadMsg)
                         .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log($"Error sending (Force Dead): " + publishResponse.Status.ErrorData.Information);
            }
        }

        //  Only the master client can initiate a power up spawn, notify all players to spawn a power-up in the same location (index)
        public async void SpawnPowerUp(Pubnub pubnub, int index, int powerUpIndex)
        {
            int[] spawnPowerUpMsg = new int[3];
            spawnPowerUpMsg[0] = MessageConstants.idMsgSpawnPowerUp;
            spawnPowerUpMsg[1] = index;
            spawnPowerUpMsg[2] = powerUpIndex;
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                         .Channel(ToGameChannel(chanItems))
                         .Message(spawnPowerUpMsg)
                         .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log("Error sending PubNub Message (Spawn Power Up " + index + ")");
            }
        }

        //  Notify the player instance that they have recevied a power up (only sent from Master, who controls all power ups)
        public async void ReceivePowerUp(Pubnub pubnub, int playerID, int powerUpIndex, int spawnPointIndex)
        {
            int[] receivePowerUpMsg = new int[4];
            receivePowerUpMsg[0] = playerID;
            receivePowerUpMsg[1] = MessageConstants.idMsgReceivePowerUp;
            receivePowerUpMsg[2] = powerUpIndex;
            receivePowerUpMsg[3] = spawnPointIndex;
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                         .Channel(ToGameChannel(chanItems))
                         .Message(receivePowerUpMsg)
                         .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log("Error sending PubNub Message (Receive Power Up " + powerUpIndex + ")");
            }
        }

        //  A player has picked up a power-up, notify everyone else to ensure a consistent game state
        public async void PickedUpPowerUp(Pubnub pubnub, int index)
        {
            int[] pickedUpPowerUpMsg = new int[2];
            pickedUpPowerUpMsg[0] = MessageConstants.idMsgPickedUpPowerUp;
            pickedUpPowerUpMsg[1] = index;
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                                     .Channel(ToGameChannel(chanItems))
                                     .Message(pickedUpPowerUpMsg)
                                     .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log("Error sending PubNub Message (Picked Power Up " + index + ")");
            }
        }

        //  All players can see other players' health and shield status.  This message ensures a consistent game state
        public async void UpdateOthersPlayerStatus(Pubnub pubnub, int playerID, int health, int shield)
        {
            //  Notify other players that my health and shield have updated so they can render me correctly
            int[] updateOthersMsg = new int[4];
            updateOthersMsg[0] = playerID;
            updateOthersMsg[1] = MessageConstants.idMsgUpdateOthers;
            updateOthersMsg[2] = health;
            updateOthersMsg[3] = shield;
            string channelName = ToGameChannel(chanPrefixPlayerActions + playerID);
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                                    .Channel(channelName)
                                    .Message(updateOthersMsg)
                                    .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log("Error sending PubNub Message (Update Others)");
            }
        }

        //  Only the master client can initiate a weapon spawn, notify all players to spawn a weapon in the same location (index)
        public async void SpawnWeapon(Pubnub pubnub, int index, int weaponIndex)
        {
            int[] spawnWeaponMsg = new int[3];
            spawnWeaponMsg[0] = MessageConstants.idMsgSpawnWeapon;
            spawnWeaponMsg[1] = index;
            spawnWeaponMsg[2] = weaponIndex;
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                                     .Channel(ToGameChannel(chanItems))
                                     .Message(spawnWeaponMsg)
                                     .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log("Error sending PubNub Message (Spawn Weapon" + index + ")");
            }
        }

        //  Notify the player instance that they have recevied a weapon (only sent from Master, who controls all weapons)
        public async void GrabWeapon(Pubnub pubnub, int playerID, int weaponIndex, int spawnPointIndex)
        {
            int[] grabWeaponMsg = new int[4];
            grabWeaponMsg[0] = playerID;
            grabWeaponMsg[1] = MessageConstants.idMsgGrabWeapon;
            grabWeaponMsg[2] = weaponIndex;
            grabWeaponMsg[3] = spawnPointIndex;
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                                     .Channel(ToGameChannel(chanItems))
                                     .Message(grabWeaponMsg)
                                     .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log("Error sending PubNub Message (Grab Weapon " + weaponIndex + ")");
            }
        }

        //  A player has picked up a weapon, notify everyone else to ensure a consistent game state
        public async void PickedUpWeapon(Pubnub pubnub, int index)
        {
            int[] pickedUpWeaponMsg = new int[2];
            pickedUpWeaponMsg[0] = MessageConstants.idMsgPickedUpWeapon;
            pickedUpWeaponMsg[1] = index;
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                                     .Channel(ToGameChannel(chanItems))
                                     .Message(pickedUpWeaponMsg)
                                     .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log("Error sending PubNub Message (Picked Up Weapon " + index + ")");
            }
        }

        //  You have been a right wally and falling in a hole, tell everyone else so they can laugh at you
        public async void TriggerDeadZone(Pubnub pubnub, int playerID, Vector2 position)
        {
            float[] triggerDeadZoneData = new float[4];
            triggerDeadZoneData[0] = playerID;
            triggerDeadZoneData[1] = MessageConstants.idMsgTriggerDeadZone;
            triggerDeadZoneData[2] = position.x;
            triggerDeadZoneData[3] = position.y;
            string channelName = ToGameChannel(chanPrefixPlayerPos + playerID);
            PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                                     .Channel(channelName)
                                     .Message(triggerDeadZoneData)
                                     .ExecuteAsync();
            if (publishResponse.Status.Error)
            {
                Debug.Log("Error sending PubNub Message (Trigger Dead Zone): " + publishResponse.Status.ErrorData.Information);
            }
        }

        //  Called frequently to notify everyone else what your position is (so they can update their game state)
        public void UpdatePlayerPosition(Pubnub pubnub, int playerID, Vector2 position, Vector2 velocity)
        {
            //  Send player movement as a PubNub signal since we can sacrifice reliability for cost here.
            //  Maximum size of a signal is 64 bytes but the floats in the payload are serialized to doubles.
            if (cachedPosition && (position == cachePosition && velocity == cacheVelocity))
                return;
            else
            {
                cachedPosition = true;
                cachePosition = position;
                cacheVelocity = velocity;
            }
            float[] movementData = new float[6];
            movementData[0] = playerID;     
            movementData[1] = MessageConstants.idMsgPosition;
            movementData[2] = position.x;
            movementData[3] = position.y;
            movementData[4] = velocity.x;
            movementData[5] = velocity.y;

            string channelName = ToGameChannel(chanPrefixPlayerPos + playerID);
            pubnub.Signal().Message(movementData).Channel(channelName).Execute((result, status) =>
            {
                if (status.Error)
                {
                    Debug.Log("Error sending PubNub Signal (Position): " + status.ErrorData.Information);
                }
            });
        }

        //  Called frequently to notify everyone else what your cursor is (so they can rotate your player's gun).
        //  Other data is also sent to help with animations
        public void UpdatePlayerCursor(Pubnub pubnub, int playerID, Vector3 mousePos, bool moving, bool isFalling, float xInput)
        {
            //  Send player cursor as a PubNub signal since we can sacrifice reliability for cost here.
            //  Maximum size of a signal is 64 bytes but the floats in the payload are serialized to doubles.
            if (cachedCursor && (mousePos == cacheMousePos && moving == cacheMoving && isFalling == cacheIsFalling && xInput == cacheXInput))
                return;
            else
            {
                cachedCursor = true;
                cacheMousePos = mousePos;
                cacheMoving = moving;
                cacheIsFalling = isFalling;
                cacheXInput = xInput;
            }
            float[] cursorData = new float[6];
            cursorData[0] = playerID;       
            cursorData[1] = MessageConstants.idMsgCursor;
            cursorData[2] = mousePos.x;
            cursorData[3] = mousePos.y;
            float movingFalling = 0;
            if (moving) movingFalling += 1;
            if (isFalling) movingFalling += 10;
            cursorData[4] = movingFalling;
            cursorData[5] = xInput;

            string channelName = ToGameChannel(chanPrefixPlayerCursor + playerID);
            pubnub.Signal().Message(cursorData).Channel(channelName).Execute((result, status) =>
            {
                if (status.Error)
                {
                    Debug.Log("Error sending PubNub Signal (Cursor): " + status.ErrorData.Information);
                }
            });
        }

        //  Create an object in the game and assign it some initial parameters.  Used when spawning weapons and power-ups.
        public GameObject InstantiateItem(string prefabId, Vector3 position, Quaternion rotation, int itemIndex, int spawnPointIndex, int index)
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
            PubNubItemProps properties = instance.GetComponent<PubNubItemProps>() as PubNubItemProps;
            if (properties == null)
            {
                Debug.LogError("Collectable items must have a PubNubItem associated with the Prefab");
            }
            else
            {
                properties.itemIndex = itemIndex;
                properties.spawnPointIndex = spawnPointIndex;
                properties.index = index;
            }
            instance.SetActive(true);
            return instance;
        }

        public async void PubNubSendRoomProperties(Pubnub pubnub, Dictionary<string, object> payload)
        {
            await pubnub.Publish()
                .Channel(PubNubUtilities.chanRoomStatus)
                .Message(payload)
                .ExecuteAsync();
        }

        public void PubNubSendRoomProperties(Pubnub pubnub, string property, object value)
        {
            Dictionary<string, object> payload = new Dictionary<string, object>();
            payload[property] = value;
            PubNubSendRoomProperties(pubnub, payload);
        }

        public static string GetCurrentMethodName()
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            System.Diagnostics.StackFrame stackFrame = stackTrace.GetFrame(1);
            return stackFrame.GetMethod().Name;
        }

        public static string ToGameChannel(string channelName)
        {
            return ToGameChannel(Visyde.Connector.instance.CurrentRoom, channelName);
        }

        public static string ToGameChannel(PNRoomInfo roomInfo, string channelName)
        {
            if (roomInfo == null) return channelName;
            return "" + roomInfo.ID + "_" + channelName;
        }
    }
}
