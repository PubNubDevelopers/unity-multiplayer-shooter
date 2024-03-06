using UnityEngine;
using PubnubApi;
using PubnubApi.Unity;
using PubNubUnityShowcase;
using Newtonsoft.Json;
using System;

namespace Visyde
{
    /// <summary>
    /// Weapon Pickup
    /// - The component script for weapon pickup prefabs.
    /// </summary>

    public class WeaponPickup : MonoBehaviour
    {
        [Header("References:")]
        public SpriteRenderer itemGraphic;
        public GameObject pickUpEffect;

        GameManager gm;
        Weapon itemHandled;

        bool allowPickup = true;
        int weaponIndex;
        int spawnPointIndex;
        int index;

        PubNubUtilities pubNubUtilities;
        private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }

        // Use this for initialization
        void Start()
        {

            // References:
            gm = FindObjectOfType<GameManager>();
            pubNubUtilities = new PubNubUtilities();
            PubNubItemProps initProps = GetComponent<PubNubItemProps>();

            //Listeners
            PNManager.pubnubInstance.onPubNubMessage += OnPnMessage;

            if (initProps)
            {
                weaponIndex = initProps.itemIndex;
                spawnPointIndex = initProps.spawnPointIndex;
                index = initProps.index;
            }
            else
            {
                Debug.LogError("Failed to receive initialization properties");
            }

            // data from network (by master client):

            itemHandled = gm.maps[gm.chosenMap].spawnableWeapons[weaponIndex];
            if (spawnPointIndex != -1) itemHandled = gm.maps[gm.chosenMap].weaponSpawnPoints[spawnPointIndex].onlySpawnThisHere;

            // Visual:
            itemGraphic.sprite = itemHandled.spriteRenderer.sprite;
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
            PNManager.pubnubInstance.onPubNubMessage -= OnPnMessage;
        }

        void Allow()
        {
            allowPickup = true;
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (allowPickup)
            {
                if (col.CompareTag("Player"))
                {
                    PlayerController p = col.GetComponent<PlayerController>();
                    if (p && pubNubUtilities != null && gm != null)
                    {
                        allowPickup = false;

                        // Sound and VFX:
                        Instantiate(pickUpEffect, transform.position, Quaternion.identity);

                        if (PubNubUtilities.IsMasterClient)
                        {
                            pubNubUtilities.GrabWeapon(pubnub, p.playerInstance.playerID, weaponIndex, spawnPointIndex);
                            pubNubUtilities.PickedUpWeapon(pubnub, index);
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
        private void OnPnMessage(PNMessageResult<object> result)
        {
            //  There is one subscribe handler per weapon
            if (result.Message != null && result.Channel.Equals(PubNubUtilities.ToGameChannel(PubNubUtilities.chanItems)))
            {
                long[] payload = JsonConvert.DeserializeObject<long[]>(result.Message.ToString());
                if (payload != null)
                {
                    if (payload[0] == MessageConstants.idMsgPickedUpWeapon)
                    {
                        //Debug.Log("Player Picked Up Weapon");

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
            gm.itemSpawner.WeaponPickedUp(index);
        }
    }
}
