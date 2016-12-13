using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using Steamworks;
//using ProjectDeagle;
using UnityEngine.UI;

public class ProgramInterface : MonoBehaviour
{
    public SplashScript splash;
    public GameObject userInterface;

    //DemoParser testParser;

    Dictionary<string, DemoController> demosLoaded = new Dictionary<string, DemoController>();
    string currentReplay;
    int currentMap = 0;

    #region Thread Ender
    private static bool _isQuitting;
    private static object quittingLock = new object();
    public static bool isQuitting
    {
        get
        {
            lock(quittingLock)
            {
                return _isQuitting;
            }
        }
        set
        {
            lock(quittingLock)
            {
                _isQuitting = value;
            }
        }
    }
    #endregion

    void Start()
    {
        LoadSettings();
        SourceTexture.LoadDefaults();
        //ApplicationPreferences.UpdateVPKParser();
        //ApplicationPreferences.ResetValues();

        if (splash) splash.StartSplash(userInterface);

        #region Testing Region
        //ParseFBX("C:/Users/oxter/OneDrive/Projects/3D Models/Homecooked/ProjectMotherload/Digger.FBX");
        //Debug.Log(DataParser.ReadBits(new byte[] { 164, 1 }, 4, 12)[0]);
        //Debug.Log("Middle: " + BitConverter.ToUInt16(DataParser.ReadBits(new byte[] { 128, 2, 60 }, 6, 16), 0)); //61450
        //ParseDEM("C:/Users/oxter/OneDrive/Replays/Replay1.dem");
        //ParseDEM("C:/Users/oxter/OneDrive/Replays/BlazingBlace.dem");
        //ParseDEM("C:/Program Files (x86)/Steam/steamapps/common/Counter-Strike Global Offensive/csgo/replays/match730_003134331647877447733_1083018565_191.dem");
        //ParseVPK(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)").Replace("\\", "/") + "/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/pak01_dir.vpk");
        //Demo.ConvertToHexString("Ahmed");
        //VPKParser vpkTest = new VPKParser(new FileStream("D:/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/pak01_dir.vpk", FileMode.Open));
        //Debug.Log(vpkTest.Parse());
        //ParseMDL();
        //ParseVVD();
        //ParseModel("ctm_fbi", "C:/Users/oxter/Documents/csgo/models/player/");
        //ApplicationPreferences.UpdateVPKParser();
        //ParseModel("tm_leet_variantA", "/models/player/");
        #endregion
    }
    void Update()
    {
        //if (showSplash)
        //{
        //    PlaySplashScreen();
        //    return;
        //}
        //userInterface.SetActive(true);

        //if (testParser != null) Debug.Log("Ticks Parsed: " + testParser.ticksParsed + "/" + testParser.demoHeader.ticks);
        /*if (currentReplay != null)
        {
            currentReplay.Stream();
            RefreshPlayerList();
            if (replaySeeker != null)
            {
                replaySeeker.progress = (((float) currentReplay.seekIndex) / currentReplay.totalTicks);
                replaySeeker.text = currentReplay.seekIndex.ToString();
            }
        }*/
    }
    void OnApplicationQuit()
    {
        isQuitting = true;
    }
    private void OnDestroy()
    {
        /*if (GetComponent<SteamManager>() != null)
        {
            if (SteamManager.Initialized)
            {
                SteamAPI.Shutdown();
            }
        }*/
    }

