using UnityEngine;
//using UnityEngine.UI;

public class TabbedPanel : MonoBehaviour
{
    public GameObject[] panels;
    private int selectedTab;

	void Start ()
    {
        SetSelectedTab(selectedTab);
	}
	
    public void SetSelectedTab(int tabID)
    {
        if(panels != null && tabID >= 0 && tabID < panels.Length) selectedTab = tabID;
        Refresh();
    }
	private void Refresh ()
    {
	    if(panels != null)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].SetActive(i == selectedTab);
            }
        }
	}
}
