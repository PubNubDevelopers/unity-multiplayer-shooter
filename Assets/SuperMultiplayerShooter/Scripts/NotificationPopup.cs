using UnityEngine;
using UnityEngine.UI; // Include if you're using the standard UI

public class NotificationPopup : MonoBehaviour
{
    public GameObject popup; // Assign your Panel to this in the inspector
    public Text notificationMessage;

    // Call this method to show the popup
    public void ShowPopup(string message)
    {
        // If using standard UI Text
        notificationMessage.text = message;
        popup.SetActive(true);
    }

    // Method to be called when the popup is clicked
    public void ClosePopup()
    {
        popup.SetActive(false);
    }
}
