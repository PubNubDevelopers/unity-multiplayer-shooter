using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

namespace Visyde{

    /// <summary>
    /// Scoreboard Item
    /// - The script for the item that is populated in the scoreboard.
    /// </summary>

    public class ScoreboardItem : MonoBehaviour {

		[Header("Settings:")]
		public Color others;
		public Color you;

		[Header("References:")]
		public Image bgPanel;
		public Text nameText;
		public Text killsText;
		public Text deathsText;
		public Text scoreText;

		[HideInInspector] public PlayerInstance represented;

		// Use this for initialization
		void Start () {

			// BG:
			if (represented.isMine) {
				nameText.color = you;
			} else {
				nameText.color = others;
			}

			// Stats:
			nameText.text = represented.playerName;
		}
	}
}