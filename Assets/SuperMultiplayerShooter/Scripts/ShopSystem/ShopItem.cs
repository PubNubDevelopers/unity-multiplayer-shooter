using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Visyde;

public class ShopItem : MonoBehaviour
{
   // 
   // [SerializeField]
   // private Text NameText;
   // [SerializeField]
   // private Text DescriptionText;
    [SerializeField]
    private Image Icon;
    [SerializeField]
    private Text PriceText;

    // public delegate void PurchaseEvent(Product Model, Action OnComplete);
    // public event PurchaseEvent OnPurchase;
    // internals
    private ShopItemData shopItem;// = new ShopItemData();
    //private Product Model;

    public void Setup(ShopItemData shopItemData)
    {
        shopItem = shopItemData;
        PriceText.text = shopItem.original_cost.ToString();        // Configure the Displayed Icon and Price to be displayed in the shop
        LoadImage();       
    }
 
    // TODO: HANDLE DUPLICATE PURCHASE ATTEMPTS - PERHAPS DONE THROUGH UI BY DISABLING ITEM BY COMPARING TO PLAYER"S INVENTORY?
    public void Purchase()
    {
        // Update the player's app context for inventory and resources based on purchase (item obtained and cost)
        // Open the Purchase Popup ti display success
        Connector.instance.OpenPurchasePopup(Icon.sprite);

        int cost = shopItem.currency_type.Equals("coins") ? DataCarrier.coins-- : DataCarrier.gems--;

        // Update and Display New Value of Purchase
        Connector.instance.CurrencyUpdated(shopItem.currency_type, cost);

    }

    /// <summary>
    /// Loads the image from Assets > Resources based on category and product id
    /// </summary>
    void LoadImage()
    {
        // Construct the path with category and id to load the specific image.
        // For example, the path could be "Images/Category1/imageID"
        string imagePath = shopItem.category + "/" + shopItem.id; // Adjust folder structure as needed.

        // Load the image as a Sprite.
        Sprite loadedSprite = Resources.Load<Sprite>(imagePath);

        // Check if the image was successfully loaded.
        if (loadedSprite != null)
        {
            // Set the loaded image to your display component.
            Icon.sprite = loadedSprite;
        }
        else
        {
            Debug.LogError("Image with ID " + shopItem.id + " in category " + shopItem.category + " could not be loaded.");
        }
    }

}
