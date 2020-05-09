using UnityEngine;
using UnityEngine.EventSystems;
using UnitySourceEngine;

using System;
using System.IO;
using System.Collections.Generic;
using Doozy.Engine.UI;

public class SettingsController : MonoBehaviour
{
    public static SettingsController settingsInScene { get; private set; }

    private const string USER = "User", RAN_BEFORE = "Ran_Before", SHOW_DEBUG_LOG = "Show_Debug_Log", MATCHES_LOC = "Matches_Location", GAME_LOC = "Game_Location", RESOURCE_PAKS = "Resource_Paks",
        AUTO_RESOURCE_PER_MAP = "Auto_Resource_Per_Map", SHOW_OVERVIEW = "Show_Overview", EXCLUDE_MAP_FACES_PERCENT = "Exclude_Map_Faces_Percent", EXCLUDE_MAP_MODELS_PERCENT = "Exclude_Map_Models_Percent", FLAT_TEXTURES = "Flat_Tex", MAX_TEXTURE_RESOLUTION = "Tex_Res",
        PERSONA_STATE = "Persona_State", SHOW_FRAME_RATE = "Show_Frame_Rate", TARGET_FRAME_RATE = "Target_Frame_Rate", RENDER_PERCENT = "Render_Percent", MODEL_DECIMATION_PERCENT = "Model_Decimation_Percent";

    public Sprite defaultAccountIcon;

    public static bool ranBefore { get; private set; }
    private static List<string> resourcePaks = new List<string>();
    public static string user;
    public static int personaState;
    public static bool showDebugLog, autoResourcePerMap, showOverview;
    public static string matchesLocation, gameLocation;
    public static bool showFrameRate;
    public static int targetFrameRate;
    public static float renderPercent;

    public UserViewController viewingCamera;
    public GameObject mainRenderCamera;
    public Material renderTextureMaterial;

    public GameObject frameRateObject;

    public UIDrawer navigatorView;
    public UIDrawer debugView;
    public UIButton debugViewToggle;

    public GameObject freeLookGuide, orbitGuide, firstPersonGuide;

    private GameObject previouslySelectedGameObject;
    public event SelectionChangedCallback onSelectionChanged;
    public delegate void SelectionChangedCallback(EventSystem eventSystem, GameObject previousSelection);

    private static Coroutine guideShowRoutine;

    private void Awake()
    {
        settingsInScene = this;
        #if UNITY_ANDROID
        //Screen.fullScreen = false;
        #endif
        LoadSettings();
    }
    private void OnEnable()
    {
        Doozy.Engine.Message.AddListener<Doozy.Engine.GameEventMessage>(OnGameEventReceived);
    }
    private void OnDisable()
    {
        Doozy.Engine.Message.RemoveListener<Doozy.Engine.GameEventMessage>(OnGameEventReceived);
    }
    private void Update()
    {
        if (previouslySelectedGameObject != EventSystem.current.currentSelectedGameObject)
        {
            onSelectionChanged?.Invoke(EventSystem.current, previouslySelectedGameObject);
            previouslySelectedGameObject = EventSystem.current.currentSelectedGameObject;
        }
    }

