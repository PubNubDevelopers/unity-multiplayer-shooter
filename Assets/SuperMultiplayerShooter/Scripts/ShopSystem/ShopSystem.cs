using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Visyde;

public class ShopSystem : MonoBehaviour
{
    public ShopItem shopItemPrefab; // Assign your item prefab in the Inspector
    public Transform itemsParent; // Assign the content area of your Scroll View in the Inspector
    [SerializeField]
    public PurchasePopup PurchasePopup;

    private List<ShopItemData> shopItems;


    public static ShopSystem instance;

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadShopData();
    }
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

            shopItems = new List<ShopItemData>(shopDataWrapper.items);

            // Post-process items if necessary (e.g., converting category strings to enum, loading sprites)
            foreach (var item in shopItems)
            {
                //item.CategoryEnum = (Categories)Enum.Parse(typeof(Categories), item.category, true);
                // Load icon sprite for each item here if applicable
                ShopItem shopItem = Instantiate(shopItemPrefab, itemsParent);
                shopItem.Setup(item);
            }

            // Now shopItems contains all your shop item data

            // ShopItem item = Instantiate(shopItemPrefab, itemsParent);
            //item.Setup(shopField);
        }

        catch (Exception e)
        {
            // Log the error to the Unity console
            Debug.LogError($"Failed to load or parse shop data: {e.Message}");
        }
    }
 
    // Open the Purchased Item Popup and set new sprite.
    public void OpenPurchasePopup(Sprite purchasedItemSprite)
    {
        PurchasePopup.Show(purchasedItemSprite);
    }

    [System.Serializable]
    public class ShopDataWrapper
    {
        public ShopItemData[] items;
    }
}
