using Newtonsoft.Json;
using PubnubApi;
using System;
using System.Collections.Generic;
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
    public Image CostIcon;
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
        // Use Discounted price in place of price if the item is marked as such.
        if(shopItem.discounted)
        {
            shopItem.price = shopItem.discounted_price;
        }

        // Configure the Displayed Icon and Price to be displayed in the shop
        PriceText.text = shopItem.currency_type.Equals("usd") ? "$" + shopItem.price.ToString() : shopItem.price.ToString();

        // Only set the image active for coin based items.
        if (!shopItem.category.Equals("hats"))
        {
            CostIcon.gameObject.SetActive(false);
        }

        LoadImage();
    }
 
    public void Purchase()
    {
        bool canPurchase = true;
        // Check to determine if the player can purchase the item.
        if((shopItem.currency_type.Equals("coins") && shopItem.price > DataCarrier.coins))
        {
            canPurchase = false;
        }

        else
        {
            int cost = 0;
            if (shopItem.currency_type.Equals("coins"))
            {
                //Calculate cost of item
                cost = DataCarrier.coins -= shopItem.price;

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
            }

            // currency type is usd - update with new number of coins.
            else
            {
                cost = DataCarrier.coins += shopItem.quantity_given;
            }
           

            // Update and Display New Value of Purchase, including new items in inventory.
            Connector.instance.CurrencyUpdated("coins", cost);
        }

        string message = "You've purchased the following item.";

        // Open the Purchase Popup to display success/failure
        Connector.instance.OpenPurchasePopup(Icon.sprite, message,canPurchase);

        // Refresh shop items
        Connector.instance.FilterShopItems(shopItem.category);

        // Signal that we've successfully purchased an item by publishing the item in message contents.
        SendMessage();

    }

    public async void SendMessage()
    {
        string pubnubMessage = $"Purchased {shopItem.id}";
        string channelId = pubnub.GetCurrentUserId() + shopItem.id;
        PNResult<PNPublishResult> publishResponse = await pubnub.Publish()
                                                    .Message(pubnubMessage)
                                                    .Channel(channelId)
                                                    .ExecuteAsync();
        PNPublishResult publishResult = publishResponse.Result;
        PNStatus status = publishResponse.Status;
        Debug.Log("pub timetoken: " + publishResult.Timetoken.ToString());
        Debug.Log("pub status code : " + status.StatusCode.ToString());
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
