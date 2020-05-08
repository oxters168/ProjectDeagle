using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityHelpers;

public class MapsPanel : MonoBehaviour
{
    public static MapsPanel mapsPanelInScene;
    public CustomListController mapsList;
    public Image mapPreview;
    public GameObject noMapSelectedPanel, mapSelectedPanel;
    public GameObject bottomLoadingPanel;
    public TMPro.TextMeshProUGUI bottomLoadingText;
    public TMPro.TextMeshProUGUI titleText;
    private Sprite currentPreview;
    public TwoPartButton twoDeeViewButton, threeDeeViewButton;
    public Sprite downloadIcon, downloadingIcon, builtIcon;

    //public Sprite checkIcon;
    public TwoPartButton[] filterButtons;

    public GameObject loadingBar;
    public Image loadingBarBar;
    public TMPro.TextMeshProUGUI loadingText;

    public TMPro.TextMeshProUGUI emptyListLabel;
    public string loggedInEmptyListMessage, notLoggedInEmptyListMessage;

    private MapListFilter listFilter = MapListFilter.online | MapListFilter.onDevice2D | MapListFilter.onDevice3D;

    [System.Flags]
    public enum MapListFilter { online = 1, onDevice2D = 2, onDevice3D = 4 };

    private void Start()
    {
        mapsPanelInScene = this;
    }
    private void OnEnable()
    {
        //PopulateList();
        RepopulateList();
        //SelectMap(null);
        mapsList.onItemSelected += MapsList_onItemSelected;
    }
    private void OnDisable()
    {
        //SelectMap(null);
        mapsList.onItemSelected -= MapsList_onItemSelected;
    }
    private void Update()
    {
        UpdateMapUI();
        UpdateFilterButtons();
    }

    private void MapsList_onItemSelected(object item)
    {
        SelectMap((MapData)item);
    }

