using UnityEngine;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Sample Emote Mobile Item
    /// - The script for the sample mobile emote interface's emote item
    /// </summary>

    public class SampleEmoteMobileItem : MonoBehaviour
    {
        public int emoteID;
        public Image spriteImage;

        public void Init(int id, EmotePopup emoteSource)
        {
            spriteImage.sprite = emoteSource.emotes[id];
            emoteID = id;
        }

        public void Select()
        {
            GameManager.instance.DoEmote(emoteID);
        }
    }
}