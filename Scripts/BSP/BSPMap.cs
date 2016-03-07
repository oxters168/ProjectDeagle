using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BSPMap
{
    #region Map Variables
    public static Dictionary<string, BSPMap> loadedMaps = new Dictionary<string, BSPMap>();
    public string mapName;
    public GameObject mapGameObject;

    public string mapLocation;
    //public static bool averageTextures = false, decreaseTextureSizes = true, combineMeshes = true;
    //public static int maxSizeAllowed = 128;
    //public static string mapsDir = "D:/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/maps/";
    //public static string mapsLocation = "/storage/emulated/0/Download/CSGO/Maps/";
    //public static string texturesDir = "D:/CSGOModels/Textures/";
    //public static string texturesDir = "/storage/emulated/0/Download/CSGO/Textures/";
    private static List<Texture2D> mapTextures;
    private static List<string> textureLocations;

    private Material mainSurfaceMaterial = Resources.Load<Material>("Materials/MapMaterial");

    private Vector3[] vertices;
    //private dplane_t[] planes;
    private dedge_t[] edges;
    //private dface_t[] origFaces;
    private dface_t[] faces;
    private int[] surfedges;

    //private dbrush_t[] brushes;
    //private dbrushside_t[] brushSides;
    //private ddispinfo_t[] dispInfo;
    //private dDispVert[] dispVerts;

    private texinfo_t[] texInfo;
    private dtexdata_t[] texData;
    private int[] texStringTable;
    private string textureStringData;

    public bool alreadyMade = false;
    #endregion

    public BSPMap(string mName)
    {
        mapName = mName;
        if (mapName.LastIndexOf(".") == mapName.Length - 4) mapName = mapName.Substring(0, mapName.LastIndexOf("."));
        mapLocation = ApplicationPreferences.mapsDir;
        if (!loadedMaps.ContainsKey(mapName)) loadedMaps.Add(mapName, this);
        else alreadyMade = true;
    }

    //public void BuildMap()
    //{
    //    StartCoroutine("StartBuilding");
    //}

    public void BuildMap()
    {
        mapTextures = new List<Texture2D>();
        textureLocations = new List<string>();
        bool usingPlainTextures = false;

        System.IO.FileStream mapFile = null;
        try
        {
            if (mapLocation.Length > 0 && System.IO.File.Exists(mapLocation + mapName + ".bsp")) mapFile = new System.IO.FileStream(mapLocation + mapName + ".bsp", System.IO.FileMode.Open);
            else if (System.IO.File.Exists("Assets\\Resources\\Maps\\" + mapName + ".bsp")) mapFile = new System.IO.FileStream("Assets\\Resources\\Maps\\" + mapName + ".bsp", System.IO.FileMode.Open);
        }
        catch (System.Exception e) { Debug.Log(e.Message); }

        if (mapFile != null)
        {
            #region Read map
            BSPParser bsp = new BSPParser(mapFile);

            string entities = bsp.GetEntities();
            Debug.Log("Map Entities: " + entities);
            vertices = bsp.GetVertices();
            
            //vertices = bsp.lumpData[3];
            //planes = bsp.GetPlanes();
            edges = bsp.GetEdges();
            //origFaces = bsp.GetOriginalFaces();
            faces = bsp.GetFaces();
            surfedges = bsp.GetSurfedges();

            //brushes = bsp.GetBrushes();
            //brushSides = bsp.GetBrushSides();
            //dispInfo = bsp.GetDispInfo();
            //dispVerts = bsp.GetDispVerts();

            texInfo = bsp.GetTextureInfo();
            texData = bsp.GetTextureData();
            texStringTable = bsp.GetTextureStringTable();
            textureStringData = bsp.GetTextureStringData();

            mapFile.Close();
            #endregion

            List<string> undesiredTextures = new List<string>(new string[] { "TOOLS/TOOLSTRIGGER", "TOOLS/TOOLSBLACK", "TOOLS/CLIMB", "TOOLS/CLIMB_ALPHA", "TOOLS/FOGVOLUME", "TOOLS/TOOLSAREAPORTAL-DX10", "TOOLS/TOOLSBLACK", "TOOLS/TOOLSBLOCK_LOS",
                "TOOLS/TOOLSBLOCK_LOS-DX10", "TOOLS/TOOLSBLOCKBOMB", "TOOLS/TOOLSBLOCKBULLETS", "TOOLS/TOOLSBLOCKBULLETS-DX10", "TOOLS/TOOLSBLOCKLIGHT", "TOOLS/TOOLSCLIP", "TOOLS/TOOLSCLIP-DX10", "TOOLS/TOOLSDOTTED", "TOOLS/TOOLSFOG", "TOOLS/TOOLSFOG-DX10",
                "TOOLS/TOOLSHINT", "TOOLS/TOOLSHINT-DX10", "TOOLS/TOOLSINVISIBLE", "TOOLS/TOOLSINVISIBLE-DX10", "TOOLS/TOOLSINVISIBLELADDER", "TOOLS/TOOLSNODRAW", "TOOLS/TOOLSNPCCLIP", "TOOLS/TOOLSOCCLUDER", "TOOLS/TOOLSOCCLUDER-DX10", "TOOLS/TOOLSORIGIN",
                "TOOLS/TOOLSPLAYERCLIP", "TOOLS/TOOLSPLAYERCLIP-DX10", "TOOLS/TOOLSSKIP", "TOOLS/TOOLSSKIP-DX10", "TOOLS/TOOLSSKYBOX2D", "TOOLS/TOOLSSKYFOG", "TOOLS/TOOLSTRIGGER", "TOOLS/TOOLSTRIGGER-DX10" });

            mainSurfaceMaterial = Resources.Load<Material>("Materials/MapMaterial");
            mapGameObject = new GameObject(mapName);

            List<FaceMesh> allFaces = new List<FaceMesh>();
            #region Parse Faces
            foreach (dface_t face in faces)
            {
                FaceMesh currentFace = new FaceMesh();
                currentFace.face = face;

                #region Get Texture Info
                texflags textureFlag = texflags.SURF_NODRAW;
                try { textureFlag = ((texflags)texInfo[face.texinfo].flags); }
                catch (System.Exception) { }

                currentFace.rawTexture = textureStringData.Substring(Mathf.Abs(texStringTable[Mathf.Abs(texData[Mathf.Abs(texInfo[Mathf.Abs(face.texinfo)].texdata)].nameStringTableID)]));
                currentFace.rawTexture = currentFace.rawTexture.Substring(0, currentFace.rawTexture.IndexOf(BSPParser.TEXTURE_STRING_DATA_SPLITTER));
                currentFace.rawTexture = RemoveMisleadingPath(currentFace.rawTexture);
                //currentFace.materialLocation = texturesDir + currentFace.rawTexture + ".vmt";
                //currentFace.textureLocation = texturesDir + currentFace.rawTexture + ".png";

                bool undesired = false;
                foreach (string undesiredTexture in undesiredTextures)
                {
                    if (currentFace.rawTexture.Equals(undesiredTexture)) { undesired = true; break; }
                }
                #endregion

                if (!undesired && (textureFlag & texflags.SURF_SKY2D) != texflags.SURF_SKY2D && (textureFlag & texflags.SURF_SKY) != texflags.SURF_SKY && (textureFlag & texflags.SURF_NODRAW) != texflags.SURF_NODRAW && (textureFlag & texflags.SURF_SKIP) != texflags.SURF_SKIP)
                {
                    currentFace.s = new Vector3(texInfo[face.texinfo].textureVecs[0][0], texInfo[face.texinfo].textureVecs[0][2], texInfo[face.texinfo].textureVecs[0][1]);
                    currentFace.t = new Vector3(texInfo[face.texinfo].textureVecs[1][0], texInfo[face.texinfo].textureVecs[1][2], texInfo[face.texinfo].textureVecs[1][1]);
                    currentFace.xOffset = texInfo[face.texinfo].textureVecs[0][3];
                    currentFace.yOffset = texInfo[face.texinfo].textureVecs[1][3];

                    string vmtFile = "";
                    //currentFace.materialLocation = PatchName(currentFace.materialLocation);
                    if (Directory.Exists(ApplicationPreferences.texturesDir))
                    {
                        currentFace.rawTexture = PatchName(ApplicationPreferences.texturesDir, currentFace.rawTexture, "vmt");
                        vmtFile = ApplicationPreferences.texturesDir + currentFace.rawTexture + ".vmt";
                        if (!File.Exists(vmtFile)) vmtFile = ApplicationPreferences.texturesDir + currentFace.rawTexture + ".txt";
                    }
                    else
                    {
                        currentFace.rawTexture = PatchName(currentFace.rawTexture, "vmt");
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
                        TextAsset vmtTextAsset = Resources.Load<TextAsset>("Textures/Plain/" + currentFace.rawTexture);
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
                            currentFace.rawTexture = baseTexture;
                        }
                    }

                    //currentFace.textureLocation = PatchName(currentFace.textureLocation);
                    if (Directory.Exists(ApplicationPreferences.texturesDir)) currentFace.rawTexture = PatchName(ApplicationPreferences.texturesDir, currentFace.rawTexture, "png");
                    else currentFace.rawTexture = PatchName(currentFace.rawTexture, "png");

                    //int textureIndex = textureLocations.IndexOf(currentFace.textureLocation);
                    int textureIndex = textureLocations.IndexOf(currentFace.rawTexture);
                    Texture2D faceTexture = null;
                    if (textureIndex > -1)
                    {
                        faceTexture = mapTextures[textureIndex];
                    }
                    else
                    {
                        //if (File.Exists(currentFace.textureLocation))
                        if (File.Exists(ApplicationPreferences.texturesDir + currentFace.rawTexture + ".png"))
                        {
                            byte[] bytes = null;
                            //try { bytes = File.ReadAllBytes(currentFace.textureLocation); } catch(System.Exception e) { Debug.Log(e.Message); }
                            try { bytes = File.ReadAllBytes(ApplicationPreferences.texturesDir + currentFace.rawTexture + ".png"); }
                            catch (System.Exception e) { Debug.Log(e.Message); }
                            if (bytes != null)
                            {
                                faceTexture = new Texture2D(0, 0);
                                faceTexture.LoadImage(bytes);
                                bytes = null;
                                //if (averageTextures) AverageTexture(faceTexture);
                                //else if (decreaseTextureSizes) DecreaseTextureSize(faceTexture, maxSizeAllowed);
                                //faceTexture.wrapMode = TextureWrapMode.Repeat;
                                //bytes = null;
                                //textureLocations.Add(currentFace.textureLocation);
                                //textureLocations.Add(currentFace.rawTexture);
                                //mapTextures.Add(faceTexture);
                            }
                        }
                        else
                        {
                            usingPlainTextures = true;
                            faceTexture = Resources.Load<Texture2D>("Textures/Plain/" + currentFace.rawTexture);
                        }

                        if (faceTexture != null)
                        {
                            //faceTexture.LoadImage(bytes);
                            if (ApplicationPreferences.averageTextures) AverageTexture(faceTexture);
                            else if (ApplicationPreferences.decreaseTextureSizes) DecreaseTextureSize(faceTexture, ApplicationPreferences.maxSizeAllowed);
                            faceTexture.wrapMode = TextureWrapMode.Repeat;
                            //bytes = null;
                            //textureLocations.Add(currentFace.textureLocation);
                            textureLocations.Add(currentFace.rawTexture);
                            mapTextures.Add(faceTexture);
                        }
                    }

                    currentFace.mesh = MakeFace(face);
                    allFaces.Add(currentFace);
                }

                //Debug.Log("Added Face");
                //yield return null;
            }
            #endregion

            Debug.Log("Parsed " + allFaces.Count + " Faces");

            if (!ApplicationPreferences.combineMeshes)
            {
                foreach (FaceMesh faceMesh in allFaces)
                {
                    //GameObject faceGO = new GameObject(faceMesh.textureLocation);
                    GameObject faceGO = new GameObject(faceMesh.rawTexture);
                    faceGO.transform.parent = mapGameObject.transform;
                    MeshFilter theFilter = faceGO.AddComponent<MeshFilter>();
                    theFilter.mesh = faceMesh.mesh;

                    #region Add Vertices as Children
                    /*foreach (Vector3 vertex in theFilter.mesh.vertices)
                {
                    GameObject sphereVertex = new GameObject();
                    sphereVertex.name = vertex.ToString();
                    sphereVertex.transform.position = vertex;
                    sphereVertex.transform.localScale = new Vector3(10f, 10f, 10f);
                    sphereVertex.transform.parent = faceGO.transform;
                }*/
                    #endregion

                    #region Set Material of GameObject
                    Material faceMaterial = mainSurfaceMaterial;

                    //int textureIndex = textureLocations.IndexOf(faceMesh.textureLocation);
                    int textureIndex = textureLocations.IndexOf(faceMesh.rawTexture);
                    Texture2D faceTexture = null;
                    if (textureIndex > -1)
                    {
                        faceTexture = mapTextures[textureIndex];
                    }
                    if (faceTexture != null)
                    {
                        faceMaterial = new Material(Shader.Find("Legacy Shaders/Diffuse"));
                        faceMaterial.mainTextureScale = new Vector2(1, 1);
                        faceMaterial.mainTextureOffset = new Vector2(0, 0);
                        faceMaterial.mainTexture = faceTexture;
                        faceTexture = null;
                    }
                    faceGO.AddComponent<MeshRenderer>().material = faceMaterial;
                    #endregion

                    //yield return null;
                }

                Debug.Log("Made Seperate Meshes");
            }
            else
            {
                #region Create Atlas & Remap UVs
                AtlasMapper customAtlas = new AtlasMapper();
                if (usingPlainTextures) customAtlas.cushion = 0;
                customAtlas.AddTextures(mapTextures.ToArray());
                Texture2D packedMapTextures = customAtlas.atlas;
                Rect[] uvReMappers = customAtlas.mappedUVs;
                Material mapAtlas = new Material(Shader.Find("Custom/Atlas Tiling"));
                if(usingPlainTextures) mapAtlas.SetFloat("_uv1FracOffset", 0.07f);
                mapAtlas.mainTextureScale = new Vector2(1f, 1f);
                mapAtlas.mainTexture = packedMapTextures;
                //mapAtlas.mainTexture.wrapMode = TextureWrapMode.Clamp;
                for (int i = 0; i < allFaces.Count; i++)
                {
                    //if (i < 10) { Debug.Log(i + " Triangles: " + allFaces[i].mesh.triangles.Length); }
                    //int textureIndex = textureLocations.IndexOf(allFaces[i].textureLocation);
                    int textureIndex = textureLocations.IndexOf(allFaces[i].rawTexture);
                    //Texture2D faceTexture = null;
                    if (textureIndex > -1 && textureIndex < uvReMappers.Length)
                    {
                        //faceTexture = mapTextures[textureIndex];
                        Rect surfaceTextureRect = uvReMappers[textureIndex];
                        Mesh surfaceMesh = allFaces[i].mesh;
                        Vector2[] atlasTexturePosition = new Vector2[surfaceMesh.uv.Length];
                        Vector2[] atlasTextureSize = new Vector2[surfaceMesh.uv.Length];
                        for (int j = 0; j < atlasTexturePosition.Length; j++)
                        {
                            atlasTexturePosition[j] = new Vector2(surfaceTextureRect.x + 0.0f, surfaceTextureRect.y + 0.0f);
                            atlasTextureSize[j] = new Vector2(surfaceTextureRect.width - 0.0f, surfaceTextureRect.height - 0.0f);

                            //yield return null;
                        }
                        surfaceMesh.uv2 = atlasTexturePosition;
                        surfaceMesh.uv3 = atlasTextureSize;
                    }

                    //yield return null;
                }
                #endregion
                Debug.Log("Created Atlas and Remapped UVs");
                #region Calculate Minimum Submeshes Needed
                List<List<int>> combinesIndices = new List<List<int>>();
                combinesIndices.Add(new List<int>());
                int vertexCount = 0;
                for (int i = 0; i < allFaces.Count; i++)
                {
                    if (vertexCount + allFaces[i].mesh.vertices.Length >= System.UInt16.MaxValue)
                    {
                        combinesIndices.Add(new List<int>());
                        vertexCount = 0;
                    }

                    combinesIndices[combinesIndices.Count - 1].Add(i);
                    vertexCount += allFaces[i].mesh.vertices.Length;

                    //yield return null;
                }
                #endregion
                Debug.Log("Calculated Submeshes needed");
                #region Combine Meshes to Submeshes
                if (combinesIndices.Count == 1)
                {
                    CombineInstance[] currentCombine = new CombineInstance[combinesIndices[0].Count];
                    for (int i = 0; i < currentCombine.Length; i++)
                    {
                        currentCombine[i].mesh = allFaces[combinesIndices[0][i]].mesh;
                        currentCombine[i].transform = mapGameObject.transform.localToWorldMatrix;

                        //yield return null;
                    }

                    Mesh combinedMesh = new Mesh();
                    combinedMesh.name = "Custom Combined Mesh";
                    combinedMesh.CombineMeshes(currentCombine);
                    mapGameObject.AddComponent<MeshFilter>().mesh = combinedMesh;
                    mapGameObject.AddComponent<MeshRenderer>().material = mapAtlas;
                }
                else
                {
                    GameObject[] partialMeshes = new GameObject[combinesIndices.Count];
                    for (int i = 0; i < combinesIndices.Count; i++)
                    {
                        CombineInstance[] currentCombine = new CombineInstance[combinesIndices[i].Count];
                        for (int j = 0; j < currentCombine.Length; j++)
                        {
                            currentCombine[j].mesh = allFaces[combinesIndices[i][j]].mesh;
                            currentCombine[j].transform = mapGameObject.transform.localToWorldMatrix;
                        }

                        partialMeshes[i] = new GameObject(mapName + " Part " + (i + 1));
                        Mesh combinedMesh = new Mesh();
                        combinedMesh.name = "Custom Combined Mesh " + (i + 1);
                        combinedMesh.CombineMeshes(currentCombine);
                        partialMeshes[i].AddComponent<MeshFilter>().mesh = combinedMesh;
                        partialMeshes[i].AddComponent<MeshRenderer>().material = mapAtlas;
                        partialMeshes[i].AddComponent<MeshCollider>();
                        partialMeshes[i].transform.parent = mapGameObject.transform;

                        //yield return null;
                    }
                }
                #endregion
                Debug.Log("Combined Meshes into Submeshes");
            }
        }

        //return mapGameObject;
    }

    /*private string PatchName(string original)
    {
        string prep = original.Replace("\\", "/");
        string directory = "";
        List<string> extensions = new List<string>();
        string patched = "";
        if (prep.LastIndexOf("/") > -1) directory = prep.Substring(0, prep.LastIndexOf("/") + 1);
        if (prep.LastIndexOf(".") > -1) extensions.Add(prep.Substring(prep.LastIndexOf(".") + 1));
        if (prep.LastIndexOf("/") > -1) patched = prep.Substring(prep.LastIndexOf("/") + 1);
        if (patched.LastIndexOf(".") > -1) patched = patched.Substring(0, patched.LastIndexOf("."));
        if (extensions.Count > 0 && extensions[0].Equals("vmt", System.StringComparison.InvariantCultureIgnoreCase)) extensions.Add("txt");
        //if (!Directory.Exists(directory)) Debug.Log(directory);
        while (patched.Length > 0)
        {
            try
            {
                //if(extension.Equals("vmt", System.StringComparison.InvariantCultureIgnoreCase) 
                bool found = false;
                foreach (string extension in extensions)
                {
                    if (File.Exists(directory + "/" + patched + "." + extension)) { prep = directory + "/" + patched + "." + extension; found = true; break; }
                }
                if (found) break;
                //string[] matches = new string[0];
                //if (Directory.Exists(directory)) matches = Directory.GetFiles(directory, patched + "." + extension);
                //else break;
                //if (matches.Length == 1) return matches[0].Replace("\\", "/").ToLower();
                //else if (matches.Length > 1) break;
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }
            patched = patched.Substring(0, patched.Length - 1);
        }

        //if (!File.Exists(prep)) prep = original.Replace("\\", "/");
        return prep;
    }*/
    private string PatchName(string rootPath, string original, string ext)
    {
        string path = rootPath.Replace("\\", "/").ToLower();
        string prep = original.Replace("\\", "/").ToLower();
        
        string subDir = "";
        List<string> extensions = new List<string>();
        string patched = "";
        extensions.Add(ext);

        if (prep.LastIndexOf("/") > -1) subDir = prep.Substring(0, prep.LastIndexOf("/") + 1);
        if (prep.LastIndexOf("/") > -1) patched = prep.Substring(prep.LastIndexOf("/") + 1);
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
    private string PatchName(string original, string ext)
    {
        //string path = rootPath.Replace("\\", "/").ToLower();
        string prep = original.Replace("\\", "/").ToLower();

        string subDir = "";
        List<string> extensions = new List<string>();
        string patched = "";
        extensions.Add(ext);

        if (prep.LastIndexOf("/") > -1) subDir = prep.Substring(0, prep.LastIndexOf("/") + 1);
        if (prep.LastIndexOf("/") > -1) patched = prep.Substring(prep.LastIndexOf("/") + 1);
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
    private string RemoveMisleadingPath(string original)
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

    public Mesh MakeFace(dface_t face)
    {
        Mesh mesh = null;

        //texflags textureFlag = texflags.SURF_NODRAW;
        //try { textureFlag = ((texflags)texInfo[face.texinfo].flags); }
        //catch (System.Exception) { }

        #region Get all vertices of face
        List<Vector3> surfaceVertices = new List<Vector3>();
        List<Vector3> originalVertices = new List<Vector3>();
        for (int i = 0; i < face.numedges; i++)
        {
            ushort[] currentEdge = edges[Mathf.Abs(surfedges[face.firstedge + i])].v;
            Vector3 point1 = vertices[currentEdge[0]], point2 = vertices[currentEdge[1]];
            point1 = new Vector3(point1.x, point1.z, point1.y);
            point2 = new Vector3(point2.x, point2.z, point2.y);

            if (surfedges[face.firstedge + i] >= 0)
            {
                if (surfaceVertices.IndexOf(point1) < 0) surfaceVertices.Add(point1);
                originalVertices.Add(point1);
                if (surfaceVertices.IndexOf(point2) < 0) surfaceVertices.Add(point2);
                originalVertices.Add(point2);
            }
            else
            {
                if (surfaceVertices.IndexOf(point2) < 0) surfaceVertices.Add(point2);
                originalVertices.Add(point2);
                if (surfaceVertices.IndexOf(point1) < 0) surfaceVertices.Add(point1);
                originalVertices.Add(point1);
            }
        }
        #endregion

        #region Triangulate
        List<int> triangleIndices = new List<int>();

        for (int i = 0; i < (originalVertices.Count / 2) - 0; i++)
        {
            int firstOrigIndex = (i * 2), secondOrigIndex = (i * 2) + 1, thirdOrigIndex = 0;
            int firstIndex = surfaceVertices.IndexOf(originalVertices[firstOrigIndex]);
            int secondIndex = surfaceVertices.IndexOf(originalVertices[secondOrigIndex]);
            int thirdIndex = surfaceVertices.IndexOf(originalVertices[thirdOrigIndex]);

            triangleIndices.Add(firstIndex);
            triangleIndices.Add(secondIndex);
            triangleIndices.Add(thirdIndex);
        }
        #endregion

        #region Get UV Points
        Vector3 s = Vector3.zero, t = Vector3.zero;
        float xOffset = 0, yOffset = 0;

        try
        {
            s = new Vector3(texInfo[face.texinfo].textureVecs[0][0], texInfo[face.texinfo].textureVecs[0][2], texInfo[face.texinfo].textureVecs[0][1]);
            t = new Vector3(texInfo[face.texinfo].textureVecs[1][0], texInfo[face.texinfo].textureVecs[1][2], texInfo[face.texinfo].textureVecs[1][1]);
            xOffset = texInfo[face.texinfo].textureVecs[0][3];
            yOffset = texInfo[face.texinfo].textureVecs[1][3];
        }
        catch (System.Exception) { }

        Vector2[] uvPoints = new Vector2[surfaceVertices.Count];
        int textureWidth = 0, textureHeight = 0;

        try { textureWidth = texData[texInfo[face.texinfo].texdata].width; textureHeight = texData[texInfo[face.texinfo].texdata].height; }
        catch (System.Exception) { }

        for (int i = 0; i < uvPoints.Length; i++)
        {
            uvPoints[i] = new Vector2((Vector3.Dot(surfaceVertices[i], s) + xOffset) / textureWidth, (textureHeight - (Vector3.Dot(surfaceVertices[i], t) + yOffset)) / textureHeight);
        }
        #endregion

        #region Make Mesh
        mesh = new Mesh();
        mesh.name = "Custom Mesh";
        mesh.vertices = surfaceVertices.ToArray();
        mesh.triangles = triangleIndices.ToArray();
        mesh.uv = uvPoints;
        #endregion

        return mesh;
    }

    public void DecreaseTextureSize(Texture2D texture, float maxSize)
    {
        if (Mathf.Max(texture.width, texture.height) > maxSize)
        {
            float ratio = Mathf.Max(texture.width, texture.height) / maxSize;
            int decreasedWidth = (int) (texture.width / ratio), decreasedHeight = (int) (texture.height / ratio);
            
            TextureScale.Point(texture, decreasedWidth, decreasedHeight);
        }
    }
    public void AverageTexture(Texture2D original)
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

    public void SetVisibility(bool visibleState)
    {
        if(mapGameObject != null) RecursiveVisibillity(mapGameObject.transform, visibleState);
    }
    private void RecursiveVisibillity(Transform what, bool state)
    {
        if (what != null)
        {
            Renderer theRenderer = what.GetComponent<Renderer>();
            if (theRenderer != null) theRenderer.enabled = state;
            foreach (Transform child in what)
            {
                RecursiveVisibillity(child, state);
            }
        }
    }
}

public class FaceMesh
{
    public dface_t face;
    public Mesh mesh;
    public Vector3 s, t;
    public float xOffset, yOffset;
    public string rawTexture;
    //public string textureLocation, materialLocation;
}
