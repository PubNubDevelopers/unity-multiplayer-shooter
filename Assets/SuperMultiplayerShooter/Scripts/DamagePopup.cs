using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Damage Popup
    /// - The component script for the damage popup prefab.
    /// </summary>

    public class DamagePopup : MonoBehaviour
    {
        public Text damageText;
        [Header("Settings:")]
		public Vector2 randomPos;

        public void Set(int damage)
        {
            damageText.text = damage.ToString();
            transform.position += new Vector3(Random.Range(-randomPos.x, randomPos.x), Random.Range(-randomPos.y, randomPos.y));
        }

        void Disable(){
            gameObject.SetActive(false);
        }
    }
}
