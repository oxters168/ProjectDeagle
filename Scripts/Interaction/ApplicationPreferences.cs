using UnityEngine;

public class ApplicationPreferences {

    public const string
        FIRST_RUN = "First_Run",
        MANUAL_FONT_SIZE_PREFS = "Manual_Font_Size", FONT_SIZE_PREFS = "Font_Size", COMBINE_PREFS = "Combine_Meshes", AVERAGE_PREFS = "Average_Textures", DECREASE_PREFS = "Decrease_Textures", MAX_SIZE = "Max_Texture_Size",
        USE_VPK = "Use_VPK", USE_TEX = "Use_Textures", USE_MAPS = "Use_Maps", USE_MODELS = "Use_Models", USE_SFX = "Use_SFX",
        VPK_LOC = "VPK_Location", MAPS_LOC = "Maps_Location", TEX_LOC = "Textures_Location", MODELS_LOC = "Models_Location", SFX_LOC = "SFX_Location",
        CURRENT_REPLAYS_DIR = "Current_Replays_Dir", CURRENT_MAPS_DIR = "Current_Maps_Dir";

    public static bool firstRun;
    public static bool fullscreen;
    //public static int fontSize = 12;
    //public static bool manualFontSize = false;

    public static bool averageTextures, decreaseTextureSizes, combineMeshes;
    public static int maxSizeAllowed = 128, simultaneousFace = 32;
    public static bool useVPK, useTextures, useMaps, useModels, useSFX;
    public static string vpkDir, mapsDir, texturesDir, modelsDir, sfxDir;
    public static string currentReplaysDir, currentMapsDir;
    public static Material playerMaterial = Resources.Load<Material>("Materials/PlayerMaterial"), mapMaterial = Resources.Load<Material>("Materials/MapMaterial"), mapAtlasMaterial = Resources.Load<Material>("Materials/MapAtlasMaterial");
    public static Color ctColor = new Color((67f / 255f), (94f / 255f), (124f / 255f)), tColor = new Color((243f / 255f), (179f / 255f), (73f / 255f));

    public static VPKParser vpkParser = null;

