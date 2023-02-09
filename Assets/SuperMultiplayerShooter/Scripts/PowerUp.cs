using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Power Up
    /// - A scriptable object for the creation of power-ups.
    /// </summary>

    [CreateAssetMenu(fileName = "New Power-up", menuName = "Visyde/Power-up")]
    public class PowerUp : ScriptableObject
    {
        public Sprite icon;
        public GameObject pickUpEffect;

        [Space]
        [Header("Effects:")]
        public int addedHealth;
        public bool fullRefillHealth;
        public int addedShield;
        public bool fullRefillShield;
        public int addedGrenade;
        public bool fullRefillAmmo;
    }
}
