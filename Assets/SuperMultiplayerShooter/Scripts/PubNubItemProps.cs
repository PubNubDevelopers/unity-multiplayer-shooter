using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PubNubUnityShowcase
{
    /// <summary>
    /// PubNubItemProps
    /// - Instantiation properties for weapons and power-ups.
    /// </summary>
    /// 
    public class PubNubItemProps : MonoBehaviour
    {
        //  The Weapon or PowerUp Index
        public int itemIndex { get; set; }
        //  The item spawn index on the map
        public int spawnPointIndex { get; set; }
        //  The index of the item being spawned (for local lookup)
        public int index { get; set; }
    }
}