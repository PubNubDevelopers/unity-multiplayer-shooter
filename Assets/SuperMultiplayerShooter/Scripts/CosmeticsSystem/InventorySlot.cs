using UnityEngine;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Inventory Slot
    /// - Sample main menu inventory slot.
    /// - Each slot has this component.
    /// </summary>

    public class InventorySlot : MonoBehaviour
    {
        public CosmeticItemData curItem;
        public Image iconImage;
        public GameObject check;

        [HideInInspector] public CharacterCustomizer cc;

        public void Refresh(){
            // Display icon:
            if (curItem){
                iconImage.sprite = curItem.icon;
                iconImage.enabled = true;
            }
            else{
                iconImage.enabled = false;
            }

            // Show/hide checkmark:
            check.SetActive(DataCarrier.chosenHat >= 0 && ItemDatabase.instance.hats[DataCarrier.chosenHat] == curItem);
        }

        public void Select(){
            if (curItem) cc.Equip(curItem);
        }
    }
}