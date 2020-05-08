using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;

public class ResourcePacksPanel : MonoBehaviour
{
    public static readonly string[] BYTES = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

    public static ResourcePacksPanel resourcePacksPanelInScene;
    public CustomListController resourcePacksList;
    private ChainedTask downloadTask;
    public TwoPartButton downloadButton;
    public GameObject loadingBarBack;
    public Image loadingBar;
    public bool IsLoading { get { return TaskMaker.HasChainedTask(downloadTask); } }

    public TMPro.TextMeshProUGUI sizeOnDeviceLabel, downloadSizeLabel;

    public TwoPartButton[] filterButtons;
    private ResourceListFilter listFilter = ResourceListFilter.online | ResourceListFilter.onDevice;

    [System.Flags]
    public enum ResourceListFilter { online = 1, onDevice = 2 };

    private void Awake()
    {
        resourcePacksPanelInScene = this;
    }
    private void OnEnable()
    {
        RepopulateList();
        RecheckSizeOnDisk();
        RecheckDownloadSize();
        Canvas.ForceUpdateCanvases();
    }
    private void Update()
    {
        bool loading = IsLoading;
        downloadButton.button.interactable = !TaskMaker.IsBusy() || loading;
        downloadButton.buttonText.text = loading ? "Cancel" : "Download/Update";
        loadingBarBack.SetActive(loading);
        if (loading)
            loadingBar.fillAmount = DepotDownloader.ContentDownloader.DownloadPercent;

        UpdateFilterButtons();
    }

    private void UpdateFilterButtons()
    {
        var filters = System.Enum.GetValues(typeof(ResourceListFilter)) as ResourceListFilter[];
        for (int i = 0; i < filterButtons.Length; i++)
        {
            bool filterActive = (listFilter & filters[i]) != 0;
            filterButtons[i].leftHalf.gameObject.SetActive(filterActive);
        }
    }

    public static int GetBytePrefix(ulong byteSize, out string prefix)
    {
        int index = 0;
        while (byteSize >= 1000)
        {
            byteSize /= 1000;
            index++;
        }
        prefix = BYTES[index];
        return (int)byteSize;
    }
    public void RecheckSizeOnDisk()
    {
        IEnumerable<string> resourcePaksInFileSystem = System.IO.Directory.GetFiles(System.IO.Path.Combine(SettingsController.gameLocation, "csgo"), "*.vpk", System.IO.SearchOption.TopDirectoryOnly);
        resourcePaksInFileSystem = resourcePaksInFileSystem.Where(file => !file.Contains("pakxv_") && !file.Contains("_dir"));
        ulong totalByteSize = 0;
        foreach (string filePath in resourcePaksInFileSystem)
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            totalByteSize += (ulong)fileInfo.Length;
        }
        string prefix;
        int number = GetBytePrefix(totalByteSize, out prefix);
        sizeOnDeviceLabel.text = number + " " + prefix + " on device";
    }
    public void RecheckDownloadSize()
    {
        ulong totalByteSize = 0;
        foreach (var listItem in resourcePacksList.GetListItems())
        {
            var castedItem = ((ResourcePackItemController)listItem);
            if (castedItem.downloadToggle.isOn && !castedItem.deleteButton.interactable)
            {
                var fileData = (DepotDownloader.ProtoManifest.FileData)castedItem.GetItem();
                totalByteSize += fileData.TotalSize;
            }
        }

        string prefix;
        int number = GetBytePrefix(totalByteSize, out prefix);
        downloadSizeLabel.text = number + " " + prefix + " to download";
    }
    private void RepopulateList()
    {
        resourcePacksList.ClearItems();
        var resourcePaksAvailable = new List<DepotDownloader.ProtoManifest.FileData>();
        if ((listFilter & ResourceListFilter.online) != 0)
            resourcePaksAvailable.AddRange(GetResourcePaksOnline());
        if ((listFilter & ResourceListFilter.onDevice) != 0)
            resourcePaksAvailable.AddRange(GetResourcePaksOnDevice());

        resourcePacksList.AddToList(resourcePaksAvailable.Distinct());
    }
    private IEnumerable<DepotDownloader.ProtoManifest.FileData> GetResourcePaksOnline()
    {
        List<DepotDownloader.ProtoManifest.FileData> resourcePaksAvailable = SteamController.steamInScene.GetFilesInManifestWithExtension(".vpk");
        return resourcePaksAvailable.Where(file => !file.FileName.Contains("pakxv_") && !file.FileName.Contains("_dir"));
    }
    private IEnumerable<DepotDownloader.ProtoManifest.FileData> GetResourcePaksOnDevice()
    {
        List<DepotDownloader.ProtoManifest.FileData> onlinePaks = SteamController.steamInScene.GetFilesInManifestWithExtension(".vpk");

        var resourcePaksAvailable = new List<DepotDownloader.ProtoManifest.FileData>();
        string[] resourcePaksInFileSystem = System.IO.Directory.GetFiles(System.IO.Path.Combine(SettingsController.gameLocation, "csgo"), "*.vpk", System.IO.SearchOption.TopDirectoryOnly);
        foreach(var resourcePakInFileSystem in resourcePaksInFileSystem)
        {
            string properPath = "csgo\\" + System.IO.Path.GetFileName(resourcePakInFileSystem);
            var customFileData = new DepotDownloader.ProtoManifest.FileData();
            customFileData.FileName = properPath;

            var onlineFileData = onlinePaks.FirstOrDefault(fileData => fileData.Equals(customFileData));
            if (onlineFileData == null)
            {
                var fileInfo = new System.IO.FileInfo(resourcePakInFileSystem);
                customFileData.TotalSize = (ulong)fileInfo.Length;
            }
            else
                customFileData = onlineFileData;

            resourcePaksAvailable.Add(customFileData);
        }
        return resourcePaksAvailable.Where(file => !file.FileName.Contains("pakxv_") && !file.FileName.Contains("_dir"));
    }

    public void Download()
    {
        CancelUpdateChecks();
        if (!IsLoading)
        {
            SettingsController.SaveSettings();
            if (SteamController.steamInScene.IsLoggedIn)
            {
                var paksToDownload = SettingsController.GetTrackedPaks();
                downloadTask = TaskMaker.DownloadFromSteam(paksToDownload);
            }
            else
                SteamController.ShowErrorPopup("Download Error", "You must be logged in to download resources");
        }
        else
        {
            downloadTask.Cancel();
            downloadTask = null;
        }
    }
    private void CancelUpdateChecks()
    {
        foreach(var listItem in resourcePacksList.GetListItems())
        {
            ((ResourcePackItemController)listItem).CancelUpdateCheckTask();
        }
    }
    public void ToggleAll()
    {
        bool toggleTo = true;
        foreach (var listItem in resourcePacksList.GetListItems())
        {
            toggleTo &= !((ResourcePackItemController)listItem).downloadToggle.isOn;
        }
        foreach (var listItem in resourcePacksList.GetListItems())
        {
            ((ResourcePackItemController)listItem).downloadToggle.isOn = toggleTo;
        }
    }

    public void ToggleFilter(ResourceListFilter filter)
    {
        if ((listFilter & filter) != 0)
            listFilter &= ~filter;
        else
            listFilter |= filter;

        RepopulateList();
    }
    public void ToggleFilter(int filterIndex)
    {
        var filter = (System.Enum.GetValues(typeof(ResourceListFilter)) as ResourceListFilter[])[filterIndex];
        ToggleFilter(filter);
    }
}
