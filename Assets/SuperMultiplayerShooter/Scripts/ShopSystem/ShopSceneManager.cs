using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;


public class ShopSceneManager : MonoBehaviour
{
    public ShopSampleView virtualShopSampleView;

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();

            // Check that scene has not been unloaded while processing async wait to prevent throw.
            if (this == null) return;

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (this == null) return;
            }

            Debug.Log($"Player id:{AuthenticationService.Instance.PlayerId}");

            await EconomyManager.instance.RefreshEconomyConfiguration();
            if (this == null) return;

            EconomyManager.instance.InitializeVirtualPurchaseLookup();

            // Note: We want these methods to use the most up to date configuration data, so we will wait to
            // call them until the previous two methods (which update the configuration data) have completed.
            await AddressablesManager.instance.PreloadAllEconomySprites();
            if (this == null) return;
            await RemoteConfigManager.instance.FetchConfigs();
            if (this == null) return;
            await EconomyManager.instance.RefreshCurrencyBalances();
            if (this == null) return;

            // Read all badge addressables
            // Note: must be done after Remote Config values have been read (above).
            await AddressablesManager.instance.PreloadAllShopBadgeSprites(
                RemoteConfigManager.instance.virtualShopConfig.categories);

            // Initialize all shops.
            // Note: must be done after all other initialization has completed (above).
            ShopManager.instance.Initialize();

            virtualShopSampleView.Initialize(ShopManager.instance.virtualShopCategories);

            var firstCategoryId = RemoteConfigManager.instance.virtualShopConfig.categories[0].id;
            if (!ShopManager.instance.virtualShopCategories.TryGetValue(
                    firstCategoryId, out var firstCategory))
            {
                throw new KeyNotFoundException($"Unable to find shop category {firstCategoryId}.");
            }

            virtualShopSampleView.ShowCategory(firstCategory);

            Debug.Log("Initialization and sign in complete.");

            EnablePurchases();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void EnablePurchases()
    {
        virtualShopSampleView.SetInteractable();
    }

    public void OnCategoryButtonClicked(string categoryId)
    {
        var virtualShopCategory = ShopManager.instance.virtualShopCategories[categoryId];
        virtualShopSampleView.ShowCategory(virtualShopCategory);
    }

    public async Task OnPurchaseClicked(ShopItem virtualShopItem)
    {
        try
        {
            var result = await EconomyManager.instance.MakeVirtualPurchaseAsync(virtualShopItem.id);
            if (this == null) return;

            await EconomyManager.instance.RefreshCurrencyBalances();
            if (this == null) return;

            ShowRewardPopup(result.Rewards);
        }
        catch (EconomyException e)
            when (e.ErrorCode == k_EconomyPurchaseCostsNotMetStatusCode)
        {
            virtualShopSampleView.ShowVirtualPurchaseFailedErrorPopup();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async void OnGainCurrencyDebugButtonClicked()
    {
        try
        {
            await EconomyManager.instance.GrantDebugCurrency("GEM", 30);
            if (this == null) return;

            await EconomyManager.instance.RefreshCurrencyBalances();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void ShowRewardPopup(Rewards rewards)
    {
        var addressablesManager = AddressablesManager.instance;

        var rewardDetails = new List<RewardDetail>();
        foreach (var inventoryReward in rewards.Inventory)
        {
            rewardDetails.Add(new RewardDetail
            {
                id = inventoryReward.Id,
                quantity = inventoryReward.Amount,
                sprite = addressablesManager.preloadedSpritesByEconomyId[inventoryReward.Id]
            });
        }

        foreach (var currencyReward in rewards.Currency)
        {
            rewardDetails.Add(new RewardDetail
            {
                id = currencyReward.Id,
                quantity = currencyReward.Amount,
                sprite = addressablesManager.preloadedSpritesByEconomyId[currencyReward.Id]
            });
        }

        virtualShopSampleView.ShowRewardPopup(rewardDetails);
    }
}
