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
    private string currentCategoryId;
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
    }

    /// <summary>
    /// Loads the shop data from the json file.
    /// </summary>
    /// <param name="currentCategoryId"></param>
    private async Task<bool> LoadShopData()
    {
        // Simply return if the shop data has already been loaded.
        if (Connector.instance.ShopItemDataList != null && Connector.instance.ShopItemDataList.Count > 0)
        {
            return true;
        }

        else
        {
            try
            {
                PNGetChannelMetadataResult channelMetadata = await PNManager.pubnubInstance.GetChannelMetadata("shop_items");
                if (channelMetadata != null && channelMetadata.Custom != null && channelMetadata.Custom.Count > 0)
                {
                    var shopItems = new Dictionary<string, ShopItemData>();

                    foreach (KeyValuePair<string, object> item in channelMetadata.Custom)
                    {
                        string itemId = item.Key;
                        string itemJson = item.Value.ToString(); // This is the JSON string representation of the item's data

                        // Deserialize the JSON string back into a ShopItemData object or a Dictionary as needed
                        var itemData = JsonConvert.DeserializeObject<Dictionary<string, object>>(itemJson);
                        ShopItemData shopItem = new ShopItemData
                        {
                            id = itemId,
                            description = itemData["description"].ToString(),
                            category = itemData["category"].ToString(),
                            currency_type = itemData["currency_type"].ToString(),
                            original_cost = Convert.ToInt32(itemData["original_cost"]),
                            quantity_given = Convert.ToInt32(itemData["quantity_given"]),
                            discounted = Convert.ToBoolean(itemData["discounted"]),
                            discount_codes = JsonConvert.DeserializeObject<List<string>>(itemData["discount_codes"].ToString())
                        };

                        // Add the deserialized ShopItemData to the dictionary
                        shopItems.Add(itemId, shopItem);
                    }

                    Connector.instance.ShopItemDataList = shopItems;

                    return true;
                }
                return false;
            }

            catch (Exception e)
            {
                // Log the error to the Unity console
                Debug.LogError($"Failed to load or parse shop data: {e.Message}");
                return false;
            }
        }     
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
    public void OpenPurchasePopup(Sprite purchasedItemSprite, bool canPurchase)
    {
        PurchasePopup.Show(purchasedItemSprite, canPurchase);

        //TODO: Deduct rewards points from player
        //TODO: Also, if a player attempts to purchase an item they don't have enough money for, don't allow them. (show purchaseFailedWindow).

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
        var filteredItems = Connector.instance.ShopItemDataList.Values
              .Where(item => item.category.Equals(categoryId, StringComparison.OrdinalIgnoreCase))
              .ToList();

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
            if(availableHats != null && availableHats.Contains(int.Parse(item.id) - 1))
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
