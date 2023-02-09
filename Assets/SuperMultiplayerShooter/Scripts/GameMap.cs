using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Game Map
    /// - handles map properties such as world bounds and spawn points
    /// </summary>

    public class GameMap : MonoBehaviour
    {
        [System.Serializable]
        public class WeaponSpawnPoint
        {
            public Transform point;
            public Weapon onlySpawnThisHere;
        }
        [System.Serializable]
        public class PowerUpSpawnPoint
        {
            public Transform point;
            public PowerUp onlySpawnThisHere;
        }

        [Header("Player Spawn Points:")]
        public List<Transform> playerSpawnPoints;
        [Space]
        [Header("Item Spawn Points:")]
        public WeaponSpawnPoint[] weaponSpawnPoints;
        public PowerUpSpawnPoint[] powerUpSpawnPoints;
        [Space]
        [Header("Spawnable Items:")]
        public Weapon[] spawnableWeapons;
        public PowerUp[] spawnablePowerUps;

        [Space]
        [Header("Others:")]
        public GameObject deadZoneVFX;

        [Space]
        [Header("Camera View:")]
        public Vector2 bounds;
        public Vector2 boundOffset;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(boundOffset.x, boundOffset.y), new Vector3(bounds.x * 2, bounds.y * 2, 0));
        }
    }
}
