using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PurchasePopup : MonoBehaviour
{
    [SerializeField]
    private Text MessageText;
    // [SerializeField]
    // private Text DescriptionText;
    [SerializeField]
    private Image PurchasedItemImage;

    [SerializeField]
    private Sprite ErrorSprite;

    // Open the Purchased Item Popup and set new sprite.
    public void Show(Sprite purchasedItemSprite, string message, bool canPurchase)
    {
        this.gameObject.SetActive(true);
        if(canPurchase)
        {
            MessageText.text = "Success!\r\n" + message;
            PurchasedItemImage.sprite = purchasedItemSprite;
        }

        else
        {
            // Reset back to error message
            PurchasedItemImage.sprite = ErrorSprite;
            MessageText.text = "Failed! \r\nPlease Try Again.";
        }

    } 
}
