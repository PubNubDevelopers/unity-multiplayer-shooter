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
 
    // TODO: HANDLE DUPLICATE PURCHASE ATTEMPTS - PERHAPS DONE THROUGH UI BY DISABLING ITEM BY COMPARING TO PLAYER"S INVENTORY?
    public void Purchase()
    {
        // Update the player's app context for inventory and resources based on purchase (item obtained and cost)

        // 

        // Open the Purchase Popup ti display success
        Connector.instance.OpenPurchasePopup(Icon.sprite);        
    }

    private void HandlePurchaseComplete()
    {
       // PurchaseButton.enabled = true;
    }
    
}