    private void SelectMap(MapData map)
    {
        //if (MapLoaderController.mapLoaderInScene.currentMap == null || MapLoaderController.mapLoaderInScene.currentMap != map)
            MapLoaderController.mapLoaderInScene.SetCurrentMap(map);
        //else
        //    MapLoaderController.mapLoaderInScene.SetCurrentMap(null);

        currentPreview = null;

        map = MapLoaderController.mapLoaderInScene.currentMap;

        if (map != null)
        {
            if (map.IsPreviewAvailable())
                currentPreview = map.GetPreview();
            else
                DownloadPreview();
        }
    }
    private void UpdateFilterButtons()
    {
        var filters = System.Enum.GetValues(typeof(MapListFilter)) as MapListFilter[];
        for (int i = 0; i < filterButtons.Length; i++)
        {
            bool filterActive = (listFilter & filters[i]) != 0;
            filterButtons[i].leftHalf.gameObject.SetActive(filterActive);
        }
    }
    private void UpdateMapUI()
    {
        emptyListLabel.text = SteamController.steamInScene.IsLoggedIn ? loggedInEmptyListMessage : notLoggedInEmptyListMessage;

        MapData currentMap = MapLoaderController.mapLoaderInScene.currentMap;

        noMapSelectedPanel.SetActive(currentMap == null);
        mapSelectedPanel.SetActive(currentMap != null);
        if (currentMap != null)
        {
            mapPreview.gameObject.SetActive(currentPreview != null);
            if (currentPreview != null)
                mapPreview.sprite = currentPreview;

            bool mapLoading2D = currentMap.IsLoading2D;
            bool mapLoading3D = currentMap.IsLoading3D;

            bool taskMakerBusy = TaskMaker.IsBusy();
            twoDeeViewButton.button.interactable = !taskMakerBusy || mapLoading2D;
            threeDeeViewButton.button.interactable = !taskMakerBusy || mapLoading3D;

            loadingBar.SetActive(mapLoading2D || mapLoading3D);
            loadingBarBar.fillAmount = (currentMap.IsDownloading2D || currentMap.IsDownloading3D || currentMap.IsDownloadingDependencies) ? DepotDownloader.ContentDownloader.DownloadPercent : currentMap.GetPercentBuilt();
            loadingText.text = mapLoading3D ? currentMap.GetStatus3D() : currentMap.GetStatus2D();

            //bool overviewAvailable = currentMap.IsOverviewAvailable();
            twoDeeViewButton.leftHalf.gameObject.SetActive(!mapLoading2D && !currentMap.IsOverviewAvailable());
            twoDeeViewButton.buttonText.text = !mapLoading2D ? "2D View" : "Cancel";
            //twoDeeViewButton.icon.sprite = !overviewAvailable ? downloadIcon : (currentMap.IsDownloading2D ? downloadingIcon : builtIcon);

            threeDeeViewButton.leftHalf.gameObject.SetActive((!currentMap.IsMapAvailable() || currentMap.IsBuilt) && !mapLoading3D);
            threeDeeViewButton.icon.sprite = currentMap.IsBuilt ? builtIcon : downloadIcon;
            threeDeeViewButton.buttonText.text = !currentMap.IsLoading3D ? "3D View" : "Cancel";

            titleText.text = currentMap.GetMapName();
        }

        bottomLoadingPanel.SetActive(SteamController.steamInScene.isManifestDownloading);
        if (SteamController.steamInScene.isManifestDownloading)
            bottomLoadingText.text = "Retrieving maps from Steam";
    }
    public void DownloadPreview()
    {
        MapData currentMap = MapLoaderController.mapLoaderInScene.currentMap;
        if (currentMap != null && !currentMap.IsPreviewAvailable() && SteamController.steamInScene.ManifestHasPreview(currentMap.mapName))
        {
            //SteamController.LogToConsole("Downloading " + currentMap.mapName + " preview");
            TaskMaker.DownloadPreview(currentMap, () =>
            {
                SetPreview(currentMap);
            });
        }
    }
    public void SetPreview(MapData map)
    {
        TaskManagerController.RunAction(() =>
        {
            if (MapLoaderController.mapLoaderInScene.currentMap == map && map.IsPreviewAvailable())
                currentPreview = map.GetPreview();
        });
    }
    public void TwoDeeButtonPressed()
    {
        MapData currentMap = MapLoaderController.mapLoaderInScene.currentMap;
        if (currentMap.IsLoading2D)
            currentMap.CancelLoading2D();
        else
            currentMap.LoadMap2D(true, false);
        /*else if (!currentMap.IsOverviewAvailable())
        {
            if (SteamController.steamInScene.IsLoggedIn)
                TaskMaker.DownloadMap2D(currentMap, true, false);
            else
                SteamController.ShowErrorPopup("Download Error", "You must be logged in to download a map");
        }
        else if (!currentMap.IsDownloading2D)
        {
            MapLoaderController.mapLoaderInScene.View2DMap(currentMap);
        }*/
    }
    public void ThreeDeeButtonPressed()
    {
        MapData currentMap = MapLoaderController.mapLoaderInScene.currentMap;
        if (currentMap.IsLoading3D)
            currentMap.CancelLoading3D();
        else
            currentMap.LoadMap3D(true, false);
        /*else if (!currentMap.IsMapAvailable())
        {
            if (SteamController.steamInScene.IsLoggedIn)
                //currentMap.Download3DMapPrompt();
                TaskMaker.DownloadMap3D(currentMap, SettingsController.autoResourcePerMap, SettingsController.autoResourcePerMap, true);
            else
                SteamController.ShowErrorPopup("Download Error", "You must be logged in to download a map");
        }
        else if (currentMap.IsBuilt)
        {
            MapLoaderController.mapLoaderInScene.View3DMap(currentMap, UserViewController.ViewMode.freeLook);
        }
        else
        {
            //TaskMaker.DownloadMap3D(currentMap, true, true, true);
            TaskMaker.LoadMap(currentMap, true, false);
        }*/
    }

