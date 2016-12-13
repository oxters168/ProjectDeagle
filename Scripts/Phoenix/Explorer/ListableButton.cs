using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.EventSystems;

public class ListableButton : MonoBehaviour
{
    public Button buttonComponent;
    public Image imageComponent;
    public Text textComponent;
    public ListableItem listableItem;
    public Slider progressBar;

    public void SetupListableItem(ListableItem item)
    {
        name = item.shownName;
        if (item.image) imageComponent.sprite = item.image;
        textComponent.text = item.shownName;
        listableItem = item;
    }
}
