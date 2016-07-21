using UnityEngine;
//using System.Collections;

public class ApplicationPreferences {

    public const string MANUAL_FONT_SIZE_PREFS = "Manual_Font_Size", FONT_SIZE_PREFS = "Font_Size", COMBINE_PREFS = "Combine_Meshes", AVERAGE_PREFS = "Average_Textures", DECREASE_PREFS = "Decrease_Textures", MAX_SIZE = "Max_Texture_Size", MAPS_LOC = "Maps_Location", TEX_LOC = "Textures_Location", MODELS_LOC = "Models_Location";

    public static bool fullscreen;
    public static int fontSize = 12;
    public static bool manualFontSize = false;

    public static bool averageTextures = false, decreaseTextureSizes = true, combineMeshes = true;
    public static int maxSizeAllowed = 128;
    public static string mapsDir = "";
    public static string texturesDir = "";
    public static string modelsDir = "";
    public static Material playerMaterial = Resources.Load<Material>("Materials/PlayerMaterial"), mapMaterial = Resources.Load<Material>("Materials/MapMaterial"), mapAtlasMaterial = Resources.Load<Material>("Materials/MapAtlasMaterial");
    public static Color ctColor = new Color(0.263f, 0.369f, 0.486f), tColor = new Color(0.953f, 0.702f, 0.286f);

    public static void LoadSavedPreferences()
    {
        manualFontSize = PlayerPrefs.GetInt(MANUAL_FONT_SIZE_PREFS) != 0;
        fontSize = PlayerPrefs.GetInt(FONT_SIZE_PREFS);
        combineMeshes = PlayerPrefs.GetInt(COMBINE_PREFS) != 0;
        averageTextures = PlayerPrefs.GetInt(AVERAGE_PREFS) != 0;
        decreaseTextureSizes = PlayerPrefs.GetInt(DECREASE_PREFS) != 0;
        maxSizeAllowed = PlayerPrefs.GetInt(MAX_SIZE);
        mapsDir = PlayerPrefs.GetString(MAPS_LOC);
        texturesDir = PlayerPrefs.GetString(TEX_LOC);
        modelsDir = PlayerPrefs.GetString(MODELS_LOC);

        fullscreen = Screen.fullScreen;
        OxGUI.OxBase.manualSizeAllText = manualFontSize;
        OxGUI.OxBase.allTextSize = fontSize;
    }
}
