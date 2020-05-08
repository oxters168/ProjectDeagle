using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ResourcePackItemController : ListItemController
{
    public TMPro.TextMeshProUGUI packNameLabel, packSizeLabel;
    public Sprite updateIcon, uptodateIcon;
    public GameObject checkingIcon;
    public Image updateImage;
    public Button deleteButton;
    public Toggle downloadToggle;
    private UnityHelpers.TaskWrapper hashTask;
    private bool fileNeedsUpdate;

    private void OnEnable()
    {
        downloadToggle.onValueChanged.AddListener(ToggleValueChanged);
    }
    private void OnDisable()
    {
        CancelUpdateCheckTask();
    }
    private void Update()
    {
        bool isChecking = false;
        if (hashTask != null)
            isChecking = UnityHelpers.TaskManagerController.HasTask(hashTask);
        checkingIcon.SetActive(isChecking);
        updateImage.gameObject.SetActive(!isChecking && deleteButton.interactable && ((DepotDownloader.ProtoManifest.FileData)item).Chunks.Count > 0);
        updateImage.sprite = fileNeedsUpdate ? updateIcon : uptodateIcon;
    }

    private void RefreshItemInfo()
    {
        if (item != null && item is DepotDownloader.ProtoManifest.FileData)
        {
            var resourcePakData = (DepotDownloader.ProtoManifest.FileData)item;

            var fullFilePath = Path.Combine(SettingsController.gameLocation, resourcePakData.FileName);

            packNameLabel.text = Path.GetFileNameWithoutExtension(resourcePakData.FileName);
            packSizeLabel.text = (resourcePakData.TotalSize / 1000000) + "MB";
            deleteButton.interactable = File.Exists(fullFilePath);
            downloadToggle.isOn = SettingsController.HasPak(resourcePakData.FileName);

            fileNeedsUpdate = false;
            if (!TaskMaker.IsBusy() && deleteButton.interactable)
            {
                if (resourcePakData.Chunks != null && resourcePakData.Chunks.Count > 0)
                {
                    hashTask = UnityHelpers.TaskManagerController.CreateTask((ct) => { CheckForUpdates(fullFilePath, resourcePakData, ct); });
                    UnityHelpers.TaskManagerController.QueueTask(hashTask);
                }
            }
        }
    }

    public void DeleteFile()
    {
        CancelUpdateCheckTask();
        if (item != null && item is DepotDownloader.ProtoManifest.FileData)
        {
            var resourcePakData = (DepotDownloader.ProtoManifest.FileData)item;

            string fileName = Path.GetFileNameWithoutExtension(resourcePakData.FileName);
            SteamController.ShowPromptPopup("Delete File", "Are you sure you want to delete the file '" + fileName + "'? This cannot be undone.", (yes) =>
            {
                if (yes)
                {
                    string fullFilePath = Path.Combine(SettingsController.gameLocation, resourcePakData.FileName);
                    if (File.Exists(fullFilePath))
                        File.Delete(fullFilePath);
                    else
                        SteamController.ShowErrorPopup("File Not Found", "Could not find file '" + fileName + "' in filesystem.");

                    RefreshItemInfo();
                    ResourcePacksPanel.resourcePacksPanelInScene.RecheckSizeOnDisk();
                    ResourcePacksPanel.resourcePacksPanelInScene.RecheckDownloadSize();
                }
            }, "Yes", "No");
        }
        else
            SteamController.ShowErrorPopup("Unexpected Error", "An unexpected error occured.");
    }

    public void CancelUpdateCheckTask()
    {
        if (hashTask != null && UnityHelpers.TaskManagerController.HasTask(hashTask))
        {
            UnityHelpers.TaskManagerController.CancelTask(hashTask);
            hashTask = null;
        }
    }
    private void CheckForUpdates(string fullFilePath, DepotDownloader.ProtoManifest.FileData resourcePakData, System.Threading.CancellationToken cancelToken)
    {
        bool sameFile = true;
        using (var stream = File.Open(fullFilePath, FileMode.Open, FileAccess.Read))
        {
            if ((ulong)stream.Length == resourcePakData.TotalSize)
            {
                foreach (var chunk in resourcePakData.Chunks)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    stream.Seek((long)chunk.Offset, SeekOrigin.Begin);

                    byte[] tmp = new byte[chunk.UncompressedLength];
                    stream.Read(tmp, 0, tmp.Length);

                    byte[] currentHash = AdlerHash(tmp);
                    if (!CompareHashes(currentHash, chunk.Checksum))
                    {
                        sameFile = false;
                        break;
                    }
                }
            }
            else
                sameFile = false;
        }
        fileNeedsUpdate = !sameFile;
    }
    public static byte[] AdlerHash(byte[] input)
    {
        uint a = 0, b = 0;
        for (int i = 0; i < input.Length; i++)
        {
            a = (a + (uint)input[i]) % 65521;
            b = (b + a) % 65521;
        }
        return System.BitConverter.GetBytes(a | (b << 16));
    }
    public static bool CompareHashes(byte[] hash, byte[] otherHash)
    {
        bool equal = true;
        if (hash != null && otherHash != null && hash.Length == otherHash.Length)
        {
            for (int i = 0; i < hash.Length; i++)
            {
                if (hash[i] != otherHash[i])
                {
                    equal = false;
                    break;
                }
            }
        }
        else
            equal = false;

        return equal;
    }

    private void ToggleValueChanged(bool isOn)
    {
        string fileName = ((DepotDownloader.ProtoManifest.FileData)item).FileName;
        if (isOn)
            SettingsController.AddPak(fileName);
        else
            SettingsController.RemovePak(fileName);

        ResourcePacksPanel.resourcePacksPanelInScene.RecheckDownloadSize();
    }

    public override void SetItem(object o)
    {
        base.SetItem(o);
        RefreshItemInfo();
    }
}
