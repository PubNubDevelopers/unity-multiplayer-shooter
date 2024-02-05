using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class ShopSystem : MonoBehaviour
{
    public UIProduct shopItemPrefab; // Assign your item prefab in the Inspector
    public Transform itemsParent; // Assign the content area of your Scroll View in the Inspector
    private IStoreController m_StoreController; // The Unity Purchasing system's store controller

    // Start is called before the first frame update
    void Start()
    {
        DisplayIAPItems();

    }
    private void DisplayIAPItems()
    {
        var productCatalog = ProductCatalog.LoadDefaultCatalog();

        foreach (var product in productCatalog.allValidProducts)
        {
            // Instantiate an item prefab for each product
            UIProduct item = Instantiate(shopItemPrefab, itemsParent);

            // Here we directly access Unity IAP's store controller to fetch the price
            if (CodelessIAPStoreListener.Instance.HasProductInCatalog(product.id))
            {
                var storeProduct = CodelessIAPStoreListener.Instance.GetProduct(product.id);
                if (storeProduct != null)
                {
                    item.Setup(storeProduct);
                }
            }
        }
    }

    private void PurchaseProduct(string productId)
    {
        // Code to initiate purchase...
        //CodelessIAPStoreListener.Instance.InitiatePurchase(productId);
    }
}
