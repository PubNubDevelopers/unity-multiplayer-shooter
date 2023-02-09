using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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

        void Start()
        {

            // Set-up the arrays that will check for current spawns:
            currentWeaponSpawns = new Transform[gm.maps[gm.chosenMap].weaponSpawnPoints.Length];
            currentPowerUpSpawns = new Transform[gm.maps[gm.chosenMap].powerUpSpawnPoints.Length];

            nextWeaponSpawnIn = new double[currentWeaponSpawns.Length];
            nextPowerUpSpawnIn = new double[currentPowerUpSpawns.Length];
        }

        // Update is called once per frame
        void Update()
        {
            if (PhotonNetwork.IsMasterClient && gm.gameStarted && !gm.isGameOver)
            {
                // Spawn weapons:
                for (int i = 0; i < nextWeaponSpawnIn.Length; i++)
                {
                    if (PhotonNetwork.Time >= nextWeaponSpawnIn[i])
                    {
                        SpawnWeapon(i);
                    }
                }

                // Spawn power-ups:
                for (int i = 0; i < nextPowerUpSpawnIn.Length; i++)
                {
                    if (PhotonNetwork.Time >= nextPowerUpSpawnIn[i])
                    {
                        SpawnPowerUp(i);
                    }
                }
            }
        }

        // Updates the next weapon spawn time.
        void MarkWhenToSpawnNextWeapon(int index)
        {
            nextWeaponSpawnIn[index] = PhotonNetwork.Time + weaponSpawnTime;

            if (PhotonNetwork.IsMasterClient)
            {
                ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                h.Add("NextWeaponSpawn" + index, nextWeaponSpawnIn[index]);
                PhotonNetwork.CurrentRoom.SetCustomProperties(h);
            }
        }
        // Updates the next power-up spawn time.
        void MarkWhenToSpawnNextPowerUp(int index)
        {
            nextPowerUpSpawnIn[index] = PhotonNetwork.Time + powerUpSpawnTime;

            if (PhotonNetwork.IsMasterClient)
            {
                ExitGames.Client.Photon.Hashtable h = new ExitGames.Client.Photon.Hashtable();
                h.Add("NextPowerUpSpawn" + index, nextPowerUpSpawnIn[index]);
                PhotonNetwork.CurrentRoom.SetCustomProperties(h);
            }
        }

        void SpawnWeapon(int index)
        {

            // Mark next spawn time:
            MarkWhenToSpawnNextWeapon(index);

            // The game map:
            GameMap map = gm.maps[gm.chosenMap];

            // The spawns (pick a random weapon id to be spawned for each of the weapon spawn points):
            int spawnId = Random.Range(0, map.spawnableWeapons.Length);

            // Then do spawn them through the network:
            if (!currentWeaponSpawns[index])
            {
                // Create the data for weapon reference. Basically if the "onlySpawnThisHere" of the weapon spawn point is empty then 
                // we will gonna spawn a random one, but if it's not then we should only spawn that weapon:
                object[] data = new object[3];
                data[0] = spawnId;                                                          // The weapon index
                data[1] = map.weaponSpawnPoints[index].onlySpawnThisHere ? index : -1;      // The spawn point index
                data[2] = index;

                currentWeaponSpawns[index] = PhotonNetwork.Instantiate(weaponPickupPrefab, map.weaponSpawnPoints[index].point.position, Quaternion.identity, 0, data).transform;
            }
        }
        void SpawnPowerUp(int index)
        {

            // Mark next spawn time:
            MarkWhenToSpawnNextPowerUp(index);

            // The game map:
            GameMap map = gm.maps[gm.chosenMap];

            // The spawns (pick a random power-up id to be spawned for each of the power-up spawn points):
            int spawnId = Random.Range(0, map.spawnablePowerUps.Length);

            // Then do spawn them through the network:
            if (!currentPowerUpSpawns[index])
            {
                // Create the data for power-up reference. Basically if the "onlySpawnThisHere" of the power-up spawn point is empty then 
                // we will gonna spawn a random one, but if it's not then we should only spawn that power-up:
                object[] data = new object[3];
                data[0] = spawnId;                                                          // The power-up index
                data[1] = map.powerUpSpawnPoints[index].onlySpawnThisHere ? index : -1;     // The spawn point index
                data[2] = index;

                currentPowerUpSpawns[index] = PhotonNetwork.Instantiate(powerUpPickupPrefab, map.powerUpSpawnPoints[index].point.position, Quaternion.identity, 0, data).transform;
            }
        }

        // "Picked up" calls:
        public void WeaponPickedUp(int index)
        {
            MarkWhenToSpawnNextWeapon(index);
        }
        public void PowerUpPickedUp(int index)
        {
            MarkWhenToSpawnNextPowerUp(index);
        }

        #region Photon calls
        void OnPhotonCustomRoomPropertiesChanged(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            // For weapon spawns:
            for (int i = 0; i < nextWeaponSpawnIn.Length; i++)
            {
                if (propertiesThatChanged.ContainsKey("NextWeaponSpawn" + i))
                {
                    nextWeaponSpawnIn[i] = (double)propertiesThatChanged["NextWeaponSpawn" + i];
                }
            }
            // For power-up spawns:
            for (int i = 0; i < nextPowerUpSpawnIn.Length; i++)
            {
                if (propertiesThatChanged.ContainsKey("NextPowerUpSpawn" + i))
                {
                    nextPowerUpSpawnIn[i] = (double)propertiesThatChanged["NextPowerUpSpawn" + i];
                }
            }
        }
        #endregion
    }
}