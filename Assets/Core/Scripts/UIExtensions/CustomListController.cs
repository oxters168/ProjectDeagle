using UnityHelpers;
using System.Collections.Generic;
using UnityEngine;

public class CustomListController : MonoBehaviour
{
    public UnityEngine.UI.ScrollRect scrollRect;
    public ListItemController listItemPrefab;
    private ObjectPool<ListItemController> _listItemsPool;
    private ObjectPool<ListItemController> ListItemsPool { get { if (_listItemsPool == null) _listItemsPool = new ObjectPool<ListItemController>(listItemPrefab, 5, false, true, contentRect, false); return _listItemsPool; } }
    public RectTransform contentRect;
    public GameObject emptyListPlaceHolder;

    private List<ListItemController> itemsInList = new List<ListItemController>();

    public event ItemSelectedHandler onItemSelected;
    public delegate void ItemSelectedHandler(object item);

    private void Update()
    {
        emptyListPlaceHolder.SetActive(itemsInList.Count <= 0);
    }

    public void AddToList(IEnumerable<object> items)
    {
        foreach (var item in items)
        {
            AddToList(item);
        }
    }
    public void AddToList(object item)
    {
        var listItem = ListItemsPool.Get();
        listItem.transform.SetAsLastSibling();
        if (listItem is IInteractableListItem)
            ((IInteractableListItem)listItem).onClick += ListItem_onClick;
        listItem.SetItem(item);
        itemsInList.Add(listItem);
    }

    public float GetCurrentVerticalScrollValue()
    {
        return scrollRect.verticalScrollbar.value;
    }
    public void SetCurrentVerticalScrollValue(float value)
    {
        scrollRect.verticalScrollbar.value = value;
    }
    public float GetCurrentHorizontalScrollValue()
    {
        return scrollRect.horizontalScrollbar.value;
    }
    public void SetCurrentHorizontalScrollValue(float value)
    {
        scrollRect.horizontalScrollbar.value = value;
    }
    public ListItemController[] GetListItems()
    {
        return itemsInList.ToArray();
    }
    public object[] GetItems()
    {
        object[] items = new object[itemsInList.Count];
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = itemsInList[i].GetItem();
        }
        return items;
    }
    public void ClearItems()
    {
        foreach (var listItem in itemsInList)
            if (listItem is IInteractableListItem)
                ((IInteractableListItem)listItem).onClick -= ListItem_onClick;

        ListItemsPool.ReturnAll();
        itemsInList.Clear();
    }

    private void ListItem_onClick(object item)
    {
        onItemSelected?.Invoke(item);
    }
}
