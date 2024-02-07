using System.Collections;
using System.Collections.Generic;

public class ShopCategory
{
    public string id { get; private set; }
    public bool enabledFlag { get; private set; }
    public List<ShopItem> virtualShopItems { get; private set; }

    public ShopCategory(RemoteConfigManager.CategoryConfig categoryConfig)
    {
        id = categoryConfig.id;
        enabledFlag = categoryConfig.enabledFlag;
        virtualShopItems = new List<ShopItem>();

        foreach (var item in categoryConfig.items)
        {
            virtualShopItems.Add(new ShopItem(item));
        }
    }

    public override string ToString()
    {
        return $"\"{id}\" enabled:{enabledFlag} items:{virtualShopItems?.Count}";
    }
}
