using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PurchasePopup : MonoBehaviour
{
    // 
    // [SerializeField]
    // private Text NameText;
    // [SerializeField]
    // private Text DescriptionText;
    [SerializeField]
    private Image PurchasedItemImage;
   
    // Open the Purchased Item Popup and set new sprite.
    public void Show(Sprite purchasedItemSprite)
    {
        this.gameObject.SetActive(true);
        PurchasedItemImage.sprite = purchasedItemSprite;     
    } 
}
