using System.Collections.Generic;
using UnityEngine;

/*
 * Data Container for Shop Items
 * 
 */

[System.Serializable]
public class ShopItemData
{
    public string id;
    public string description;
    public string category;
    public string currency_type;
    public int original_cost;
    public int quantity_given;
    public bool discounted;
    public List<string> discount_codes;
}