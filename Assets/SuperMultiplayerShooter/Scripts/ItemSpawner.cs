using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PubnubApi;
using PubnubApi.Unity;
using PubNubUnityShowcase;
using Newtonsoft.Json;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace Visyde
{
    /// <summary>
    /// Item Spawner
    /// - uses item id's (int) to spawn pickups. These id's are specified in the GameManager's map spawnables. The MasterClient picks an item to be spawned
    /// by choosing a random item id then sending it through the network. The other clients will then recieve the 
    /// item's id, find the item with the matching id, and then spawn them locally.
    /// </summary>

    public class ItemSpawner : UnityEngine.MonoBehaviour
    {
        [Header("Weapon spawning:")]
        public float weaponSpawnTime = 10;
        public string weaponPickupPrefab;
        [Header("Power-Up spawning:")]
        public float powerUpSpawnTime = 6;
        public string powerUpPickupPrefab;

        [Space]
        [Header("References:")]
        public GameManager gm;

        [HideInInspector] public double[] nextWeaponSpawnIn = new double[0];
        [HideInInspector] public double[] nextPowerUpSpawnIn = new double[0];
        [HideInInspector] public Transform[] currentWeaponSpawns;                       // used for checking if a certain spawn point still has a weapon pickup
        [HideInInspector] public Transform[] currentPowerUpSpawns;                      // used for checking if a certain spawn point still has a power-up pickup

        private PubNubUtilities pubNubUtilities;
        private SubscribeCallbackListener listener = new SubscribeCallbackListener();

        void Start()
        {
            // Set-up the arrays that will check for current spawns:
            currentWeaponSpawns = new Transform[gm.maps[gm.chosenMap].weaponSpawnPoints.Length];
            currentPowerUpSpawns = new Transform[gm.maps[gm.chosenMap].powerUpSpawnPoints.Length];

            nextWeaponSpawnIn = new double[currentWeaponSpawns.Length];
            nextPowerUpSpawnIn = new double[currentPowerUpSpawns.Length];

            pubNubUtilities = new PubNubUtilities();
            //Add Listeners
            gm.pubnub.AddListener(listener);
            listener.onMessage += OnPnMessage;
        }

        // Update is called once per frame
        void Update()
        {
            if (PubNubUtilities.IsMasterClient && gm.gameStarted && !gm.isGameOver)
            {
                // Spawn weapons:
                for (int i = 0; i < nextWeaponSpawnIn.Length; i++)
                {
                    if (Time.time >= nextWeaponSpawnIn[i])
                    {
                        MarkWhenToSpawnNextWeapon(i);
                        SpawnWeaponOnClients(i);
                    }
                }

                // Spawn power-ups:
                for (int i = 0; i < nextPowerUpSpawnIn.Length; i++)
                {
                    if (Time.time >= nextPowerUpSpawnIn[i])
                    {
                        MarkWhenToSpawnNextPowerUp(i);
                        SpawnPowerUpOnClients(i);
                    }
                }
            }

        }
        /// <summary>
        /// Called whenever the scene or game ends. Unsubscribe from PubNub listeners.
        /// </summary>
        private void OnDestroy()
        {
            listener.onMessage -= OnPnMessage;
        }

        /// <summary>
        /// Event listener to handle PubNub Message events
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="result"></param>
        private void OnPnMessage(Pubnub pn, PNMessageResult<object> result)
        {
            //  There is one subscribe handler per character
            if (result.Message != null)
            {           
                long[] payload = JsonConvert.DeserializeObject<long[]>(result.Message.ToString());
                if( payload != null)
                {
                    if (payload[0] == MessageConstants.idMsgSpawnPowerUp)
                    {
                        //  Spawn a Power Up
                        int index = System.Convert.ToInt32(payload[1]);
                        int powerUpIndex = System.Convert.ToInt32(payload[2]);
                        SpawnPowerUp(index, powerUpIndex);
                    }
                    else if (payload[0] == MessageConstants.idMsgSpawnWeapon)
                    {
                        //  Spawn a Weapon
                        int index = System.Convert.ToInt32(payload[1]);
                        int weaponIndex = System.Convert.ToInt32(payload[2]);
                        SpawnWeapon(index, weaponIndex);
                    }
                }
            }
        }

        // Updates the next weapon spawn time.
        void MarkWhenToSpawnNextWeapon(int index)
        {
            nextWeaponSpawnIn[index] = Time.time + weaponSpawnTime;
        }
        // Updates the next power-up spawn time.
        void MarkWhenToSpawnNextPowerUp(int index)
        {
            nextPowerUpSpawnIn[index] = Time.time + powerUpSpawnTime;
        }

        private void SpawnWeaponOnClients(int i)
        {
            GameMap map = gm.maps[gm.chosenMap];
            int weaponIndex = Random.Range(0, map.spawnableWeapons.Length);
            pubNubUtilities.SpawnWeapon(gm.pubnub, i, weaponIndex);
        }

        void SpawnWeapon(int index, int spawnId)
        {

            // Mark next spawn time:
            MarkWhenToSpawnNextWeapon(index);

            // The game map:
            GameMap map = gm.maps[gm.chosenMap];

            // Then do spawn them through the network:
            if (!currentWeaponSpawns[index])
            {
                // Create the data for weapon reference. Basically if the "onlySpawnThisHere" of the weapon spawn point is empty then 
                // we will gonna spawn a random one, but if it's not then we should only spawn that weapon:
                int spawnPointIndex = map.weaponSpawnPoints[index].onlySpawnThisHere ? index : -1;

                currentWeaponSpawns[index] = new PubNubUtilities().InstantiateItem(weaponPickupPrefab,
                    map.weaponSpawnPoints[index].point.position, Quaternion.identity, spawnId, spawnPointIndex, index).transform;
            }
        }

        private void SpawnPowerUpOnClients(int i)
        {
            GameMap map = gm.maps[gm.chosenMap];
            int powerUpIndex = Random.Range(0, map.spawnablePowerUps.Length);
            pubNubUtilities.SpawnPowerUp(gm.pubnub, i, powerUpIndex);
        }

        void SpawnPowerUp(int index, int spawnId)
        {

            // Mark next spawn time:
            MarkWhenToSpawnNextPowerUp(index);

            // The game map:
            GameMap map = gm.maps[gm.chosenMap];

            // Then do spawn them through the network:
            if (!currentPowerUpSpawns[index])
            {
                // Create the data for power-up reference. Basically if the "onlySpawnThisHere" of the power-up spawn point is empty then 
                // we will gonna spawn a random one, but if it's not then we should only spawn that power-up:
                int spawnPointIndex = map.powerUpSpawnPoints[index].onlySpawnThisHere ? index : -1;

                currentPowerUpSpawns[index] = new PubNubUtilities().InstantiateItem(powerUpPickupPrefab,
                    map.powerUpSpawnPoints[index].point.position, Quaternion.identity, spawnId, spawnPointIndex, index).transform;
            }

        }

        // "Picked up" calls:
        public void WeaponPickedUp(int index)
        {
            currentWeaponSpawns[index] = null;
            MarkWhenToSpawnNextWeapon(index);
        }
        public void PowerUpPickedUp(int index)
        {
            currentPowerUpSpawns[index] = null;
            MarkWhenToSpawnNextPowerUp(index);
        }
    }
}