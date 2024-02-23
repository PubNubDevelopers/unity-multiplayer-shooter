using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PurchasePopup : MonoBehaviour
{
    // 
    [SerializeField]
    private Text MessageText;
    // [SerializeField]
    // private Text DescriptionText;
    [SerializeField]
    private Image PurchasedItemImage;
   
    // Open the Purchased Item Popup and set new sprite.
    public void Show(Sprite purchasedItemSprite, bool canPurchase)
    {
        this.gameObject.SetActive(true);
        if(canPurchase)
        {
            MessageText.text = "Success!\r\nYou've Purchased";
        }

        else
        {
            MessageText.text = "Failed! \r\nYou don't have enough to purchase this item";
        }

        PurchasedItemImage.sprite = purchasedItemSprite;     
    } 
}