    public void RepopulateList()
    {
        mapsList.ClearItems();

        List<string> allMapNames = new List<string>();

        if ((listFilter & MapListFilter.onDevice3D) != 0)
            allMapNames.AddRange(GetMapsFromDir());
        if ((listFilter & MapListFilter.onDevice2D) != 0)
            allMapNames.AddRange(GetMapsFromOverviewsDir());
        if ((listFilter & MapListFilter.online) != 0)
            allMapNames.AddRange(GetMapsFromManifest());

        AddAvailableMap(allMapNames.Distinct().ToArray());
    }
    private List<string> GetMapsFromDir()
    {
        List<string> mapNames = new List<string>();

        IEnumerable<string> mapFiles = null;
        string mapsPath = SettingsController.GetMapsLoc();
        if (Directory.Exists(mapsPath))
            mapFiles = Directory.EnumerateFiles(mapsPath, "*.bsp");

        if (mapFiles != null)
        {
            foreach (string file in mapFiles)
            {
                //string mapDir = Path.GetDirectoryName(file);
                string mapName = Path.GetFileNameWithoutExtension(file);
                mapNames.Add(mapName);
            }
        }

        return mapNames;
    }
    private List<string> GetMapsFromOverviewsDir()
    {
        List<string> mapNames = new List<string>();

        IEnumerable<string> overviewFiles = null;
        if (Directory.Exists(SettingsController.GetOverviewLoc()))
            overviewFiles = Directory.EnumerateFiles(SettingsController.GetOverviewLoc(), "*.dds");

        if (overviewFiles != null)
        {
            foreach (string file in overviewFiles)
            {
                //string overviewDir = Path.GetDirectoryName(file);
                string[] nameParts = Path.GetFileName(file).Split('_'); //splitting due to there being radar, lower, etc as part of the name sometimes
                if (nameParts.Length > 2)
                {
                    string mapName = nameParts[0] + "_" + nameParts[1]; //putting first two parts together, since they're supposed be the name of the map
                    if (OverviewData.IsOverviewAvailable(mapName))
                        mapNames.Add(mapName);
                }
            }
        }
        return mapNames;
    }
    private List<string> GetMapsFromManifest()
    {
        List<string> mapNames = new List<string>();
        if (SteamController.steamInScene != null)
        {
            var mapFiles = SteamController.steamInScene.GetFilesInManifestWithExtension(".bsp");
            foreach (var file in mapFiles)
            {
                string mapName = Path.GetFileNameWithoutExtension(file.FileName);
                mapNames.Add(mapName);
                //AddAvailableMap(mapName);
            }
        }
        return mapNames;
    }
    public void AddAvailableMap(params string[] mapNames)
    {
        MapData[] mapsInList = mapsList.GetItems().Cast<MapData>().ToArray();
        List<MapData> mapsToBeAdded = new List<MapData>();
        foreach (string mapName in mapNames)
        {
            //Debug.Log("Attempting to add " + mapName + " to maps list");
            if (!mapsToBeAdded.Exists(map => map.IsSameMap(mapName)) && mapsInList.Where(mapInList => mapInList.IsSameMap(mapName)).Count() <= 0)
                mapsToBeAdded.Add(MapData.FindOrCreateMap(mapName));
        }
        mapsList.AddToList(mapsToBeAdded);
    }

    public void ToggleFilter(MapListFilter filter)
    {
        if ((listFilter & filter) != 0)
            listFilter &= ~filter;
        else
            listFilter |= filter;

        RepopulateList();
        //ClearList();
        //PopulateList();
    }
    public void ToggleFilter(int filterIndex)
    {
        var filter = (System.Enum.GetValues(typeof(MapListFilter)) as MapListFilter[])[filterIndex];
        ToggleFilter(filter);
    }
}
