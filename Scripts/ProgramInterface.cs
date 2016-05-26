using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DemoInfo;
using System;
using System.IO;
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
    OxMenu mainMenu, settingsMenu;
    OxButton viewLiveButton, viewReplaysButton, viewMapsButton, settingsButton, exitButton;
    OxMenu liveMenu;
    OxTextBox addressBox;
    OxButton connectButton;
    OxSplit replaysMenu, replayChooserStatsSplitter, replayFilesChooserBackSplitter, replayStatsButtonsSplitter;
    OxMenu loadedReplayButtonMenu, importReplayButtonMenu;
    OxList replayFileList, loadedReplayList;
    OxButton importReplayButton, watchReplayButton, removeReplayButton;
    OxSplit replayInterface, exploreInterface;
    //OxWindow interfaceControlWindow;
    OxList playerList;
    OxSlider replaySeeker;
    OxButton playButton;
    OxSplit mapMenu, mapChooserSplit, mapButtonSplit, navButtonSplit;
    OxList mapFileChooser;
    OxButton loadMapButton, nextMapButton, prevMapButton, exploreMapButton;
    OxCheckBox fullscreenCheckBox, averageTexturesCheckBox, decreaseTexturesCheckBox, combineMeshesCheckBox;
    OxLabel maxTextureSizeLabel, texturesLocationLabel, mapsLocationLabel, modelsLocationLabel, sfxLocationLabel;
    OxTextBox maxTextureSizeTextBox, texturesLocationTextBox, mapsLocationTextBox, modelsLocationTextBox, sfxLocationTextBox;
    OxButton browseTexturesLocationButton, browseMapsLocationButton, browseModelsLocationButton, browseSFXLocationButton;
    OxChooser textureDirChooser, mapDirChooser, modelDirChooser, sfxDirChooser;
    OxButton backButton;
    //Replay aReplay;

    private bool showSplash = true;
    private int splashFrame = 1;
    private float splashFPS = 24, lastFrameChange = 0;

    Demo currentReplay;
    int currentMap = 0;

	// Use this for initialization
	void Start ()
    {
        ApplicationPreferences.LoadSavedPreferences();

        //if (false && (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxPlayer))
        //{
        //    gameObject.AddComponent<SteamManager>();
        //}
        MakeMenus();

        //Demo.ConvertToHexString("Ahmed");
        //VPKParser vpkTest = new VPKParser(new FileStream("D:/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/pak01_dir.vpk", FileMode.Open));
        //Debug.Log(vpkTest.Parse());
        //ParseMDL();
        //ParseVVD();
        //ParseModel("ctm_fbi", "C:/Users/oxter/Documents/csgo/models/player/");
	}

    private void ParseModel(string name, string location)
    {
        SourceModel model = SourceModel.GrabModel(name, location);
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
                replaySeeker.SetValue(((float) currentReplay.seekIndex) / currentReplay.totalTicks);
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
        backButton = new OxButton("Back", "MenuButton");
        backButton.clicked += Button_clicked;

        #region Main Menu
        mainMenu = new OxMenu(false);
        viewLiveButton = new OxButton("Live", "MenuButton");
        viewReplaysButton = new OxButton("Replays", "MenuButton");
        viewMapsButton = new OxButton("Maps", "MenuButton");
        settingsButton = new OxButton("Settings", "MenuButton");
        exitButton = new OxButton("Exit", "MenuButton");
        viewLiveButton.clicked += Button_clicked;
        viewReplaysButton.clicked += Button_clicked;
        viewMapsButton.clicked += Button_clicked;
        settingsButton.clicked += Button_clicked;
        exitButton.clicked += Button_clicked;
        mainMenu.AddItem(viewLiveButton, viewReplaysButton, viewMapsButton, settingsButton, exitButton);
        #endregion

        #region Live Menu
        liveMenu = new OxMenu(false);
        addressBox = new OxTextBox("127.0.0.1", "");
        connectButton = new OxButton("Connect", "MenuButton");
        connectButton.clicked += Button_clicked;
        liveMenu.AddItem(addressBox, connectButton, backButton);
        #endregion

        #region Replays Menu
        replaysMenu = new OxSplit();
        replayChooserStatsSplitter = new OxSplit();
        replayFilesChooserBackSplitter = new OxSplit();
        replayStatsButtonsSplitter = new OxSplit();
        replayFileList = new OxList();
        loadedReplayList = new OxList();
        loadedReplayButtonMenu = new OxMenu(true);
        importReplayButtonMenu = new OxMenu(false);
        importReplayButton = new OxButton("Import", "MenuButton");
        watchReplayButton = new OxButton("Watch", "MenuButton");
        removeReplayButton = new OxButton("Remove", "MenuButton");

        importReplayButton.clicked += Button_clicked;
        watchReplayButton.clicked += Button_clicked;
        removeReplayButton.clicked += Button_clicked;

        replaysMenu.division = 0.7f;
        replaysMenu.westPercentSize = 0.9f;
        replaysMenu.westComponent = replayChooserStatsSplitter;
        replaysMenu.eastComponent = replayFilesChooserBackSplitter;
        replayFilesChooserBackSplitter.horizontal = false;
        replayFilesChooserBackSplitter.division = 0.75f;
        replayFilesChooserBackSplitter.eastPercentSize = 0.9f;
        replayFilesChooserBackSplitter.westComponent = replayFileList;
        replayFilesChooserBackSplitter.eastComponent = importReplayButtonMenu;
        importReplayButtonMenu.AddItem(importReplayButton, backButton);
        replayChooserStatsSplitter.horizontal = false;
        replayChooserStatsSplitter.division = 0.2f;
        replayChooserStatsSplitter.westComponent = loadedReplayList;
        replayChooserStatsSplitter.eastComponent = replayStatsButtonsSplitter;
        loadedReplayList.horizontal = true;
        replayStatsButtonsSplitter.horizontal = false;
        replayStatsButtonsSplitter.division = 0.8f;
        replayStatsButtonsSplitter.westComponent = new OxLabel();
        replayStatsButtonsSplitter.eastComponent = loadedReplayButtonMenu;
        loadedReplayButtonMenu.AddItem(watchReplayButton, removeReplayButton);

        replayFileList.FillBrowserList("", true, "dem");
        //replaysMenu = new OxMenu(false);
        //replaysMenu.AddItem(backButton);
        #endregion

        #region Replay Interface
        replayInterface = new OxSplit();
        OxSplit lowerInterfaceSplit = new OxSplit(), lowerInterfaceSecondSplit = new OxSplit();
        OxSplit playerListSplit = new OxSplit(), mediaControlsSplit = new OxSplit(), centerPlayButtonSplit = new OxSplit(), playButtonSplit = new OxSplit();
        playerList = new OxList();
        playerList.indexChanged += playerList_indexChanged;
        //interfaceControlWindow = new OxWindow();
        playButton = new OxButton("Play");
        playButton.clicked += Button_clicked;
        replaySeeker = new OxSlider();
        replaySeeker.valueChanged += replaySeeker_valueChanged;
        //seekButton = new OxButton("");
        replayInterface.horizontal = false;
        replayInterface.division = 0.6f;
        replayInterface.eastComponent = lowerInterfaceSplit;
        replayInterface.westComponent = new OxLabel();
        lowerInterfaceSplit.division = 0.75f;
        lowerInterfaceSplit.eastPercentSize = 0.25f;
        lowerInterfaceSplit.eastComponent = backButton;
        lowerInterfaceSplit.westComponent = lowerInterfaceSecondSplit;
        lowerInterfaceSecondSplit.division = 0.33f;
        lowerInterfaceSecondSplit.eastComponent = playerListSplit;
        lowerInterfaceSecondSplit.westComponent = new OxLabel();
        //interfaceControlWindow.showMinimizeButton = false;
        //interfaceControlWindow.showMaximizeButton = false;
        //interfaceControlWindow.showCloseButton = false;
        //interfaceControlWindow.layout = mediaControlsSplit;
        playerListSplit.horizontal = false;
        playerListSplit.division = 0.9f;
        playerListSplit.westComponent = mediaControlsSplit;
        playerListSplit.eastComponent = playerList;
        playerList.horizontal = true;
        mediaControlsSplit.horizontal = false;
        mediaControlsSplit.division = 0.1f;
        mediaControlsSplit.eastComponent = centerPlayButtonSplit;
        mediaControlsSplit.westComponent = replaySeeker;
        centerPlayButtonSplit.division = 0.3f;
        centerPlayButtonSplit.eastComponent = playButtonSplit;
        centerPlayButtonSplit.westComponent = new OxLabel();
        playButtonSplit.division = 0.6f;
        playButtonSplit.westPercentSize = 0.5f;
        playButtonSplit.eastComponent = new OxLabel();
        playButtonSplit.westComponent = playButton;
        #endregion

        #region Maps Menu
        //mapMenu, mapChooserSplit, mapButtonSplit
        mapMenu = new OxSplit();
        mapChooserSplit = new OxSplit();
        mapButtonSplit = new OxSplit();
        navButtonSplit = new OxSplit();
        OxSplit buttonSplitWithEmpty = new OxSplit();
        OxSplit navGapSplit = new OxSplit();
        mapFileChooser = new OxList();
        loadMapButton = new OxButton("Import Map", "MenuButton");
        nextMapButton = new OxButton("Next", "MenuButton");
        prevMapButton = new OxButton("Previous", "MenuButton");
        exploreMapButton = new OxButton("Explore", "MenuButton");
        loadMapButton.clicked += Button_clicked;
        nextMapButton.clicked += Button_clicked;
        prevMapButton.clicked += Button_clicked;
        exploreMapButton.clicked += Button_clicked;
        //mapFileChooser.done += Chooser_done;
        string mapsLoc = "";
        if (ApplicationPreferences.mapsDir != null && ApplicationPreferences.mapsDir.Length > 0) mapsLoc = ApplicationPreferences.mapsDir.Substring(0, ApplicationPreferences.mapsDir.Length - 1);
        mapFileChooser.FillBrowserList(mapsLoc, true, "bsp");
        mapMenu.division = 0.8f;
        mapMenu.westPercentSize = 0.9f;
        mapMenu.eastComponent = mapChooserSplit;
        mapMenu.westComponent = buttonSplitWithEmpty;
        mapChooserSplit.division = 0.75f;
        mapChooserSplit.eastPercentSize = 0.9f;
        mapChooserSplit.horizontal = false;
        mapChooserSplit.westComponent = mapFileChooser;
        mapChooserSplit.eastComponent = mapButtonSplit;
        mapButtonSplit.horizontal = false;
        mapButtonSplit.westComponent = loadMapButton;
        mapButtonSplit.eastComponent = backButton;
        buttonSplitWithEmpty.division = 0.9f;
        buttonSplitWithEmpty.horizontal = false;
        buttonSplitWithEmpty.westComponent = new OxSplit();
        buttonSplitWithEmpty.eastComponent = navButtonSplit;
        navButtonSplit.division = 0.75f;
        navButtonSplit.eastComponent = nextMapButton;
        navButtonSplit.westComponent = navGapSplit;
        navGapSplit.division = 0.33f;
        navGapSplit.eastComponent = exploreMapButton;
        navGapSplit.westComponent = prevMapButton;
        //mapMenu.AddItem(mapFileChooser, backButton);
        #endregion

        #region ExploreInterface
        exploreInterface = new OxSplit();
        exploreInterface.horizontal = false;
        exploreInterface.division = 0.9f;
        OxSplit exploreInterfaceBackSplit = new OxSplit();
        exploreInterface.westComponent = new OxLabel();
        exploreInterface.eastComponent = exploreInterfaceBackSplit;
        exploreInterfaceBackSplit.division = 0.9f;
        exploreInterfaceBackSplit.westComponent = new OxLabel();
        exploreInterfaceBackSplit.eastComponent = backButton;
        #endregion

        #region Settings Menu
        settingsMenu = new OxMenu(false);

        fullscreenCheckBox = new OxCheckBox(ApplicationPreferences.fullscreen, "Fullscreen");
        fullscreenCheckBox.checkBoxSwitched += CheckBox_Switched;
        combineMeshesCheckBox = new OxCheckBox(ApplicationPreferences.combineMeshes, "Combine Map Meshes");
        combineMeshesCheckBox.checkBoxSwitched += CheckBox_Switched;
        averageTexturesCheckBox = new OxCheckBox(ApplicationPreferences.averageTextures, "Average Textures");
        averageTexturesCheckBox.checkBoxSwitched += CheckBox_Switched;
        decreaseTexturesCheckBox = new OxCheckBox(ApplicationPreferences.decreaseTextureSizes, "Decrease Texture Sizes");
        decreaseTexturesCheckBox.checkBoxSwitched += CheckBox_Switched;

        maxTextureSizeLabel = new OxLabel("Max Size", Color.black, TextAnchor.LowerCenter);
        texturesLocationLabel = new OxLabel("Textures Location", Color.black, TextAnchor.LowerCenter);
        mapsLocationLabel = new OxLabel("Maps Location", Color.black, TextAnchor.LowerCenter);
        modelsLocationLabel = new OxLabel("Models Location", Color.black, TextAnchor.LowerCenter);
        sfxLocationLabel = new OxLabel("SFX Location", Color.black, TextAnchor.LowerCenter);

        maxTextureSizeTextBox = new OxTextBox(ApplicationPreferences.maxSizeAllowed.ToString(), "");
        maxTextureSizeTextBox.textChanged += TextBox_textChanged;

        texturesLocationTextBox = new OxTextBox(ApplicationPreferences.texturesDir, "");
        texturesLocationTextBox.textChanged += TextBox_textChanged;
        browseTexturesLocationButton = new OxButton("Browse", "MenuButton");
        browseTexturesLocationButton.clicked += Button_clicked;

        mapsLocationTextBox = new OxTextBox(ApplicationPreferences.mapsDir, "");
        mapsLocationTextBox.textChanged += TextBox_textChanged;
        browseMapsLocationButton = new OxButton("Browse", "MenuButton");
        browseMapsLocationButton.clicked += Button_clicked;

        modelsLocationTextBox = new OxTextBox(ApplicationPreferences.modelsDir, "");
        modelsLocationTextBox.textChanged += TextBox_textChanged;
        browseModelsLocationButton = new OxButton("Browse", "MenuButton");
        browseModelsLocationButton.clicked += Button_clicked;

        sfxLocationTextBox = new OxTextBox("", "");
        sfxLocationTextBox.textChanged += TextBox_textChanged;
        browseSFXLocationButton = new OxButton("Browse", "MenuButton");
        browseSFXLocationButton.clicked += Button_clicked;

        settingsMenu.AddItem(fullscreenCheckBox, combineMeshesCheckBox, averageTexturesCheckBox, decreaseTexturesCheckBox, maxTextureSizeLabel, maxTextureSizeTextBox, 
            texturesLocationLabel, texturesLocationTextBox, browseTexturesLocationButton, 
            mapsLocationLabel, mapsLocationTextBox, browseMapsLocationButton, 
            modelsLocationLabel, modelsLocationTextBox, browseModelsLocationButton, 
            sfxLocationLabel, sfxLocationTextBox, browseSFXLocationButton, 
            backButton);
        #endregion

        #region Texture Dir Browser
        textureDirChooser = new OxChooser();
        textureDirChooser.done += Chooser_done;
        #endregion

        #region Map Dir Browser
        mapDirChooser = new OxChooser();
        mapDirChooser.done += Chooser_done;
        #endregion

        #region Model Dir Browser
        modelDirChooser = new OxChooser();
        modelDirChooser.done += Chooser_done;
        #endregion

        #region SFX Dir Browser
        sfxDirChooser = new OxChooser();
        sfxDirChooser.done += Chooser_done;
        #endregion
    }

    private void MainMenu()
    {
        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 3f, 4f, 0.6f, 0.8f);
        float menuWidth = pratio * 3f;
        float menuHeight = pratio * 4f;

        mainMenu.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        mainMenu.Resize(menuWidth, menuHeight);
        mainMenu.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void LiveMenu()
    {
        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 3f, 4f, 0.6f, 0.8f);
        float menuWidth = pratio * 3f;
        float menuHeight = pratio * 4f;

        liveMenu.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        liveMenu.Resize(menuWidth, menuHeight);
        liveMenu.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void ReplaysMenu()
    {
        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 7f, 5f, 0.9f, 0.7f);
        float menuWidth = pratio * 7f;
        float menuHeight = pratio * 5f;

        replaysMenu.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        replaysMenu.Resize(menuWidth, menuHeight);
        replaysMenu.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void ReplayInterfaceMenu()
    {
        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 7f, 5f, 0.9f, 0.7f);
        float menuWidth = pratio * 7f;
        float menuHeight = pratio * 5f;

        replayInterface.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        replayInterface.Resize(menuWidth, menuHeight);
        replayInterface.Draw();
    }
    private void MapsMenu()
    {
        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 7f, 5f, 0.9f, 0.7f);
        float menuWidth = pratio * 7f;
        float menuHeight = pratio * 5f;

        //Debug.Log("Maps Menu");

        mapMenu.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        mapMenu.Resize(menuWidth, menuHeight);
        mapMenu.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void ExploreInterfaceMenu()
    {
        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 7f, 5f, 0.9f, 0.7f);
        float menuWidth = pratio * 7f;
        float menuHeight = pratio * 5f;

        exploreInterface.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        exploreInterface.Resize(menuWidth, menuHeight);
        exploreInterface.Draw();
    }
    private void SettingsMenu()
    {
        //Debug.Log("Settings Menu");

        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 3f, 4f, 0.6f, 0.8f);
        float menuWidth = pratio * 3f;
        float menuHeight = pratio * 4f;

        settingsMenu.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        settingsMenu.Resize(menuWidth, menuHeight);
        settingsMenu.Draw();

        Screen.fullScreen = ApplicationPreferences.fullscreen;
        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void MapDirBrowser()
    {
        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 3f, 4f, 0.6f, 0.8f);
        float menuWidth = pratio * 3f;
        float menuHeight = pratio * 4f;

        mapDirChooser.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        mapDirChooser.Resize(menuWidth, menuHeight);
        mapDirChooser.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void TextureDirBrowser()
    {
        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 3f, 4f, 0.6f, 0.8f);
        float menuWidth = pratio * 3f;
        float menuHeight = pratio * 4f;

        textureDirChooser.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        textureDirChooser.Resize(menuWidth, menuHeight);
        textureDirChooser.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }
    private void ModelDirBrowser()
    {
        float pratio = OxGUI.GetPratio(Screen.width, Screen.height, 3f, 4f, 0.6f, 0.8f);
        float menuWidth = pratio * 3f;
        float menuHeight = pratio * 4f;

        modelDirChooser.Reposition((Screen.width / 2f) - (menuWidth / 2f), (Screen.height / 2f) - (menuHeight / 2f));
        modelDirChooser.Resize(menuWidth, menuHeight);
        modelDirChooser.Draw();

        //Debug.Log(mainMenu.Position() + ", " + mainMenu.Size() + ", " + "(" + menuWidth + ", " + menuHeight + ")");
    }

    //string browseMapDir, browseTextureDir;
    void Button_clicked(OxGUI sender)
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
            Demo loadedReplay = new Demo(replayFileList.text, false);
            loadedReplay.ParseReplay();
            if(!loadedReplay.alreadyParsed) loadedReplay.demoMap.SetVisibility(false);
            RefreshReplaysList();
        }
        if (sender == watchReplayButton)
        {
            //currentReplay = Demo.loadedDemos[loadedReplayList.SelectedIndex()];
            currentReplay = Demo.loadedDemos[loadedReplayList.items[loadedReplayList.selectedIndex].text];
            currentMenu = Menu.ReplayInterface;
            currentReplay.demoMap.SetVisibility(true);
            Camera.main.transform.GetComponent<CameraControl>().blockControl = false;
        }
        if (sender == removeReplayButton)
        {
            if (Demo.loadedDemos.Count > 0)
            {
                //Demo.loadedDemos[loadedReplayList.SelectedIndex()].SelfDestruct();
                Demo.loadedDemos[loadedReplayList.items[loadedReplayList.selectedIndex].text].SelfDestruct();
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
        if (sender == loadMapButton)
        {
            //Debug.Log("Displaying: " + mapFileChooser.text);
            BSPMap loadedMap = new BSPMap(mapFileChooser.text.Substring(mapFileChooser.text.LastIndexOf("/") + 1));
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
            //browseMapDir = BSPMap.mapsDir.Substring(0, BSPMap.mapsDir.Length - 1);
            mapDirChooser.FillBrowserList(ApplicationPreferences.mapsDir.Substring(0, ApplicationPreferences.mapsDir.Length - 1), false);
            currentMenu = Menu.MapDir;
        }
        if (sender == browseTexturesLocationButton)
        {
            //browseTextureDir = BSPMap.texturesDir.Substring(0, BSPMap.texturesDir.Length - 1);
            textureDirChooser.FillBrowserList(ApplicationPreferences.texturesDir.Substring(0, ApplicationPreferences.texturesDir.Length - 1), false);
            currentMenu = Menu.TextureDir;
        }
        if(sender == browseModelsLocationButton)
        {
            modelDirChooser.FillBrowserList(ApplicationPreferences.modelsDir.Substring(0, ApplicationPreferences.modelsDir.Length - 1), false);
            currentMenu = Menu.ModelDir;
        }
        #endregion

        #region General Buttons
        if (sender == backButton)
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
            }
            if (currentMenu == Menu.ExploreInterface)
            {
                currentMenu = Menu.Maps;
                Camera.main.transform.GetComponent<CameraControl>().blockControl = true;
                Camera.main.transform.GetComponent<CameraControl>().GoToDefault();
            }
        }
        #endregion
    }
    void playerList_indexChanged(int itemIndex)
    {
        int entityID = -1;
        try { entityID = Convert.ToInt32(playerList.items[itemIndex].text); } catch(Exception) {}
        if (currentReplay != null && entityID > -1 && entityID < currentReplay.demoParser.PlayerInformations.Length && currentReplay.demoParser.PlayerInformations[entityID] != null && currentReplay.playerObjects.ContainsKey(currentReplay.demoParser.PlayerInformations[entityID])) Camera.main.GetComponent<CameraControl>().target = currentReplay.playerObjects[currentReplay.demoParser.PlayerInformations[entityID]].transform;
    }
    void replaySeeker_valueChanged(OxGUI sender, float amount)
    {
        if (sender == replaySeeker && currentReplay != null)
        {
            currentReplay.seekIndex = (int)(replaySeeker.value * currentReplay.totalTicks);
            replaySeeker.text = currentReplay.seekIndex.ToString();
        }
    }
    void CheckBox_Switched(OxGUI sender, bool check)
    {
        #region Settings Menu
        if (sender == fullscreenCheckBox)
        {
            ApplicationPreferences.fullscreen = check;
        }
        if (sender == combineMeshesCheckBox)
        {
            ApplicationPreferences.combineMeshes = check;
            PlayerPrefs.SetInt(ApplicationPreferences.COMBINE_PREFS, ApplicationPreferences.combineMeshes ? 1 : 0);
        }
        if (sender == averageTexturesCheckBox)
        {
            if (check) decreaseTexturesCheckBox.isChecked = false;
            ApplicationPreferences.averageTextures = averageTexturesCheckBox.isChecked;
            ApplicationPreferences.decreaseTextureSizes = decreaseTexturesCheckBox.isChecked;
            PlayerPrefs.SetInt(ApplicationPreferences.AVERAGE_PREFS, ApplicationPreferences.averageTextures ? 1 : 0);
            PlayerPrefs.SetInt(ApplicationPreferences.DECREASE_PREFS, ApplicationPreferences.decreaseTextureSizes ? 1 : 0);
        }
        if (sender == decreaseTexturesCheckBox)
        {
            if (check) averageTexturesCheckBox.isChecked = false;
            ApplicationPreferences.averageTextures = averageTexturesCheckBox.isChecked;
            ApplicationPreferences.decreaseTextureSizes = decreaseTexturesCheckBox.isChecked;
            PlayerPrefs.SetInt(ApplicationPreferences.AVERAGE_PREFS, ApplicationPreferences.averageTextures ? 1 : 0);
            PlayerPrefs.SetInt(ApplicationPreferences.DECREASE_PREFS, ApplicationPreferences.decreaseTextureSizes ? 1 : 0);
        }
        #endregion
    }
    void TextBox_textChanged(OxGUI sender)
    {
        #region Settings Menu
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
    void Chooser_done(OxChooser sender, bool accepted)
    {
        if (sender == mapDirChooser)
        {
            if (accepted)
            {
                ApplicationPreferences.mapsDir = mapDirChooser.text + "/";
                mapsLocationTextBox.text = ApplicationPreferences.mapsDir;
                PlayerPrefs.SetString(ApplicationPreferences.MAPS_LOC, ApplicationPreferences.mapsDir);
            }
            currentMenu = Menu.Settings;
        }
        if (sender == textureDirChooser)
        {
            if (accepted)
            {
                ApplicationPreferences.texturesDir = textureDirChooser.text + "/";
                texturesLocationTextBox.text = ApplicationPreferences.texturesDir;
                PlayerPrefs.SetString(ApplicationPreferences.TEX_LOC, ApplicationPreferences.texturesDir);
            }
            currentMenu = Menu.Settings;
        }
        if(sender == modelDirChooser)
        {
            if(accepted)
            {
                ApplicationPreferences.modelsDir = modelDirChooser.text + "/";
                modelsLocationTextBox.text = ApplicationPreferences.modelsDir;
                PlayerPrefs.SetString(ApplicationPreferences.MODELS_LOC, ApplicationPreferences.modelsDir);
            }
            currentMenu = Menu.Settings;
        }
        //if (sender == mapFileChooser)
        //{
        //    if (accepted)
        //    {
        //        Debug.Log("Displaying: " + mapFileChooser.text);
        //        BSPMap loadedMap = new BSPMap(mapFileChooser.text.Substring(mapFileChooser.text.LastIndexOf("/") + 1));
        //        loadedMap.MakeMap();
        //    }
        //}
    }

    public void RefreshReplaysList()
    {
        loadedReplayList.Clear();
        //for (int i = 0; i < Demo.loadedDemos.Count; i++)
        foreach(KeyValuePair<string, Demo> entry in Demo.loadedDemos)
        {
            string replayName = entry.Key;
            //replayName.Replace("\\", "/");
            //if (replayName.LastIndexOf("/") > -1) replayName = replayName.Substring(replayName.LastIndexOf("/") + 1);
            //if (replayName.LastIndexOf(".") > -1) replayName = replayName.Substring(0, replayName.LastIndexOf("."));
            OxButton replayListButton = new OxButton(replayName, "MenuButton");
            replayListButton.replaceWhat = "\\";
            replayListButton.replaceWith = "/";
            replayListButton.substringBefore = "/";
            replayListButton.substringAfter = ".";
            loadedReplayList.AddItem(replayListButton);
        }
    }
    public void RefreshPlayerList()
    {
        if (currentReplay != null)
        {
            List<int> entityIDs = new List<int>();
            for (int i = playerList.items.Count - 1; i >= 0; i--)
            {
                int entityId = -1;
                try { entityId = Convert.ToInt32(playerList.items[i].text); } catch(Exception) {}
                if (i < 0 || currentReplay.demoParser.PlayerInformations[i] == null) { playerList.items.RemoveAt(i); continue; }
                entityIDs.Add(entityId);
            }

            if (currentReplay.seekIndex > -1 && currentReplay.demoTicks != null && currentReplay.seekIndex < currentReplay.demoTicks.Count && currentReplay.demoTicks[currentReplay.seekIndex].playersInTick != null)
            {
                foreach (Player player in currentReplay.demoTicks[currentReplay.seekIndex].playersInTick)
                {
                    if (entityIDs.IndexOf(player.EntityID) < 0) playerList.AddItem(new OxButton(player.EntityID.ToString()));
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