    public static void LoadSavedPreferences()
    {
        firstRun = PlayerPrefs.GetInt(FIRST_RUN) == 0;
        if (!firstRun)
        {
            //manualFontSize = PlayerPrefs.GetInt(MANUAL_FONT_SIZE_PREFS) != 0;
            combineMeshes = PlayerPrefs.GetInt(COMBINE_PREFS) != 0;
            averageTextures = PlayerPrefs.GetInt(AVERAGE_PREFS) != 0;
            decreaseTextureSizes = PlayerPrefs.GetInt(DECREASE_PREFS) != 0;
            maxSizeAllowed = PlayerPrefs.GetInt(MAX_SIZE);

            useVPK = PlayerPrefs.GetInt(USE_VPK) != 0;
            useTextures = PlayerPrefs.GetInt(USE_TEX) != 0;
            useMaps = PlayerPrefs.GetInt(USE_MAPS) != 0;
            useModels = PlayerPrefs.GetInt(USE_MODELS) != 0;
            useSFX = PlayerPrefs.GetInt(USE_SFX) != 0;

            vpkDir = PlayerPrefs.GetString(VPK_LOC);
            mapsDir = PlayerPrefs.GetString(MAPS_LOC);
            texturesDir = PlayerPrefs.GetString(TEX_LOC);
            modelsDir = PlayerPrefs.GetString(MODELS_LOC);
            sfxDir = PlayerPrefs.GetString(SFX_LOC);

            currentReplaysDir = PlayerPrefs.GetString(CURRENT_REPLAYS_DIR);
            currentMapsDir = PlayerPrefs.GetString(CURRENT_MAPS_DIR);
        }
        else
        {
            PlayerPrefs.SetInt(FIRST_RUN, 1);
            //PlayerPrefs.SetInt(FONT_SIZE_PREFS, fontSize = 12);
            //PlayerPrefs.SetInt(MANUAL_FONT_SIZE_PREFS, manualFontSize ? 1 : 0);
            PlayerPrefs.SetInt(COMBINE_PREFS, (combineMeshes = true) ? 1 : 0);
            PlayerPrefs.SetInt(AVERAGE_PREFS, averageTextures ? 1 : 0);
            PlayerPrefs.SetInt(DECREASE_PREFS, (decreaseTextureSizes = true) ? 1 : 0);
            PlayerPrefs.SetInt(MAX_SIZE, maxSizeAllowed = 128);

            string steamCSGOLocation = "";
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) steamCSGOLocation = System.Environment.GetEnvironmentVariable("PROGRAMFILES(X86)").Replace("\\", "/") + "/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/";
            else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor) steamCSGOLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal).Replace("\\", "/") + "/Library/Application Support/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/";
            else if (Application.platform == RuntimePlatform.LinuxPlayer) steamCSGOLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal).Replace("\\", "/") + "/.local/share/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/";
            vpkDir = steamCSGOLocation + "pak01_dir.vpk";
            currentMapsDir = mapsDir = steamCSGOLocation + "maps/";
            currentReplaysDir = steamCSGOLocation + "replays/";
            if (vpkDir.Length > 0 && !System.IO.File.Exists(vpkDir)) vpkDir = "";
            if (mapsDir.Length > 0 && !System.IO.Directory.Exists(mapsDir)) { mapsDir = ""; currentMapsDir = ""; }
            if (currentReplaysDir.Length > 0 && !System.IO.Directory.Exists(currentReplaysDir)) currentReplaysDir = "";

            if (vpkDir.Length > 0) useVPK = true;
            if (mapsDir.Length > 0) useMaps = true;
            PlayerPrefs.SetInt(USE_VPK, useVPK ? 1 : 0);
            PlayerPrefs.SetInt(USE_TEX, useTextures ? 1 : 0);
            PlayerPrefs.SetInt(USE_MAPS, useMaps ? 1 : 0);
            PlayerPrefs.SetInt(USE_MODELS, useModels ? 1 : 0);
            PlayerPrefs.SetInt(USE_SFX, useSFX ? 1 : 0);

            PlayerPrefs.SetString(VPK_LOC, vpkDir);
            PlayerPrefs.SetString(MAPS_LOC, mapsDir);
            PlayerPrefs.SetString(TEX_LOC, texturesDir = "");
            PlayerPrefs.SetString(MODELS_LOC, modelsDir = "");
            PlayerPrefs.SetString(SFX_LOC, sfxDir = "");

            PlayerPrefs.SetString(CURRENT_MAPS_DIR, currentMapsDir);
            PlayerPrefs.SetString(CURRENT_REPLAYS_DIR, currentReplaysDir);
        }

        fullscreen = Screen.fullScreen;
        //OxGUI.OxBase.manualSizeAllText = manualFontSize;
        //OxGUI.OxBase.allTextSize = fontSize;
    }

    public static void UpdateVPKParser()
    {
        if (vpkParser == null || vpkParser.location != vpkDir.Replace("\\", "/").ToLower() || !vpkParser.parsed)
        {
            vpkParser = new VPKParser(vpkDir);
            vpkParser.Parse();
        }
    }

    public static void ResetValues()
    {
        PlayerPrefs.SetInt(FONT_SIZE_PREFS, 0);
        PlayerPrefs.SetInt(MANUAL_FONT_SIZE_PREFS, 0);
        PlayerPrefs.SetInt(COMBINE_PREFS, 0);
        PlayerPrefs.SetInt(AVERAGE_PREFS, 0);
        PlayerPrefs.SetInt(DECREASE_PREFS, 0);
        PlayerPrefs.SetInt(MAX_SIZE, 0);
        PlayerPrefs.SetInt(USE_VPK, 0);
        PlayerPrefs.SetInt(USE_TEX, 0);
        PlayerPrefs.SetInt(USE_MAPS, 0);
        PlayerPrefs.SetInt(USE_MODELS, 0);
        PlayerPrefs.SetInt(USE_SFX, 0);
        PlayerPrefs.SetString(VPK_LOC, "");
        PlayerPrefs.SetString(MAPS_LOC, "");
        PlayerPrefs.SetString(TEX_LOC, "");
        PlayerPrefs.SetString(MODELS_LOC, "");
        PlayerPrefs.SetString(SFX_LOC, "");
        PlayerPrefs.SetString(CURRENT_MAPS_DIR, "");
        PlayerPrefs.SetString(CURRENT_REPLAYS_DIR, "");
        PlayerPrefs.SetInt(FIRST_RUN, 0);
    }
}
