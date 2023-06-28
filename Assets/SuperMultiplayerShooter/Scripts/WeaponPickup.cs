using UnityEngine;
using PubNubAPI;
using PubNubUnityShowcase;

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

        // Use this for initialization
        void Start()
        {

            // References:
            gm = FindObjectOfType<GameManager>();
            pubNubUtilities = new PubNubUtilities();
            PubNubItemProps initProps = GetComponent<PubNubItemProps>();
            gm.pubnub.SubscribeCallback += SubscribeCallbackHandler;
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
                    if (p)
                    {
                        allowPickup = false;

                        // Sound and VFX:
                        Instantiate(pickUpEffect, transform.position, Quaternion.identity);

                        if (PubNubUtilities.IsMasterClient)
                        {
                            pubNubUtilities.GrabWeapon(gm.pubnub, p.playerInstance.playerID, weaponIndex, spawnPointIndex);
                            pubNubUtilities.PickedUpWeapon(gm.pubnub, index);
                        }
                    }
                }
            }
        }

        private void SubscribeCallbackHandler(object sender, System.EventArgs e)
        {
            //  There is one subscribe handler per weapon
            SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;
            if (mea.MessageResult != null)
            {
                if (mea.MessageResult.Payload is long[])
                {
                    long[] payload = (long[])mea.MessageResult.Payload;
                    if (payload[0] == MessageConstants.idMsgPickedUpWeapon)
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
            gm.itemSpawner.WeaponPickedUp(index);
        }
    }
}