    public static void SaveSettings()
    {
        PlayerPrefs.SetInt(RAN_BEFORE, ranBefore ? 1 : 0);
        PlayerPrefs.SetInt(SHOW_FRAME_RATE, showFrameRate ? 1 : 0);
        PlayerPrefs.SetInt(TARGET_FRAME_RATE, targetFrameRate);
        PlayerPrefs.SetString(USER, user);
        PlayerPrefs.SetInt(SHOW_DEBUG_LOG, showDebugLog ? 1 : 0);
        PlayerPrefs.SetString(MATCHES_LOC, matchesLocation);
        PlayerPrefs.SetString(GAME_LOC, gameLocation);
        PlayerPrefs.SetInt(PERSONA_STATE, personaState);
        PlayerPrefs.SetInt(AUTO_RESOURCE_PER_MAP, autoResourcePerMap ? 1 : 0);
        PlayerPrefs.SetInt(SHOW_OVERVIEW, showOverview ? 1 : 0);
        PlayerPrefs.SetFloat(EXCLUDE_MAP_FACES_PERCENT, BSPMap.FaceLoadPercent);
        PlayerPrefs.SetFloat(EXCLUDE_MAP_MODELS_PERCENT, BSPMap.ModelLoadPercent);
        PlayerPrefs.SetInt(FLAT_TEXTURES, SourceTexture.averageTextures ? 1 : 0);
        PlayerPrefs.SetInt(MAX_TEXTURE_RESOLUTION, SourceTexture.maxTextureSize);
        PlayerPrefs.SetString(RESOURCE_PAKS, ListToCSV(resourcePaks));
        PlayerPrefs.SetFloat(RENDER_PERCENT, renderPercent);
        PlayerPrefs.SetFloat(MODEL_DECIMATION_PERCENT, SourceModel.decimationPercent);

        ApplyTargetFrameRate();
        CreateDirectoriesIfNotPresent();
        UpdateParsers();
        UpdateDebugView();
        UpdateRenderPercent();
    }
    public static void LoadSettings()
    {
        ranBefore = PlayerPrefs.GetInt(RAN_BEFORE) != 0;
        if (!ranBefore)
            SetDefaults();

        showFrameRate = PlayerPrefs.GetInt(SHOW_FRAME_RATE) != 0;
        targetFrameRate = PlayerPrefs.GetInt(TARGET_FRAME_RATE);
        user = PlayerPrefs.GetString(USER);
        showDebugLog = PlayerPrefs.GetInt(SHOW_DEBUG_LOG) != 0;
        matchesLocation = PlayerPrefs.GetString(MATCHES_LOC);
        gameLocation = PlayerPrefs.GetString(GAME_LOC);
        personaState = PlayerPrefs.GetInt(PERSONA_STATE);
        autoResourcePerMap = PlayerPrefs.GetInt(AUTO_RESOURCE_PER_MAP) != 0;
        showOverview = PlayerPrefs.GetInt(SHOW_OVERVIEW) != 0;
        BSPMap.FaceLoadPercent = PlayerPrefs.GetFloat(EXCLUDE_MAP_FACES_PERCENT);
        BSPMap.ModelLoadPercent = PlayerPrefs.GetFloat(EXCLUDE_MAP_MODELS_PERCENT);
        SourceTexture.averageTextures = PlayerPrefs.GetInt(FLAT_TEXTURES) != 0;
        SourceTexture.maxTextureSize = PlayerPrefs.GetInt(MAX_TEXTURE_RESOLUTION);
        resourcePaks.AddRange(CSVToList(PlayerPrefs.GetString(RESOURCE_PAKS)));
        renderPercent = PlayerPrefs.GetFloat(RENDER_PERCENT);
        SourceModel.decimationPercent = PlayerPrefs.GetFloat(MODEL_DECIMATION_PERCENT);

        ApplyTargetFrameRate();
        CreateDirectoriesIfNotPresent();
        UpdateParsers();
        UpdateDebugView();
        UpdateRenderPercent();
    }
    public static void SetDefaults()
    {
        ranBefore = true;
        showDebugLog = false;

        targetFrameRate = 30;
        showFrameRate = false;
        gameLocation = GetDefaultGOLocation();
        matchesLocation = Path.Combine(gameLocation, "csgo", "replays").Replace('\\', '/');
        personaState = (int)SteamKit2.EPersonaState.Invisible;
        autoResourcePerMap = true;
        showOverview = true;
        BSPMap.FaceLoadPercent = 1;
        SourceTexture.averageTextures = false;
        SourceModel.decimationPercent = 0;

        #if UNITY_ANDROID
        BSPMap.ModelLoadPercent = 0.5f;
        SourceTexture.maxTextureSize = 128;
        renderPercent = 0.5f;
        #else
        BSPMap.ModelLoadPercent = 1;
        SourceTexture.maxTextureSize = 2048;
        renderPercent = 1;
        #endif

        SaveSettings();
    }
    private static string GetDefaultGOLocation()
    {
        string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Counter-Strike Global Offensive");
        if (!Directory.Exists(defaultPath))
            defaultPath = Path.Combine(Application.persistentDataPath, "Counter-Strike Global Offensive");

        return defaultPath.Replace("\\", "/");
    }
    public static string GetOverviewLoc()
    {
        return !string.IsNullOrEmpty(gameLocation) ? Path.Combine(gameLocation, "csgo", "resource", "overviews").Replace("\\", "/") : null;
    }
    public static string GetMapsLoc()
    {
        return !string.IsNullOrEmpty(gameLocation) ? Path.Combine(gameLocation, "csgo", "maps").Replace("\\", "/") : null;
    }
    public static string GetVPKLoc()
    {
        return !string.IsNullOrEmpty(gameLocation) ? Path.Combine(gameLocation, "csgo").Replace("\\", "/") : null;
    }

