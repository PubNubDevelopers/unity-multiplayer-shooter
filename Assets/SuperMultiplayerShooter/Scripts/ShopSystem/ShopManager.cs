using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{

    public static ShopManager instance { get; private set; }

    public Dictionary<string, ShopCategory> virtualShopCategories { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Initialize()
    {
        virtualShopCategories = new Dictionary<string, ShopCategory>();

        foreach (var categoryConfig in RemoteConfigManager.instance.virtualShopConfig.categories)
        {
            var virtualShopCategory = new ShopCategory(categoryConfig);
            virtualShopCategories[categoryConfig.id] = virtualShopCategory;
        }
    }
}
