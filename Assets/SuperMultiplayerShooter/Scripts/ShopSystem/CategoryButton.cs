using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Visyde;

public class CategoryButton : MonoBehaviour
{
    public Text CategoryText;
    //public Color activeTextColor;
    //public Material activeTextMaterial;

    public void Setup(string category)
    {
        CategoryText.text = category;
    }
    /*
    public void UpdateCategoryButtonUIState(string selectedCategoryId)
    {
        targetButton.interactable = text.text != selectedCategoryId;
        text.color = text.text == selectedCategoryId ? activeTextColor : defaultTextColor;
        text.fontMaterial = text.text == selectedCategoryId ? activeTextMaterial : defaultTextMaterial;
    }
    */

    /// <summary>
    /// When a category is clicked, filters the items based on the category text.
    /// </summary>
    public void OnClick()
    {  
        Connector.instance.FilterShopItems(CategoryText.text);
    }  
}
