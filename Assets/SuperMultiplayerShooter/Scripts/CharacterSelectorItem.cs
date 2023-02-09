using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Visyde{
    /// <summary>
    /// Character Selector Item
    /// - The component for item that is populated in the character selection screen.
    /// </summary>

    public class CharacterSelectorItem : MonoBehaviour {

		public CharacterData data;

		public Text nameText;
		public Image icon;
		public Fillbar hp;
		public Fillbar ms;
        public Image startingWeaponIcon;

        [HideInInspector] public CharacterSelector cs;

		void Start () {
			nameText.text = data.name;
			icon.sprite = data.icon;
			hp.value = data.maxHealth;
			ms.value = data.moveSpeed;
            startingWeaponIcon.sprite = data.startingWeapon.hudIcon;
        }

		public void Select(){
			cs.SelectCharacter (data);
		}
	}
}