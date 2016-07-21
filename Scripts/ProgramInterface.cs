using UnityEngine;
using OxGUI;
using System.Collections;
using System.Collections.Generic;
using DemoInfo;
using System;
using System.Linq;
//using Steamworks;

public class ProgramInterface : MonoBehaviour
{
    public enum Menu
    {
        Main = 1,
        Live,
        Replays,
        ReplayInterface,
        Maps,
        ExploreInterface,
        Settings,
        MapDir,
        TextureDir,
        ModelDir,
    }

    Menu currentMenu = Menu.Main;
    OxMenu mainMenu;
    OxTabbedPanel settingsMenu;
    OxCheckbox manualFontSizeCheckbox;
    //OxPanel manualFontPanel;
    OxTextbox manualFontSizeBox;
    OxScrollbar manualFontSizeScroll;
    OxButton viewLiveButton, viewReplaysButton, viewMapsButton, settingsButton, exitButton;
    OxMenu liveMenu;
    OxTextbox addressBox;
    OxButton connectButton;
    OxPanel replaysMenu;
    OxListFileSelector replayFileList;
    OxMenu loadedReplayList;
    OxButton importReplayButton, watchReplayButton, removeReplayButton;
    OxWindow replayInterface, exploreInterface;
    //OxWindow interfaceControlWindow;
    OxMenu playerList;
    OxScrollbar replaySeeker;
    OxButton playButton;
    OxPanel mapMenu;
    OxListFileSelector mapFileChooser;
    OxButton loadMapButton, nextMapButton, prevMapButton, exploreMapButton;
    OxCheckbox fullscreenCheckBox, averageTexturesCheckBox, decreaseTexturesCheckBox, combineMeshesCheckBox;
    //OxLabel maxTextureSizeLabel, texturesLocationLabel, mapsLocationLabel, modelsLocationLabel, sfxLocationLabel;
    OxTextbox maxTextureSizeTextBox, texturesLocationTextBox, mapsLocationTextBox, modelsLocationTextBox, sfxLocationTextBox;
    OxButton browseTexturesLocationButton, browseMapsLocationButton, browseModelsLocationButton, browseSFXLocationButton;
    OxListFileSelectorPrompt textureDirChooser, mapDirChooser, modelDirChooser, sfxDirChooser;
    //OxButton liveMenuBackButton, replaysMenuBackButton, replayInterfaceBackButton, mapsMenuBackButton, exploreInterfaceBackButton, settingsMenuBackButton;
    //Replay aReplay;
    public Texture2D loaded;

    private bool showSplash = true;
    private int splashFrame = 1;
    private float splashFPS = 24, lastFrameChange = 0;

    Demo currentReplay;
    int currentMap = 0;

	// Use this for initialization
	void Start ()
    {
        OxBase.autoSizeAllText = true;
        ApplicationPreferences.LoadSavedPreferences();
        SourceTexture.LoadDefaults();

        //if (false && (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxPlayer))
        //{
        //    gameObject.AddComponent<SteamManager>();
        //}
        MakeMenus();

        ParseVPK(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)").Replace("\\", "/") + "/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/pak01_dir.vpk");
        //Demo.ConvertToHexString("Ahmed");
        //VPKParser vpkTest = new VPKParser(new FileStream("D:/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/pak01_dir.vpk", FileMode.Open));
        //Debug.Log(vpkTest.Parse());
        //ParseMDL();
        //ParseVVD();
        //ParseModel("ctm_fbi", "C:/Users/oxter/Documents/csgo/models/player/");
	}