    #region Testing Stuff
    private void ParseModel(string name, string location)
    {
        SourceModel.GrabModel(name, location);
    }
    private void ParseVPK(string location)
    {
        VPKParser vpk = new VPKParser(location);
        vpk.Parse();

        //loaded = ValveAudio.LoadRawMP3("mainmenu", vpk.LoadFile("/sound/music/valve_csgo_02/mainmenu.mp3"));
        //loaded = SourceAudio.LoadRawWAV("m1_finish", vpk.LoadFile("/sound/coop_radio/m1_finish.wav"));
        //loaded = SourceAudio.LoadRawWAV("opera", vpk.LoadFile("/sound/ambient/opera.wav"));

        //AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        //audioSource.loop = true;
        //audioSource.clip = loaded;
        //audioSource.Play();

        //loaded = SourceTexture.LoadVTFFile(vpk.LoadFile("/materials/de_nuke/nukfloorc_detaile.vtf"));
        //loaded = SourceTexture.LoadVTFFile(vpk.LoadFile("/materials/brick/infwllb_overlay_b.vtf"));
        //loaded = SourceTexture.LoadVTFFile(vpk.LoadFile("/materials/brick/brickwall031b_snow.vtf"));
        //loaded = SourceTexture.LoadVTFFile(vpk.LoadFile("/materials/ads/ad01.vtf"));

        //System.IO.File.WriteAllBytes("C:/Users/oxter/Documents/ad01.vtf", vpk.LoadFile("/materials/ads/ad01.vtf"));
        //System.IO.File.WriteAllBytes("C:/Users/oxter/Documents/opera.wav", vpk.LoadFile("/sound/ambient/opera.wav"));
        //System.IO.File.WriteAllBytes("C:/Users/oxter/Documents/mainmenu.mp3", vpk.LoadFile("sound/music/valve_csgo_02/mainmenu.mp3"));
    }
    //private void ParseDEM(string location)
    //{
    //    DemoParser demoParser = new DemoParser(new System.IO.FileStream(location, System.IO.FileMode.Open));
    //    demoParser.ParseHeader();
    //    demoParser.ParseToEnd();
    //}
    private void ParseFBX(string location)
    {
        Fbx.FbxDocument fbxDocument;
        using(System.IO.FileStream fbxFileStream = new System.IO.FileStream(location, System.IO.FileMode.Open))
        {
            Fbx.FbxBinaryReader fbxReader = new Fbx.FbxBinaryReader(fbxFileStream);
            fbxDocument = fbxReader.Read();
        }
        Debug.Log("Version " + fbxDocument.Version);
        DebugNodes(fbxDocument.Nodes);
    }
    private void DebugNodes(List<Fbx.FbxNode> nodeList)
    {
        foreach (Fbx.FbxNode fbxNode in nodeList)
        {
            if (fbxNode != null && !fbxNode.IsEmpty)
            {
                Debug.Log("Name: " + fbxNode.Name);
                Debug.Log("Value: " + fbxNode.Value);
                foreach (object property in fbxNode.Properties)
                {
                    Debug.Log("Property: " + property);
                }
                DebugNodes(fbxNode.Nodes);
            }
        }
    }
    #endregion

