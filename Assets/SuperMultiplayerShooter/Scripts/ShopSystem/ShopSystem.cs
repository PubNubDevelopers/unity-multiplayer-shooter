using Newtonsoft.Json;
using PubnubApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Visyde;

public class ShopSystem : MonoBehaviour
{
    public ShopItem shopItemPrefab; // Assign your item prefab in the Inspector
    public Transform itemsParent; // Assign the content area of your Scroll View in the Inspector
    public GameObject categoryButtonsContainerGroup;
    public CategoryButton categoryButtonPrefab;
    [SerializeField]
    public PurchasePopup PurchasePopup;

    public static ShopSystem instance;
    public string currentCategoryId;
    private Pubnub pubnub { get { return PNManager.pubnubInstance.pubnub; } }


    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    async void Start()
    {
        LoadCategories();
        currentCategoryId = SetDefaultCategory();
        await LoadShopData();
        FilterShopItems(currentCategoryId);

        // Event Listeners
    }

  

    /// <summary>
    /// Loads the shop data from the json file.
    /// </summary>
    public async Task<bool> LoadShopData()
    {
        /*
        // Simply return if the shop data has already been loaded.
        if (Connector.instance.ShopItemDataList != null && Connector.instance.ShopItemDataList.Count > 0)
        {
            return true;
        }
        */
        Connector.instance.ShopItemDataList = new List<ShopItemData>();

        var allChannelMetadata = await PNManager.pubnubInstance.GetAllChannelMetadata();

        //Filter the channel metadata for shop items.
       // var shopItemChannels = allChannelMetadata.Channels.Where(channel => String.IsNullOrWhiteSpace(channel.Channel) && channel.Channel.StartsWith("shop_items.")).ToList();
        foreach(var item in allChannelMetadata.Channels)
        {
            if (item.Channel.StartsWith("shop_items."))
            {
                // Setup Shop Items
                ShopItemData shopItem = new ShopItemData
                {
                    id = item.Custom["id"].ToString(),
                    description = item.Custom["description"].ToString(),
                    category = item.Custom["category"].ToString(),
                    currency_type = item.Custom["currency_type"].ToString(),
                    price = Convert.ToInt32(item.Custom["price"]),
                    quantity_given = Convert.ToInt32(item.Custom["quantity_given"]),
                    discounted = Convert.ToBoolean(item.Custom["discounted"]),
                    discount_codes = JsonConvert.DeserializeObject<List<string>>(item.Custom["discount_codes"].ToString())
                };
                Connector.instance.ShopItemDataList.Add(shopItem);
            }      
        }

        // No Channels Matched
        if(Connector.instance.ShopItemDataList == null)
        {
            return false;
        }

        return true;
    }

    void LoadCategories()
    {
        foreach (Categories category in Enum.GetValues(typeof(Categories)))
        {
            // Instantiate a new button from the prefab
            CategoryButton categoryButton = Instantiate(categoryButtonPrefab, categoryButtonsContainerGroup.transform);

            // Set the button text
            // enum value to have space and proper casing
            categoryButton.CategoryText.text = FormatCategoryName(category.ToString());

            // temporarily disable every category but hats and currency
            if(!categoryButton.CategoryText.text.ToLowerInvariant().Equals("hats") && !categoryButton.CategoryText.text.ToLowerInvariant().Equals("currency"))
            {
                categoryButton.gameObject.GetComponent<Button>().interactable = false;
            }
        }    
    }

    public string SetDefaultCategory()
    {
        // Convert the enum to an array and take the first value
        Categories firstCategory = (Categories)Enum.GetValues(typeof(Categories)).GetValue(0);

        // Normalize the category name
        string normalizedCategoryName = FormatCategoryName(firstCategory.ToString());

        return normalizedCategoryName;
    }

    public string FormatCategoryName(string enumName)
    {
        // Convert enum string to Title Case (e.g., "HATS" to "Hats")
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(enumName.ToLower());
    }

    // Open the Purchased Item Popup and set new sprite.
    public void OpenPurchasePopup(Sprite purchasedItemSprite, string message,bool canPurchase)
    {
        PurchasePopup.Show(purchasedItemSprite, message,canPurchase);
    }

    /// <summary>
    /// Filters the shop items based on the category id (name).
    /// Activates when the player clicks on the category button.
    /// </summary>
    /// <param name="categoryId"></param>
    public void FilterShopItems(string categoryId)
    {
        //Clear any previous shop items.
        ClearShopItems();

        // Filter the shop items based on the category.
        var filteredItems = Connector.instance.ShopItemDataList.Where(item => item.category.Equals(categoryId, StringComparison.OrdinalIgnoreCase)).ToList();

        //  Populate the available hat inventory and other settings, read from PubNub App Context
        Dictionary<string, object> customData = PNManager.pubnubInstance.CachedPlayers[pubnub.GetCurrentUserId()].Custom;
        List<int> availableHats = new List<int>();
        if (customData.ContainsKey("hats"))
        {
            availableHats = JsonConvert.DeserializeObject<List<int>>(customData["hats"].ToString());
        }
        // Post-process items if necessary (e.g., converting category strings to enum, loading sprites)
        foreach (var item in filteredItems)
        {
            ShopItem shopItem = Instantiate(shopItemPrefab, itemsParent);
            shopItem.Setup(item);
            if(categoryId.ToLowerInvariant() == "hats" && availableHats != null && availableHats.Contains(int.Parse(item.id) - 1))
            {
                shopItem.ItemButton.interactable = false;
            }    
        }
    }

    /// <summary>
    /// Removes all shop items from the shop (when opening for the first time, changing fitlers, etc)
    /// </summary>
    public void ClearShopItems()
    {
        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = itemsParent.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    /// <summary>
    /// When the coin debug button is clicked, gain a coin immediately.
    /// </summary>
    public void OnCoinGainedDebug()
    {
        Connector.instance.CurrencyUpdated("coins", DataCarrier.coins++);
    }

    [System.Serializable]
    public class ShopDataWrapper
    {
        public List<ShopItemData> items;
    }
}