    public static void ShowTouchGuide(UserViewController.ViewMode viewMode)
    {
        if (guideShowRoutine != null)
            settingsInScene.StopCoroutine(guideShowRoutine);

        settingsInScene.freeLookGuide.SetActive(viewMode == UserViewController.ViewMode.freeLook);
        settingsInScene.orbitGuide.SetActive(viewMode == UserViewController.ViewMode.thirdPerson);
        settingsInScene.firstPersonGuide.SetActive(viewMode == UserViewController.ViewMode.firstPerson);
        guideShowRoutine = settingsInScene.StartCoroutine(UnityHelpers.CommonRoutines.WaitToDoAction((success) =>
        {
            settingsInScene.freeLookGuide.SetActive(false);
            settingsInScene.orbitGuide.SetActive(false);
            settingsInScene.firstPersonGuide.SetActive(false);
        }));
    }
    public static void AddPak(string path)
    {
        string pakName = path.Replace("/", "\\");
        if (!resourcePaks.Contains(pakName))
            resourcePaks.Add(pakName);
    }
    public static void RemovePak(string path)
    {
        string pakName = path.Replace("/", "\\");
        var pakIndex = resourcePaks.IndexOf(pakName);
        if (pakIndex > -1)
            resourcePaks.RemoveAt(pakIndex);
    }
    public static bool HasPak(string path)
    {
        return resourcePaks.Contains(path.Replace("/", "\\"));
    }
    public static string[] GetTrackedPaks()
    {
        return resourcePaks.ToArray();
    }
    public static string ListToCSV(IEnumerable<string> list)
    {
        string csv = "";
        foreach (var item in list)
        {
            csv += item + ",";
        }
        return csv;
    }
    public static string[] CSVToList(string csv)
    {
        return csv.Split(',');
    }

    public static void RememberUser(string username)
    {
        user = username;
        PlayerPrefs.SetString(USER, user);
    }
    public static void ForgetUser()
    {
        user = "";
        PlayerPrefs.SetString(USER, user);
    }

    public void SetViewingCamera(bool onOff)
    {
        viewingCamera.gameObject.SetActive(onOff);
        mainRenderCamera.SetActive(onOff);
    }
    private void OnGameEventReceived(Doozy.Engine.GameEventMessage message)
    {
        if (message.EventName.Equals("EnableNavigator"))
            SetNavigatorDraggable(true);
        else if (message.EventName.Equals("DisableNavigator"))
            SetNavigatorDraggable(false);
        if (message.EventName.Equals("EnableDebugView"))
            SetDebugDraggable(true);
        else if (message.EventName.Equals("DisableDebugView"))
            SetDebugDraggable(false);
    }
    public void SetNavigatorDraggable(bool onOff)
    {
        navigatorView.DetectGestures = onOff;
    }
    public void SetDebugDraggable(bool onOff)
    {
        debugView.DetectGestures = showDebugLog && onOff;
    }
    public static string LogonLoc()
    {
        string logonDir = Application.persistentDataPath;
        if (!Directory.Exists(logonDir))
            Directory.CreateDirectory(logonDir);
        return logonDir;
    }

    public void OpenPrivacyPolicy()
    {
        Application.OpenURL("https://www.privacypolicies.com/generic/");
    }
    private static void UpdateRenderPercent()
    {
        int pixelSize = Mathf.RoundToInt(Mathf.Max(Screen.width, Screen.height) * Mathf.Clamp(renderPercent, 0.01f, 1));
        settingsInScene.viewingCamera.viewingCamera.targetTexture.Release();
        settingsInScene.viewingCamera.viewingCamera.targetTexture = new RenderTexture(pixelSize, pixelSize, 24, RenderTextureFormat.RGB565);
        settingsInScene.viewingCamera.viewingCamera.targetTexture.antiAliasing = 1;
        settingsInScene.viewingCamera.viewingCamera.targetTexture.anisoLevel = 0;
        //settingsInScene.viewingCamera.viewingCamera.targetTexture.useMipMap = false;
        settingsInScene.viewingCamera.viewingCamera.targetTexture.useDynamicScale = false;
        settingsInScene.viewingCamera.viewingCamera.targetTexture.filterMode = FilterMode.Point;
        settingsInScene.renderTextureMaterial.mainTexture = settingsInScene.viewingCamera.viewingCamera.targetTexture;
    }
    private static void ApplyTargetFrameRate()
    {
        Application.targetFrameRate = targetFrameRate;
        settingsInScene.frameRateObject.SetActive(showFrameRate);
    }
    private static void UpdateDebugView()
    {
        settingsInScene.debugViewToggle.gameObject.SetActive(showDebugLog);
        if (!showDebugLog)
            settingsInScene.debugView.Close();
        settingsInScene.debugView.DetectGestures = showDebugLog;
    }

    private static void UpdateParsers()
    {
        BSPMap.vpkLoc = GetVPKLoc();
    }
    private static void CreateDirectoriesIfNotPresent()
    {
        if (!Directory.Exists(gameLocation))
            Directory.CreateDirectory(gameLocation);

        string mapsLoc = GetMapsLoc();
        string overviewsLoc = GetOverviewLoc();
        if (!Directory.Exists(mapsLoc))
            Directory.CreateDirectory(mapsLoc);
        if (!Directory.Exists(overviewsLoc))
            Directory.CreateDirectory(overviewsLoc);
    }
}
