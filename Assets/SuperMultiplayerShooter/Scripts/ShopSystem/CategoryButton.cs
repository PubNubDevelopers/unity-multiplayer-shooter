using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    public Button targetButton;
    public Text text;
    public Color defaultTextColor;
    public Material defaultTextMaterial;
    public Color activeTextColor;
    public Material activeTextMaterial;

    ShopSceneManager m_VirtualShopSceneManager;

    public void Initialize(ShopSceneManager virtualShopSceneManager, string category)
    {
        m_VirtualShopSceneManager = virtualShopSceneManager;
        text.text = category;
    }

    public void UpdateCategoryButtonUIState(string selectedCategoryId)
    {
        targetButton.interactable = text.text != selectedCategoryId;
        text.color = text.text == selectedCategoryId ? activeTextColor : defaultTextColor;
       // text == selectedCategoryId ? activeTextMaterial : defaultTextMaterial;
    }

    public void OnClick()
    {
        m_VirtualShopSceneManager.OnCategoryButtonClicked(text.text);
    }
}
