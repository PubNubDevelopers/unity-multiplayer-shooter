using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
        //PriceText.text = ($"{Product.metadata.localizedPriceString} " +
            
        /*
        Texture2D texture = StoreIconProvider.GetIcon(Product.definition.id);
        if (texture != null)
        {
            Sprite sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                Vector2.one / 2f
            );

            Icon.sprite = sprite;
        }
        else
        {
            Debug.LogError($"No Sprite found for {Product.definition.id}!");
        }
        */

        //Set the Item Data
        //shopItemData.Icon = TODO

    }

    public void ShopItemSelected()
    {
        //TODO: If item is selected, open pop-up asking if want to purchase.
        // Send Item Data with it
    }
    
    public void Purchase()
    {
       // PurchaseButton.enabled = false;
       // OnPurchase?.Invoke(Model, HandlePurchaseComplete);
    }

    private void HandlePurchaseComplete()
    {
       // PurchaseButton.enabled = true;
    }
    
}