    #region General
    BSPMap currentlyViewedMap;
    public ListableButton browserSelection;
    public Button exitButton;
    public void Exit()
    {
        Application.Quit();
    }
    private bool CheckPlatform()
    {
        return (Application.isEditor || Application.isMobilePlatform || Application.isConsolePlatform || Application.isWebPlayer);
    }
    #endregion
    #region Replays Menu
    public Explorer replaysBrowser;
    public FileExplorer replayFilesBrowser;
    public Sprite demIcon;
    public void LoadReplay()
    {
        GameObject demoObject = new GameObject();
        DemoController demoController = demoObject.AddComponent<DemoController>();
        demoController.StartParsing((string)browserSelection.listableItem.value);
        replaysBrowser.AddItem(new ListableItem(demoController.demoFileName, "Replay", demIcon, demoController));
        replaysBrowser.Refresh();
    }
    #endregion
    #region Maps Menu
    public Explorer mapsBrowser;
    public FileExplorer mapFilesBrowser;
    public Sprite bspIcon;
    public void LoadMap()
    {
        LoadMap((string)browserSelection.listableItem.value);
    }
    public ListableItem LoadMap(string mapLocation)
    {
        BSPMap map;
        if (BSPMap.Parse(mapLocation, out map))
        {
            ListableItem mapItem = new ListableItem(map.mapName, "Map", bspIcon, map);
            mapsBrowser.AddItem(mapItem);
            mapsBrowser.Refresh();
            StartCoroutine(WaitForMap(mapItem));
            return mapItem;
        }
        return null;
    }
    private IEnumerator WaitForMap(ListableItem item)
    {
        while(!((BSPMap)item.value).IsDone)
        {
            item.SetProgressBarVisibility(true);
            item.SetPercent(((BSPMap)item.value).percentParsed);
            yield return null;
        }
        StartCoroutine(((BSPMap)item.value).MakeGameObject());
        while (!((BSPMap)item.value).completedParse)
        {
            item.SetProgressBarVisibility(true);
            item.SetPercent(((BSPMap)item.value).percentParsed);
            yield return null;
        }
        item.SetProgressBarVisibility(false);
    }
    public void ShowMap(bool isOn)
    {
        if (isOn) currentlyViewedMap = ((BSPMap)browserSelection.listableItem.value);
        currentlyViewedMap.mapGameObject.SetActive(isOn);
        if (!isOn) currentlyViewedMap = null;
    }
    public void RemoveMap()
    {
        BSPMap map = ((BSPMap)browserSelection.listableItem.value);
        map.Dispose();
        mapsBrowser.RemoveItem(browserSelection.listableItem);
        mapsBrowser.Refresh();
    }
    #endregion
    #region Settings Menu
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;
    public Toggle combinePrefToggle, averageTexturesToggle, decreaseTextureSizeToggle;
    public InputField maxSizeField;
    public Toggle useVPKToggle, useTexturesToggle, useMapsToggle, useModelsToggle, useSFXToggle;
    public InputField vpkDirField, texturesDirField, mapsDirField, modelsDirField, sfxDirField;
    public Button vpkBrowseButton, texturesBrowseButton, mapsBrowseButton, modelsBrowseButton, sfxBrowseButton;

