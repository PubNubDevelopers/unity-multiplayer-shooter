using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Visyde;

public class CategoryButton : MonoBehaviour
{
    public Text CategoryText;

    public void Setup(string category)
    {
        CategoryText.text = category;
    }
   
    /// <summary>
    /// When a category is clicked, filters the items based on the category text.
    /// </summary>
    public void OnClick()
    {  
        Connector.instance.FilterShopItems(CategoryText.text);
    }  
}
