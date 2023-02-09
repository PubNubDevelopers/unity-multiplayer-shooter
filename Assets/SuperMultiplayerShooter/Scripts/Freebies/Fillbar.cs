using UnityEngine;
using UnityEngine.UI;

namespace Visyde
{
    /// <summary>
    /// Fillbar
    /// - A script for displaying a repeat-style bar (used in the character stats bars of character selection screen)
    /// </summary>

    [ExecuteInEditMode]
    public class Fillbar : MonoBehaviour
    {
        public float maxValue = 100;
        public float value = 50;
        public float minValue;
        public bool reverse;
        public Image[] images;
        public Sprite filled;
        public Sprite half;
        public Sprite empty;

        // Internals:
        float lastValue;
        float lastMaxValue;
        float finalValue;
        bool refresh;


        // Update is called once per frame
        void Update()
        {
            finalValue = (((value - minValue) / (maxValue - minValue))) * images.Length;

            // Refresh when values are changed:
            if (lastValue != value)
            {
                lastValue = value;
                refresh = true;
            }
            if (lastMaxValue != maxValue)
            {
                lastMaxValue = maxValue;
                refresh = true;
            }

            // Do filling:
            if (!refresh) return;
            if (reverse)
            {
                for (int i = 0; i < images.Length; i++)
                {
                    if (finalValue > i + 0.1f)
                    {
                        if (finalValue < i + 0.9f)
                        {
                            images[(images.Length - 1) - i].sprite = half;
                        }
                        else
                        {
                            if (finalValue >= i + 0.9f)
                            {
                                images[(images.Length - 1) - i].sprite = filled;
                            }
                            else
                            {
                                images[(images.Length - 1) - i].sprite = empty;
                            }
                        }
                    }
                    else
                    {
                        images[(images.Length - 1) - i].sprite = empty;
                    }
                }
            }
            else
            {
                for (int i = 0; i < images.Length; i++)
                {
                    if (finalValue > i + 0.1f)
                    {
                        if (finalValue < i + 0.9f)
                        {
                            images[i].sprite = half;
                        }
                        else
                        {
                            if (finalValue >= i + 0.9f)
                            {
                                images[i].sprite = filled;
                            }
                            else
                            {
                                images[i].sprite = empty;
                            }
                        }
                    }
                    else
                    {
                        images[i].sprite = empty;
                    }
                }
            }
            refresh = false;
        }
    }
}