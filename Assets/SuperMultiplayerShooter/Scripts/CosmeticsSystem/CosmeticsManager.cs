using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Cosmetics Manager
    /// - Manages all the player cosmetic side of things
    /// - Attached on the player prefab (along with the PlayerController component)
    /// </summary>

    public class CosmeticsManager : MonoBehaviour
    {
        // Internals:
        public int chosenHat;
        PlayerController player;
        List<GameObject> spawnedItems = new List<GameObject>();

        void OnEnable(){
            player = GetComponent<PlayerController>();

            if (!player){
                Destroy(this);
                Debug.LogError("Cosmetics Manager should be attached to the same object the Player Controller is attached to.");
            }
        }

        public void Refresh(Cosmetics cosmetics){
            chosenHat = cosmetics.hat;

            Refresh();
        }
        public void Refresh()
        {
            // Remove existing cosmetic items if there's any:
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i]) Destroy(spawnedItems[i]);
            }
            spawnedItems = new List<GameObject>();

            // If has hat:
            if (chosenHat >= 0)
            {
                PlayerController player = GetComponent<PlayerController>();
                GameObject item = Instantiate(ItemDatabase.instance.hats[chosenHat].prefab, player.character.hatPoint);
                ResetItemPosition(item.transform);
                spawnedItems.Add(item);
            }
        }

        void ResetItemPosition(Transform item){
            item.localEulerAngles = Vector3.zero;
            item.localPosition = Vector3.zero;
            item.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                Destroy(spawnedItems[i]);
            }
        }
    }
}