using UnityEngine;
using UnityEngine.UI;

public class LoadFileMenu : MonoBehaviour
{
    public Button loadButton;
    public FileExplorer fileExplorer;
    public MenuType menuType;

    public enum MenuType { none = 0, mapMenu, replayMenu, }

	void Start()
    {
        fileExplorer.itemSelectedEvent += MapFileExplorer_itemSelected;
        fileExplorer.itemDoubleClickedEvent += MapFileExplorer_itemDoubleClickedEvent;
        fileExplorer.itemDeselected += MapFileExplorer_itemDeselected;
    }

    void OnDisable()
    {
        SetSelection(null);
        fileExplorer.selectedItem = null;
    }

    private void MapFileExplorer_itemSelected(ListableButton item)
    {
        if (item.listableItem.itemType == "File") SetSelection(item);
    }
    private void MapFileExplorer_itemDoubleClickedEvent(ListableButton item)
    {
        if (item.listableItem.itemType == "File") loadButton.onClick.Invoke();
        else if (item.listableItem.itemType == "Folder")
        {
            if (menuType == MenuType.mapMenu) PlayerPrefs.SetString(ApplicationPreferences.CURRENT_MAPS_DIR, (ApplicationPreferences.currentMapsDir = fileExplorer.currentDirectory));
            else if (menuType == MenuType.replayMenu) PlayerPrefs.SetString(ApplicationPreferences.CURRENT_REPLAYS_DIR, (ApplicationPreferences.currentReplaysDir = fileExplorer.currentDirectory));
        }
    }
    private void MapFileExplorer_itemDeselected(ListableButton item)
    {
        SetSelection(null);
    }

    private void SetSelection(ListableButton item)
    {
        if (Camera.main) { Camera.main.GetComponent<ProgramInterface>().browserSelection = item; loadButton.interactable = item != null; }
    }
}
