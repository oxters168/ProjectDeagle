using UnityEngine;
using UnityEngine.UI;

public class MapsMenu : MonoBehaviour
{
    public Explorer mapsMenu;
    public Button exploreButton, removeButton;

	void Start ()
    {
        //mapsMenu.itemSelectedEvent += MapsMenu_itemSelectedEvent;
        mapsMenu.itemDeselected += MapsMenu_itemDeselected;
	}
    void Update()
    {
        if (mapsMenu.selectedItem != null)
            SelectMap(mapsMenu.selectedItem, ((BSPMap)mapsMenu.selectedItem.listableItem.value).IsDone, true);
    }
    void OnDisable()
    {
        mapsMenu.selectedItem = null;
        SelectMap(null, false, false);
    }

    private void MapsMenu_itemDeselected(ListableButton item)
    {
        SelectMap(null, false, false);
    }
    /*private void MapsMenu_itemSelectedEvent(ListableButton item)
    {
        if (((BSPMap)item.listableItem.value).IsDone)
            SelectMap(item);
        else
            SelectMap(null);
    }*/

    private void SelectMap(ListableButton item, bool canExplore, bool canRemove)
    {
        //ProgramInterface controls = Camera.main.GetComponent<ProgramInterface>();
        if (Camera.main)
        {
            Camera.main.GetComponent<ProgramInterface>().browserSelection = item;
            exploreButton.interactable = canExplore;
            removeButton.interactable = canRemove;
        }
    }
}
