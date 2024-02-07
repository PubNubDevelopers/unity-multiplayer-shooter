using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopSampleView : MonoBehaviour
{
    public ShopSceneManager shopSceneManager;

    public GameObject comingSoonPanel;

    public Button inventoryButton;
    //public InventoryPopupView inventoryPopupView;

    public Button gainCurrencyDebugButton;

    public GameObject itemsContainer;

    public ShopItemView shopItemPrefab;

    public GameObject categoryButtonsContainerGroup;
    public CategoryButton categoryButtonPrefab;

    public RewardPopupView rewardPopup;

    public MessagePopup messagePopup;

    List<CategoryButton> m_CategoryButtons = new List<CategoryButton>();

    public void SetInteractable(bool isInteractable = true)
    {
        inventoryButton.interactable = isInteractable;
        gainCurrencyDebugButton.interactable = isInteractable;
    }

    /*
    public async void OnInventoryButtonPressed()
    {
        try
        {
            await inventoryPopupView.Show();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    */

    public void Initialize(Dictionary<string, ShopCategory> virtualShopCategories)
    {
        foreach (var kvp in virtualShopCategories)
        {
            var categoryButtonGameObject = Instantiate(categoryButtonPrefab.gameObject,
                categoryButtonsContainerGroup.transform);
            var categoryButton = categoryButtonGameObject.GetComponent<CategoryButton>();
            categoryButton.Initialize(shopSceneManager, kvp.Value.id);
            m_CategoryButtons.Add(categoryButton);
        }
    }

    public void ShowCategory(ShopCategory virtualShopCategory)
    {
        ShowItems(virtualShopCategory);

        foreach (var categoryButton in m_CategoryButtons)
        {
            categoryButton.UpdateCategoryButtonUIState(virtualShopCategory.id);
        }

        comingSoonPanel.SetActive(!virtualShopCategory.enabledFlag);
    }

    void ShowItems(ShopCategory virtualShopCategory)
    {
        if (shopItemPrefab is null)
        {
            throw new NullReferenceException("Shop Item Prefab was null.");
        }

        ClearContainer();

        foreach (var virtualShopItem in virtualShopCategory.virtualShopItems)
        {
            var virtualShopItemGameObject = Instantiate(shopItemPrefab.gameObject,
                itemsContainer.transform);
            virtualShopItemGameObject.GetComponent<ShopItemView>().Initialize(
                shopSceneManager, virtualShopItem);//, AddressablesManager.instance);
        }
    }

    void ClearContainer()
    {
        var itemsContainerTransform = itemsContainer.transform;
        for (var i = itemsContainerTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsContainerTransform.GetChild(i).gameObject);
        }
    }

    public void ShowRewardPopup(List<RewardDetail> rewards)
    {
        rewardPopup.Show(rewards);
    }

    public void OnCloseRewardPopupClicked()
    {
        rewardPopup.Close();
    }

    public void ShowVirtualPurchaseFailedErrorPopup()
    {
        messagePopup.Show("Purchase Failed.",
            "Please ensure that you have sufficient funds to complete your purchase.");
    }
}
