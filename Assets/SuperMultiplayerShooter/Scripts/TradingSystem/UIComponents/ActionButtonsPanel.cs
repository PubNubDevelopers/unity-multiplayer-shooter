using PubnubApi;
using PubNubUnityShowcase;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionButtonsPanel : MonoBehaviour
{
    [SerializeField] private ActionButton buttonPrefab;
    [SerializeField] private RectTransform root;

    private Dictionary<string, ActionButton> buttons = new Dictionary<string, ActionButton>();

    public void AddButton(string id, string label, Action<string> callback)
    {
        if(buttons.ContainsKey(id))
        {
            ChangeButton(id, callback, label);
            return;
        }

        var btn = Instantiate(buttonPrefab, root, false);
        btn.Construct(id, $"{label}");
        btn.SetClickInteraction(callback);
        buttons.Add(id, btn);
    }

    public void RemoveButton(string id)
    {
        if (buttons.ContainsKey(id))
            Destroy(buttons[id].gameObject);

        buttons.Remove(id);
    }

    public void RemoveAll()
    {
        foreach (var btn in buttons.Values)
        {
            Destroy(btn.gameObject);
        }

        buttons.Clear();
    }

    public void ChangeButton(string id, Action<string> callback, string label = "")
    {
        if (buttons.ContainsKey(id))
        {
            buttons[id].SetClickInteraction(callback);

            if (!string.IsNullOrEmpty(label))
                buttons[id].SetLabel(label);
        }
    }

    public void SetButtonInteractable(string id , bool state)
    {
        if (buttons.ContainsKey(id))
        {
            buttons[id].SetInteraction(state);
        }
        else
            Debug.LogWarning($"Can't find key {id}");
    }

    public void Arrange(List<string> idInOrder, bool reverse = false)
    {
        if (reverse)
            idInOrder.Reverse();

        Queue<string> queue = new Queue<string>(idInOrder);
        Queue<ActionButton> btnsInOrder = new Queue<ActionButton>();

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();

            if (buttons.ContainsKey(id))
                btnsInOrder.Enqueue(buttons[id]);
        }

        while (btnsInOrder.Count > 0)
        {
            var btn = btnsInOrder.Dequeue();
            btn.transform.SetAsFirstSibling();
        }
    }
}
