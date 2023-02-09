using UnityEngine;

namespace Visyde{
    /// <summary>
    /// Character Data
    /// - handles character properties such as max health, movement speed, starting grenades etc.
    /// </summary>

    [CreateAssetMenu(fileName = "New Character", menuName = "Visyde/Character Data")]
	public class CharacterData : ScriptableObject {

		public Sprite icon;
		public int maxHealth = 100;
		public float moveSpeed = 5;
		public float jumpForce = 9;
        public Weapon startingWeapon;
        public int grenades;
		[Space]
		public AudioClip[] footstepSFX;
		public AudioClip[] jumpSFX;
		public AudioClip[] landingsSFX;
	}
}