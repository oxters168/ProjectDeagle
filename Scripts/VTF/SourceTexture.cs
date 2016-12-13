using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class SourceTexture
{
    private enum TextureType { None = 0, VTF, ColorData, Plain, }

    private static Dictionary<string, SourceTexture> loadedTextures = new Dictionary<string, SourceTexture>();
    private static Dictionary<string, Color> plainTextures = new Dictionary<string, Color>();
    private static Dictionary<string, string[]> vmtFiles = new Dictionary<string, string[]>();
    private static Texture2D sharedMissing = Resources.Load<Texture2D>("Textures/Plain/Missing");
    //public FaceMesh face { get; private set; }
    public string location { get; private set; }

    private Texture2D texture;
    private object textureData;
    private TextureType textureType;

    private SourceTexture(string textureLocation)
    {
        location = textureLocation;
        //texture = sharedMissing;
        //textureType = TextureType.Plain;
        loadedTextures.Add(location, this);
    }

    public Texture2D GetTexture()
    {
        if (texture == null)
        {
            if(textureType == TextureType.VTF)
            {
                texture = LoadVTFFile((byte[])textureData);
            }
            else if(textureType == TextureType.ColorData)
            {
                texture = new Texture2D(0, 0);
                texture.LoadImage((byte[])textureData);
            }
            else if(textureType == TextureType.Plain)
            {
                texture = new Texture2D(32, 32);
                Color[] plainTexturePixels = new Color[texture.width * texture.height];
                for (int i = 0; i < plainTexturePixels.Length; i++) plainTexturePixels[i] = (Color)textureData;
                texture.SetPixels(plainTexturePixels);
                texture.Apply();
            }
            else
            {
                texture = sharedMissing;
                textureType = TextureType.Plain;
            }

            if (texture != null)
            {
                if (ApplicationPreferences.averageTextures) AverageTexture(texture);
                else if (ApplicationPreferences.decreaseTextureSizes) DecreaseTextureSize(texture, ApplicationPreferences.maxSizeAllowed);
                texture.wrapMode = TextureWrapMode.Repeat;
            }
        }

        return texture;
    }
    public static SourceTexture GrabTexture(string rawStringPath)
    {
        SourceTexture srcTexture = null;

        string actualLocation = "";
        bool foundInFileSystem = false, foundInVPK = false;
        if (ApplicationPreferences.useTextures) { actualLocation = LocateInFileSystem(rawStringPath); if (File.Exists(ApplicationPreferences.texturesDir + actualLocation + ".vtf") || File.Exists(ApplicationPreferences.texturesDir + actualLocation + ".png")) foundInFileSystem = true; }
        if (!foundInFileSystem && ApplicationPreferences.useVPK) { actualLocation = LocateInVPK(rawStringPath); if (ApplicationPreferences.vpkParser.FileExists("/materials/" + actualLocation + ".vtf")) foundInVPK = true; }
        if (!foundInFileSystem && !foundInVPK) actualLocation = LocateInResources(rawStringPath);

        if (loadedTextures.ContainsKey(actualLocation))
        {
            srcTexture = loadedTextures[actualLocation];
        }
        else
        {
            srcTexture = new SourceTexture(actualLocation);

            if (foundInFileSystem)
            {
                bool isVTF = true;
                string inFileSystem = ApplicationPreferences.texturesDir + srcTexture.location + ".vtf";
                if (!File.Exists(inFileSystem)) { inFileSystem = ApplicationPreferences.texturesDir + srcTexture.location + ".png"; isVTF = false; }

                byte[] bytes = null;
                try { bytes = File.ReadAllBytes(inFileSystem); } catch (Exception e) { Debug.Log(e.Message); }
                if (bytes != null)
                {
                    if (isVTF)
                    {
                        //srcTexture.texture = LoadVTFFile(bytes);
                        srcTexture.textureType = TextureType.VTF;
                        srcTexture.textureData = bytes;
                    }
                    else
                    {
                        //srcTexture.texture = new Texture2D(0, 0);
                        //srcTexture.texture.LoadImage(bytes);
                        srcTexture.textureType = TextureType.ColorData;
                        srcTexture.textureData = bytes;
                    }
                    bytes = null;
                }
            }
            else if(foundInVPK)
            {
                //srcTexture.texture = LoadVTFFile(ApplicationPreferences.vpkParser.LoadFile("/materials/" + srcTexture.location + ".vtf"));
                srcTexture.textureType = TextureType.VTF;
                srcTexture.textureData = ApplicationPreferences.vpkParser.LoadFile("/materials/" + srcTexture.location + ".vtf");
            }
            else if(plainTextures.ContainsKey(srcTexture.location + ".png"))
            {
                //Color currentPlainTextureColor = plainTextures[srcTexture.location + ".png"];
                //srcTexture.texture = new Texture2D(32, 32);
                //Color[] plainTexturePixels = new Color[srcTexture.texture.width * srcTexture.texture.height];
                //for (int i = 0; i < plainTexturePixels.Length; i++) plainTexturePixels[i] = currentPlainTextureColor;
                //srcTexture.texture.SetPixels(plainTexturePixels);
                //srcTexture.texture.Apply();
                srcTexture.textureType = TextureType.Plain;
                srcTexture.textureData = plainTextures[srcTexture.location + ".png"];
            }
            else
            {
                Debug.Log("Could not find Texture: " + srcTexture.location + " Raw Path: " + rawStringPath);
            }

            //if (srcTexture.texture != null)
            //{
            //    if (ApplicationPreferences.averageTextures) AverageTexture(srcTexture.texture);
            //    else if (ApplicationPreferences.decreaseTextureSizes) DecreaseTextureSize(srcTexture.texture, ApplicationPreferences.maxSizeAllowed);
            //    srcTexture.texture.wrapMode = TextureWrapMode.Repeat;
            //}
        }

        return srcTexture;
    }
    private static string LocateInFileSystem(string rawPath)
    {
        string fixedLocation = PatchNameInFileSystem(RemoveMisleadingPath(rawPath.Replace("\\", "/").ToLower()), "vtf", "png");

        if (!File.Exists(ApplicationPreferences.texturesDir + fixedLocation + ".vtf") && !File.Exists(ApplicationPreferences.texturesDir + fixedLocation + ".png"))
        {
            fixedLocation = PatchNameInFileSystem(RemoveMisleadingPath(rawPath.Replace("\\", "/").ToLower()), "vmt", "txt");

            string vmtFile = "";
            vmtFile = ApplicationPreferences.texturesDir + fixedLocation + ".vmt";
            if (!File.Exists(vmtFile)) vmtFile = ApplicationPreferences.texturesDir + fixedLocation + ".txt";

            string[] vmtLines = null;
            if (File.Exists(vmtFile))
            {
                try { vmtLines = File.ReadAllLines(vmtFile); }
                catch (Exception e) { Debug.Log(e.ToString()); }
            }
            string vmtPointingTo = GetLocationFromVMT(vmtLines);
            if (vmtPointingTo.Length > 0) fixedLocation = PatchNameInFileSystem(RemoveMisleadingPath(vmtPointingTo.Replace("\\", "/").ToLower()), "vtf", "png");
        }

        return fixedLocation;
    }
    private static string LocateInVPK(string rawPath)
    {
        string fixedLocation = PatchNameInVPK(RemoveMisleadingPath(rawPath.Replace("\\", "/").ToLower()), "vtf");

        if (!ApplicationPreferences.vpkParser.FileExists("/materials/" + fixedLocation + ".vtf"))
        {
            fixedLocation = PatchNameInVPK(RemoveMisleadingPath(rawPath.Replace("\\", "/").ToLower()), "vmt");

            //Debug.Log("/materials/" + fixedLocation + ".vmt" + ": " + ApplicationPreferences.vpkParser.FileExists("/materials/" + fixedLocation + ".vmt"));
            string[] vmtLines = null;
            if (ApplicationPreferences.vpkParser.FileExists("/materials/" + fixedLocation + ".vmt"))
            {
                byte[] bytes = ApplicationPreferences.vpkParser.LoadFile("/materials/" + fixedLocation + ".vmt");
                //Debug.Log("File Size: " + bytes.Length);
                vmtLines = ReadAllLines(bytes, System.Text.Encoding.Unicode, true);
            }
            string vmtPointingTo = GetLocationFromVMT(vmtLines);
            //if (vmtLines.Length > 0) Debug.Log("First VMT Line: " + vmtLines[0]);
            if (vmtPointingTo.Length > 0) fixedLocation = PatchNameInVPK(RemoveMisleadingPath(vmtPointingTo.Replace("\\", "/").ToLower()), "vtf");
        }

        return fixedLocation;
    }
    private static string LocateInResources(string rawPath)
    {
        string fixedLocation = PatchNameInResources(RemoveMisleadingPath(rawPath.Replace("\\", "/").ToLower()), "png");

        if (!plainTextures.ContainsKey(fixedLocation))
        {
            fixedLocation = PatchNameInResources(RemoveMisleadingPath(rawPath.Replace("\\", "/").ToLower()), "vmt");

            string[] vmtLines = null;
            if (vmtFiles.ContainsKey(fixedLocation + ".vmt"))
            {
                vmtLines = vmtFiles[fixedLocation + ".vmt"];
            }
            string vmtPointingTo = GetLocationFromVMT(vmtLines);
            if (vmtPointingTo.Length > 0) fixedLocation = PatchNameInResources(RemoveMisleadingPath(vmtPointingTo.Replace("\\", "/").ToLower()), "png");
        }

        return fixedLocation;
    }
    private static string GetLocationFromVMT(string[] vmtLines)
    {
        string textureLocation = "";
        
        if (vmtLines != null)
        {
            string baseTexture = "";

            foreach (string line in vmtLines)
            {
                if (line.IndexOf("$") > -1)
                {
                    if (line.IndexOf("//") < 0 || line.IndexOf("$") < line.IndexOf("//"))
                    {
                        string materialInfo = line.Substring(line.IndexOf("$") + 1);
                        if (materialInfo.ToLower().IndexOf("basetexture") > -1 && !char.IsLetter(materialInfo, materialInfo.ToLower().IndexOf("basetexture") + "basetexture".Length) && !char.IsNumber(materialInfo, materialInfo.ToLower().IndexOf("basetexture") + "basetexture".Length))
                        {
                            if (materialInfo.Length - materialInfo.Replace("\"", "").Length == 3)
                            {
                                baseTexture = materialInfo.Substring(materialInfo.IndexOf("\"") + 1);
                                baseTexture = baseTexture.Substring(baseTexture.IndexOf("\"") + 1);
                                baseTexture = baseTexture.Substring(0, baseTexture.IndexOf("\""));
                            }
                            else if (materialInfo.Length - materialInfo.Replace("\"", "").Length == 2)
                            {
                                baseTexture = materialInfo.Substring(materialInfo.IndexOf("\"") + 1);
                                baseTexture = baseTexture.Substring(0, baseTexture.IndexOf("\""));
                            }
                        }
                    }
                }
            }

            baseTexture = RemoveMisleadingPath(baseTexture.Replace("\\", "/").ToLower());
            if (baseTexture.LastIndexOf(".") == baseTexture.Length - 4) baseTexture = baseTexture.Substring(0, baseTexture.LastIndexOf("."));
            if (baseTexture.Length > 0)
            {
                textureLocation = baseTexture;
            }
        }

        return textureLocation;
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
    public static string PatchNameInFileSystem(string original, params string[] ext)
    {
        string path = ApplicationPreferences.texturesDir.Replace("\\", "/").ToLower();
        string prep = original.Replace("\\", "/").ToLower();
        if (prep.IndexOf("/") == 0) prep = prep.Substring(1);

        string subDir = "";
        List<string> extensions = new List<string>();
        string patched = "";
        extensions.AddRange(ext);

        if (prep.LastIndexOf("/") > -1)
        {
            subDir = prep.Substring(0, prep.LastIndexOf("/") + 1);
            patched = prep.Substring(prep.LastIndexOf("/") + 1);
        }
        else patched = prep;

        //if (extensions.Count > 0 && extensions[0].Equals("vmt", System.StringComparison.InvariantCultureIgnoreCase)) extensions.Add("txt");

        while (patched.Length > 3)
        {
            try
            {
                bool found = false;
                foreach (string extension in extensions)
                {
                    if (File.Exists(path + subDir + patched + "." + extension)) { prep = subDir + patched; found = true; break; }
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
    public static string PatchNameInVPK(string original, params string[] ext)
    {
        //string path = rootPath.Replace("\\", "/").ToLower();
        string prep = original.Replace("\\", "/").ToLower();
        if (prep.IndexOf("/") == 0) prep = prep.Substring(1);

        string subDir = "";
        List<string> extensions = new List<string>();
        string patched = "";
        extensions.AddRange(ext);

        if (prep.LastIndexOf("/") > -1)
        {
            subDir = prep.Substring(0, prep.LastIndexOf("/") + 1);
            patched = prep.Substring(prep.LastIndexOf("/") + 1);
        }
        else patched = prep;

        //bool texturesLookup = true;
        //if (extensions.IndexOf("vmt") > -1 || extensions.IndexOf("txt") > -1) texturesLookup = false;
        //if (extensions.Count > 0 && extensions[0].Equals("vmt", System.StringComparison.InvariantCultureIgnoreCase)) extensions[0] = "txt";

        while (patched.Length > 0)
        {
            try
            {
                bool found = false;
                foreach (string extension in extensions)
                {
                    if (ApplicationPreferences.vpkParser.FileExists("/materials/" + subDir + patched + "." + extension)) { prep = subDir + patched; found = true; break; }
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
    public static string PatchNameInResources(string original, params string[] ext)
    {
        //string path = rootPath.Replace("\\", "/").ToLower();
        string prep = original.Replace("\\", "/").ToLower();
        if (prep.IndexOf("/") == 0) prep = prep.Substring(1);

        string subDir = "";
        List<string> extensions = new List<string>();
        string patched = "";
        extensions.AddRange(ext);

        if (prep.LastIndexOf("/") > -1)
        {
            subDir = prep.Substring(0, prep.LastIndexOf("/") + 1);
            patched = prep.Substring(prep.LastIndexOf("/") + 1);
        }
        else patched = prep;

        bool texturesLookup = true;
        if (extensions.IndexOf("vmt") > -1 || extensions.IndexOf("txt") > -1) texturesLookup = false;
        //if (extensions.Count > 0 && extensions[0].Equals("vmt", System.StringComparison.InvariantCultureIgnoreCase)) extensions[0] = "txt";

        while (patched.Length > 0)
        {
            try
            {
                bool found = false;
                foreach (string extension in extensions)
                {
                    if (texturesLookup) { if (plainTextures.ContainsKey(subDir + patched + "." + extension)) { prep = subDir + patched; found = true; break; } }
                    else { if (vmtFiles.ContainsKey(subDir + patched + "." + extension)) { prep = subDir + patched; found = true; break; } }
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
    /// Removes "maps/" from the path and underscore
    /// </summary>
    /// <param name="original">The path of the texture</param>
    /// <returns>A non misleading path</returns>
    public static string RemoveMisleadingPath(string original)
    {
        string goodPath = original.Replace("\\", "/").ToLower();
        if (goodPath.IndexOf("maps/") > -1)
        {
            goodPath = goodPath.Substring(goodPath.IndexOf("maps/") + ("maps/").Length);
            goodPath = goodPath.Substring(goodPath.IndexOf("/") + 1);
            if(goodPath.IndexOf("_-") > -1)
            {
                string underscoreDashPhenomenon = goodPath.Substring(goodPath.LastIndexOf("_-") + 2);
                goodPath = goodPath.Substring(0, goodPath.LastIndexOf("_-"));
                if (underscoreDashPhenomenon.IndexOf("_") < 0)
                {
                    if (goodPath.IndexOf("_") > -1) goodPath = goodPath.Substring(0, goodPath.LastIndexOf("_"));
                    if (goodPath.IndexOf("_") > -1) goodPath = goodPath.Substring(0, goodPath.LastIndexOf("_"));
                }
            }
            //while (goodPath.LastIndexOf("_") > -1 && (goodPath.Substring(goodPath.LastIndexOf("_") + 1).StartsWith("-") || char.IsDigit(goodPath.Substring(goodPath.LastIndexOf("_") + 1)[0])))
            //{
            //    goodPath = goodPath.Substring(0, goodPath.LastIndexOf("_"));
            //}
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

    public static string[] ReadAllLines(byte[] fileData, System.Text.Encoding encoding, bool detectEncodingFromFile)
    {
        MemoryStream byteStream = new MemoryStream(fileData);
        //StreamReader streamReader = new StreamReader(byteStream, encoding, detectEncodingFromFile);
        StreamReader streamReader = new StreamReader(byteStream);
        List<string> lines = new List<string>();
        string line;
        while((line = streamReader.ReadLine()) != null)
        {
            lines.Add(line);
        }
        byteStream.Close();
        streamReader.Close();
        return lines.ToArray();
    }

    public static void LoadDefaults()
    {
        LoadPlainTextures();
        LoadVMTFiles();
    }
    private static void LoadPlainTextures()
    {
        plainTextures = new Dictionary<string, Color>();

        TextAsset texturesTextAsset = Resources.Load<TextAsset>("Textures/Plain/allTextures");
        string[] allTexturesFileLines = new string[0];
        if (texturesTextAsset != null) allTexturesFileLines = texturesTextAsset.text.Split('\n');
        for (int i = 0; i < allTexturesFileLines.Length; i++)
        {
            string currentTextureLine = allTexturesFileLines[i];
            if (currentTextureLine.Length > 0 && currentTextureLine.Split(' ').Length > 3)
            {
                string[] currentTextureParts = new string[4];
                currentTextureParts[0] = currentTextureLine.Substring(0, currentTextureLine.IndexOf(" "));
                currentTextureLine = currentTextureLine.Substring(currentTextureLine.IndexOf(" ") + 1);
                currentTextureParts[1] = currentTextureLine.Substring(0, currentTextureLine.IndexOf(" "));
                currentTextureLine = currentTextureLine.Substring(currentTextureLine.IndexOf(" ") + 1);
                currentTextureParts[2] = currentTextureLine.Substring(0, currentTextureLine.IndexOf(" "));
                currentTextureLine = currentTextureLine.Substring(currentTextureLine.IndexOf(" ") + 1);
                currentTextureParts[3] = currentTextureLine.Replace("\n", "");
                currentTextureParts[3] = currentTextureParts[3].Replace("\r", "");

                Color currentTextureColor = Color.green;
                try { currentTextureColor = new Color((float)Convert.ToDouble(currentTextureParts[0]), (float)Convert.ToDouble(currentTextureParts[1]), (float)Convert.ToDouble(currentTextureParts[2])); } catch (Exception) { }
                plainTextures.Add(currentTextureParts[3], currentTextureColor);
            }
        }
    }
    private static void LoadVMTFiles()
    {
        vmtFiles = new Dictionary<string, string[]>();

        TextAsset vmtTextAsset = Resources.Load<TextAsset>("Textures/Plain/vmtFiles");
        string[] allVMTFilesLines = new string[0];
        if (vmtTextAsset != null) allVMTFilesLines = vmtTextAsset.text.Split('\n');
        int currentIndex = 0;
        while(currentIndex < allVMTFilesLines.Length)
        {
            string vmtDescriptionLine = allVMTFilesLines[currentIndex];
            if (vmtDescriptionLine.Length > 0 && vmtDescriptionLine.IndexOf(" ") > -1)
            {
                string[] beginningOfVMT = new string[2];
                beginningOfVMT[0] = vmtDescriptionLine.Substring(0, vmtDescriptionLine.IndexOf(" "));
                beginningOfVMT[1] = vmtDescriptionLine.Substring(vmtDescriptionLine.IndexOf(" ") + 1).Replace("\n", "");
                beginningOfVMT[1] = beginningOfVMT[1].Replace("\r", "");
                currentIndex++;
                int linesInVMT = 0;
                try { linesInVMT = Convert.ToInt32(beginningOfVMT[0]); } catch (Exception) { }
                string[] currentVMTFile = new string[linesInVMT];

                vmtFiles.Add(beginningOfVMT[1], currentVMTFile);
                for (int i = 0; i < linesInVMT; i++)
                {
                    currentVMTFile[i] = allVMTFilesLines[currentIndex];
                    currentIndex++;
                }
            }
            else currentIndex++;
        }
    }

    public static Texture2D LoadVTFFile(byte[] vtfBytes)
    {
        Texture2D extracted = null;
        if (vtfBytes != null)
        {
            MemoryStream stream = new MemoryStream(vtfBytes);
            //Debug.Log("File Size: " + vtfBytes.Length + ", " + stream.Length);
            int signature = DataParser.ReadInt(stream); //+4=4
            if (signature == VTFHeader.signature)
            {
                #region Read Header
                VTFHeader vtfHeader;
                uint[] version = new uint[] { DataParser.ReadUInt(stream), DataParser.ReadUInt(stream) };
                vtfHeader.version = (version[0]) + (version[1] / 10f); //+8=12
                vtfHeader.headerSize = DataParser.ReadUInt(stream); //+4=16
                vtfHeader.width = DataParser.ReadUShort(stream); //+2=18
                vtfHeader.height = DataParser.ReadUShort(stream); //+2=20
                vtfHeader.flags = DataParser.ReadUInt(stream); //+4=24
                vtfHeader.frames = DataParser.ReadUShort(stream); //+2=26
                vtfHeader.firstFrame = DataParser.ReadUShort(stream); //+2=28
                vtfHeader.padding0 = DataParser.ReadBytes(stream, 4); //+4=32
                vtfHeader.reflectivity = new float[] { DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream) }; //+12=44
                vtfHeader.padding1 = DataParser.ReadBytes(stream, 4); //+4=48
                vtfHeader.bumpmapScale = DataParser.ReadFloat(stream); //+4=52
                vtfHeader.highResImageFormat = (VTFImageFormat)DataParser.ReadUInt(stream); //+4=56
                vtfHeader.mipmapCount = DataParser.ReadByte(stream); //+1=57
                vtfHeader.lowResImageFormat = (VTFImageFormat)DataParser.ReadUInt(stream); //+4=61
                vtfHeader.lowResImageWidth = DataParser.ReadByte(stream); //+1=62
                vtfHeader.lowResImageHeight = DataParser.ReadByte(stream); //+1=63

                vtfHeader.depth = 1;
                vtfHeader.resourceCount = 0;
                vtfHeader.resources = new VTFResource[0];

                if (vtfHeader.version >= 7.2f)
                {
                    vtfHeader.depth = DataParser.ReadUShort(stream); //+2=65

                    if (vtfHeader.version >= 7.3)
                    {
                        vtfHeader.padding2 = DataParser.ReadBytes(stream, 3); //+3=68
                        vtfHeader.resourceCount = DataParser.ReadUInt(stream); //+4=72

                        if (vtfHeader.version >= 7.4)
                        {
                            vtfHeader.padding3 = DataParser.ReadBytes(stream, 8); //+8=80
                            vtfHeader.resources = new VTFResource[vtfHeader.resourceCount];
                            for (int i = 0; i < vtfHeader.resources.Length; i++)
                            {
                                vtfHeader.resources[i].type = DataParser.ReadUInt(stream);
                                //vtfHeader.resources[i].id = FileReader.ReadBytes(stream, 3);
                                //vtfHeader.resources[i].flags = FileReader.ReadByte(stream);
                                vtfHeader.resources[i].data = DataParser.ReadUInt(stream);
                            } //+(8*resourceCount)=76+(8*resourcesCount)
                        }
                    }
                }

                //Debug.Log(vtfHeader.version + ", " + vtfHeader.headerSize + ", " + vtfHeader.width + ", " + vtfHeader.height + ", " + vtfHeader.flags + ", " + vtfHeader.frames + ", " + vtfHeader.firstFrame + ", " + vtfHeader.bumpmapScale + ", " + vtfHeader.highResImageFormat + ", " + vtfHeader.mipmapCount + ", " + vtfHeader.lowResImageFormat + ", " + vtfHeader.lowResImageWidth + ", " + vtfHeader.lowResImageHeight + ", " + vtfHeader.depth + ", " + vtfHeader.resourceCount);
                #endregion

                uint thumbnailBufferSize = 0, imageBufferSize = ComputeImageBufferSize(vtfHeader.width, vtfHeader.height, vtfHeader.depth, vtfHeader.mipmapCount, vtfHeader.highResImageFormat) * vtfHeader.frames;
                if (vtfHeader.lowResImageFormat != VTFImageFormat.IMAGE_FORMAT_NONE) thumbnailBufferSize = ComputeImageBufferSize(vtfHeader.lowResImageWidth, vtfHeader.lowResImageHeight, 1, vtfHeader.lowResImageFormat);

                uint thumbnailBufferOffset = 0, imageBufferOffset = 0;

                #region Read Resource Directories
                if (vtfHeader.resources.Length > 0)
                {
                    for (int i = 0; i < vtfHeader.resources.Length; i++)
                    {
                        //Debug.Log(vtfHeader.resources[i].data + ", " + (VTFResourceEntryType)vtfHeader.resources[i].type + ", " + (VTFResourceEntryFlag)vtfHeader.resources[i].flags);
                        if ((VTFResourceEntryType)vtfHeader.resources[i].type == VTFResourceEntryType.VTF_LEGACY_RSRC_LOW_RES_IMAGE)
                        {
                            thumbnailBufferOffset = vtfHeader.resources[i].data;
                        }
                        if ((VTFResourceEntryType)vtfHeader.resources[i].type == VTFResourceEntryType.VTF_LEGACY_RSRC_IMAGE)
                        {
                            imageBufferOffset = vtfHeader.resources[i].data;
                        }
                    }
                }
                else
                {
                    thumbnailBufferOffset = vtfHeader.headerSize;
                    imageBufferOffset = thumbnailBufferOffset + thumbnailBufferSize;
                }
                #endregion

                if (vtfHeader.highResImageFormat != VTFImageFormat.IMAGE_FORMAT_NONE)
                {
                    uint mipmapBufferOffset = 0;
                    for(uint i = 1; i <= vtfHeader.mipmapCount; i++)
                    {
                        mipmapBufferOffset += ComputeMipmapSize(vtfHeader.width, vtfHeader.height, vtfHeader.depth, i, vtfHeader.highResImageFormat);
                    }
                    stream.Position = imageBufferOffset + mipmapBufferOffset;
                    byte[] imageData = DataParser.ReadBytes(stream, imageBufferSize);
                    //Debug.Log("Image Buffer Info: " + imageBufferOffset + ", " + imageBufferSize);

                    Color[] vtfColors = DecompressImage(imageData, vtfHeader.width, vtfHeader.height, vtfHeader.highResImageFormat);

                    extracted = new Texture2D(vtfHeader.width, vtfHeader.height);
                    extracted.SetPixels(vtfColors);
                    extracted.Apply();
                }
            }
            else Debug.Log("Signature Mismatch: " + signature + " != " + VTFHeader.signature);

            stream.Close();
        }
        return extracted;
    }
    private static Color[] DecompressImage(byte[] data, ushort width, ushort height, VTFImageFormat imageFormat)
    {
        Color[] vtfColors = new Color[width * height];

        if (imageFormat == VTFImageFormat.IMAGE_FORMAT_DXT1 || imageFormat == VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA) vtfColors = DecompressDXT1(data, width, height);
        else if (imageFormat == VTFImageFormat.IMAGE_FORMAT_DXT3) vtfColors = DecompressDXT3(data, width, height);
        else if (imageFormat == VTFImageFormat.IMAGE_FORMAT_DXT5) vtfColors = DecompressDXT5(data, width, height);

        vtfColors = RotateProperly(vtfColors, width, height);

        return vtfColors;
    }
    private static Color[] DecompressDXT1(byte[] data, ushort width, ushort height)
    {
        Color[] texture2DColors = new Color[width * height];

        int currentDataIndex = 0;
        bool exceededArray = false;
        for (int row = 0; row < height; row += 4)
        {
            for (int col = 0; col < width; col += 4)
            {
                ushort color0Data = 0;
                ushort color1Data = 0;
                uint bitmask = 0;

                if (currentDataIndex + 7 < data.Length)
                {
                    color0Data = BitConverter.ToUInt16(data, currentDataIndex);
                    color1Data = BitConverter.ToUInt16(data, currentDataIndex + 2);
                    bitmask = BitConverter.ToUInt32(data, currentDataIndex + 4);
                }
                else { Debug.Log("Error: " + currentDataIndex + " > " + data.Length); exceededArray = true; break; }
                currentDataIndex += 8;

                int[] colors0 = new int[] { ((color0Data >> 11) & 0x1F) << 3, ((color0Data >> 5) & 0x3F) << 2, (color0Data & 0x1F) << 3 };
                int[] colors1 = new int[] { ((color1Data >> 11) & 0x1F) << 3, ((color1Data >> 5) & 0x3F) << 2, (color1Data & 0x1F) << 3 };

                Color[] colorPalette = new Color[]
                {
                    new Color(colors0[0] / 255f, colors0[1] / 255f, colors0[2] / 255f),
                    new Color(colors1[0] / 255f, colors1[1] / 255f, colors1[2] / 255f),
                    new Color(((colors0[0] * 2 + colors1[0] + 1) / 3) / 255f, ((colors0[1] * 2 + colors1[1] + 1) / 3) / 255f, ((colors0[2] * 2 + colors1[2] + 1) / 3) / 255f),
                    new Color(((colors1[0] * 2 + colors0[0] + 1) / 3) / 255f, ((colors1[1] * 2 + colors0[1] + 1) / 3) / 255f, ((colors1[2] * 2 + colors0[2] + 1) / 3) / 255f)
                };

                if(color0Data < color1Data)
                {
                    colorPalette[2] = new Color(((colors0[0] + colors1[0]) / 2) / 255f, ((colors0[1] + colors1[1]) / 2) / 255f, ((colors0[2] + colors1[2]) / 2) / 255f);
                    colorPalette[3] = new Color(((colors1[0] * 2 + colors0[0] + 1) / 3) / 255f, ((colors1[1] * 2 + colors0[1] + 1) / 3) / 255f, ((colors1[2] * 2 + colors0[2] + 1) / 3) / 255f);
                }

                int blockIndex = 0;
                for (int blockY = 0; blockY < 4; blockY++)
                {
                    for (int blockX = 0; blockX < 4; blockX++)
                    {
                        Color colorInBlock = colorPalette[(bitmask & (0x03 << blockIndex * 2)) >> blockIndex * 2];
                        texture2DColors[((row * width) + col) + ((blockY * width) + blockX)] = colorInBlock;
                        blockIndex++;
                    }
                }
            }
            if (exceededArray) break;
        }

        return texture2DColors.ToArray();
    }
    private static Color[] DecompressDXT3(byte[] data, ushort width, ushort height)
    {
        Color[] texture2DColors = new Color[width * height];

        int currentDataIndex = 0;
        bool exceededArray = false;
        for (int row = 0; row < height; row += 4)
        {
            for (int col = 0; col < width; col += 4)
            {
                ushort color0Data = 0;
                ushort color1Data = 0;
                uint bitmask = 0;

                currentDataIndex += 8;
                if (currentDataIndex + 7 < data.Length)
                {
                    color0Data = BitConverter.ToUInt16(data, currentDataIndex);
                    color1Data = BitConverter.ToUInt16(data, currentDataIndex + 2);
                    bitmask = BitConverter.ToUInt32(data, currentDataIndex + 4);
                }
                else { Debug.Log("Error: " + currentDataIndex + " > " + data.Length); exceededArray = true; break; }
                currentDataIndex += 8;

                int[] colors0 = new int[] { ((color0Data >> 11) & 0x1F) << 3, ((color0Data >> 5) & 0x3F) << 2, (color0Data & 0x1F) << 3 };
                int[] colors1 = new int[] { ((color1Data >> 11) & 0x1F) << 3, ((color1Data >> 5) & 0x3F) << 2, (color1Data & 0x1F) << 3 };

                Color[] colorPalette = new Color[]
                {
                    new Color(colors0[0] / 255f, colors0[1] / 255f, colors0[2] / 255f),
                    new Color(colors1[0] / 255f, colors1[1] / 255f, colors1[2] / 255f),
                    new Color(((colors0[0] * 2 + colors1[0] + 1) / 3) / 255f, ((colors0[1] * 2 + colors1[1] + 1) / 3) / 255f, ((colors0[2] * 2 + colors1[2] + 1) / 3) / 255f),
                    new Color(((colors1[0] * 2 + colors0[0] + 1) / 3) / 255f, ((colors1[1] * 2 + colors0[1] + 1) / 3) / 255f, ((colors1[2] * 2 + colors0[2] + 1) / 3) / 255f)
                };

                if (color0Data < color1Data)
                {
                    colorPalette[2] = new Color(((colors0[0] + colors1[0]) / 2) / 255f, ((colors0[1] + colors1[1]) / 2) / 255f, ((colors0[2] + colors1[2]) / 2) / 255f);
                    colorPalette[3] = new Color(((colors1[0] * 2 + colors0[0] + 1) / 3) / 255f, ((colors1[1] * 2 + colors0[1] + 1) / 3) / 255f, ((colors1[2] * 2 + colors0[2] + 1) / 3) / 255f);
                }

                int blockIndex = 0;
                for (int blockY = 0; blockY < 4; blockY++)
                {
                    for (int blockX = 0; blockX < 4; blockX++)
                    {
                        Color colorInBlock = colorPalette[(bitmask & (0x03 << blockIndex * 2)) >> blockIndex * 2];
                        texture2DColors[((row * width) + col) + ((blockY * width) + blockX)] = colorInBlock;
                        blockIndex++;
                    }
                }
            }
            if (exceededArray) break;
        }

        return texture2DColors.ToArray();
    }
    private static Color[] DecompressDXT5(byte[] data, ushort width, ushort height)
    {
        Color[] texture2DColors = new Color[width * height];

        int currentDataIndex = 0;
        bool exceededArray = false;
        for(int row = 0; row < height; row += 4)
        {
            for(int col = 0; col < width; col += 4)
            {
                #region Alpha Information
                byte alpha0Data = 0;
                byte alpha1Data = 0;
                uint alphamask = 0;

                if(currentDataIndex + 7 < data.Length)
                {
                    alpha0Data = data[currentDataIndex];
                    alpha1Data = data[currentDataIndex + 1];
                    alphamask = BitConverter.ToUInt32(new byte[] { data[currentDataIndex + 2], data[currentDataIndex + 3], data[currentDataIndex + 4], data[currentDataIndex + 5], data[currentDataIndex + 6], data[currentDataIndex + 7] }, 0);
                }
                else { Debug.Log("Error: " + currentDataIndex + " > " + data.Length); exceededArray = true; break; }
                currentDataIndex += 8;

                float[] alphaPalette = new float[]
                {
                    alpha0Data / 255f, 
                    alpha1Data / 255f, 
                    ((6 * alpha0Data + 1 * alpha1Data + 3) / 7) / 255f, 
                    ((5 * alpha0Data + 2 * alpha1Data + 3) / 7) / 255f, 
                    ((4 * alpha0Data + 3 * alpha1Data + 3) / 7) / 255f, 
                    ((3 * alpha0Data + 4 * alpha1Data + 3) / 7) / 255f, 
                    ((2 * alpha0Data + 5 * alpha1Data + 3) / 7) / 255f, 
                    ((1 * alpha0Data + 6 * alpha1Data + 3) / 7) / 255f
                };
                  
                if (alpha0Data <= alpha1Data)
                {
                    alphaPalette[2] = (4 * alpha0Data + 1 * alpha1Data + 2) / 5;
                    alphaPalette[3] = (3 * alpha0Data + 2 * alpha1Data + 2) / 5;
                    alphaPalette[4] = (2 * alpha0Data + 3 * alpha1Data + 2) / 5;
                    alphaPalette[5] = (1 * alpha0Data + 4 * alpha1Data + 2) / 5;
                    alphaPalette[6] = 0;
                    alphaPalette[7] = 1;
                }
                #endregion

                #region Color Information
                ushort color0Data = 0;
                ushort color1Data = 0;
                uint bitmask = 0;

                if (currentDataIndex + 7 < data.Length)
                {
                    color0Data = BitConverter.ToUInt16(data, currentDataIndex);
                    color1Data = BitConverter.ToUInt16(data, currentDataIndex + 2);
                    bitmask = BitConverter.ToUInt32(data, currentDataIndex + 4);
                }
                else { Debug.Log("Error: " + currentDataIndex + " > " + data.Length); exceededArray = true; break; }
                currentDataIndex += 8;

                int[] colors0 = new int[] { ((color0Data >> 11) & 0x1F) << 3, ((color0Data >> 5) & 0x3F) << 2, (color0Data & 0x1F) << 3 };
                int[] colors1 = new int[] { ((color1Data >> 11) & 0x1F) << 3, ((color1Data >> 5) & 0x3F) << 2, (color1Data & 0x1F) << 3 };

                Color[] colorPalette = new Color[]
                {
                    new Color(colors0[0] / 255f, colors0[1] / 255f, colors0[2] / 255f),
                    new Color(colors1[0] / 255f, colors1[1] / 255f, colors1[2] / 255f),
                    new Color(((colors0[0] * 2 + colors1[0] + 1) / 3) / 255f, ((colors0[1] * 2 + colors1[1] + 1) / 3) / 255f, ((colors0[2] * 2 + colors1[2] + 1) / 3) / 255f),
                    new Color(((colors1[0] * 2 + colors0[0] + 1) / 3) / 255f, ((colors1[1] * 2 + colors0[1] + 1) / 3) / 255f, ((colors1[2] * 2 + colors0[2] + 1) / 3) / 255f)
                };
                #endregion

                #region Place All Information
                int blockIndex = 0;
                uint alphaBlockIndex1 = alphamask & 0x07, alphaBlockIndex2 = alphamask & 0x38;
                for(int blockY = 0; blockY < 4; blockY++)
                {
                    for(int blockX = 0; blockX < 4; blockX++)
                    {
                        Color colorInBlock = colorPalette[(bitmask & (0x03 << blockIndex * 2)) >> blockIndex * 2];
                        if (blockY < 2) colorInBlock.a = alphaPalette[alphaBlockIndex1 & 0x07];
                        else colorInBlock.a = alphaPalette[alphaBlockIndex2 & 0x07];
                        texture2DColors[((row * width) + col) + ((blockY * width) + blockX)] = colorInBlock;
                        blockIndex++;
                    }
                    alphaBlockIndex1 >>= 3;
                    alphaBlockIndex2 >>= 3;
                }
                #endregion
            }
            if (exceededArray) break;
        }

        return texture2DColors.ToArray();
    }
    private static uint ComputeImageBufferSize(uint width, uint height, uint depth, VTFImageFormat imageFormat)
    {
        uint tempWidth = width, tempHeight = height;

        if(imageFormat == VTFImageFormat.IMAGE_FORMAT_DXT1 || imageFormat == VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA)
        {
            if (tempWidth < 4 && tempWidth > 0)
                tempWidth = 4;

            if (tempHeight < 4 && tempHeight > 0)
                tempHeight = 4;

            return ((tempWidth + 3) / 4) * ((tempHeight + 3) / 4) * 8 * depth;
        }
        else if(imageFormat == VTFImageFormat.IMAGE_FORMAT_DXT3 || imageFormat == VTFImageFormat.IMAGE_FORMAT_DXT5)
        {
            if (tempWidth < 4 && tempWidth > 0)
                tempWidth = 4;

            if (tempHeight < 4 && tempHeight > 0)
                tempHeight = 4;

            return ((tempWidth + 3) / 4) * ((tempHeight + 3) / 4) * 16 * depth;
        }
        else return (uint)(tempWidth * tempHeight * depth * VTFImageConvertInfo[(int)imageFormat, (int)VTFImageConvertInfoIndex.bytesPerPixel]);
    }
    private static uint ComputeImageBufferSize(uint width, uint height, uint depth, uint mipmaps, VTFImageFormat imageFormat)
    {
        uint uiImageSize = 0, tempWidth = width, tempHeight = height;

        if (tempWidth > 0 && tempHeight > 0 && depth > 0)
        {
            for (int i = 0; i < mipmaps; i++)
            {
                uiImageSize += ComputeImageBufferSize(tempWidth, tempHeight, depth, imageFormat);

                tempWidth >>= 1;
                tempHeight >>= 1;
                depth >>= 1;

                if (tempWidth < 1)
                    tempWidth = 1;

                if (tempHeight < 1)
                    tempHeight = 1;

                if (depth < 1)
                    depth = 1;
            }
        }

        return uiImageSize;
    }
    private static void ComputeMipmapDimensions(uint width, uint height, uint depth, uint mipmapLevel, out uint mipmapWidth, out uint mipmapHeight, out uint mipmapDepth)
    {
        // work out the width/height by taking the orignal dimension
        // and bit shifting them down uiMipmapLevel times
        mipmapWidth = width >> (int)mipmapLevel;
        mipmapHeight = height >> (int)mipmapLevel;
        mipmapDepth = depth >> (int)mipmapLevel;

        // stop the dimension being less than 1 x 1
        if (mipmapWidth < 1)
            mipmapWidth = 1;

        if (mipmapHeight < 1)
            mipmapHeight = 1;

        if (mipmapDepth < 1)
            mipmapDepth = 1;
    }
    private static uint ComputeMipmapSize(uint width, uint height, uint depth, uint mipmapLevel, VTFImageFormat ImageFormat)
    {
        // figure out the width/height of this MIP level
        uint uiMipmapWidth, uiMipmapHeight, uiMipmapDepth;
        ComputeMipmapDimensions(width, height, depth, mipmapLevel, out uiMipmapWidth, out uiMipmapHeight, out uiMipmapDepth);

        // return the memory requirements
        return ComputeImageBufferSize(uiMipmapWidth, uiMipmapHeight, uiMipmapDepth, ImageFormat);
    }
    private static Color[] RotateProperly(Color[] vtfColors, uint width, uint height)
    {
        Color[] proper = vtfColors.Reverse().ToArray();
        for(uint row = 0; row < height; row++)
        {
            for(uint col = 0; col < (width / 2); col++)
            {
                uint currentRowIndex = row * width;
                Color tempColor = proper[currentRowIndex + col];
                proper[currentRowIndex + col] = proper[currentRowIndex + (width - col - 1)];
                proper[currentRowIndex + (width - col - 1)] = tempColor;
            }
        }
        return proper;
    }

    #region Image Convert Info
    enum VTFImageConvertInfoIndex
    {
        bitsPerPixel, // Format bits per color.
        bytesPerPixel, // Format bytes per pixel.
        redBitsPerPixel, // Format conversion red bits per pixel.  0 for N/A.
        greenBitsPerPixel, // Format conversion green bits per pixel.  0 for N/A.
        blueBitsPerPixel, // Format conversion blue bits per pixel.  0 for N/A.
        alphaBitsPerPixel, // Format conversion alpha bits per pixel.  0 for N/A.
        redIndex, // "Red" index.
        greenIndex, // "Green" index.
        blueIndex, // "Blue" index.
        alphaIndex, // "Alpha" index.
    }

    static short[,] VTFImageConvertInfo = new short[,] {
        { 32,  4,  8,  8,  8,  8, 0,  1,  2,  3 },
        { 32,  4,  8,  8,  8,  8, 3,  2,  1,  0 },
        { 24,  3,  8,  8,  8,  0, 0,  1,  2, -1 },
        { 24,  3,  8,  8,  8,  0, 2,  1,  0, -1 },
        { 16,  2,  5,  6,  5,  0, 0,  1,  2, -1 },
        { 8,  1,  8,  8,  8,  0, 0, -1, -1, -1 },
        { 16,  2,  8,  8,  8,  8, 0, -1, -1,  1 },
        { 8,  1,  0,  0,  0,  0, -1, -1, -1, -1 },
        { 8,  1,  0,  0,  0,  8, -1, -1, -1,  0 },
        { 24,  3,  8,  8,  8,  8, 0,  1,  2, -1 },
        { 24,  3,  8,  8,  8,  8, 2,  1,  0, -1 },
        { 32,  4,  8,  8,  8,  8, 3,  0,  1,  2 },
        { 32,  4,  8,  8,  8,  8, 2,  1,  0,  3 },
        { 4,  0,  0,  0,  0,  0, -1, -1, -1, -1 },
        { 8,  0,  0,  0,  0,  8, -1, -1, -1, -1 },
        { 8,  0,  0,  0,  0,  8, -1, -1, -1, -1 },
        { 32,  4,  8,  8,  8,  0, 2,  1,  0, -1 },
        { 16,  2,  5,  6,  5,  0, 2,  1,  0, -1 },
        { 16,  2,  5,  5,  5,  0, 2,  1,  0, -1 },
        { 16,  2,  4,  4,  4,  4, 2,  1,  0,  3 },
        { 4,  0,  0,  0,  0,  1, -1, -1, -1, -1 },
        { 16,  2,  5,  5,  5,  1, 2,  1,  0,  3 },
        { 16,  2,  8,  8,  0,  0, 0,  1, -1, -1 },
        { 32,  4,  8,  8,  8,  8, 0,  1,  2,  3 },
        { 64,  8, 16, 16, 16, 16, 0,  1,  2,  3 },
        { 64,  8, 16, 16, 16, 16, 0,  1,  2,  3 },
        { 32,  4,  8,  8,  8,  8, 0,  1,  2,  3 },
        { 32,  4, 32,  0,  0,  0, 0, -1, -1, -1 },
        { 96, 12, 32, 32, 32,  0, 0,  1,  2, -1 },
        { 128, 16, 32, 32, 32, 32, 0,  1,  2,  3 },
        { 16,  2, 16,  0,  0,  0, 0, -1, -1, -1 },
        { 24,  3, 24,  0,  0,  0, 0, -1, -1, -1 },
        { 32,  4,  0,  0,  0,  0, -1, -1, -1, -1 },
        { 24,  3,  0,  0,  0,  0, -1, -1, -1, -1 },
        { 16,  2, 16,  0,  0,  0, 0, -1, -1, -1 },
        { 24,  3, 24,  0,  0,  0, 0, -1, -1, -1 },
        { 32,  4,  0,  0,  0,  0, -1, -1, -1, -1 },
        { 4,  0,  0,  0,  0,  0, -1, -1, -1, -1 },
        { 8,  0,  0,  0,  0,  0, -1, -1, -1, -1 }
    };
    #endregion
    #region Texture Flags
    [System.Flags]
    enum VTFImageFlag
    {
        TEXTUREFLAGS_POINTSAMPLE = 0x00000001,
        TEXTUREFLAGS_TRILINEAR = 0x00000002,
        TEXTUREFLAGS_CLAMPS = 0x00000004,
        TEXTUREFLAGS_CLAMPT = 0x00000008,
        TEXTUREFLAGS_ANISOTROPIC = 0x00000010,
        TEXTUREFLAGS_HINT_DXT5 = 0x00000020,
        TEXTUREFLAGS_SRGB = 0x00000040, // Originally internal to VTex as TEXTUREFLAGS_NOCOMPRESS.
        TEXTUREFLAGS_DEPRECATED_NOCOMPRESS = 0x00000040,
        TEXTUREFLAGS_NORMAL = 0x00000080,
        TEXTUREFLAGS_NOMIP = 0x00000100,
        TEXTUREFLAGS_NOLOD = 0x00000200,
        TEXTUREFLAGS_MINMIP = 0x00000400,
        TEXTUREFLAGS_PROCEDURAL = 0x00000800,
        TEXTUREFLAGS_ONEBITALPHA = 0x00001000, //!< Automatically generated by VTex.
        TEXTUREFLAGS_EIGHTBITALPHA = 0x00002000, //!< Automatically generated by VTex.
        TEXTUREFLAGS_ENVMAP = 0x00004000,
        TEXTUREFLAGS_RENDERTARGET = 0x00008000,
        TEXTUREFLAGS_DEPTHRENDERTARGET = 0x00010000,
        TEXTUREFLAGS_NODEBUGOVERRIDE = 0x00020000,
        TEXTUREFLAGS_SINGLECOPY = 0x00040000,
        TEXTUREFLAGS_UNUSED0 = 0x00080000, //!< Originally internal to VTex as TEXTUREFLAGS_ONEOVERMIPLEVELINALPHA.
        TEXTUREFLAGS_DEPRECATED_ONEOVERMIPLEVELINALPHA = 0x00080000,
        TEXTUREFLAGS_UNUSED1 = 0x00100000, //!< Originally internal to VTex as TEXTUREFLAGS_PREMULTCOLORBYONEOVERMIPLEVEL.
        TEXTUREFLAGS_DEPRECATED_PREMULTCOLORBYONEOVERMIPLEVEL = 0x00100000,
        TEXTUREFLAGS_UNUSED2 = 0x00200000, //!< Originally internal to VTex as TEXTUREFLAGS_NORMALTODUDV.
        TEXTUREFLAGS_DEPRECATED_NORMALTODUDV = 0x00200000,
        TEXTUREFLAGS_UNUSED3 = 0x00400000, //!< Originally internal to VTex as TEXTUREFLAGS_ALPHATESTMIPGENERATION.
        TEXTUREFLAGS_DEPRECATED_ALPHATESTMIPGENERATION = 0x00400000,
        TEXTUREFLAGS_NODEPTHBUFFER = 0x00800000,
        TEXTUREFLAGS_UNUSED4 = 0x01000000, //!< Originally internal to VTex as TEXTUREFLAGS_NICEFILTERED.
        TEXTUREFLAGS_DEPRECATED_NICEFILTERED = 0x01000000,
        TEXTUREFLAGS_CLAMPU = 0x02000000,
        TEXTUREFLAGS_VERTEXTEXTURE = 0x04000000,
        TEXTUREFLAGS_SSBUMP = 0x08000000,
        TEXTUREFLAGS_UNUSED5 = 0x10000000, //!< Originally TEXTUREFLAGS_UNFILTERABLE_OK.
        TEXTUREFLAGS_DEPRECATED_UNFILTERABLE_OK = 0x10000000,
        TEXTUREFLAGS_BORDER = 0x20000000,
        TEXTUREFLAGS_DEPRECATED_SPECVAR_RED = 0x40000000,
        //TEXTUREFLAGS_DEPRECATED_SPECVAR_ALPHA = 0x80000000,
        TEXTUREFLAGS_LAST = 0x20000000,
        TEXTUREFLAGS_COUNT = 30
    }
    #endregion
}
