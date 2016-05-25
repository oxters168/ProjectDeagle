using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SourceTexture
{
    private static Dictionary<string, SourceTexture> loadedTextures = new Dictionary<string, SourceTexture>();
    private static Texture2D sharedMissing = Resources.Load<Texture2D>("Textures/Plain/Missing");
    //public FaceMesh face { get; private set; }
    public string location { get; private set; }
    public Texture2D texture { get; private set; }

    /*private SourceTexture(FaceMesh f)
    {
        face = f;
        loadedTextures.Add(face.rawTexture, this);
    }*/
    private SourceTexture(string textureLocation)
    {
        location = textureLocation;
        texture = sharedMissing;
        loadedTextures.Add(location, this);
    }

    public static SourceTexture GrabTexture(string rawStringPath)
    {
        SourceTexture srcTexture = null;

        string actualLocation = Locate(rawStringPath);

        if (loadedTextures.ContainsKey(actualLocation))
        {
            srcTexture = loadedTextures[actualLocation];
        }
        else
        {
            srcTexture = new SourceTexture(actualLocation);

            if (File.Exists(ApplicationPreferences.texturesDir + srcTexture.location + ".png"))
            {
                byte[] bytes = null;
                try { bytes = File.ReadAllBytes(ApplicationPreferences.texturesDir + srcTexture.location + ".png"); }
                catch (System.Exception e) { Debug.Log(e.Message); }
                if (bytes != null)
                {
                    srcTexture.texture = new Texture2D(0, 0);
                    srcTexture.texture.LoadImage(bytes);
                    bytes = null;
                }

                //Debug.Log("New Texture Loaded: " + ApplicationPreferences.texturesDir + srcTexture.location + ".png");
            }
            else if(Resources.Load<Texture2D>("Textures/Plain/" + srcTexture.location) != null)
            {
                srcTexture.texture = Resources.Load<Texture2D>("Textures/Plain/" + srcTexture.location);
                //Debug.Log("New Texture Loaded: " + "Textures/Plain/" + srcTexture.location + ".png");
            }
            else
            {
                Debug.Log("Could not find Texture: " + srcTexture.location + ".png");
            }

            if (srcTexture.texture != null)
            {
                if (ApplicationPreferences.averageTextures) AverageTexture(srcTexture.texture);
                else if (ApplicationPreferences.decreaseTextureSizes) DecreaseTextureSize(srcTexture.texture, ApplicationPreferences.maxSizeAllowed);
                srcTexture.texture.wrapMode = TextureWrapMode.Repeat;
            }
        }

        return srcTexture;
    }

    private static string Locate(string rawPath)
    {
        string finalLocation = RemoveMisleadingPath(rawPath);

        string vmtFile = "";
        //currentFace.materialLocation = PatchName(currentFace.materialLocation);
        if (Directory.Exists(ApplicationPreferences.texturesDir))
        {
            finalLocation = PatchName(ApplicationPreferences.texturesDir, finalLocation, "vmt");
            vmtFile = ApplicationPreferences.texturesDir + finalLocation + ".vmt";
            if (!File.Exists(vmtFile)) vmtFile = ApplicationPreferences.texturesDir + finalLocation + ".txt";
        }
        else
        {
            finalLocation = PatchName(finalLocation, "vmt");
        }

        //if (File.Exists(currentFace.materialLocation))
        string[] vmtLines = null;
        if (File.Exists(vmtFile))
        {
            try { vmtLines = File.ReadAllLines(@vmtFile); }
            catch (System.Exception e) { Debug.Log(e.Message); }
        }
        else
        {
            TextAsset vmtTextAsset = Resources.Load<TextAsset>("Textures/Plain/" + finalLocation);
            if (vmtTextAsset != null) vmtLines = vmtTextAsset.text.Split('\n');
        }

        if (vmtLines != null)
        {
            string baseTexture = "";

            foreach (string line in vmtLines)
            {
                if (line.IndexOf("$") > -1)
                {
                    string materialInfo = line.Substring(line.IndexOf("$") + 1);
                    if (materialInfo.IndexOf(" ") > -1 && materialInfo.IndexOf(" ") < materialInfo.IndexOf("\"") && materialInfo.Substring(0, materialInfo.IndexOf(" ")).Equals("basetexture", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        baseTexture = materialInfo.Substring(materialInfo.IndexOf(" ") + 1);
                        baseTexture = baseTexture.Substring(baseTexture.IndexOf("\"") + 1);
                        baseTexture = baseTexture.Substring(0, baseTexture.IndexOf("\""));
                        break;
                    }
                    else if (materialInfo.IndexOf("\"") > -1 && materialInfo.IndexOf("\"") < materialInfo.IndexOf(" ") && materialInfo.Substring(0, materialInfo.IndexOf("\"")).Equals("basetexture", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        baseTexture = materialInfo.Substring(materialInfo.IndexOf("\"") + 1);
                        baseTexture = baseTexture.Substring(baseTexture.IndexOf("\"") + 1);
                        baseTexture = baseTexture.Substring(0, baseTexture.IndexOf("\""));
                        break;
                    }
                }
            }

            baseTexture = RemoveMisleadingPath(baseTexture);
            if (baseTexture.Length > 0)
            {
                //currentFace.textureLocation = texturesDir + baseTexture + ".png";
                finalLocation = baseTexture;
            }
        }

        //currentFace.textureLocation = PatchName(currentFace.textureLocation);
        if (Directory.Exists(ApplicationPreferences.texturesDir)) finalLocation = PatchName(ApplicationPreferences.texturesDir, RemoveMisleadingPath(finalLocation), "png");
        else finalLocation = PatchName(RemoveMisleadingPath(finalLocation), "png");

        return finalLocation;
    }

    /// <summary>
    /// Tries to find the file specified by the original string
    /// within the rootPath string directory with the extension
    /// provided as ext. Starting with the full file name, then
    /// removing one character at a time, and finally returning
    /// the string of the file found.
    /// </summary>
    /// <param name="rootPath">The root directory path containing all materials</param>
    /// <param name="original">The file path within the root directory</param>
    /// <param name="ext">The extension of the file</param>
    /// <returns>The file path of a file with the closest name in the same directory</returns>
    public static string PatchName(string rootPath, string original, string ext)
    {
        string path = rootPath.Replace("\\", "/").ToLower();
        string prep = original.Replace("\\", "/").ToLower();
        if (prep.IndexOf("/") == 0) prep = prep.Substring(1);

        string subDir = "";
        List<string> extensions = new List<string>();
        string patched = "";
        extensions.Add(ext);

        if (prep.LastIndexOf("/") > -1)
        {
            subDir = prep.Substring(0, prep.LastIndexOf("/"));
            patched = prep.Substring(prep.LastIndexOf("/") + 1);
        }
        else patched = prep;

        if (extensions.Count > 0 && extensions[0].Equals("vmt", System.StringComparison.InvariantCultureIgnoreCase)) extensions.Add("txt");

        while (patched.Length > 0)
        {
            try
            {
                bool found = false;
                foreach (string extension in extensions)
                {
                    if (File.Exists(path + subDir + "/" + patched + "." + extension)) { prep = subDir + "/" + patched; found = true; break; }
                }
                if (found) break;
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }

            patched = patched.Substring(0, patched.Length - 1);
        }

        return prep;
    }
    /// <summary>
    /// Tries to find the file specified by the original string
    /// within the resources with the extension provided by ext.
    /// Starting with the full file name, then removing one
    /// character at a time, and finally returning the string
    /// of the file found.
    /// </summary>
    /// <param name="original">The file path within the resources</param>
    /// <param name="ext">The extension of the file</param>
    /// <returns>The file path of a file with the closest name in the same directory</returns>
    public static string PatchName(string original, string ext)
    {
        //string path = rootPath.Replace("\\", "/").ToLower();
        string prep = original.Replace("\\", "/").ToLower();
        if (prep.IndexOf("/") == 0) prep = prep.Substring(1);

        string subDir = "";
        List<string> extensions = new List<string>();
        string patched = "";
        extensions.Add(ext);

        if (prep.LastIndexOf("/") > -1)
        {
            subDir = prep.Substring(0, prep.LastIndexOf("/"));
            patched = prep.Substring(prep.LastIndexOf("/") + 1);
        }
        else patched = prep;

        if (extensions.Count > 0 && extensions[0].Equals("vmt", System.StringComparison.InvariantCultureIgnoreCase)) extensions[0] = "txt";

        while (patched.Length > 0)
        {
            try
            {
                bool found = false;
                //foreach (string extension in extensions)
                //{
                if (Resources.Load(subDir + "/" + patched) != null) { prep = subDir + "/" + patched; found = true; break; }
                //}
                if (found) break;
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }

            patched = patched.Substring(0, patched.Length - 1);
        }

        return prep;
    }
    /// <summary>
    /// Removes "maps/" from the path and underscore for some reason,
    /// should have commented this earlier
    /// </summary>
    /// <param name="original">The path of the texture</param>
    /// <returns>A non misleading path</returns>
    public static string RemoveMisleadingPath(string original)
    {
        string goodPath = original.Substring(0);
        if (goodPath.IndexOf("maps/") > -1)
        {
            goodPath = goodPath.Substring(goodPath.IndexOf("maps/") + ("maps/").Length);
            goodPath = goodPath.Substring(goodPath.IndexOf("/") + 1);
            while (goodPath.LastIndexOf("_") > -1 && (goodPath.Substring(goodPath.LastIndexOf("_") + 1).StartsWith("-") || char.IsDigit(goodPath.Substring(goodPath.LastIndexOf("_") + 1)[0])))
            {
                goodPath = goodPath.Substring(0, goodPath.LastIndexOf("_"));
            }
        }

        return goodPath.ToString();
    }
    public static void DecreaseTextureSize(Texture2D texture, float maxSize)
    {
        if (Mathf.Max(texture.width, texture.height) > maxSize)
        {
            float ratio = Mathf.Max(texture.width, texture.height) / maxSize;
            int decreasedWidth = (int)(texture.width / ratio), decreasedHeight = (int)(texture.height / ratio);

            TextureScale.Point(texture, decreasedWidth, decreasedHeight);
        }
    }
    public static void AverageTexture(Texture2D original)
    {
        Color allColorsInOne = new Color();
        Color[] originalColors = original.GetPixels();

        foreach (Color color in originalColors)
        {
            allColorsInOne.r += color.r;
            allColorsInOne.g += color.g;
            allColorsInOne.b += color.b;
            allColorsInOne.a += color.a;
        }

        allColorsInOne.r /= originalColors.Length;
        allColorsInOne.g /= originalColors.Length;
        allColorsInOne.b /= originalColors.Length;
        allColorsInOne.a /= originalColors.Length;

        original.Resize(16, 16);
        Color[] newColors = original.GetPixels();
        for (int i = 0; i < newColors.Length; i++)
        {
            newColors[i] = allColorsInOne;
        }

        original.wrapMode = TextureWrapMode.Clamp;
        original.SetPixels(newColors);
        original.Apply();
    }
}
