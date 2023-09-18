using PubnubApi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Character Selector
    /// - displays a list of characters for the character selection screen
    /// </summary>

    public class CharacterSelector : MonoBehaviour
    {
        public CharacterSelectorItem itemPrefab;
        public CharacterData[] characters;
        public Transform content;
        public Connector connector;
        public SampleMainMenu mainMenu;

        private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }


        void Start()
        {
            DataCarrier.characters = characters;
        }

        /// <summary>
        /// Refresh the character selection window.
        /// </summary>
        public void Refresh()
        {
            // Clear items:
            foreach (Transform t in content)
            {
                Destroy(t.gameObject);
            }

            // Repopulate items:
            for (int i = 0; i < characters.Length; i++)
            {
                CharacterSelectorItem item = Instantiate(itemPrefab, content);
                item.data = characters[i];
                item.cs = this;
            }
        }

        // Character selection:
        public async void SelectCharacter(CharacterData data)
        {
            // Close the character selection panel:
            mainMenu.characterSelectionPanel.SetActive(false);

            // ...then set the "character using" in the DataCarrier:
            for (int i = 0; i < characters.Length; i++)
            {
                if (data == characters[i])
                {
                    DataCarrier.chosenCharacter = i;
                }
            }

            mainMenu.characterIconPresenter.sprite = data.icon;

            //Update local metadata
            var metadata = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom;
            if (metadata != null)
            {
                if (metadata.ContainsKey("chosen_character"))
                {
                    metadata["chosen_character"] = DataCarrier.chosenCharacter;
                }

                //First time saving a new hat.
                else
                {
                    metadata.Add("chosen_character", DataCarrier.chosenCharacter);
                }

                PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom = metadata;

                //Store the new update in the metadata
                await PNManager.pubnubInstance.UpdateUserMetadata(pubnub.GetCurrentUserId(), PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Name, metadata);
            }
        }
    }
}
