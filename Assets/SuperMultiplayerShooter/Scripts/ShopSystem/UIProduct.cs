using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class UIProduct : MonoBehaviour
{
    [SerializeField]
    private Text NameText;
    [SerializeField]
    private Text DescriptionText;
    [SerializeField]
    private Image Icon;
    [SerializeField]
    private Text PriceText;
    [SerializeField]
    private Button PurchaseButton;

   // public delegate void PurchaseEvent(Product Model, Action OnComplete);
   // public event PurchaseEvent OnPurchase;

    //private Product Model;

    public void Setup(UnityEngine.Purchasing.Product Product)
    {
        NameText.text = Product.metadata.localizedTitle;
        DescriptionText.text = Product.metadata.localizedDescription;
        PriceText.text = ($"{Product.metadata.localizedPriceString} " +
            $"{Product.metadata.isoCurrencyCode}");
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
    }
    /*
    public void Purchase()
    {
        PurchaseButton.enabled = false;
        OnPurchase?.Invoke(Model, HandlePurchaseComplete);
    }

    private void HandlePurchaseComplete()
    {
        PurchaseButton.enabled = true;
    }
    */
}
