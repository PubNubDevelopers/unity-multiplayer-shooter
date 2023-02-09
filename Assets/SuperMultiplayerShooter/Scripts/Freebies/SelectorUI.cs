using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Visyde
{
    /// <summary>
    /// Selector UI
    /// - A prev/next-style selector for Unity's built-in UI
    /// </summary>

    public class SelectorUI : MonoBehaviour
    {
        [System.Serializable]
        public class Item
        {
            public string name;
            public int value;
            public Sprite icon;
        }
        public Item[] items;
        public bool loop;

        [Space]
        public Button next;
        public Button prev;
        public Text nameText;
        public Text valueText;
        public Image preview;

        [Space]
        public UnityEvent onChangeSelected;

        [HideInInspector] public int curSelected;

        public Item GetSelectedItem
        {
            get
            {
                return items[curSelected];
            }
        }

        // Use this for initialization
        void Start()
        {
            curSelected = 0;
            next.interactable = true;
            prev.interactable = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (items.Length > 0)
            {

                // Represents:
                if (nameText) nameText.text = items[curSelected].name;
                if (valueText) valueText.text = items[curSelected].value.ToString();
                if (preview) preview.sprite = items[curSelected].icon;

                if (!loop)
                {
                    next.interactable = curSelected < items.Length - 1;
                    prev.interactable = curSelected > 0;
                }
            }
        }

        public void Next()
        {
            if (curSelected >= items.Length - 1)
            {
                if (loop)
                {
                    curSelected = 0;

                    onChangeSelected.Invoke();
                }
            }
            else
            {
                curSelected++;

                onChangeSelected.Invoke();
            }
        }
        public void Prev()
        {
            if (curSelected <= 0)
            {
                if (loop)
                {
                    curSelected = items.Length - 1;

                    onChangeSelected.Invoke();
                }
            }
            else
            {
                curSelected--;

                onChangeSelected.Invoke();
            }
        }
    }
}