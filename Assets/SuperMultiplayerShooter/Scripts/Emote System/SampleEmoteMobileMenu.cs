using UnityEngine;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Sample Emote Mobile Menu
    /// - The script for the sample mobile emote interface's menu
    /// </summary>

    public class SampleEmoteMobileMenu : MonoBehaviour
    {
        public GameObject list;
        public EmotePopup emoteSource;
		public SampleEmoteMobileItem templateEmoteItem;
        public Transform content;

        // Use this for initialization
        void Start()
        {
            for (int i = 0; i < emoteSource.emotes.Length; i++){
                SampleEmoteMobileItem e = Instantiate(templateEmoteItem, content);
                e.Init(i, emoteSource);
            }
            templateEmoteItem.gameObject.SetActive(false);
        }

		public void ToggleMenu(){
            list.SetActive(!list.activeSelf);
        }
    }
}