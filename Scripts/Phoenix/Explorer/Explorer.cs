using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Explorer : MonoBehaviour, IPointerClickHandler
{
    protected ScrollRect scrollRect;
    protected List<ListableItem> items;

    public float buttonSize = 50f, spacing = 5f;

    public ListableButton itemButton;

    public float doubleClickMaxWait = 0.2f;
    private float timeSelected;
    public ListableButton selectedItem;

    public event ItemSelected itemSelectedEvent;
    public event ItemDoubleClicked itemDoubleClickedEvent;
    public event ItemDeselected itemDeselected;

    protected virtual void Start()
    {
        
    }

    public void Refresh()
    {
        if (!scrollRect) scrollRect = GetComponent<ScrollRect>();
        if (itemButton)
        {
            foreach (Transform child in scrollRect.content.transform)
                Destroy(child.gameObject);

            GridLayoutGroup contentLayout = scrollRect.content.GetComponent<GridLayoutGroup>();
            contentLayout.cellSize = new Vector2(buttonSize, buttonSize);
            contentLayout.spacing = new Vector2(spacing, Mathf.Abs(itemButton.textComponent.rectTransform.rect.y + itemButton.textComponent.rectTransform.rect.height) + spacing);

            scrollRect.normalizedPosition = new Vector2(0, 1);

            foreach (ListableItem item in items)
            {
                ListableButton child = Instantiate(itemButton);
                item.SetCorrespondingButton(child);
                child.SetupListableItem(item);
                child.buttonComponent.onClick.AddListener(delegate { ButtonClicked(child); });

                child.transform.SetParent(scrollRect.content.transform, false);
            }
        }
    }
    public void AddItem(ListableItem item)
    {
        if (items == null) items = new List<ListableItem>();
        items.Add(item);
    }
    public void RemoveItem(ListableItem item)
    {
        if (selectedItem.listableItem == item) selectedItem = null;
        items.Remove(item);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ListableButton prevSelection = selectedItem;
        selectedItem = null;
        if (prevSelection != null && itemDeselected != null) itemDeselected(prevSelection);
    }
    protected virtual void ButtonClicked(ListableButton button)
    {
        if (button == selectedItem)
        {
            if (Time.time - timeSelected <= doubleClickMaxWait)
            {
                //ChangeDirectory((string)button.listableItem.value);
                //selectedItem = null;
                if (itemDoubleClickedEvent != null) itemDoubleClickedEvent(button);
            }

            timeSelected = Time.time;
        }
        else
        {
            selectedItem = button;
            timeSelected = Time.time;
            if (itemSelectedEvent != null) itemSelectedEvent(button);
        }
    }
    public delegate void ItemSelected(ListableButton item);
    public delegate void ItemDoubleClicked(ListableButton item);
    public delegate void ItemDeselected(ListableButton item);
}
