using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadCategories();
        currentCategoryId = SetDefaultCategory();
        LoadShopData();
        FilterShopItems(currentCategoryId);
    }

    /// <summary>
    /// Loads the shop data from the json file.
    /// </summary>
    /// <param name="currentCategoryId"></param>
    private void LoadShopData()
    {
        try
        {
            // Assuming the JSON string is loaded into jsonText from Resources or another source
            TextAsset jsonText = Resources.Load<TextAsset>("shop_data");
            if (jsonText == null)
            {
                Debug.LogError("Failed to load shop_data.json. Make sure the file exists and the path is correct.");
                return;
            }

            ShopDataWrapper shopDataWrapper = JsonUtility.FromJson<ShopDataWrapper>(jsonText.text);

            if (shopDataWrapper?.items == null)
            {
                Debug.LogError("Failed to parse shop items from JSON.");
                return;
            }

            Connector.instance.ShopItemDataList = shopDataWrapper.items;        
        }

        catch (Exception e)
        {
            // Log the error to the Unity console
            Debug.LogError($"Failed to load or parse shop data: {e.Message}");
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
    public void OpenPurchasePopup(Sprite purchasedItemSprite)
    {
        PurchasePopup.Show(purchasedItemSprite);

        // Deduct rewards points from player

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
        var filteredItems = Connector.instance.ShopItemDataList.Where(item => item.category == categoryId.ToLowerInvariant()).ToList();

        // Post-process items if necessary (e.g., converting category strings to enum, loading sprites)
        foreach (var item in filteredItems)
        {
            ShopItem shopItem = Instantiate(shopItemPrefab, itemsParent);
            shopItem.Setup(item);
        }
    }

    /// <summary>
    /// Removes all shop items from the shop (when opening for the first time, changing fitlers, etc)
    /// </summary>
    private void ClearShopItems()
    {
        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = itemsParent.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    [System.Serializable]
    public class ShopDataWrapper
    {
        public List<ShopItemData> items;
    }
}