    // Update is called once per frame
    void Update()
    {
        if (currentReplay != null)
        {
            currentReplay.Stream();
            RefreshPlayerList();
            if (replaySeeker != null)
            {
                replaySeeker.progress = (((float) currentReplay.seekIndex) / currentReplay.totalTicks);
                replaySeeker.text = currentReplay.seekIndex.ToString();
            }
        }
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

    private void ParseModel(string name, string location)
    {
        SourceModel.GrabModel(name, location);
    }
    private void ParseVPK(string location)
    {
        VPKParser vpk = new VPKParser(location);
        vpk.Parse();
        //loaded = SourceTexture.LoadVTFFile(vpk.LoadFile("/materials/de_nuke/nukfloorc_detaile.vtf"));
        loaded = SourceTexture.LoadVTFFile(vpk.LoadFile("/materials/brick/infwllb_overlay_b.vtf"));
        //loaded = SourceTexture.LoadVTFFile(vpk.LoadFile("/materials/brick/brickwall031b_snow.vtf"));
        //loaded = SourceTexture.LoadVTFFile(vpk.LoadFile("/materials/ads/ad01.vtf"));
        //System.IO.File.WriteAllBytes("C:/Users/oxter/Documents/ad01.vtf", vpk.LoadFile("/materials/ads/ad01.vtf"));
        //System.IO.File.WriteAllBytes("C:/Users/oxter/Documents/opera.wav", vpk.LoadFile("/sound/ambient/opera.wav"));
    }

    //string demLocation = "/storage/emulated/0/Download/CSGO/replays/Replay1.dem";
    //string demLocation = "C:/Users/Oxters/Downloads/replays/Replay1.dem";

    void OnGUI()
    {
        if (showSplash)
        {
            PlaySplashScreen();
            return;
        }

        if (currentMenu == Menu.Main) MainMenu();
        else if (currentMenu == Menu.Live) LiveMenu();
        else if (currentMenu == Menu.Replays) ReplaysMenu();
        else if (currentMenu == Menu.ReplayInterface) ReplayInterfaceMenu();
        else if (currentMenu == Menu.Maps) MapsMenu();
        else if (currentMenu == Menu.ExploreInterface) ExploreInterfaceMenu();
        else if (currentMenu == Menu.Settings) SettingsMenu();
        else if (currentMenu == Menu.MapDir) MapDirBrowser();
        else if (currentMenu == Menu.TextureDir) TextureDirBrowser();
        else if (currentMenu == Menu.ModelDir) ModelDirBrowser();
    }
    private void PlaySplashScreen()
    {
        Texture2D currentFrame = Resources.Load<Texture2D>("Textures/Splash/SplashFrame" + splashFrame);
        if (currentFrame != null)
        {
            int newWidth = currentFrame.width, newHeight = currentFrame.height;
            if (Screen.width < Screen.height)
            {
                newWidth = (int)(Screen.width * 0.25f);
                newHeight = (int)(currentFrame.height / (currentFrame.width / ((float)newWidth)));
            }
            else
            {
                newHeight = (int)(Screen.height * 0.25f);
                newWidth = (int)(currentFrame.width / (currentFrame.height / ((float)newHeight)));
            }
            TextureScale.Point(currentFrame, newWidth, newHeight);
            GUI.DrawTexture(new Rect(Screen.width / 2f - currentFrame.width / 2f, Screen.height / 2f - currentFrame.height / 2f, currentFrame.width, currentFrame.height), currentFrame);
            if (Time.time - lastFrameChange >= 1 / splashFPS)
            {
                splashFrame++;
                lastFrameChange = Time.time;
            }
        }
        else
        {
            Camera.main.backgroundColor = new Color((243f / 255f), (179f / 255f), (73f / 255f));
            showSplash = false;
        }
    }

    private void MakeMenus()
    {
        #region Main Menu
        mainMenu = new OxMenu(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        mainMenu.Reposition(new Vector2((Screen.width / 2f) - (mainMenu.width / 2f), (Screen.height / 2f) - (mainMenu.height / 2f)));

        viewLiveButton = new OxButton("Live");
        viewReplaysButton = new OxButton("Replays");
        viewMapsButton = new OxButton("Maps");
        settingsButton = new OxButton("Settings");
        exitButton = new OxButton("Exit");
        viewLiveButton.clicked += Button_clicked;
        viewReplaysButton.clicked += Button_clicked;
        viewMapsButton.clicked += Button_clicked;
        settingsButton.clicked += Button_clicked;
        exitButton.clicked += Button_clicked;
        mainMenu.AddItems(viewLiveButton, viewReplaysButton, viewMapsButton, settingsButton, exitButton);
        mainMenu.itemsShown = mainMenu.itemsCount;
        #endregion

        #region Live Menu
        liveMenu = new OxMenu(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        liveMenu.Reposition(new Vector2((Screen.width / 2f) - (liveMenu.width / 2f), (Screen.height / 2f) - (liveMenu.height / 2f)));

        addressBox = new OxTextbox("127.0.0.1");
        connectButton = new OxButton("Connect");
        OxButton liveMenuBackButton = new OxButton("Back");

        liveMenu.AddItems(addressBox, connectButton, liveMenuBackButton);
        liveMenu.itemsShown = liveMenu.itemsCount;

        liveMenuBackButton.clicked += Button_clicked;
        connectButton.clicked += Button_clicked;

        liveMenuBackButton.elementFunction = OxHelpers.ElementType.Back;
        #endregion

        #region Replays Menu
        replaysMenu = new OxPanel(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(7, 5), new Vector2(0.9f, 0.7f)));
        replaysMenu.Reposition(new Vector2((Screen.width / 2f) - (replaysMenu.width / 2f), (Screen.height / 2f) - (replaysMenu.height / 2f)));

        replayFileList = new OxListFileSelector();
        loadedReplayList = new OxMenu();
        watchReplayButton = new OxButton("Watch");
        importReplayButton = new OxButton("Import");
        removeReplayButton = new OxButton("Remove");
        OxButton replaysMenuBackButton = new OxButton("Back");

        watchReplayButton.clicked += Button_clicked;
        importReplayButton.clicked += Button_clicked;
        removeReplayButton.clicked += Button_clicked;
        replaysMenuBackButton.clicked += Button_clicked;
        replaysMenu.AddItems(replayFileList, loadedReplayList, watchReplayButton, importReplayButton, removeReplayButton, replaysMenuBackButton);

        AppearanceInfo dimensions = replaysMenu.CurrentAppearanceInfo();
        replaysMenuBackButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.1f);
        replaysMenuBackButton.position = new Vector2(dimensions.centerWidth - replaysMenuBackButton.width, dimensions.centerHeight - replaysMenuBackButton.height);
        replaysMenuBackButton.elementFunction = OxHelpers.ElementType.Back;
        replaysMenuBackButton.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Right);

        removeReplayButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.1f);
        removeReplayButton.position = new Vector2(dimensions.centerWidth - removeReplayButton.width, dimensions.centerHeight - removeReplayButton.height - replaysMenuBackButton.height);
        removeReplayButton.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Right);

        importReplayButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.1f);
        importReplayButton.position = new Vector2(dimensions.centerWidth - importReplayButton.width, dimensions.centerHeight - removeReplayButton.height - importReplayButton.height - replaysMenuBackButton.height);
        importReplayButton.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Right);

        replayFileList.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight - replaysMenuBackButton.height - removeReplayButton.height - importReplayButton.height);
        replayFileList.position = new Vector2(dimensions.centerWidth - replayFileList.width, 0);
        replayFileList.AddExtensions("dem");
        replayFileList.anchor = (OxHelpers.Anchor.Right | OxHelpers.Anchor.Top | OxHelpers.Anchor.Bottom);

        loadedReplayList.horizontal = true;
        loadedReplayList.size = new Vector2(dimensions.centerWidth - replayFileList.width, dimensions.centerHeight * 0.2f);
        loadedReplayList.position = Vector2.zero;
        loadedReplayList.anchor = (OxHelpers.Anchor.Left | OxHelpers.Anchor.Right | OxHelpers.Anchor.Top);

        watchReplayButton.size = new Vector2(dimensions.centerWidth * 0.1f, removeReplayButton.height + importReplayButton.height);
        watchReplayButton.position = new Vector2(((dimensions.centerWidth - removeReplayButton.width) / 2f) - (watchReplayButton.width / 2f), dimensions.centerHeight - watchReplayButton.height);
        watchReplayButton.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Left);
        #endregion

        #region Replay Interface
        replayInterface = new OxWindow(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(5, 2), new Vector2(0.5f, 0.2f)));
        replayInterface.Reposition(new Vector2((Screen.width / 2f) - (replayInterface.width / 2f), (Screen.height) - (replayInterface.height) - 10));

        playerList = new OxMenu();
        playButton = new OxButton("Play");
        replaySeeker = new OxScrollbar();
        OxButton replayInterfaceBackButton = new OxButton("Back");

        replayInterface.AddItems(playerList, playButton, replaySeeker, replayInterfaceBackButton);

        playerList.selectionChanged += playerList_selectionChanged;
        playButton.clicked += Button_clicked;
        replaySeeker.scrollValueChanged += replaySeeker_valueChanged;
        replayInterfaceBackButton.clicked += Button_clicked;

        dimensions = replayInterface.CurrentAppearanceInfo();
        replayInterfaceBackButton.size = new Vector2(dimensions.centerHeight * 0.3f, dimensions.centerHeight * 0.3f);
        replayInterfaceBackButton.position = new Vector2(dimensions.centerWidth - replayInterfaceBackButton.width - 5, dimensions.centerHeight - replayInterfaceBackButton.height - 5);
        replayInterfaceBackButton.elementFunction = OxHelpers.ElementType.Back;
        replayInterfaceBackButton.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Right);

        replaySeeker.size = new Vector2(dimensions.centerWidth * 0.9f, dimensions.centerHeight * 0.3f);
        replaySeeker.position = new Vector2((dimensions.centerWidth / 2f) - (replaySeeker.width / 2f), 5);
        replaySeeker.anchor = (OxHelpers.Anchor.Top | OxHelpers.Anchor.Left | OxHelpers.Anchor.Right);

        playButton.size = new Vector2(dimensions.centerHeight * 0.15f, dimensions.centerHeight * 0.15f);
        playButton.position = new Vector2((dimensions.centerWidth / 2f) - (playButton.width / 2f), (dimensions.centerHeight / 2f) - (playButton.height / 2f));
        playButton.anchor = (OxHelpers.Anchor.Top | OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Left | OxHelpers.Anchor.Right);

        playerList.horizontal = true;
        playerList.size = new Vector2((dimensions.centerWidth - replayInterfaceBackButton.width) * 0.9f, dimensions.centerHeight * 0.3f);
        playerList.position = new Vector2(((dimensions.centerWidth - replayInterfaceBackButton.width - 5) / 2f) - (playerList.width / 2f), dimensions.centerHeight - playerList.height - 5);
        playerList.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Left | OxHelpers.Anchor.Right);
        #endregion

        #region Maps Menu
        mapMenu = new OxPanel(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(7, 5), new Vector2(0.9f, 0.7f)));
        mapMenu.Reposition(new Vector2((Screen.width / 2f) - (mapMenu.width / 2f), (Screen.height / 2f) - (mapMenu.height / 2f)));

        mapFileChooser = new OxListFileSelector();
        loadMapButton = new OxButton("Import Map");
        nextMapButton = new OxButton("Next");
        prevMapButton = new OxButton("Previous");
        exploreMapButton = new OxButton("Explore");
        OxButton mapsMenuBackButton = new OxButton("Back");
        mapMenu.AddItems(mapFileChooser, loadMapButton, nextMapButton, prevMapButton, exploreMapButton, mapsMenuBackButton);

        loadMapButton.clicked += Button_clicked;
        nextMapButton.clicked += Button_clicked;
        prevMapButton.clicked += Button_clicked;
        exploreMapButton.clicked += Button_clicked;
        mapsMenuBackButton.clicked += Button_clicked;

        dimensions = mapMenu.CurrentAppearanceInfo();
        mapsMenuBackButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.1f);
        mapsMenuBackButton.position = new Vector2(dimensions.centerWidth - mapsMenuBackButton.width, dimensions.centerHeight - mapsMenuBackButton.height);
        mapsMenuBackButton.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Right);
        mapsMenuBackButton.elementFunction = OxHelpers.ElementType.Back;

        loadMapButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.1f);
        loadMapButton.position = new Vector2(dimensions.centerWidth - loadMapButton.width, dimensions.centerHeight - loadMapButton.height - mapsMenuBackButton.height);
        loadMapButton.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Right);

        mapFileChooser.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight - loadMapButton.height - mapsMenuBackButton.height);
        mapFileChooser.position = new Vector2(dimensions.centerWidth - mapFileChooser.width, 0);
        mapFileChooser.anchor = (OxHelpers.Anchor.Right | OxHelpers.Anchor.Top | OxHelpers.Anchor.Bottom);
        mapFileChooser.currentDirectory = (ApplicationPreferences.mapsDir != null && ApplicationPreferences.mapsDir.Length > 0) ? ApplicationPreferences.mapsDir : "";
        mapFileChooser.AddExtensions("bsp");

        exploreMapButton.size = new Vector2(dimensions.centerWidth * 0.25f, dimensions.centerHeight * 0.25f);
        exploreMapButton.position = new Vector2(((dimensions.centerWidth - mapsMenuBackButton.width) / 2f) - (exploreMapButton.width / 2f), dimensions.centerHeight - exploreMapButton.height);
        exploreMapButton.anchor = (OxHelpers.Anchor.Bottom);

        nextMapButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.25f);
        nextMapButton.position = new Vector2(exploreMapButton.x + exploreMapButton.width, dimensions.centerHeight - nextMapButton.height);
        nextMapButton.anchor = (OxHelpers.Anchor.Bottom);

        prevMapButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.25f);
        prevMapButton.position = new Vector2(exploreMapButton.x - prevMapButton.width, dimensions.centerHeight - prevMapButton.height);
        prevMapButton.anchor = (OxHelpers.Anchor.Bottom);
        #endregion

        #region ExploreInterface
        exploreInterface = new OxWindow(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(5, 2), new Vector2(0.5f, 0.2f)));
        exploreInterface.Reposition(new Vector2((Screen.width / 2f) - (replayInterface.width / 2f), (Screen.height) - (replayInterface.height) - 10));

        OxButton exploreInterfaceBackButton = new OxButton("Back");
        exploreInterfaceBackButton.elementFunction = OxHelpers.ElementType.Back;
        exploreInterfaceBackButton.clicked += Button_clicked;
        exploreInterface.AddItems(exploreInterfaceBackButton);

        dimensions = exploreInterface.CurrentAppearanceInfo();
        exploreInterfaceBackButton.size = new Vector2(dimensions.centerHeight * 0.5f, dimensions.centerHeight * 0.5f);
        exploreInterface.position = new Vector2((dimensions.centerWidth / 2f) - (exploreInterfaceBackButton.width / 2f), (dimensions.centerHeight / 2f) - (exploreInterfaceBackButton.height / 2f));
        #endregion

        #region Settings Menu
        settingsMenu = new OxTabbedPanel(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(7, 5), new Vector2(0.9f, 0.7f)));
        settingsMenu.Reposition(new Vector2((Screen.width / 2f) - (settingsMenu.width / 2f), (Screen.height / 2f) - (settingsMenu.height / 2f)));

        OxPanel generalTab = settingsMenu.AddTab("General");
        OxPanel resourcesTab = settingsMenu.AddTab("Resources");
        OxPanel optimizationsTab = settingsMenu.AddTab("Optimizations");

        fullscreenCheckBox = new OxCheckbox("Fullscreen", ApplicationPreferences.fullscreen);
        manualFontSizeCheckbox = new OxCheckbox("Manual Font", ApplicationPreferences.manualFontSize);
        manualFontSizeBox = new OxTextbox(ApplicationPreferences.fontSize.ToString());
        manualFontSizeScroll = new OxScrollbar();
        manualFontSizeScroll.progress = (ApplicationPreferences.fontSize - OxBase.MIN_FONT_SIZE) / ((float)(OxBase.MAX_FONT_SIZE - OxBase.MIN_FONT_SIZE)); 
        combineMeshesCheckBox = new OxCheckbox("Combine Map Meshes", ApplicationPreferences.combineMeshes);
        averageTexturesCheckBox = new OxCheckbox("Average Textures", ApplicationPreferences.averageTextures);
        decreaseTexturesCheckBox = new OxCheckbox("Decrease Texture Sizes", ApplicationPreferences.decreaseTextureSizes);
        OxLabel maxTextureSizeLabel = new OxLabel("Max Size", Color.black, OxHelpers.Alignment.Center);
        OxLabel texturesLocationLabel = new OxLabel("Textures Location", Color.black, OxHelpers.Alignment.Center);
        OxLabel mapsLocationLabel = new OxLabel("Maps Location", Color.black, OxHelpers.Alignment.Center);
        OxLabel modelsLocationLabel = new OxLabel("Models Location", Color.black, OxHelpers.Alignment.Center);
        OxLabel sfxLocationLabel = new OxLabel("SFX Location", Color.black, OxHelpers.Alignment.Center);
        maxTextureSizeTextBox = new OxTextbox(ApplicationPreferences.maxSizeAllowed.ToString());
        texturesLocationTextBox = new OxTextbox(ApplicationPreferences.texturesDir);
        browseTexturesLocationButton = new OxButton("Browse");
        mapsLocationTextBox = new OxTextbox(ApplicationPreferences.mapsDir);
        browseMapsLocationButton = new OxButton("Browse");
        modelsLocationTextBox = new OxTextbox(ApplicationPreferences.modelsDir);
        browseModelsLocationButton = new OxButton("Browse");
        sfxLocationTextBox = new OxTextbox("");
        browseSFXLocationButton = new OxButton("Browse");
        OxButton generalTabSettingsBackButton = new OxButton("Back");
        OxButton resourcesTabSettingsBackButton = new OxButton("Back");
        OxButton optimizationsTabSettingsBackButton = new OxButton("Back");

        fullscreenCheckBox.checkboxSwitched += CheckBox_Switched;
        manualFontSizeCheckbox.checkboxSwitched += CheckBox_Switched;
        combineMeshesCheckBox.checkboxSwitched += CheckBox_Switched;
        averageTexturesCheckBox.checkboxSwitched += CheckBox_Switched;
        decreaseTexturesCheckBox.checkboxSwitched += CheckBox_Switched;
        manualFontSizeBox.textChanged += TextBox_textChanged;
        maxTextureSizeTextBox.textChanged += TextBox_textChanged;
        texturesLocationTextBox.textChanged += TextBox_textChanged;
        mapsLocationTextBox.textChanged += TextBox_textChanged;
        modelsLocationTextBox.textChanged += TextBox_textChanged;
        sfxLocationTextBox.textChanged += TextBox_textChanged;
        manualFontSizeScroll.scrollValueChanged += Scrollbar_scrollValueChanged;
        browseTexturesLocationButton.clicked += Button_clicked;
        browseMapsLocationButton.clicked += Button_clicked;
        browseModelsLocationButton.clicked += Button_clicked;
        browseSFXLocationButton.clicked += Button_clicked;
        generalTabSettingsBackButton.clicked += Button_clicked;
        generalTabSettingsBackButton.elementFunction = OxHelpers.ElementType.Back;
        resourcesTabSettingsBackButton.clicked += Button_clicked;
        resourcesTabSettingsBackButton.elementFunction = OxHelpers.ElementType.Back;
        optimizationsTabSettingsBackButton.clicked += Button_clicked;
        optimizationsTabSettingsBackButton.elementFunction = OxHelpers.ElementType.Back;

        dimensions = generalTab.CurrentAppearanceInfo();
        AppearanceInfo innerPanelDimensions;

        #region General Tab
        OxPanel screenSettingsPanel = new OxPanel();
        generalTab.AddItems(screenSettingsPanel);
        screenSettingsPanel.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.4f);
        screenSettingsPanel.position = Vector2.zero;
        screenSettingsPanel.anchor = (OxHelpers.Anchor.Left | OxHelpers.Anchor.Top);
        screenSettingsPanel.AddItems(fullscreenCheckBox);

        innerPanelDimensions = screenSettingsPanel.CurrentAppearanceInfo();

        fullscreenCheckBox.size = new Vector2(innerPanelDimensions.centerWidth, innerPanelDimensions.centerHeight * 0.3f);
        fullscreenCheckBox.position = Vector2.zero;
        fullscreenCheckBox.anchor = (OxHelpers.Anchor.Top | OxHelpers.Anchor.Left | OxHelpers.Anchor.Right);

        OxPanel fontSettingsPanel = new OxPanel();
        generalTab.AddItems(fontSettingsPanel);
        fontSettingsPanel.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.4f);
        fontSettingsPanel.position = new Vector2(dimensions.centerWidth - fontSettingsPanel.width, 0);
        fontSettingsPanel.anchor = (OxHelpers.Anchor.Right | OxHelpers.Anchor.Top);
        fontSettingsPanel.AddItems(manualFontSizeCheckbox, manualFontSizeBox, manualFontSizeScroll);

        innerPanelDimensions = fontSettingsPanel.CurrentAppearanceInfo();

        manualFontSizeCheckbox.size = new Vector2(innerPanelDimensions.centerWidth, innerPanelDimensions.centerHeight * 0.3f);
        manualFontSizeCheckbox.position = Vector2.zero;
        manualFontSizeCheckbox.anchor = (OxHelpers.Anchor.Top | OxHelpers.Anchor.Left | OxHelpers.Anchor.Right);

        manualFontSizeBox.size = new Vector2(innerPanelDimensions.centerWidth, innerPanelDimensions.centerHeight * 0.4f);
        manualFontSizeBox.position = new Vector2(0, manualFontSizeCheckbox.height);
        manualFontSizeBox.anchor = (OxHelpers.Anchor.Left | OxHelpers.Anchor.Right);

        manualFontSizeScroll.size = new Vector2(innerPanelDimensions.centerWidth, innerPanelDimensions.centerHeight * 0.3f);
        manualFontSizeScroll.position = new Vector2(0, manualFontSizeCheckbox.height + manualFontSizeBox.height);
        manualFontSizeScroll.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Left | OxHelpers.Anchor.Right);

        generalTab.AddItems(generalTabSettingsBackButton);
        generalTabSettingsBackButton.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.15f);
        generalTabSettingsBackButton.position = new Vector2((dimensions.centerWidth / 2f) - (generalTabSettingsBackButton.width / 2f), dimensions.centerHeight - generalTabSettingsBackButton.height - 5f);
        generalTabSettingsBackButton.anchor = (OxHelpers.Anchor.Bottom | OxHelpers.Anchor.Left | OxHelpers.Anchor.Right);
        #endregion

        dimensions = resourcesTab.CurrentAppearanceInfo();

        #region Resources Tab
        resourcesTab.AddItems(mapsLocationLabel, mapsLocationTextBox, browseMapsLocationButton, texturesLocationLabel, texturesLocationTextBox, browseTexturesLocationButton, modelsLocationLabel, modelsLocationTextBox, browseModelsLocationButton, sfxLocationLabel, sfxLocationTextBox, browseSFXLocationButton);

        mapsLocationLabel.size = new Vector2(dimensions.centerWidth * 0.3f, dimensions.centerHeight * 0.2f);
        mapsLocationLabel.position = Vector2.zero;
        mapsLocationTextBox.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.2f);
        mapsLocationTextBox.position = new Vector2(mapsLocationLabel.width, 0);
        mapsLocationTextBox.textAlignment = OxHelpers.Alignment.Right;
        browseMapsLocationButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.2f);
        browseMapsLocationButton.position = new Vector2(mapsLocationLabel.width + mapsLocationTextBox.width, 0);

        texturesLocationLabel.size = new Vector2(dimensions.centerWidth * 0.3f, dimensions.centerHeight * 0.2f);
        texturesLocationLabel.position = new Vector2(0, mapsLocationLabel.height);
        texturesLocationTextBox.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.2f);
        texturesLocationTextBox.position = new Vector2(texturesLocationLabel.width, mapsLocationTextBox.height);
        texturesLocationTextBox.textAlignment = OxHelpers.Alignment.Right;
        browseTexturesLocationButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.2f);
        browseTexturesLocationButton.position = new Vector2(texturesLocationLabel.width + texturesLocationTextBox.width, browseMapsLocationButton.height);

        modelsLocationLabel.size = new Vector2(dimensions.centerWidth * 0.3f, dimensions.centerHeight * 0.2f);
        modelsLocationLabel.position = new Vector2(0, mapsLocationLabel.height + texturesLocationLabel.height);
        modelsLocationTextBox.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.2f);
        modelsLocationTextBox.position = new Vector2(modelsLocationLabel.width, mapsLocationTextBox.height + texturesLocationTextBox.height);
        modelsLocationTextBox.textAlignment = OxHelpers.Alignment.Right;
        browseModelsLocationButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.2f);
        browseModelsLocationButton.position = new Vector2(modelsLocationLabel.width + modelsLocationTextBox.width, browseMapsLocationButton.height + browseTexturesLocationButton.height);

        sfxLocationLabel.size = new Vector2(dimensions.centerWidth * 0.3f, dimensions.centerHeight * 0.2f);
        sfxLocationLabel.position = new Vector2(0, mapsLocationLabel.height + texturesLocationLabel.height + modelsLocationLabel.height);
        sfxLocationTextBox.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.2f);
        sfxLocationTextBox.position = new Vector2(sfxLocationLabel.width, mapsLocationTextBox.height + texturesLocationTextBox.height + modelsLocationTextBox.height);
        sfxLocationTextBox.textAlignment = OxHelpers.Alignment.Right;
        browseSFXLocationButton.size = new Vector2(dimensions.centerWidth * 0.2f, dimensions.centerHeight * 0.2f);
        browseSFXLocationButton.position = new Vector2(sfxLocationLabel.width + sfxLocationTextBox.width, browseMapsLocationButton.height + browseTexturesLocationButton.height + browseModelsLocationButton.height);

        resourcesTab.AddItems(resourcesTabSettingsBackButton);
        resourcesTabSettingsBackButton.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.15f);
        resourcesTabSettingsBackButton.position = new Vector2((dimensions.centerWidth / 2f) - (resourcesTabSettingsBackButton.width / 2f), dimensions.centerHeight - resourcesTabSettingsBackButton.height - 5);
        #endregion

        dimensions = optimizationsTab.CurrentAppearanceInfo();

        #region Optimizations Tab
        optimizationsTab.AddItems(combineMeshesCheckBox, averageTexturesCheckBox, decreaseTexturesCheckBox, maxTextureSizeLabel, maxTextureSizeTextBox);

        combineMeshesCheckBox.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.2f);
        combineMeshesCheckBox.position = new Vector2((dimensions.centerWidth / 2f) - (combineMeshesCheckBox.width / 2f), 0);

        averageTexturesCheckBox.size = new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.2f);
        averageTexturesCheckBox.position = new Vector2(0, combineMeshesCheckBox.height);

        decreaseTexturesCheckBox.size =  new Vector2(dimensions.centerWidth * 0.5f, dimensions.centerHeight * 0.2f);
        decreaseTexturesCheckBox.position = new Vector2(averageTexturesCheckBox.width, combineMeshesCheckBox.height);

        maxTextureSizeLabel.size = new Vector2(dimensions.centerWidth * 0.25f, dimensions.centerHeight * 0.2f);
        maxTextureSizeLabel.position = new Vector2((dimensions.centerWidth / 2f) - (maxTextureSizeLabel.width), combineMeshesCheckBox.height + averageTexturesCheckBox.height);
        maxTextureSizeTextBox.size = new Vector2(dimensions.centerWidth * 0.25f, dimensions.centerHeight * 0.2f);
        maxTextureSizeTextBox.position = new Vector2((dimensions.centerWidth / 2f), combineMeshesCheckBox.height + decreaseTexturesCheckBox.height);
        #endregion
        #endregion

        #region Texture Dir Browser
        textureDirChooser = new OxListFileSelectorPrompt(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        textureDirChooser.Reposition(new Vector2((Screen.width / 2f) - (textureDirChooser.width / 2f), (Screen.height / 2f) - (textureDirChooser.height / 2f)));

        textureDirChooser.directorySelection = true;
        textureDirChooser.selectionDone += Chooser_done;
        #endregion

        #region Map Dir Browser
        mapDirChooser = new OxListFileSelectorPrompt(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        mapDirChooser.Reposition(new Vector2((Screen.width / 2f) - (mapDirChooser.width / 2f), (Screen.height / 2f) - (mapDirChooser.height / 2f)));

        mapDirChooser.directorySelection = true;
        mapDirChooser.selectionDone += Chooser_done;
        #endregion

        #region Model Dir Browser
        modelDirChooser = new OxListFileSelectorPrompt(Vector2.zero, OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        modelDirChooser.Reposition(new Vector2((Screen.width / 2f) - (modelDirChooser.width / 2f), (Screen.height / 2f) - (modelDirChooser.height / 2f)));

        modelDirChooser.directorySelection = true;
        modelDirChooser.selectionDone += Chooser_done;
        #endregion

        #region SFX Dir Browser
        sfxDirChooser = new OxListFileSelectorPrompt();
        sfxDirChooser.directorySelection = true;
        sfxDirChooser.selectionDone += Chooser_done;
        #endregion
    }

    private void MainMenu()
    {
        mainMenu.Resize(OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        mainMenu.Reposition(new Vector2((Screen.width / 2f) - (mainMenu.width / 2f), (Screen.height / 2f) - (mainMenu.height / 2f)));
        mainMenu.Draw();
    }
    private void LiveMenu()
    {
        liveMenu.Resize(OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        liveMenu.Reposition(new Vector2((Screen.width / 2f) - (liveMenu.width / 2f), (Screen.height / 2f) - (liveMenu.height / 2f)));
        liveMenu.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void ReplaysMenu()
    {
        replaysMenu.Resize(OxHelpers.CalculatePixelSize(new Vector2(7, 5), new Vector2(0.9f, 0.7f)));
        replaysMenu.Reposition(new Vector2((Screen.width / 2f) - (replaysMenu.width / 2f), (Screen.height / 2f) - (replaysMenu.height / 2f)));
        replaysMenu.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void ReplayInterfaceMenu()
    {
        //replayInterface.Resize(OxHelpers.CalculatePixelSize(new Vector2(5, 2), new Vector2(0.5f, 0.2f)));
        //replayInterface.Reposition(new Vector2((Screen.width / 2f) - (replayInterface.width / 2f), (Screen.height) - (replayInterface.height) - 10));
        replayInterface.Draw();
    }
    private void MapsMenu()
    {
        mapMenu.Resize(OxHelpers.CalculatePixelSize(new Vector2(7, 5), new Vector2(0.9f, 0.7f)));
        mapMenu.Reposition(new Vector2((Screen.width / 2f) - (mapMenu.width / 2f), (Screen.height / 2f) - (mapMenu.height / 2f)));
        mapMenu.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void ExploreInterfaceMenu()
    {
        //exploreInterface.Resize(OxHelpers.CalculatePixelSize(new Vector2(7, 5), new Vector2(0.9f, 0.7f)));
        //exploreInterface.Reposition(new Vector2((Screen.width / 2f) - (exploreInterface.width / 2f), (Screen.height / 2f) - (exploreInterface.height / 2f)));
        exploreInterface.Draw();
    }
    private void SettingsMenu()
    {
        settingsMenu.Resize(OxHelpers.CalculatePixelSize(new Vector2(7, 5), new Vector2(0.9f, 0.7f)));
        settingsMenu.Reposition(new Vector2((Screen.width / 2f) - (settingsMenu.width / 2f), (Screen.height / 2f) - (settingsMenu.height / 2f)));
        settingsMenu.Draw();

        Screen.fullScreen = ApplicationPreferences.fullscreen;
    }
    private void MapDirBrowser()
    {
        mapDirChooser.Resize(OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        mapDirChooser.Reposition(new Vector2((Screen.width / 2f) - (mapDirChooser.width / 2f), (Screen.height / 2f) - (mapDirChooser.height / 2f)));
        mapDirChooser.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void TextureDirBrowser()
    {
        textureDirChooser.Resize(OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        textureDirChooser.Reposition(new Vector2((Screen.width / 2f) - (textureDirChooser.width / 2f), (Screen.height / 2f) - (textureDirChooser.height / 2f)));
        textureDirChooser.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void ModelDirBrowser()
    {
        modelDirChooser.Resize(OxHelpers.CalculatePixelSize(new Vector2(3, 4), new Vector2(0.6f, 0.8f)));
        modelDirChooser.Reposition(new Vector2((Screen.width / 2f) - (modelDirChooser.width / 2f), (Screen.height / 2f) - (modelDirChooser.height / 2f)));
        modelDirChooser.Draw();
    }

    void Button_clicked(OxBase sender)
    {
        #region Main Menu
        if (sender == viewLiveButton)
        {
            currentMenu = Menu.Live;
        }
        if (sender == viewReplaysButton) currentMenu = Menu.Replays;
        if (sender == viewMapsButton)
        {
            currentMenu = Menu.Maps;
            if (BSPMap.loadedMaps.Count > 0)
            {
                BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].SetVisibility(true);
            }
        }
        if (sender == settingsButton) currentMenu = Menu.Settings;
        if (sender == exitButton) Application.Quit();
        #endregion

        #region Live Menu
        if (sender == connectButton)
        {
            currentMenu = Menu.ReplayInterface;
            currentReplay = new Demo(addressBox.text, true);
            currentReplay.ParseReplay();
            Camera.main.transform.GetComponent<CameraControl>().blockControl = false;
        }
        #endregion

        #region Replays Menu
        if (sender == importReplayButton)
        {
            Debug.Log(replayFileList.currentDirectory + replayFileList.selectedItem.text);
            Demo loadedReplay = new Demo(replayFileList.currentDirectory + replayFileList.selectedItem.text, false);
            loadedReplay.ParseReplay();
            if(!loadedReplay.alreadyParsed) loadedReplay.demoMap.SetVisibility(false);
            RefreshReplaysList();
        }
        if (sender == watchReplayButton && loadedReplayList.itemsCount > 0)
        {
            //currentReplay = Demo.loadedDemos[loadedReplayList.SelectedIndex()];
            currentReplay = Demo.loadedDemos[(string)loadedReplayList.GetItems()[loadedReplayList.selectedIndex].value];
            currentMenu = Menu.ReplayInterface;
            currentReplay.demoMap.SetVisibility(true);
            Camera.main.transform.GetComponent<CameraControl>().blockControl = false;
            Camera.main.transform.GetComponent<CameraControl>().ShowSkybox(true);
        }
        if (sender == removeReplayButton)
        {
            if (Demo.loadedDemos.Count > 0)
            {
                //Demo.loadedDemos[loadedReplayList.SelectedIndex()].SelfDestruct();
                Demo.loadedDemos[(string)loadedReplayList.GetItems()[loadedReplayList.selectedIndex].value].SelfDestruct();
                RefreshReplaysList();
            }
        }
        #endregion

        #region Replay Interface
        if (sender == playButton)
        {
            if (currentReplay != null)
            {
                currentReplay.play = !currentReplay.play;

                if (currentReplay.play)
                {
                    playButton.text = "Pause";
                }
                else
                {
                    playButton.text = "Play";
                }
            }
        }
        #endregion

        #region Map Menu
        if (sender == loadMapButton && mapFileChooser.selectedItem != null)
        {
            //Debug.Log("Displaying: " + mapFileChooser.text);
            BSPMap loadedMap = new BSPMap(mapFileChooser.selectedItem.text);
            if (!loadedMap.alreadyMade)
            {
                //CoroutineRunner(loadedMap.BuildMap);
                loadedMap.BuildMap();
                if (BSPMap.loadedMaps.Count > 1) BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].SetVisibility(false);
                currentMap = BSPMap.loadedMaps.Count - 1;
            }
        }
        if (sender == nextMapButton)
        {
            if (currentMap < BSPMap.loadedMaps.Count - 1)
            {
                //GameObject prevMap = BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].mapGameObject;
                BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].SetVisibility(false);
                currentMap++;
                //GameObject theMap = BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].mapGameObject;
                BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].SetVisibility(true);
            }
        }
        if (sender == exploreMapButton)
        {
            currentMenu = Menu.ExploreInterface;
            Camera.main.transform.GetComponent<CameraControl>().blockControl = false;
            Camera.main.transform.GetComponent<CameraControl>().ShowSkybox(true);
        }
        if (sender == prevMapButton)
        {
            if (currentMap > 0)
            {
                //GameObject prevMap = BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].mapGameObject;
                BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].SetVisibility(false);
                currentMap--;
                //GameObject theMap = BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].mapGameObject;
                BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].SetVisibility(true);
            }
        }
        #endregion

        #region Settings Menu
        if (sender == browseMapsLocationButton)
        {
            mapDirChooser.SetSelection(ApplicationPreferences.mapsDir);
            currentMenu = Menu.MapDir;
        }
        if (sender == browseTexturesLocationButton)
        {
            textureDirChooser.SetSelection(ApplicationPreferences.texturesDir);
            currentMenu = Menu.TextureDir;
        }
        if(sender == browseModelsLocationButton)
        {
            modelDirChooser.SetSelection(ApplicationPreferences.modelsDir);
            currentMenu = Menu.ModelDir;
        }
        #endregion

        #region General Buttons
        if (sender.elementFunction == OxHelpers.ElementType.Back)
        {
            if (currentMenu == Menu.Live || currentMenu == Menu.Replays || currentMenu == Menu.Settings)
            {
                currentMenu = Menu.Main;
            }
            if (currentMenu == Menu.Maps)
            {
                if (BSPMap.loadedMaps.Count > 0)
                {
                    BSPMap.loadedMaps[BSPMap.loadedMaps.Keys.ElementAt(currentMap)].SetVisibility(false);
                }
                currentMenu = Menu.Main;
            }
            if (currentMenu == Menu.ReplayInterface)
            {
                bool live = false;
                if (currentReplay != null)
                {
                    if (currentReplay.port > -1) live = true;
                    currentReplay.Stop();
                    if(currentReplay.demoMap != null) currentReplay.demoMap.SetVisibility(false);
                    currentReplay = null;
                }
                if (live) currentMenu = Menu.Live;
                else currentMenu = Menu.Replays;
                Camera.main.transform.GetComponent<CameraControl>().blockControl = true;
                Camera.main.transform.GetComponent<CameraControl>().GoToDefault();
                Camera.main.transform.GetComponent<CameraControl>().ShowSkybox(false);
            }
            if (currentMenu == Menu.ExploreInterface)
            {
                currentMenu = Menu.Maps;
                Camera.main.transform.GetComponent<CameraControl>().blockControl = true;
                Camera.main.transform.GetComponent<CameraControl>().GoToDefault();
                Camera.main.transform.GetComponent<CameraControl>().ShowSkybox(false);
            }
        }
        #endregion
    }
    private void Scrollbar_scrollValueChanged(OxBase obj, float delta)
    {
        if(obj == manualFontSizeScroll)
        {
            ApplicationPreferences.fontSize = Mathf.RoundToInt((manualFontSizeScroll.progress * (OxBase.MAX_FONT_SIZE - OxBase.MIN_FONT_SIZE)) + OxBase.MIN_FONT_SIZE);
            manualFontSizeBox.text = ApplicationPreferences.fontSize.ToString();
            OxBase.allTextSize = ApplicationPreferences.fontSize;
            PlayerPrefs.SetInt(ApplicationPreferences.FONT_SIZE_PREFS, ApplicationPreferences.fontSize);
        }
    }
    void playerList_selectionChanged(object sender, object selectedItem, bool selected)
    {
        int entityID = -1;
        try { entityID = Convert.ToInt32(playerList.GetItems()[playerList.IndexOf((OxBase)selectedItem)].text); } catch(Exception) {}
        if (currentReplay != null && entityID > -1 && entityID < currentReplay.demoParser.PlayerInformations.Length && currentReplay.demoParser.PlayerInformations[entityID] != null && currentReplay.playerObjects.ContainsKey(currentReplay.demoParser.PlayerInformations[entityID])) Camera.main.GetComponent<CameraControl>().target = currentReplay.playerObjects[currentReplay.demoParser.PlayerInformations[entityID]].transform;
    }
    void replaySeeker_valueChanged(object sender, float amount)
    {
        if (sender == replaySeeker && currentReplay != null)
        {
            currentReplay.seekIndex = (int)(replaySeeker.progress * currentReplay.totalTicks);
            replaySeeker.text = currentReplay.seekIndex.ToString();
        }
    }
    void CheckBox_Switched(object sender, bool check)
    {
        #region Settings Menu
        if (sender == fullscreenCheckBox)
        {
            ApplicationPreferences.fullscreen = check;
        }
        if(sender == manualFontSizeCheckbox)
        {
            ApplicationPreferences.manualFontSize = check;
            OxBase.manualSizeAllText = ApplicationPreferences.manualFontSize;
            //manualFontPanel.visible = check;
            PlayerPrefs.SetInt(ApplicationPreferences.MANUAL_FONT_SIZE_PREFS, ApplicationPreferences.manualFontSize ? 1 : 0);
        }
        if (sender == combineMeshesCheckBox)
        {
            ApplicationPreferences.combineMeshes = check;
            PlayerPrefs.SetInt(ApplicationPreferences.COMBINE_PREFS, ApplicationPreferences.combineMeshes ? 1 : 0);
        }
        if (sender == averageTexturesCheckBox)
        {
            if (check) decreaseTexturesCheckBox.checkboxChecked = false;
            ApplicationPreferences.averageTextures = averageTexturesCheckBox.checkboxChecked;
            ApplicationPreferences.decreaseTextureSizes = decreaseTexturesCheckBox.checkboxChecked;
            PlayerPrefs.SetInt(ApplicationPreferences.AVERAGE_PREFS, ApplicationPreferences.averageTextures ? 1 : 0);
            PlayerPrefs.SetInt(ApplicationPreferences.DECREASE_PREFS, ApplicationPreferences.decreaseTextureSizes ? 1 : 0);
        }
        if (sender == decreaseTexturesCheckBox)
        {
            if (check) averageTexturesCheckBox.checkboxChecked = false;
            ApplicationPreferences.averageTextures = averageTexturesCheckBox.checkboxChecked;
            ApplicationPreferences.decreaseTextureSizes = decreaseTexturesCheckBox.checkboxChecked;
            PlayerPrefs.SetInt(ApplicationPreferences.AVERAGE_PREFS, ApplicationPreferences.averageTextures ? 1 : 0);
            PlayerPrefs.SetInt(ApplicationPreferences.DECREASE_PREFS, ApplicationPreferences.decreaseTextureSizes ? 1 : 0);
        }
        #endregion
    }
    void TextBox_textChanged(object sender, string prevText)
    {
        #region Settings Menu
        if(sender == manualFontSizeBox)
        {
            int newFontSize = ApplicationPreferences.fontSize;
            try { newFontSize = Convert.ToInt32(manualFontSizeBox.text); } catch (Exception) { }
            if (newFontSize < OxBase.MIN_FONT_SIZE) newFontSize = OxBase.MIN_FONT_SIZE;
            if (newFontSize > OxBase.MAX_FONT_SIZE) newFontSize = OxBase.MAX_FONT_SIZE;
            ApplicationPreferences.fontSize = newFontSize;
            manualFontSizeScroll.progress = (ApplicationPreferences.fontSize - OxBase.MIN_FONT_SIZE) / ((float)(OxBase.MAX_FONT_SIZE - OxBase.MIN_FONT_SIZE));
            OxBase.allTextSize = ApplicationPreferences.fontSize;
            PlayerPrefs.SetInt(ApplicationPreferences.FONT_SIZE_PREFS, ApplicationPreferences.fontSize);
        }
        if (sender == maxTextureSizeTextBox)
        {
            string maxSize = maxTextureSizeTextBox.text;
            int changedSize = ApplicationPreferences.maxSizeAllowed;
            try { changedSize = Convert.ToInt32(maxSize); } catch(Exception) {}
            if (changedSize > 0) ApplicationPreferences.maxSizeAllowed = changedSize;
            maxTextureSizeTextBox.text = ApplicationPreferences.maxSizeAllowed.ToString();
            PlayerPrefs.SetInt(ApplicationPreferences.MAX_SIZE, ApplicationPreferences.maxSizeAllowed);
        }
        if (sender == mapsLocationTextBox)
        {
            ApplicationPreferences.mapsDir = mapsLocationTextBox.text;
            PlayerPrefs.SetString(ApplicationPreferences.MAPS_LOC, ApplicationPreferences.mapsDir);
        }
        if (sender == texturesLocationTextBox)
        {
            ApplicationPreferences.texturesDir = texturesLocationTextBox.text;
            PlayerPrefs.SetString(ApplicationPreferences.TEX_LOC, ApplicationPreferences.texturesDir);
        }
        if(sender == modelsLocationTextBox)
        {
            ApplicationPreferences.modelsDir = modelsLocationTextBox.text;
            PlayerPrefs.SetString(ApplicationPreferences.MODELS_LOC, ApplicationPreferences.modelsDir);
        }
        #endregion
    }
    void Chooser_done(OxBase sender, OxHelpers.ElementType selectionType)
    {
        if (sender == mapDirChooser)
        {
            if (selectionType == OxHelpers.ElementType.Accept)
            {
                ApplicationPreferences.mapsDir = mapDirChooser.currentDirectory + mapDirChooser.selectedItem.text;
                mapsLocationTextBox.text = ApplicationPreferences.mapsDir;
                PlayerPrefs.SetString(ApplicationPreferences.MAPS_LOC, ApplicationPreferences.mapsDir);
            }
            currentMenu = Menu.Settings;
        }
        if (sender == textureDirChooser)
        {
            if (selectionType == OxHelpers.ElementType.Accept)
            {
                ApplicationPreferences.texturesDir = textureDirChooser.currentDirectory + textureDirChooser.selectedItem.text;
                texturesLocationTextBox.text = ApplicationPreferences.texturesDir;
                PlayerPrefs.SetString(ApplicationPreferences.TEX_LOC, ApplicationPreferences.texturesDir);
            }
            currentMenu = Menu.Settings;
        }
        if(sender == modelDirChooser)
        {
            if(selectionType == OxHelpers.ElementType.Accept)
            {
                ApplicationPreferences.modelsDir = modelDirChooser.currentDirectory + modelDirChooser.selectedItem.text;
                modelsLocationTextBox.text = ApplicationPreferences.modelsDir;
                PlayerPrefs.SetString(ApplicationPreferences.MODELS_LOC, ApplicationPreferences.modelsDir);
            }
            currentMenu = Menu.Settings;
        }
        //if (sender == mapFileChooser)
        //{
        //    if (selectionType == OxHelpers.ElementType.Accept)
        //    {
        //        Debug.Log("Displaying: " + mapFileChooser.text);
        //        BSPMap loadedMap = new BSPMap(mapFileChooser.text.Substring(mapFileChooser.text.LastIndexOf("/") + 1));
        //        loadedMap.MakeMap();
        //    }
        //}
    }

    public void RefreshReplaysList()
    {
        loadedReplayList.ClearItems();
        //for (int i = 0; i < Demo.loadedDemos.Count; i++)
        foreach(KeyValuePair<string, Demo> entry in Demo.loadedDemos)
        {
            string replayName = entry.Key;
            replayName.Replace("\\", "/");
            if (replayName.LastIndexOf("/") > -1) replayName = replayName.Substring(replayName.LastIndexOf("/") + 1);
            if (replayName.LastIndexOf(".") > -1) replayName = replayName.Substring(0, replayName.LastIndexOf("."));
            OxButton replayListButton = new OxButton(replayName);
            replayListButton.value = entry.Key;
            //replayListButton.replaceWhat = "\\";
            //replayListButton.replaceWith = "/";
            //replayListButton.substringBefore = "/";
            //replayListButton.substringAfter = ".";
            loadedReplayList.AddItems(replayListButton);
        }
    }
    public void RefreshPlayerList()
    {
        if (currentReplay != null)
        {
            List<int> entityIDs = new List<int>();
            for (int i = playerList.itemsCount - 1; i >= 0; i--)
            {
                int entityId = -1;
                try { entityId = Convert.ToInt32(playerList.GetItems()[i].text); } catch(Exception) {}
                if (i < 0 || currentReplay.demoParser.PlayerInformations[i] == null) { playerList.RemoveAt(i); continue; }
                entityIDs.Add(entityId);
            }

            if (currentReplay.seekIndex > -1 && currentReplay.demoTicks != null && currentReplay.seekIndex < currentReplay.demoTicks.Count && currentReplay.demoTicks[currentReplay.seekIndex].playersInTick != null)
            {
                foreach (Player player in currentReplay.demoTicks[currentReplay.seekIndex].playersInTick)
                {
                    if (entityIDs.IndexOf(player.EntityID) < 0) playerList.AddItems(new OxButton(player.EntityID.ToString()));
                }
            }
        }
    }

    //public void LoadMap(BSPMap map)
    //{
    //    StartCoroutine(map.BuildMap());
    //}

    public delegate IEnumerator CoroutineMethod();
    public void CoroutineRunner(CoroutineMethod toBeRun)
    {
        StartCoroutine(toBeRun());
    }
}
