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
    public int price;
    public int quantity_given;
    public bool discounted;
    public int discounted_price;
    public List<string> discount_codes;
    public string recent_code;
    public Sprite sprite; // not storing in metadata, for internal use.
    public string channel; // the channel associated with the item
    public string name; // required for set channel metadata
}