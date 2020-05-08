using TMPro;

public class MapListItemController : ListItemController, IInteractableListItem
{
    public event InteractionEventHandler onClick;
    public TextMeshProUGUI mapNameText;
    public TextMeshProUGUI mapTypeText;

    void Update()
    {
        if (item != null && item is MapData)
        {
            var map = (MapData)item;
            mapNameText.text = map.GetMapName();
            mapTypeText.text = map.GetMapType().name;
        }
    }

    public void Click()
    {
        onClick?.Invoke(item);
    }
}
