using Newtonsoft.Json;
using PubnubApi;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditorInternal.Profiling.Memory.Experimental;
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
    [SerializeField]
    public Button ItemButton;

    // public delegate void PurchaseEvent(Product Model, Action OnComplete);
    // public event PurchaseEvent OnPurchase;
    // internals
    private ShopItemData shopItem;// = new ShopItemData();
    private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }

    //private Product Model;

    public void Setup(ShopItemData shopItemData)
    {
        shopItem = shopItemData;
        PriceText.text = shopItem.price.ToString();        // Configure the Displayed Icon and Price to be displayed in the shop
        LoadImage();       
    }
 
    public void Purchase()
    {
        bool canPurchase = true;
        // Check to determine if the player can purchase the item.
        if((shopItem.currency_type.Equals("coins") && shopItem.price > DataCarrier.coins)
            || (shopItem.currency_type.Equals("gems") && shopItem.price > DataCarrier.gems))
        {
            canPurchase = false;
        }

        else
        {
            //Calculate cost of item
            int cost = shopItem.currency_type.Equals("coins") ? DataCarrier.coins -= shopItem.price : DataCarrier.gems -= shopItem.price;

            // Add the Item to Player Inventory
            Dictionary<string, object> customData = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom;
            List<int> availableHats = new List<int>();
            if (customData.ContainsKey("hats"))
            {
                availableHats = JsonConvert.DeserializeObject<List<int>>(customData["hats"].ToString());
                availableHats.Add(int.Parse(shopItem.id) - 1); // Adjust for hats starting at index 1
                customData["hats"] = JsonConvert.SerializeObject(availableHats);
                PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom = customData;
                Connector.instance.UpdateAvailableHats(availableHats);
            }

            // Update and Display New Value of Purchase, including new items in inventory.
            Connector.instance.CurrencyUpdated(shopItem.currency_type, cost);
        }

        string message = "You've purchased the following item.";

        // Open the Purchase Popup to display success/failure
        Connector.instance.OpenPurchasePopup(Icon.sprite, message,canPurchase);

        // Refresh shop items
        Connector.instance.FilterShopItems(shopItem.category);
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
            Icon.sprite = shopItem.sprite = loadedSprite;

            int index = Connector.instance.ShopItemDataList.FindIndex(item => item.id == shopItem.id);

            // Update sprite image
            if (index != -1)
            {
                Connector.instance.ShopItemDataList[index] = shopItem; // Replace the old item with the new one
            }
        }
        else
        {
            Debug.LogError("Image with ID " + shopItem.id + " in category " + shopItem.category + " could not be loaded.");
        }
    }

}
