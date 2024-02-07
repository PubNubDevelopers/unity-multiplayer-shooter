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
    public int original_cost;
    public int quantity_given;
    public bool discounted;
    public float? discounted_price; // Nullable float to handle null values
}