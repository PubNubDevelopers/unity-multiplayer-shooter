using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Visyde;
using System.Linq;
using PubnubApi;
using Newtonsoft.Json;
using System;

public class DiscountCodePopup : MonoBehaviour
{
    public InputField codeInput;
    public Button validateCodeButton;
    public Button userCodeButton; // A prefab for displaying each discount code in the list
    public Transform userDiscountCodesParent;

    // Start is called before the first frame update
    void Start()
    {
        // Load Discount Codes from User Metadata
        LoadUserDiscountCodes();
    }

    /// <summary>
    /// Load the Player's Discount Codes
    /// </summary>
    public void LoadUserDiscountCodes()
    {
        ClearDiscountCodes();
        Dictionary<string, object> customData = PNManager.pubnubInstance.CachedPlayers[PNManager.pubnubInstance.pubnub.GetCurrentUserId()].Custom;
        List<string> discount_codes = new List<string>();
        if(customData.ContainsKey("discount_codes"))
        {
            discount_codes = JsonConvert.DeserializeObject<List<string>>(customData["discount_codes"].ToString());

            // There might be duplicates. Only display distinct codes.
            foreach(var code in discount_codes.Distinct().ToList())
            {
                // Create button and append to list.
                Button codeButton = Instantiate(userCodeButton, userDiscountCodesParent);

                codeButton.gameObject.SetActive(true); // Make sure the new button is active

                // Update the text of the new button to display the discount code
                codeButton.GetComponentInChildren<Text>().text = code;

                // Optionally, add a click listener for the new button
                codeButton.onClick.AddListener(() => OnDiscountCodeButtonClicked(code));

                // Important: Ensure the RectTransform is configured to work with your layout
               // RectTransform rt = codeButton.GetComponent<RectTransform>();
               // rt.localScale = Vector3.one; // Reset scale
               // rt.localPosition = Vector3.zero; // Reset position
               // rt.localRotation = Quaternion.identity; // Reset rotation

                // Ensure the new button is properly scaled and positioned
               // codeButton.transform.localScale = Vector3.one;
            }
        }

        //userCodeButton.gameObject.SetActive(false);

    }

    /// <summary>
    /// When the User's Discount Code Button is clicked, copies the text to the UserInput field.
    /// </summary>
    public void OnDiscountCodeButtonClicked(string code)
    {
        codeInput.text = "";
        codeInput.text = code;
    }

    /// <summary>
    /// Clears the Discount Codes Container.
    /// </summary>
    public void ClearDiscountCodes()
    {
        for (int i = userDiscountCodesParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = userDiscountCodesParent.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    public void OnValidateButtonClick()
    {
        string enteredCode = codeInput.text;
        var item = Connector.instance.ShopItemDataList.FirstOrDefault(item => item.discount_codes.Contains(enteredCode));
        // Code is valid
        if(item != null)
        {
            // Update price of item based on code. Discount codes are always broken up in the following way: item id : percetnage off
            var codePieces = enteredCode.Split(':');
            item.price = (int)((Convert.ToDouble(codePieces[1]) / 100) * item.price);

            Connector.instance.FilterShopItems(ShopSystem.instance.currentCategoryId);

            string message = $"The following item is now {codePieces[1]}% off!";

            // Display Popup indicating what occurred with a success message, including the image sprite.
            Connector.instance.OpenPurchasePopup(item.sprite, message, true);
        }

        // Code is invalid
        else
        {
            string message = $"Discount code is invalid. Please Try Again.";
            // Display Popup indicating that it failed and to try again.
            Connector.instance.OpenPurchasePopup(null, message, false);
        }
    }
}
