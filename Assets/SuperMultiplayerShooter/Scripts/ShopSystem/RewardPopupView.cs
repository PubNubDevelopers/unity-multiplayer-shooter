using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardPopupView : MonoBehaviour
{
    public Text headerText;

    public RewardDisplayView rewardDisplayView;

    public Button closeButton;

    public Text closeButtonText;

    public void Show(List<RewardDetail> rewards)
    {
        rewardDisplayView.PopulateView(rewards);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