    public void LoadSettings()
    {
        if (CheckPlatform())
        {
            exitButton.gameObject.SetActive(false);
            fullscreenToggle.gameObject.SetActive(false);
            resolutionDropdown.gameObject.SetActive(false);
        }

        ApplicationPreferences.LoadSavedPreferences();

        combinePrefToggle.isOn = ApplicationPreferences.combineMeshes;
        averageTexturesToggle.isOn = ApplicationPreferences.averageTextures;
        decreaseTextureSizeToggle.isOn = ApplicationPreferences.decreaseTextureSizes;
        maxSizeField.text = ApplicationPreferences.maxSizeAllowed.ToString();

        useVPKToggle.isOn = ApplicationPreferences.useVPK;
        useTexturesToggle.isOn = ApplicationPreferences.useTextures;
        useMapsToggle.isOn = ApplicationPreferences.useMaps;
        useModelsToggle.isOn = ApplicationPreferences.useModels;
        useSFXToggle.isOn = ApplicationPreferences.useSFX;

        vpkDirField.text = ApplicationPreferences.vpkDir;
        texturesDirField.text = ApplicationPreferences.texturesDir;
        mapsDirField.text = ApplicationPreferences.mapsDir;
        modelsDirField.text = ApplicationPreferences.modelsDir;
        sfxDirField.text = ApplicationPreferences.sfxDir;

        replayFilesBrowser.currentDirectory = ApplicationPreferences.currentReplaysDir;
        mapFilesBrowser.currentDirectory = ApplicationPreferences.currentMapsDir;

        fullscreenToggle.isOn = Screen.fullScreen;
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(new List<string>(System.Array.ConvertAll(Screen.resolutions, item => item.ToString())));
        resolutionDropdown.value = System.Array.IndexOf(Screen.resolutions, Screen.currentResolution);

        ApplyPreferencesInteractibility();
    }
    public void SaveSettings()
    {
        PlayerPrefs.SetInt(ApplicationPreferences.COMBINE_PREFS, (ApplicationPreferences.combineMeshes = combinePrefToggle.isOn) ? 1 : 0);
        PlayerPrefs.SetInt(ApplicationPreferences.AVERAGE_PREFS, (ApplicationPreferences.averageTextures = averageTexturesToggle.isOn) ? 1 : 0);
        PlayerPrefs.SetInt(ApplicationPreferences.DECREASE_PREFS, (ApplicationPreferences.decreaseTextureSizes = decreaseTextureSizeToggle.isOn) ? 1 : 0);
        PlayerPrefs.SetInt(ApplicationPreferences.MAX_SIZE, (ApplicationPreferences.maxSizeAllowed = System.Convert.ToInt32(maxSizeField.text)));

        PlayerPrefs.SetInt(ApplicationPreferences.USE_VPK, (ApplicationPreferences.useVPK = useVPKToggle.isOn) ? 1 : 0);
        PlayerPrefs.SetInt(ApplicationPreferences.USE_TEX, (ApplicationPreferences.useTextures = useTexturesToggle.isOn) ? 1 : 0);
        PlayerPrefs.SetInt(ApplicationPreferences.USE_MAPS, (ApplicationPreferences.useMaps = useMapsToggle.isOn) ? 1 : 0);
        PlayerPrefs.SetInt(ApplicationPreferences.USE_MODELS, (ApplicationPreferences.useModels = useModelsToggle.isOn) ? 1 : 0);
        PlayerPrefs.SetInt(ApplicationPreferences.USE_SFX, (ApplicationPreferences.useSFX = useSFXToggle.isOn) ? 1 : 0);

        PlayerPrefs.SetString(ApplicationPreferences.VPK_LOC, (ApplicationPreferences.vpkDir = vpkDirField.text));
        PlayerPrefs.SetString(ApplicationPreferences.TEX_LOC, (ApplicationPreferences.texturesDir = texturesDirField.text));
        PlayerPrefs.SetString(ApplicationPreferences.MAPS_LOC, (ApplicationPreferences.mapsDir = mapsDirField.text));
        PlayerPrefs.SetString(ApplicationPreferences.MODELS_LOC, (ApplicationPreferences.modelsDir = modelsDirField.text));
        PlayerPrefs.SetString(ApplicationPreferences.SFX_LOC, (ApplicationPreferences.sfxDir = sfxDirField.text));

        if (!CheckPlatform())
        {
            Resolution pickedResolution = Screen.resolutions[resolutionDropdown.value];
            Screen.SetResolution(pickedResolution.width, pickedResolution.height, fullscreenToggle.isOn, pickedResolution.refreshRate);
        }
        //Screen.fullScreen = fullscreenToggle.isOn;
    }
    public void ResetSettings()
    {
        ApplicationPreferences.ResetValues();
        LoadSettings();
    }

    public void ApplyPreferencesInteractibility()
    {
        decreaseTextureSizeToggle.interactable = !averageTexturesToggle.isOn;
        maxSizeField.interactable = !averageTexturesToggle.isOn;
        averageTexturesToggle.interactable = !decreaseTextureSizeToggle.isOn;

        vpkDirField.interactable = useVPKToggle.isOn;
        vpkBrowseButton.interactable = useVPKToggle.isOn;
        texturesDirField.interactable = useTexturesToggle.isOn;
        texturesBrowseButton.interactable = useTexturesToggle.isOn;
        mapsDirField.interactable = useMapsToggle.isOn;
        mapsBrowseButton.interactable = useMapsToggle.isOn;
        modelsDirField.interactable = useModelsToggle.isOn;
        modelsBrowseButton.interactable = useModelsToggle.isOn;
        sfxDirField.interactable = useSFXToggle.isOn;
        sfxBrowseButton.interactable = useSFXToggle.isOn;
    }
    #endregion

    public delegate IEnumerator CoroutineMethod();
    public void CoroutineRunner(CoroutineMethod toBeRun)
    {
        StartCoroutine(toBeRun());
    }
}
