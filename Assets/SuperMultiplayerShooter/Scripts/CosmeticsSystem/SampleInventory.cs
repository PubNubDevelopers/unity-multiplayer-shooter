using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Sample Inventory
    /// - Please take note that SMST doesn't come with an "inventory system".
    /// - This class is only meant for showcasing the cosmetic system. You need to implement your own inventory system.
    /// </summary>

    public class SampleInventory : MonoBehaviour
    {
        public static SampleInventory instance;

        public CosmeticItemData[] items;

        void Awake(){
            instance = this;
        }
    }
}