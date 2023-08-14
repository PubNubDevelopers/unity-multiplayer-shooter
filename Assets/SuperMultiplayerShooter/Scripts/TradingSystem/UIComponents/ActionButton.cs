using System;
using UnityEngine;
using UnityEngine.UI;

namespace PubNubUnityShowcase
{
    public class ActionButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text labelText;
        private string _id;

        private Action<string> clickCallback;

        public void Construct(string id, string label)
        {
            _id = id;
            gameObject.name = $"--button-" + id;

            if (button != null)
                button.onClick.AddListener(OnBtnClick);

            SetLabel(label);
        }

        public void SetLabel(string label)
        {
            labelText.text = label;
        }

        public void SetClickInteraction(Action<string> callback)
        {
            clickCallback = null;
            clickCallback += callback;
        }

        public void SetInteraction(bool state)
        {
            button.interactable = state;
        }

        public void RemoveClickInteractions()
        {
            clickCallback = null;
        }

        private void OnBtnClick()
        {
            clickCallback?.Invoke(_id);
        }
    }
}