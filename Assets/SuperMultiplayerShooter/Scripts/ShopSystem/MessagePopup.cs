using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessagePopup : MonoBehaviour
{
    public Text titleField;
    public Text messageField;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Show(string title, string message)
    {
        titleField.text = title;
        messageField.text = message;
        Show();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}