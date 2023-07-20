using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PubnubApi;
using PubnubApi.Unity;
using PubNubUnityShowcase;
using Newtonsoft.Json;
using static PubnubApi.Unity.PubnubExtensions;

namespace Visyde
{
    /// <summary>
    /// Power Up Pickup
    /// - The component script for power-up pickup prefabs.
    /// </summary>

    public class PowerUpPickup : MonoBehaviour
    {
        [Header("References:")]
        public SpriteRenderer itemGraphic;

        GameManager gm;
        PowerUp itemHandled;

        bool allowPickup = true;
        int powerUpIndex;
        int spawnPointIndex;
        int index;

        PubNubUtilities pubNubUtilities;
        private SubscribeCallbackListener listener = new SubscribeCallbackListener();

        // Use this for initialization
        void Start()
        {

            // References:
            gm = FindObjectOfType<GameManager>();
            pubNubUtilities = new PubNubUtilities();
            PubNubItemProps initProps = GetComponent<PubNubItemProps>();

            //Listeners
            gm.pubnub.AddListener(listener);
            listener.onMessage += OnPnMessage;

            if (initProps)
            {
                powerUpIndex = initProps.itemIndex;
                spawnPointIndex = initProps.spawnPointIndex;
                index = initProps.index;
            }
            else
            {
                Debug.LogError("Failed to receive initialization properties");
            }

            // data from network (by master client):

            try
            {
                itemHandled = gm.maps[gm.chosenMap].spawnablePowerUps[powerUpIndex];
                if (spawnPointIndex != -1) itemHandled = gm.maps[gm.chosenMap].powerUpSpawnPoints[spawnPointIndex].onlySpawnThisHere;
            }
            catch (System.Exception ex) { Debug.Log(ex.Message); }

            // Visual:
            itemGraphic.sprite = itemHandled.icon;
        }

        void Update()
        {
            // Hide when someone already picked this pickup:
            itemGraphic.enabled = allowPickup;
        }

        /// <summary>
        /// Called when the scene is changed. Remove any PubNub listeners.
        /// </summary>
        private void OnDestroy()
        {
            listener.onMessage -= OnPnMessage;
        }

        void Allow()
        {
            allowPickup = true;
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (allowPickup)
            {
                if (col.tag == "Player")
                {
                    PlayerController p = col.GetComponent<PlayerController>();
                    if (p && pubNubUtilities != null && gm != null)
                    {
                        allowPickup = false;

                        // Sound and VFX:
                        try
                        {
                            Instantiate(itemHandled.pickUpEffect, transform.position, Quaternion.identity);
                        }
                        catch (System.Exception) { }

                        // Only the master client will handle the power-up actions:
                        if (PubNubUtilities.IsMasterClient)
                        {
                            pubNubUtilities.ReceivePowerUp(gm.pubnub, p.playerInstance.playerID, powerUpIndex, spawnPointIndex);
                            pubNubUtilities.PickedUpPowerUp(gm.pubnub, index);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event listener to handle PubNub Message events
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="result"></param>
        private void OnPnMessage(Pubnub pn, PNMessageResult<object> result)
        {
            //  There is one subscribe handler per character
            if (result.Message != null && result.Channel.Equals(PubNubUtilities.chanItems))
            {
                long[] payload = JsonConvert.DeserializeObject<long[]>(result.Message.ToString());
                if (payload != null)
                {
                    if (payload[0] == MessageConstants.idMsgPickedUpPowerUp)
                    {
                        //  Power Up has been picked up.  Check whether is corresponds to our instance
                        int destIndex = System.Convert.ToInt32(payload[1]);
                        if (index == destIndex)
                            Picked(index);
                    }
                }
            }
        }

        public void Picked(int index)
        {
            gm.itemSpawner.PowerUpPickedUp(index);
        }
    }
}
