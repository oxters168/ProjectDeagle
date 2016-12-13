using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using System.Linq;

public class BSPMap : UnityThreadJob
{
    public readonly string[] undesiredTextures = new string[] { "TOOLS/TOOLSAREAPORTAL", "TOOLS/TOOLSBLACK", "TOOLS/CLIMB", "TOOLS/CLIMB_ALPHA", "TOOLS/FOGVOLUME", "TOOLS/TOOLSAREAPORTAL-DX10", "TOOLS/TOOLSBLACK", "TOOLS/TOOLSBLOCK_LOS",
                "TOOLS/TOOLSBLOCK_LOS-DX10", "TOOLS/TOOLSBLOCKBOMB", "TOOLS/TOOLSBLOCKBULLETS", "TOOLS/TOOLSBLOCKBULLETS-DX10", "TOOLS/TOOLSBLOCKLIGHT", "TOOLS/TOOLSCLIP", "TOOLS/TOOLSCLIP-DX10", "TOOLS/TOOLSDOTTED", "TOOLS/TOOLSFOG", "TOOLS/TOOLSFOG-DX10",
                "TOOLS/TOOLSHINT", "TOOLS/TOOLSHINT-DX10", "TOOLS/TOOLSINVISIBLE", "TOOLS/TOOLSINVISIBLE-DX10", "TOOLS/TOOLSINVISIBLELADDER", "TOOLS/TOOLSNODRAW", "TOOLS/TOOLSNPCCLIP", "TOOLS/TOOLSOCCLUDER", "TOOLS/TOOLSOCCLUDER-DX10", "TOOLS/TOOLSORIGIN",
                "TOOLS/TOOLSPLAYERCLIP", "TOOLS/TOOLSPLAYERCLIP-DX10", "TOOLS/TOOLSSKIP", "TOOLS/TOOLSSKIP-DX10", "TOOLS/TOOLSSKYBOX2D", "TOOLS/TOOLSSKYFOG", "TOOLS/TOOLSTRIGGER", "TOOLS/TOOLSTRIGGER-DX10" };

    #region Map Variables
    public static Dictionary<string, BSPMap> loadedMaps = new Dictionary<string, BSPMap>();
    public string mapName;
    private BSPParser bspParser;
    //private FileStream mapFile = null;
    public GameObject mapGameObject;

    public string mapLocation;
    private static List<SourceTexture> mapTextures = new List<SourceTexture>();
    private static List<string> textureLocations = new List<string>();
    private Dictionary<string, List<FaceMesh>> allFaces = new Dictionary<string, List<FaceMesh>>();

    private Material mainSurfaceMaterial = Resources.Load<Material>("Materials/MapMaterial");
    #endregion

    #region Parse Feedback
    private int totalItemsToLoad = 0;
    private int totalItemsLoaded = 0;
    private float unsafePercentParsed;
    private object percentParsedLock = new object();
    public float percentParsed
    {
        get
        {
            lock(percentParsedLock)
            {
                return unsafePercentParsed;
            }
        }
        set
        {
            lock(percentParsedLock)
            {
                unsafePercentParsed = value;
            }
        }
    }

    private string unsafeParseMessage;
    private object parseMessageLock = new object();
    public string parseMessage
    {
        get
        {
            lock(parseMessageLock)
            {
                return unsafeParseMessage;
            }
        }
        set
        {
            lock(parseMessageLock)
            {
                unsafeParseMessage = value;
            }
        }
    }

    public bool completedParse { get; private set; }
    #endregion

    private BSPMap(string _mapName, string _mapLocation)
    {
        mapName = _mapName;
        mapLocation = _mapLocation;
        //if (mapName.LastIndexOf(".") == mapName.Length - 4) mapName = mapName.Substring(0, mapName.LastIndexOf("."));
        //mapLocation = ApplicationPreferences.mapsDir;
        //if (!loadedMaps.ContainsKey(mapName)) loadedMaps.Add(mapName, this);
    }

    public void Dispose()
    {
        if (!IsDone) Abort();
        if (bspParser != null)
        {
            bspParser.Dispose();
            bspParser = null;
        }
        allFaces = null;
        if (mapGameObject) Object.Destroy(mapGameObject);
        mapGameObject = null;
        loadedMaps.Remove(mapName);
    }

    public static bool Parse(string fileLocation, out BSPMap map)
    {
        string conventionalLocation = fileLocation.Replace("\\", "/").ToLower();
        //if(File.Exists(conventionalLocation))
        //{
        string mapName = conventionalLocation;
        if (mapName.IndexOf("/") > -1) mapName = mapName.Substring(mapName.LastIndexOf("/") + 1);
        if (mapName.IndexOf(".") > -1) mapName = mapName.Substring(0, mapName.LastIndexOf("."));

        //BSPMap map;
        if (!loadedMaps.TryGetValue(mapName, out map))
        {
            if (File.Exists(conventionalLocation))
            {
                map = new BSPMap(mapName, conventionalLocation);
                loadedMaps[mapName] = map;
                map.parseMessage = "Starting parse " + mapName;
                map.Start();
                return true;
            }
            else throw new System.Exception("Could not locate map file");
        }
        //return map;
        //}
        //else throw new System.Exception("Could not find map");
        return false;
    }

    protected override void ThreadFunction()
    {
        ReadGeometry();
    }

    private void AddFaceMesh(FaceMesh faceMesh)
    {
        List<FaceMesh> groupedFaces;
        if (!allFaces.TryGetValue(faceMesh.textureLocation, out groupedFaces)) allFaces[faceMesh.textureLocation] = groupedFaces = new List<FaceMesh>();
        groupedFaces.Add(faceMesh);
    }
    public void ReadGeometry()
    {
        parseMessage = "Reading " + mapName;

        parseMessage = "Updating VPK";
        ApplicationPreferences.UpdateVPKParser();

        parseMessage = "Reading BSP Data";
        bspParser = new BSPParser(mapLocation);
        bspParser.ParseData();

        totalItemsToLoad = bspParser.faces.Length * 2 + bspParser.staticProps.staticPropDict.names.Length + bspParser.staticProps.staticPropInfo.Length;

        parseMessage = "Parsing Faces";
        #region Parse Faces
        foreach (dface_t face in bspParser.faces)
        {
            FaceMesh currentFace = new FaceMesh();
            currentFace.face = face;

            #region Get Texture Info
            try { currentFace.textureFlag = ((texflags)bspParser.texInfo[face.texinfo].flags); }
            catch (System.Exception) { }

            currentFace.rawTexture = bspParser.textureStringData.Substring(Mathf.Abs(bspParser.texStringTable[Mathf.Abs(bspParser.texData[Mathf.Abs(bspParser.texInfo[Mathf.Abs(face.texinfo)].texdata)].nameStringTableID)]));
            currentFace.rawTexture = currentFace.rawTexture.Substring(0, currentFace.rawTexture.IndexOf(BSPFileParser.TEXTURE_STRING_DATA_SPLITTER));
            SourceTexture srcTexture = SourceTexture.GrabTexture(currentFace.rawTexture);
            currentFace.textureLocation = srcTexture.location;

            currentFace.s = new Vector3(bspParser.texInfo[face.texinfo].textureVecs[0][0], bspParser.texInfo[face.texinfo].textureVecs[0][2], bspParser.texInfo[face.texinfo].textureVecs[0][1]);
            currentFace.t = new Vector3(bspParser.texInfo[face.texinfo].textureVecs[1][0], bspParser.texInfo[face.texinfo].textureVecs[1][2], bspParser.texInfo[face.texinfo].textureVecs[1][1]);
            currentFace.xOffset = bspParser.texInfo[face.texinfo].textureVecs[0][3];
            currentFace.yOffset = bspParser.texInfo[face.texinfo].textureVecs[1][3];

            bool undesired = false;
            foreach (string undesiredTexture in undesiredTextures)
            {
                if (currentFace.rawTexture.Equals(undesiredTexture)) { undesired = true; break; }
            }
            #endregion

            if (!undesired && (currentFace.textureFlag & texflags.SURF_SKY2D) != texflags.SURF_SKY2D && (currentFace.textureFlag & texflags.SURF_SKY) != texflags.SURF_SKY && (currentFace.textureFlag & texflags.SURF_NODRAW) != texflags.SURF_NODRAW && (currentFace.textureFlag & texflags.SURF_SKIP) != texflags.SURF_SKIP)
            {
                if (textureLocations.IndexOf(srcTexture.location) < 0)
                {
                    mapTextures.Add(srcTexture);
                    textureLocations.Add(srcTexture.location);
                }

                currentFace.meshData = MakeFace(face);
                //currentFace.localToWorldMatrix = mapGameObject.transform.localToWorldMatrix;

                AddFaceMesh(currentFace);
            }

            totalItemsLoaded++;
            percentParsed = (float)totalItemsLoaded / totalItemsToLoad;
        }
        #endregion

        //Debug.Log("Unique Textures: " + allFaces.Count);

        parseMessage = "Loading Static Props";
        #region Load Static Props
        for (int i = 0; i < bspParser.staticProps.staticPropDict.names.Length; i++)
        {
            string modelName = bspParser.staticProps.staticPropDict.names[i], modelLocation = bspParser.staticProps.staticPropDict.names[i];
            modelName = modelName.Substring(modelName.LastIndexOf("/") + 1);
            modelName = modelName.Substring(0, modelName.LastIndexOf("."));
            modelLocation = modelLocation.Substring(0, modelLocation.LastIndexOf("/"));

            SourceModel.GrabModel(modelName, modelLocation);
            //bspParser.staticProps.staticPropInfo[i].Origin;
            //bspParser.staticProps.staticPropInfo[i].Angles;

            totalItemsLoaded++;
            percentParsed = (float)totalItemsLoaded / totalItemsToLoad;
        }
        #endregion

        parseMessage = "Finished Reading " + mapName;
    }
    public IEnumerator MakeGameObject()
    {
        parseMessage = "Building " + mapName;

        //bool usingPlainTextures = false;
        mapGameObject = new GameObject(mapName);
        mapGameObject.SetActive(false);
        mainSurfaceMaterial = Resources.Load<Material>("Materials/MapMaterial");

        if (!ApplicationPreferences.combineMeshes)
        {
            int facesAdded = 0;
            foreach (KeyValuePair<string, List<FaceMesh>> listOfFaces in allFaces)
            {
                foreach (FaceMesh faceMesh in listOfFaces.Value)
                {
                    //GameObject faceGO = new GameObject(faceMesh.textureLocation);
                    GameObject faceGO = new GameObject(faceMesh.rawTexture);
                    faceGO.transform.parent = mapGameObject.transform;
                    MeshFilter theFilter = faceGO.AddComponent<MeshFilter>();
                    theFilter.mesh = MakeMesh(faceMesh.meshData);

                    #region Add Vertices as Children
                    //foreach (Vector3 vertex in theFilter.mesh.vertices)
                    //{
                    //    GameObject sphereVertex = new GameObject();
                    //    sphereVertex.name = vertex.ToString();
                    //    sphereVertex.transform.position = vertex;
                    //    sphereVertex.transform.localScale = new Vector3(10f, 10f, 10f);
                    //    sphereVertex.transform.parent = faceGO.transform;
                    //}
                    #endregion

                    #region Set Material of GameObject
                    Material faceMaterial = mainSurfaceMaterial;

                    //int textureIndex = textureLocations.IndexOf(faceMesh.textureLocation);
                    //int textureIndex = textureLocations.IndexOf(faceMesh.rawTexture);
                    Texture2D faceTexture = null;
                    if (textureLocations.IndexOf(faceMesh.textureLocation) > -1)
                    {
                        faceTexture = mapTextures[textureLocations.IndexOf(faceMesh.textureLocation)].GetTexture();
                    }
                    if (faceTexture != null)
                    {
                        faceMaterial = new Material(ApplicationPreferences.mapMaterial);
                        faceMaterial.mainTextureScale = new Vector2(1, 1);
                        faceMaterial.mainTextureOffset = new Vector2(0, 0);
                        faceMaterial.mainTexture = faceTexture;
                        faceTexture = null;
                    }
                    faceGO.AddComponent<MeshRenderer>().material = faceMaterial;
                    #endregion

                    totalItemsLoaded++;
                    percentParsed = (float)totalItemsLoaded / totalItemsToLoad;
                    facesAdded++;
                    if (facesAdded >= ApplicationPreferences.simultaneousFace) { yield return null; facesAdded = 0; }
                    //yield return null;
                }
            }

            #region Static Props
            GameObject staticPropsObject = new GameObject("StaticProps");
            staticPropsObject.transform.parent = mapGameObject.transform;
            for (int i = 0; i < bspParser.staticProps.staticPropInfo.Length; i++)
            {
                string modelName = bspParser.staticProps.staticPropDict.names[bspParser.staticProps.staticPropInfo[i].PropType], modelLocation = bspParser.staticProps.staticPropDict.names[bspParser.staticProps.staticPropInfo[i].PropType];
                modelName = modelName.Substring(modelName.LastIndexOf("/") + 1);
                modelName = modelName.Substring(0, modelName.LastIndexOf("."));
                modelLocation = modelLocation.Substring(0, modelLocation.LastIndexOf("/"));

                SourceModel propModel = SourceModel.GrabModel(modelName, modelLocation);
                GameObject propModelGO = propModel.InstantiateGameObject();
                propModelGO.transform.position = bspParser.staticProps.staticPropInfo[i].Origin;
                propModelGO.transform.localRotation = Quaternion.Euler(bspParser.staticProps.staticPropInfo[i].Angles.x, bspParser.staticProps.staticPropInfo[i].Angles.y + 0, bspParser.staticProps.staticPropInfo[i].Angles.z);
                propModelGO.transform.parent = staticPropsObject.transform;

                totalItemsLoaded++;
                percentParsed = (float)totalItemsLoaded / totalItemsToLoad;

                yield return null;
                //facesAdded++;
                //if (facesAdded >= ApplicationPreferences.simultaneousFace) { yield return null; facesAdded = 0; }
            }
            #endregion

            //Debug.Log("Made Seperate Meshes");
        }
        /*else
        {
            #region Add Static Prop Meshes to allFaces and mapTextures
            for (int i = 0; i < bspParser.staticProps.staticPropInfo.Length; i++)
            {
                string modelName = bspParser.staticProps.staticPropDict.names[bspParser.staticProps.staticPropInfo[i].PropType], modelLocation = bspParser.staticProps.staticPropDict.names[bspParser.staticProps.staticPropInfo[i].PropType];
                modelName = modelName.Substring(modelName.LastIndexOf("/") + 1);
                modelName = modelName.Substring(0, modelName.LastIndexOf("."));
                modelLocation = modelLocation.Substring(0, modelLocation.LastIndexOf("/"));

                SourceModel propModel = SourceModel.GrabModel(modelName, modelLocation);
                for (int j = 0; j < propModel.modelMeshes.Length; j++)
                {
                    FaceMesh propMesh = new FaceMesh();
                    propMesh.mesh = propModel.modelMeshes[j];
                    if (j < propModel.modelTextures.Length)
                    {
                        if (textureLocations.IndexOf(propModel.modelTextures[j].location) < 0)
                        {
                            mapTextures.Insert(0, propModel.modelTextures[j]);
                            textureLocations.Insert(0, propModel.modelTextures[j].location);
                        }
                        propMesh.textureLocation = propModel.modelTextures[j].location;

                        #region GameObject for Position and Rotation
                        GameObject propModelGO = new GameObject("Empty");
                        propModelGO.transform.position = bspParser.staticProps.staticPropInfo[i].Origin;
                        propModelGO.transform.localRotation = Quaternion.Euler(bspParser.staticProps.staticPropInfo[i].Angles.x, bspParser.staticProps.staticPropInfo[i].Angles.y + 0, bspParser.staticProps.staticPropInfo[i].Angles.z);
                        //propMesh.localToWorldMatrix = propModelGO.transform.localToWorldMatrix;
                        Object.DestroyImmediate(propModelGO);
                        #endregion
                    }
                    AddFaceMesh(propMesh);
                }
            }
            #endregion
            #region Create Atlas & Remap UVs
            AtlasMapper customAtlas = new AtlasMapper();
            if (usingPlainTextures) customAtlas.cushion = 0;
            customAtlas.AddTextures(GetTexturesAsArray());
            Texture2D packedMapTextures = customAtlas.atlas;
            Rect[] uvReMappers = customAtlas.mappedUVs;
            Material mapAtlas = new Material(ApplicationPreferences.mapAtlasMaterial);
            if (usingPlainTextures) mapAtlas.SetFloat("_uv1FracOffset", 0.07f);
            mapAtlas.mainTextureScale = new Vector2(1f, 1f);
            mapAtlas.mainTexture = packedMapTextures;
            //mapAtlas.mainTexture.wrapMode = TextureWrapMode.Clamp;
            //List<string> textureKeys = mapTextures.Keys.ToList();
            for (int i = 0; i < allFaces.Count; i++)
            {
                //if (i < 10) { Debug.Log(i + " Triangles: " + allFaces[i].mesh.triangles.Length); }
                int textureIndex = textureLocations.IndexOf(allFaces[i].textureLocation);
                //int textureIndex = textureKeys.IndexOf(allFaces[i].textureLocation);
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
                    }
                    surfaceMesh.uv2 = atlasTexturePosition;
                    surfaceMesh.uv3 = atlasTextureSize;
                }
            }
            #endregion
            //Debug.Log("Created Atlas and Remapped UVs");
            #region Calculate Minimum Submeshes Needed
            List<List<int>> combinesIndices = new List<List<int>>();
            combinesIndices.Add(new List<int>());
            int vertexCount = 0;
            for (int i = 0; i < allFaces.Count; i++)
            {
                if (vertexCount + allFaces[i].mesh.vertices.Length >= ushort.MaxValue)
                {
                    combinesIndices.Add(new List<int>());
                    vertexCount = 0;
                }

                combinesIndices[combinesIndices.Count - 1].Add(i);
                vertexCount += allFaces[i].mesh.vertices.Length;
            }
            #endregion
            //Debug.Log("Calculated Submeshes needed");
            #region Combine Meshes to Submeshes
            if (combinesIndices.Count == 1)
            {
                CombineInstance[] currentCombine = new CombineInstance[combinesIndices[0].Count];
                for (int i = 0; i < currentCombine.Length; i++)
                {
                    currentCombine[i].mesh = allFaces[combinesIndices[0][i]].mesh;
                    currentCombine[i].transform = allFaces[combinesIndices[0][i]].localToWorldMatrix;
                }

                Mesh combinedMesh = new Mesh();
                combinedMesh.name = "Custom Combined Mesh";
                combinedMesh.CombineMeshes(currentCombine);
                mapGameObject.AddComponent<MeshFilter>().mesh = combinedMesh;
                mapGameObject.AddComponent<MeshRenderer>().material = mapAtlas;
                mapGameObject.AddComponent<MeshCollider>();
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
                        currentCombine[j].transform = allFaces[combinesIndices[i][j]].localToWorldMatrix;
                    }

                    partialMeshes[i] = new GameObject(mapName + " Part " + (i + 1));
                    Mesh combinedMesh = new Mesh();
                    combinedMesh.name = "Custom Combined Mesh " + (i + 1);
                    combinedMesh.CombineMeshes(currentCombine);
                    partialMeshes[i].AddComponent<MeshFilter>().mesh = combinedMesh;
                    partialMeshes[i].AddComponent<MeshRenderer>().material = mapAtlas;
                    partialMeshes[i].AddComponent<MeshCollider>();
                    partialMeshes[i].transform.parent = mapGameObject.transform;
                }
            }
            #endregion
            //Debug.Log("Combined Meshes into Submeshes");
        }*/

        #if UNITY_EDITOR
        //MakeAsset();
        //SaveUVValues("C:\\Users\\oxter\\Documents\\csgo\\csgoMapModels\\" + mapName + "_UV.txt");
        #endif

        parseMessage = "Finished Building " + mapName;
        completedParse = true;
    }
    /*public void BuildMap()
    {
        ApplicationPreferences.UpdateVPKParser();

        //mapTextures = new List<Texture2D>();
        //textureLocations = new List<string>();
        bool usingPlainTextures = false;

        try
        {
            //Debug.Log(mapLocation + mapName + ".bsp");
            if (mapLocation.Length > 0 && File.Exists(mapLocation + mapName + ".bsp")) mapFile = new FileStream(mapLocation + mapName + ".bsp", FileMode.Open);
            else if (File.Exists("Assets\\Resources\\Maps\\" + mapName + ".bsp")) mapFile = new FileStream("Assets\\Resources\\Maps\\" + mapName + ".bsp", FileMode.Open);
        }
        catch (System.Exception e) { Debug.Log(e.Message); }

        if (mapFile != null)
        {
            ReadFile();

            #region Load Static Props
            for(int i = 0; i < staticProps.staticPropDict.names.Length; i++)
            {
                string modelName = staticProps.staticPropDict.names[i], modelLocation = staticProps.staticPropDict.names[i];
                modelName = modelName.Substring(modelName.LastIndexOf("/") + 1);
                modelName = modelName.Substring(0, modelName.LastIndexOf("."));
                modelLocation = modelLocation.Substring(0, modelLocation.LastIndexOf("/"));

                SourceModel.GrabModel(modelName, modelLocation);
            }
            #endregion

            mainSurfaceMaterial = Resources.Load<Material>("Materials/MapMaterial");
            mapGameObject = new GameObject(mapName);

            List<FaceMesh> allFaces = new List<FaceMesh>();
            #region Parse Faces
            foreach (dface_t face in faces)
            {
                FaceMesh currentFace = new FaceMesh();
                currentFace.face = face;

                #region Get Texture Info
                //texflags textureFlag = texflags.SURF_NODRAW;
                try { currentFace.textureFlag = ((texflags)texInfo[face.texinfo].flags); }
                catch (System.Exception) { }

                currentFace.rawTexture = textureStringData.Substring(Mathf.Abs(texStringTable[Mathf.Abs(texData[Mathf.Abs(texInfo[Mathf.Abs(face.texinfo)].texdata)].nameStringTableID)]));
                currentFace.rawTexture = currentFace.rawTexture.Substring(0, currentFace.rawTexture.IndexOf(BSPFileParser.TEXTURE_STRING_DATA_SPLITTER));
                SourceTexture srcTexture = SourceTexture.GrabTexture(currentFace.rawTexture);
                currentFace.textureLocation = srcTexture.location;

                currentFace.s = new Vector3(texInfo[face.texinfo].textureVecs[0][0], texInfo[face.texinfo].textureVecs[0][2], texInfo[face.texinfo].textureVecs[0][1]);
                currentFace.t = new Vector3(texInfo[face.texinfo].textureVecs[1][0], texInfo[face.texinfo].textureVecs[1][2], texInfo[face.texinfo].textureVecs[1][1]);
                currentFace.xOffset = texInfo[face.texinfo].textureVecs[0][3];
                currentFace.yOffset = texInfo[face.texinfo].textureVecs[1][3];

                bool undesired = false;
                foreach (string undesiredTexture in undesiredTextures)
                {
                    if (currentFace.rawTexture.Equals(undesiredTexture)) { undesired = true; break; }
                }
                #endregion

                if (!undesired && (currentFace.textureFlag & texflags.SURF_SKY2D) != texflags.SURF_SKY2D && (currentFace.textureFlag & texflags.SURF_SKY) != texflags.SURF_SKY && (currentFace.textureFlag & texflags.SURF_NODRAW) != texflags.SURF_NODRAW && (currentFace.textureFlag & texflags.SURF_SKIP) != texflags.SURF_SKIP)
                {
                    //if(!mapTextures.ContainsKey(srcTexture.location)) mapTextures.Add(srcTexture.location, srcTexture);
                    if(textureLocations.IndexOf(srcTexture.location) < 0)
                    {
                        mapTextures.Add(srcTexture);
                        textureLocations.Add(srcTexture.location);
                    }

                    currentFace.mesh = MakeFace(face);
                    currentFace.localToWorldMatrix = mapGameObject.transform.localToWorldMatrix;
                    allFaces.Add(currentFace);
                }
            }
            #endregion

            //Debug.Log("Parsed " + allFaces.Count + " Faces");

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
                    //foreach (Vector3 vertex in theFilter.mesh.vertices)
                    //{
                    //    GameObject sphereVertex = new GameObject();
                    //    sphereVertex.name = vertex.ToString();
                    //    sphereVertex.transform.position = vertex;
                    //    sphereVertex.transform.localScale = new Vector3(10f, 10f, 10f);
                    //    sphereVertex.transform.parent = faceGO.transform;
                    //}
                    #endregion

                    #region Set Material of GameObject
                    Material faceMaterial = mainSurfaceMaterial;

                    //int textureIndex = textureLocations.IndexOf(faceMesh.textureLocation);
                    //int textureIndex = textureLocations.IndexOf(faceMesh.rawTexture);
                    Texture2D faceTexture = null;
                    if (textureLocations.IndexOf(faceMesh.textureLocation) > -1)
                    {
                        faceTexture = mapTextures[textureLocations.IndexOf(faceMesh.textureLocation)].texture;
                    }
                    if (faceTexture != null)
                    {
                        faceMaterial = new Material(ApplicationPreferences.mapMaterial);
                        faceMaterial.mainTextureScale = new Vector2(1, 1);
                        faceMaterial.mainTextureOffset = new Vector2(0, 0);
                        faceMaterial.mainTexture = faceTexture;
                        faceTexture = null;
                    }
                    faceGO.AddComponent<MeshRenderer>().material = faceMaterial;
                    #endregion
                }

                #region Static Props
                GameObject staticPropsObject = new GameObject("StaticProps");
                staticPropsObject.transform.parent = mapGameObject.transform;
                for (int i = 0; i < staticProps.staticPropInfo.Length; i++)
                {
                    string modelName = staticProps.staticPropDict.names[staticProps.staticPropInfo[i].PropType], modelLocation = staticProps.staticPropDict.names[staticProps.staticPropInfo[i].PropType];
                    modelName = modelName.Substring(modelName.LastIndexOf("/") + 1);
                    modelName = modelName.Substring(0, modelName.LastIndexOf("."));
                    modelLocation = modelLocation.Substring(0, modelLocation.LastIndexOf("/"));

                    SourceModel propModel = SourceModel.GrabModel(modelName, modelLocation);
                    GameObject propModelGO = propModel.InstantiateGameObject();
                    propModelGO.transform.position = staticProps.staticPropInfo[i].Origin;
                    propModelGO.transform.localRotation = Quaternion.Euler(staticProps.staticPropInfo[i].Angles.x, staticProps.staticPropInfo[i].Angles.y + 0, staticProps.staticPropInfo[i].Angles.z);
                    propModelGO.transform.parent = staticPropsObject.transform;
                }
                #endregion

                //Debug.Log("Made Seperate Meshes");
            }
            else
            {
                #region Add Static Prop Meshes to allFaces and mapTextures
                for (int i = 0; i < staticProps.staticPropInfo.Length; i++)
                {
                    string modelName = staticProps.staticPropDict.names[staticProps.staticPropInfo[i].PropType], modelLocation = staticProps.staticPropDict.names[staticProps.staticPropInfo[i].PropType];
                    modelName = modelName.Substring(modelName.LastIndexOf("/") + 1);
                    modelName = modelName.Substring(0, modelName.LastIndexOf("."));
                    modelLocation = modelLocation.Substring(0, modelLocation.LastIndexOf("/"));

                    SourceModel propModel = SourceModel.GrabModel(modelName, modelLocation);
                    for (int j = 0; j < propModel.modelMeshes.Length; j++)
                    {
                        FaceMesh propMesh = new FaceMesh();
                        propMesh.mesh = propModel.modelMeshes[j];
                        if (j < propModel.modelTextures.Length)
                        {
                            if (textureLocations.IndexOf(propModel.modelTextures[j].location) < 0)
                            {
                                mapTextures.Insert(0, propModel.modelTextures[j]);
                                textureLocations.Insert(0, propModel.modelTextures[j].location);
                            }
                            propMesh.textureLocation = propModel.modelTextures[j].location;

                            #region GameObject for Position and Rotation
                            GameObject propModelGO = new GameObject("Empty");
                            propModelGO.transform.position = staticProps.staticPropInfo[i].Origin;
                            propModelGO.transform.localRotation = Quaternion.Euler(staticProps.staticPropInfo[i].Angles.x, staticProps.staticPropInfo[i].Angles.y + 0, staticProps.staticPropInfo[i].Angles.z);
                            propMesh.localToWorldMatrix = propModelGO.transform.localToWorldMatrix;
                            Object.DestroyImmediate(propModelGO);
                            #endregion
                        }
                        allFaces.Add(propMesh);
                    }
                }
                #endregion
                #region Create Atlas & Remap UVs
                AtlasMapper customAtlas = new AtlasMapper();
                if (usingPlainTextures) customAtlas.cushion = 0;
                customAtlas.AddTextures(GetTexturesAsArray());
                Texture2D packedMapTextures = customAtlas.atlas;
                Rect[] uvReMappers = customAtlas.mappedUVs;
                Material mapAtlas = new Material(ApplicationPreferences.mapAtlasMaterial);
                if(usingPlainTextures) mapAtlas.SetFloat("_uv1FracOffset", 0.07f);
                mapAtlas.mainTextureScale = new Vector2(1f, 1f);
                mapAtlas.mainTexture = packedMapTextures;
                //mapAtlas.mainTexture.wrapMode = TextureWrapMode.Clamp;
                //List<string> textureKeys = mapTextures.Keys.ToList();
                for (int i = 0; i < allFaces.Count; i++)
                {
                    //if (i < 10) { Debug.Log(i + " Triangles: " + allFaces[i].mesh.triangles.Length); }
                    int textureIndex = textureLocations.IndexOf(allFaces[i].textureLocation);
                    //int textureIndex = textureKeys.IndexOf(allFaces[i].textureLocation);
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
                        }
                        surfaceMesh.uv2 = atlasTexturePosition;
                        surfaceMesh.uv3 = atlasTextureSize;
                    }
                }
                #endregion
                //Debug.Log("Created Atlas and Remapped UVs");
                #region Calculate Minimum Submeshes Needed
                List<List<int>> combinesIndices = new List<List<int>>();
                combinesIndices.Add(new List<int>());
                int vertexCount = 0;
                for (int i = 0; i < allFaces.Count; i++)
                {
                    if (vertexCount + allFaces[i].mesh.vertices.Length >= ushort.MaxValue)
                    {
                        combinesIndices.Add(new List<int>());
                        vertexCount = 0;
                    }

                    combinesIndices[combinesIndices.Count - 1].Add(i);
                    vertexCount += allFaces[i].mesh.vertices.Length;
                }
                #endregion
                //Debug.Log("Calculated Submeshes needed");
                #region Combine Meshes to Submeshes
                if (combinesIndices.Count == 1)
                {
                    CombineInstance[] currentCombine = new CombineInstance[combinesIndices[0].Count];
                    for (int i = 0; i < currentCombine.Length; i++)
                    {
                        currentCombine[i].mesh = allFaces[combinesIndices[0][i]].mesh;
                        currentCombine[i].transform = allFaces[combinesIndices[0][i]].localToWorldMatrix;
                    }

                    Mesh combinedMesh = new Mesh();
                    combinedMesh.name = "Custom Combined Mesh";
                    combinedMesh.CombineMeshes(currentCombine);
                    mapGameObject.AddComponent<MeshFilter>().mesh = combinedMesh;
                    mapGameObject.AddComponent<MeshRenderer>().material = mapAtlas;
                    mapGameObject.AddComponent<MeshCollider>();
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
                            currentCombine[j].transform = allFaces[combinesIndices[i][j]].localToWorldMatrix;
                        }

                        partialMeshes[i] = new GameObject(mapName + " Part " + (i + 1));
                        Mesh combinedMesh = new Mesh();
                        combinedMesh.name = "Custom Combined Mesh " + (i + 1);
                        combinedMesh.CombineMeshes(currentCombine);
                        partialMeshes[i].AddComponent<MeshFilter>().mesh = combinedMesh;
                        partialMeshes[i].AddComponent<MeshRenderer>().material = mapAtlas;
                        partialMeshes[i].AddComponent<MeshCollider>();
                        partialMeshes[i].transform.parent = mapGameObject.transform;
                    }
                }
                #endregion
                //Debug.Log("Combined Meshes into Submeshes");
            }
        }
        else
        {
            mapGameObject = Object.Instantiate(Resources.Load("Models/CSGOMaps/" + mapName)) as GameObject;
        }

        #if UNITY_EDITOR
        //MakeAsset();
        //SaveUVValues("C:\\Users\\oxter\\Documents\\csgo\\csgoMapModels\\" + mapName + "_UV.txt");
        #endif
    }*/

    public static Texture2D[] GetTexturesAsArray()
    {
        Texture2D[] textures = new Texture2D[mapTextures.Count];
        for (int i = 0; i < textures.Length; i++)
        {
            //textures[i] = mapTextures.ElementAt(i).Value.texture;
            textures[i] = mapTextures[i].GetTexture();
        }
        return textures;
    }

    public MeshData MakeFace(dface_t face)
    {
        MeshData meshData = null;

        #region Get all vertices of face
        List<Vector3> surfaceVertices = new List<Vector3>();
        List<Vector3> originalVertices = new List<Vector3>();
        for (int i = 0; i < face.numedges; i++)
        {
            ushort[] currentEdge = bspParser.edges[Mathf.Abs(bspParser.surfedges[face.firstedge + i])].v;
            Vector3 point1 = bspParser.vertices[currentEdge[0]], point2 = bspParser.vertices[currentEdge[1]];
            point1 = new Vector3(point1.x, point1.z, point1.y);
            point2 = new Vector3(point2.x, point2.z, point2.y);

            if (bspParser.surfedges[face.firstedge + i] >= 0)
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

        #region Apply Displacement
        if (face.dispinfo > -1)
        {
            ddispinfo_t disp = bspParser.dispInfo[face.dispinfo];
            int power = Mathf.RoundToInt(Mathf.Pow(2, disp.power));

            List<Vector3> dispVertices = new List<Vector3>();
            Vector3 startingPosition = surfaceVertices[0];
            Vector3 topCorner = surfaceVertices[1], topRightCorner = surfaceVertices[2], rightCorner = surfaceVertices[3];

            #region Setting Orientation
            Vector3 dispStartingVertex = disp.startPosition;
            dispStartingVertex = new Vector3(dispStartingVertex.x, dispStartingVertex.z, dispStartingVertex.y);
            if (Vector3.Distance(dispStartingVertex, topCorner) < 0.01f)
            {
                Vector3 tempCorner = startingPosition;

                startingPosition = topCorner;
                topCorner = topRightCorner;
                topRightCorner = rightCorner;
                rightCorner = tempCorner;
            }
            else if (Vector3.Distance(dispStartingVertex, rightCorner) < 0.01f)
            {
                Vector3 tempCorner = startingPosition;

                startingPosition = rightCorner;
                rightCorner = topRightCorner;
                topRightCorner = topCorner;
                topCorner = tempCorner;
            }
            else if (Vector3.Distance(dispStartingVertex, topRightCorner) < 0.01f)
            {
                Vector3 tempCorner = startingPosition;

                startingPosition = topRightCorner;
                topRightCorner = tempCorner;
                tempCorner = rightCorner;
                rightCorner = topCorner;
                topCorner = tempCorner;
            }
            #endregion

            int orderNum = 0;
            #region Method 13 (The one and only two)
            Vector3 leftSide = (topCorner - startingPosition), rightSide = (topRightCorner - rightCorner);
            float leftSideLineSegmentationDistance = leftSide.magnitude / power, rightSideLineSegmentationDistance = rightSide.magnitude / power;
            for (int line = 0; line < (power + 1); line++)
            {
                for (int point = 0; point < (power + 1); point++)
                {
                    Vector3 leftPoint = (leftSide.normalized * line * leftSideLineSegmentationDistance) + startingPosition;
                    Vector3 rightPoint = (rightSide.normalized * line * rightSideLineSegmentationDistance) + rightCorner;
                    Vector3 currentLine = rightPoint - leftPoint;
                    Vector3 pointDirection = currentLine.normalized;
                    float pointSideSegmentationDistance = currentLine.magnitude / power;

                    Vector3 pointA = leftPoint + (pointDirection * pointSideSegmentationDistance * point);

                    Vector3 dispDirectionA = bspParser.dispVerts[disp.DispVertStart + orderNum].vec;
                    dispDirectionA = new Vector3(dispDirectionA.x, dispDirectionA.z, dispDirectionA.y);
                    dispVertices.Add(pointA + (dispDirectionA * bspParser.dispVerts[disp.DispVertStart + orderNum].dist));
                    //Debug.DrawRay(pointA, dispDirectionA * dispVerts[disp.DispVertStart + orderNum].dist, Color.yellow, 1000000f);
                    orderNum++;
                }
            }
            #endregion

            #region Debug
            Vector3 centerPoint = Vector3.zero;
            for (int i = 0; i < surfaceVertices.Count; i++)
            {
                //Vector3 direction = dispVerts[dispVertIndex].vec;
                //Vector3 displaced = surfaceVertices[i] + direction * dispVerts[dispVertIndex].dist;
                //surfaceVertices[i] = displaced;
                //dispVertIndex++;

                centerPoint += surfaceVertices[i];
            }
            centerPoint /= surfaceVertices.Count;
            //surfaceVertices.Add(centerPoint);

            //Debug.DrawRay(centerPoint, faceUp * 500f, Color.green, 1000000f);
            //Debug.DrawRay(centerPoint, faceForward * 500f, Color.blue, 1000000f);
            //Debug.DrawRay(centerPoint, faceRight * 500f, Color.red, 1000000f);
            //Debug.Log("Starting Vert: " + dispVertIndex + " Center: " + centerPoint);

            //Debug.DrawRay(surfaceVertices[0], (new Vector3(dispVerts[disp.DispVertStart].vec.x, dispVerts[disp.DispVertStart].vec.z, dispVerts[disp.DispVertStart].vec.y)) * dispVerts[disp.DispVertStart].dist, Color.yellow, 1000000f);
            #endregion

            surfaceVertices = dispVertices;
        }
        #endregion

        #region Triangulate
        List<int> triangleIndices = new List<int>();

        if (face.dispinfo > -1)
        {
            ddispinfo_t disp = bspParser.dispInfo[face.dispinfo];
            int power = Mathf.RoundToInt(Mathf.Pow(2, disp.power));

            #region Method 12 Triangulation
            for (int row = 0; row < power; row++)
            {
                for (int col = 0; col < power; col++)
                {
                    int currentLine = row * (power + 1);
                    int nextLineStart = (row + 1) * (power + 1);

                    triangleIndices.Add(currentLine + col);
                    triangleIndices.Add(nextLineStart + col);
                    triangleIndices.Add(currentLine + col + 1);

                    triangleIndices.Add(currentLine + col + 1);
                    triangleIndices.Add(nextLineStart + col);
                    triangleIndices.Add(nextLineStart + col + 1);
                }
            }
            #endregion
        }
        else
        {
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
        }
        #endregion

        #region Map UV Points
        Vector3 s = Vector3.zero, t = Vector3.zero;
        float xOffset = 0, yOffset = 0;

        try
        {
            s = new Vector3(bspParser.texInfo[face.texinfo].textureVecs[0][0], bspParser.texInfo[face.texinfo].textureVecs[0][2], bspParser.texInfo[face.texinfo].textureVecs[0][1]);
            t = new Vector3(bspParser.texInfo[face.texinfo].textureVecs[1][0], bspParser.texInfo[face.texinfo].textureVecs[1][2], bspParser.texInfo[face.texinfo].textureVecs[1][1]);
            xOffset = bspParser.texInfo[face.texinfo].textureVecs[0][3];
            yOffset = bspParser.texInfo[face.texinfo].textureVecs[1][3];
        }
        catch (System.Exception) { }

        Vector2[] uvPoints = new Vector2[surfaceVertices.Count];
        int textureWidth = 0, textureHeight = 0;

        try { textureWidth = bspParser.texData[bspParser.texInfo[face.texinfo].texdata].width; textureHeight = bspParser.texData[bspParser.texInfo[face.texinfo].texdata].height; }
        catch (System.Exception) { }

        for (int i = 0; i < uvPoints.Length; i++)
        {
            uvPoints[i] = new Vector2((Vector3.Dot(surfaceVertices[i], s) + xOffset) / textureWidth, (textureHeight - (Vector3.Dot(surfaceVertices[i], t) + yOffset)) / textureHeight);
        }
        #endregion

        #region Make Mesh
        meshData = new MeshData();
        //mesh.name = "Custom Mesh";
        meshData.vertices = surfaceVertices.ToArray();
        meshData.triangles = triangleIndices.ToArray();
        meshData.uv = uvPoints;
        //mesh.RecalculateNormals();
        #endregion

        return meshData;
    }
    public Mesh MakeMesh(MeshData meshData)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Custom Mesh";
        mesh.vertices = meshData.vertices;
        mesh.triangles = meshData.triangles;
        mesh.uv = meshData.uv;
        mesh.RecalculateNormals();
        return mesh;
    }

    public void SetVisibility(bool visibleState)
    {
        if (mapGameObject != null) RecursiveVisibillity(mapGameObject.transform, visibleState);
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

    #if UNITY_EDITOR
    public void MakeAsset()
    {
        if (mapGameObject != null)
        {
            GameObject remadeMapGameObject = new GameObject(mapName);
            Material atlasMaterial = new Material(Shader.Find("Custom/Atlas Tiling"));
            UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/Models", mapName);
            UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/Models/" + mapName, "Meshes");
            UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/Models/" + mapName, "Material");

            foreach (Transform child in mapGameObject.transform)
            {
                atlasMaterial = child.GetComponent<MeshRenderer>().material;

                Mesh partialMesh = child.GetComponent<MeshFilter>().mesh;
                GameObject partialMapGameObject = new GameObject(child.name);
                partialMapGameObject.AddComponent<MeshFilter>().mesh = partialMesh;
                //partialMapGameObject.AddComponent<MeshRenderer>().material = atlasMaterial;
                partialMapGameObject.AddComponent<MeshCollider>();
                partialMapGameObject.transform.parent = remadeMapGameObject.transform;

                UnityEditor.AssetDatabase.CreateAsset(partialMesh, "Assets/Resources/Models/" + mapName + "/Meshes/" + child.name + ".asset");
            }
            foreach(Transform child in remadeMapGameObject.transform) { child.gameObject.AddComponent<MeshRenderer>().material = atlasMaterial; }

            UnityEditor.AssetDatabase.CreateAsset(atlasMaterial.mainTexture, "Assets/Resources/Models/" + mapName + "/Material/TextureAtlas.asset");
            UnityEditor.AssetDatabase.CreateAsset(atlasMaterial, "Assets/Resources/Models/" + mapName + "/Material/AtlasMaterial.asset");
            UnityEditor.PrefabUtility.CreatePrefab("Assets/Resources/Models/" + mapName + "/" + mapName + ".prefab", remadeMapGameObject);
        }
    }

    public void SaveUVValues(string location)
    {
        List<string> lines = new List<string>();
        foreach(Transform submesh in mapGameObject.transform)
        {
            lines.Add(submesh.name);
            MeshFilter meshFilter = submesh.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.mesh != null)
            {
                if (meshFilter.mesh.uv != null && meshFilter.mesh.uv.Length > 0) lines.Add(MakeUVString(meshFilter.mesh.uv));
                if (meshFilter.mesh.uv2 != null && meshFilter.mesh.uv2.Length > 0) lines.Add(MakeUVString(meshFilter.mesh.uv2));
                if (meshFilter.mesh.uv3 != null && meshFilter.mesh.uv3.Length > 0) lines.Add(MakeUVString(meshFilter.mesh.uv3));
                if (meshFilter.mesh.uv4 != null && meshFilter.mesh.uv4.Length > 0) lines.Add(MakeUVString(meshFilter.mesh.uv4));
            }
            else lines.Add("None");
        }

        try
        {
            File.WriteAllLines(@location, lines.ToArray());
        }
        catch (System.Exception e) { Debug.Log(e.Message); }
    }
    private string MakeUVString(Vector2[] uvs)
    {
        string uvLine = "";
        string[] uvElements = System.Array.ConvertAll(uvs, element => element.ToString().Replace(" ", ""));
        uvLine = string.Join(" ", uvElements);
        //foreach (Vector2 uv in uvs)
        //{
        //    uvLine += "(" + uv.x + "," + uv.y + ") ";
        //}
        return uvLine;
    }
    #endif
}

class BSPParser
{
    public const string TEXTURE_STRING_DATA_SPLITTER = ":";

    public string fileLocation { get; private set; }
    private Stream stream;

    public int identifier { get; private set; }
    public int version { get; private set; }
    public int mapRevision { get; private set; }

    private lump_t[] lumps;
    private object[] lumpData;
    public dgamelumpheader_t gameLumpHeader { get; private set; }

    #region Geometry
    public Vector3[] vertices;
    public dedge_t[] edges;
    public dface_t[] faces;
    public int[] surfedges;

    public ddispinfo_t[] dispInfo;
    public dDispVert[] dispVerts;

    public texinfo_t[] texInfo;
    public dtexdata_t[] texData;
    public int[] texStringTable;
    public string textureStringData;

    public StaticProps_t staticProps;
    #endregion

    public BSPParser(string fileLocation)
    {
        this.fileLocation = fileLocation;
        lumps = new lump_t[64];
        lumpData = new object[64];
    }

    public void Dispose()
    {
        lumps = null;
        lumpData = null;
        vertices = null;
        edges = null;
        faces = null;
        surfedges = null;
        dispInfo = null;
        dispVerts = null;
        texInfo = null;
        texData = null;
        texStringTable = null;
        textureStringData = null;
        staticProps = null;
    }

    private void LoadLumps()
    {
        for (int i = 0; i < lumps.Length; i++)
        {
            lump_t lump = new lump_t();
            lump.fileofs = DataParser.ReadInt(stream);
            lump.filelen = DataParser.ReadInt(stream);
            lump.version = DataParser.ReadInt(stream);
            lump.fourCC = DataParser.ReadInt(stream);
            lumps[i] = lump;
        }
    }
    private void LoadGameLumps()
    {
        lump_t lump = lumps[35];
        stream.Position = lump.fileofs;

        //gameLumpHeader = new dgamelumpheader_t();
        dgamelumpheader_t gameLumpHeader = new dgamelumpheader_t();
        gameLumpHeader.lumpCount = DataParser.ReadInt(stream);
        gameLumpHeader.gamelump = new dgamelump_t[gameLumpHeader.lumpCount];

        for (int i = 0; i < gameLumpHeader.gamelump.Length; i++)
        {
            gameLumpHeader.gamelump[i] = new dgamelump_t();
            gameLumpHeader.gamelump[i].id = DataParser.ReadInt(stream);
            gameLumpHeader.gamelump[i].flags = DataParser.ReadUShort(stream);
            gameLumpHeader.gamelump[i].version = DataParser.ReadUShort(stream);
            gameLumpHeader.gamelump[i].fileofs = DataParser.ReadInt(stream);
            gameLumpHeader.gamelump[i].filelen = DataParser.ReadInt(stream);
        }

        this.gameLumpHeader = gameLumpHeader;
        lumpData[35] = gameLumpHeader.gamelump;
    }

    public void ParseData()
    {
        using (stream = new FileStream(fileLocation, FileMode.Open))
        {
            //Debug.Log("Reading Map Info");
            identifier = DataParser.ReadInt(stream);
            version = DataParser.ReadInt(stream);
            LoadLumps();
            LoadGameLumps();
            mapRevision = DataParser.ReadInt(stream);

            //Debug.Log("Reading Vertices");
            vertices = GetVertices();

            //Debug.Log("Reading Edges");
            edges = GetEdges();
            //Debug.Log("Reading Faces");
            faces = GetFaces();
            //Debug.Log("Reading Surface Edges");
            surfedges = GetSurfedges();

            //Debug.Log("Reading Displacement Info");
            dispInfo = GetDispInfo();
            //Debug.Log("Reading Displacement Vertices");
            dispVerts = GetDispVerts();

            //Debug.Log("Reading Texture Info");
            texInfo = GetTextureInfo();
            //Debug.Log("Reading Texture Dat");
            texData = GetTextureData();
            //Debug.Log("Reading Texture String Table");
            texStringTable = GetTextureStringTable();
            //Debug.Log("Reading Texture String Data");
            textureStringData = GetTextureStringData();

            //Debug.Log("Reading Static Props");
            staticProps = GetStaticProps();
        }
    }

    private string GetEntities()
    {
        lump_t lump = lumps[0];
        string allEntities = "";
        stream.Position = lump.fileofs;

        for (int i = 0; i < lump.filelen; i++)
        {
            char nextChar = DataParser.ReadChar(stream);
            allEntities += nextChar;
        }

        return allEntities;
    }

    private dbrush_t[] GetBrushes()
    {
        lump_t lump = lumps[18];
        dbrush_t[] brushes = new dbrush_t[lump.filelen / 12];
        stream.Position = lump.fileofs;

        for (int i = 0; i < brushes.Length; i++)
        {
            brushes[i].firstside = DataParser.ReadInt(stream);
            brushes[i].numsides = DataParser.ReadInt(stream);
            brushes[i].contents = DataParser.ReadInt(stream);
        }

        lumpData[18] = brushes;
        return brushes;
    }

    private dbrushside_t[] GetBrushSides()
    {
        lump_t lump = lumps[19];
        dbrushside_t[] brushSides = new dbrushside_t[lump.filelen / 8];
        stream.Position = lump.fileofs;

        for (int i = 0; i < brushSides.Length; i++)
        {
            brushSides[i].planenum = DataParser.ReadUShort(stream);
            brushSides[i].texinfo = DataParser.ReadShort(stream);
            brushSides[i].dispinfo = DataParser.ReadShort(stream);
            brushSides[i].bevel = DataParser.ReadShort(stream);
        }

        lumpData[19] = brushSides;
        return brushSides;
    }

    private ddispinfo_t[] GetDispInfo()
    {
        lump_t lump = lumps[26];
        ddispinfo_t[] displacementInfo = new ddispinfo_t[lump.filelen / 86];
        stream.Position = lump.fileofs;

        for (int i = 0; i < displacementInfo.Length; i++)
        {
            displacementInfo[i].startPosition = new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            displacementInfo[i].DispVertStart = DataParser.ReadInt(stream);
            displacementInfo[i].DispTriStart = DataParser.ReadInt(stream);
            displacementInfo[i].power = DataParser.ReadInt(stream);
            displacementInfo[i].minTess = DataParser.ReadInt(stream);
            displacementInfo[i].smoothingAngle = DataParser.ReadFloat(stream);
            displacementInfo[i].contents = DataParser.ReadInt(stream);
            displacementInfo[i].MapFace = DataParser.ReadUShort(stream);
            displacementInfo[i].LightmapAlphaStart = DataParser.ReadInt(stream);
            displacementInfo[i].LightmapSamplePositionStart = DataParser.ReadInt(stream);
            stream.Position += 90;
            displacementInfo[i].AllowedVerts = new uint[10] { DataParser.ReadUInt(stream), DataParser.ReadUInt(stream), DataParser.ReadUInt(stream), DataParser.ReadUInt(stream), DataParser.ReadUInt(stream), DataParser.ReadUInt(stream), DataParser.ReadUInt(stream), DataParser.ReadUInt(stream), DataParser.ReadUInt(stream), DataParser.ReadUInt(stream) };
        }

        lumpData[26] = displacementInfo;
        return displacementInfo;
    }

    private dDispVert[] GetDispVerts()
    {
        lump_t lump = lumps[33];
        dDispVert[] displacementVertices = new dDispVert[lump.filelen / 20];
        stream.Position = lump.fileofs;

        for (int i = 0; i < displacementVertices.Length; i++)
        {
            displacementVertices[i].vec = new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            displacementVertices[i].dist = DataParser.ReadFloat(stream);
            displacementVertices[i].alpha = DataParser.ReadFloat(stream);
        }

        lumpData[33] = displacementVertices;
        return displacementVertices;
    }

    private dedge_t[] GetEdges()
    {
        lump_t lump = lumps[12];
        dedge_t[] edges = new dedge_t[lump.filelen / 4];
        stream.Position = lump.fileofs;

        for (int i = 0; i < edges.Length; i++)
        {
            edges[i].v = new ushort[2];
            edges[i].v[0] = DataParser.ReadUShort(stream);
            edges[i].v[1] = DataParser.ReadUShort(stream);
        }

        lumpData[12] = edges;
        return edges;
    }

    private Vector3[] GetVertices()
    {
        lump_t lump = lumps[3];
        Vector3[] vertices = new Vector3[lump.filelen / 12];
        stream.Position = lump.fileofs;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
        }

        lumpData[3] = vertices;
        return vertices;
    }

    private dface_t[] GetOriginalFaces()
    {
        lump_t lump = lumps[27];
        dface_t[] faces = new dface_t[lump.filelen / 56];
        stream.Position = lump.fileofs;

        for (int i = 0; i < faces.Length; i++)
        {
            faces[i].planenum = DataParser.ReadUShort(stream);
            faces[i].side = DataParser.ReadByte(stream);
            faces[i].onNode = DataParser.ReadByte(stream);
            faces[i].firstedge = DataParser.ReadInt(stream);
            faces[i].numedges = DataParser.ReadShort(stream);
            faces[i].texinfo = DataParser.ReadShort(stream);
            faces[i].dispinfo = DataParser.ReadShort(stream);
            faces[i].surfaceFogVolumeID = DataParser.ReadShort(stream);
            faces[i].styles = new byte[4] { DataParser.ReadByte(stream), DataParser.ReadByte(stream), DataParser.ReadByte(stream), DataParser.ReadByte(stream) };
            faces[i].lightofs = DataParser.ReadInt(stream);
            faces[i].area = DataParser.ReadFloat(stream);
            faces[i].LightmapTextureMinsInLuxels = new int[2] { DataParser.ReadInt(stream), DataParser.ReadInt(stream) };
            faces[i].LightmapTextureSizeInLuxels = new int[2] { DataParser.ReadInt(stream), DataParser.ReadInt(stream) };
            faces[i].origFace = DataParser.ReadInt(stream);
            faces[i].numPrims = DataParser.ReadUShort(stream);
            faces[i].firstPrimID = DataParser.ReadUShort(stream);
            faces[i].smoothingGroups = DataParser.ReadUInt(stream);
        }

        lumpData[27] = faces;
        return faces;
    }

    private dface_t[] GetFaces()
    {
        lump_t lump = lumps[7];
        dface_t[] faces = new dface_t[lump.filelen / 56];
        stream.Position = lump.fileofs;

        for (int i = 0; i < faces.Length; i++)
        {
            faces[i].planenum = DataParser.ReadUShort(stream);
            faces[i].side = DataParser.ReadByte(stream);
            faces[i].onNode = DataParser.ReadByte(stream);
            faces[i].firstedge = DataParser.ReadInt(stream);
            faces[i].numedges = DataParser.ReadShort(stream);
            faces[i].texinfo = DataParser.ReadShort(stream);
            faces[i].dispinfo = DataParser.ReadShort(stream);
            faces[i].surfaceFogVolumeID = DataParser.ReadShort(stream);
            faces[i].styles = new byte[4] { DataParser.ReadByte(stream), DataParser.ReadByte(stream), DataParser.ReadByte(stream), DataParser.ReadByte(stream) };
            faces[i].lightofs = DataParser.ReadInt(stream);
            faces[i].area = DataParser.ReadFloat(stream);
            faces[i].LightmapTextureMinsInLuxels = new int[2] { DataParser.ReadInt(stream), DataParser.ReadInt(stream) };
            faces[i].LightmapTextureSizeInLuxels = new int[2] { DataParser.ReadInt(stream), DataParser.ReadInt(stream) };
            faces[i].origFace = DataParser.ReadInt(stream);
            faces[i].numPrims = DataParser.ReadUShort(stream);
            faces[i].firstPrimID = DataParser.ReadUShort(stream);
            faces[i].smoothingGroups = DataParser.ReadUInt(stream);
        }

        lumpData[7] = faces;
        return faces;
    }

    private dplane_t[] GetPlanes()
    {
        lump_t lump = lumps[1];
        dplane_t[] planes = new dplane_t[lump.filelen / 20];
        stream.Position = lump.fileofs;

        for (int i = 0; i < planes.Length; i++)
        {
            planes[i].normal = new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            planes[i].dist = DataParser.ReadFloat(stream);
            planes[i].type = DataParser.ReadInt(stream);
        }

        lumpData[1] = planes;
        return planes;
    }

    private int[] GetSurfedges()
    {

        lump_t lump = lumps[13];
        int[] surfedges = new int[lump.filelen / 4];
        stream.Position = lump.fileofs;

        for (int i = 0; i < lump.filelen / 4; i++)
        {
            surfedges[i] = DataParser.ReadInt(stream);
        }

        lumpData[13] = surfedges;
        return surfedges;
    }

    private texinfo_t[] GetTextureInfo()
    {
        lump_t lump = lumps[6];
        texinfo_t[] textureInfo = new texinfo_t[lump.filelen / 72];
        stream.Position = lump.fileofs;

        for (int i = 0; i < textureInfo.Length; i++)
        {
            textureInfo[i].textureVecs = new float[2][];
            textureInfo[i].textureVecs[0] = new float[4] { DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream) };
            textureInfo[i].textureVecs[1] = new float[4] { DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream) };
            textureInfo[i].lightmapVecs = new float[2][];
            textureInfo[i].lightmapVecs[0] = new float[4] { DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream) };
            textureInfo[i].lightmapVecs[1] = new float[4] { DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream) };
            textureInfo[i].flags = DataParser.ReadInt(stream);
            textureInfo[i].texdata = DataParser.ReadInt(stream);
        }

        lumpData[6] = textureInfo;
        return textureInfo;
    }

    private dtexdata_t[] GetTextureData()
    {
        lump_t lump = lumps[2];
        dtexdata_t[] textureData = new dtexdata_t[lump.filelen / 32];
        stream.Position = lump.fileofs;

        for (int i = 0; i < textureData.Length; i++)
        {
            Vector3 reflectivity = new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));
            textureData[i].reflectivity = reflectivity;
            textureData[i].nameStringTableID = DataParser.ReadInt(stream);
            textureData[i].width = DataParser.ReadInt(stream);
            textureData[i].height = DataParser.ReadInt(stream);
            textureData[i].view_width = DataParser.ReadInt(stream);
            textureData[i].view_height = DataParser.ReadInt(stream);
        }

        lumpData[2] = textureData;
        return textureData;
    }

    private int[] GetTextureStringTable()
    {
        lump_t lump = lumps[44];
        int[] textureStringTable = new int[lump.filelen / 4];
        stream.Position = lump.fileofs;

        for (int i = 0; i < textureStringTable.Length; i++)
        {
            textureStringTable[i] = DataParser.ReadInt(stream);
        }

        return textureStringTable;
    }

    private string GetTextureStringData()
    {
        lump_t lump = lumps[43];
        stream.Position = lump.fileofs;

        string textureStringData = "";
        for (int i = 0; i < lump.filelen; i++)
        {
            char nextChar = DataParser.ReadChar(stream);

            if (nextChar != '\0') textureStringData += nextChar;
            else textureStringData += TEXTURE_STRING_DATA_SPLITTER;
        }
        return textureStringData;
    }

    private StaticProps_t GetStaticProps()
    {
        dgamelump_t lump = null;

        //Debug.Log("# Game Lumps: " + gameLumpHeader.gamelump.Length);
        for (int i = 0; i < gameLumpHeader.gamelump.Length; i++)
        {
            //Debug.Log("Static Prop Dict Index: " + i + " id: " + gameLumpHeader.gamelump[i].id + " fileofs: " + gameLumpHeader.gamelump[i].fileofs + " filelen: " + gameLumpHeader.gamelump[i].filelen + " version: " + gameLumpHeader.gamelump[i].version);
            if (gameLumpHeader.gamelump[i].id == 1936749168) { lump = gameLumpHeader.gamelump[i]; }
        }

        StaticProps_t staticProps = new StaticProps_t();
        //staticProp.staticPropDict = new StaticPropDictLump_t();
        if (lump != null)
        {
            stream.Position = lump.fileofs;

            #region Dict Lump
            staticProps.staticPropDict.dictEntries = DataParser.ReadInt(stream);
            staticProps.staticPropDict.names = new string[staticProps.staticPropDict.dictEntries];

            for (int i = 0; i < staticProps.staticPropDict.names.Length; i++)
            {
                char[] nullPaddedName = new char[128];
                for (int j = 0; j < nullPaddedName.Length; j++)
                {
                    nullPaddedName[j] = DataParser.ReadChar(stream);
                }
                staticProps.staticPropDict.names[i] = new string(nullPaddedName);
                //Debug.Log(i + ": " + staticProps.staticPropDict.names[i]);
            }
            #endregion

            #region Leaf Lump
            staticProps.staticPropLeaf.leafEntries = DataParser.ReadInt(stream);
            staticProps.staticPropLeaf.leaf = new ushort[staticProps.staticPropLeaf.leafEntries];

            for (int i = 0; i < staticProps.staticPropLeaf.leaf.Length; i++)
            {
                staticProps.staticPropLeaf.leaf[i] = DataParser.ReadUShort(stream);
            }
            //Debug.Log("Leaf Entries: " + staticProps.staticPropLeaf.leaf.Length);
            #endregion

            #region Info Lump
            staticProps.staticPropInfo = new StaticPropLump_t[DataParser.ReadInt(stream)];
            //long currentSizeUsed = stream.Position - lump.fileofs;
            //Debug.Log("Used: " + currentSizeUsed + " Intended Length: " + lump.filelen + " BytesPerInfo: " + ((lump.filelen - currentSizeUsed) / staticProps.staticPropInfo.Length));
            //int largestIndex = -1;
            for (int i = 0; i < staticProps.staticPropInfo.Length; i++)
            {
                staticProps.staticPropInfo[i].Origin = new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));       // origin
                staticProps.staticPropInfo[i].Origin = new Vector3(staticProps.staticPropInfo[i].Origin.x, staticProps.staticPropInfo[i].Origin.z, staticProps.staticPropInfo[i].Origin.y);
                staticProps.staticPropInfo[i].Angles = new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));       // orientation (pitch roll yaw)
                //staticProps.staticPropInfo[i].Angles = new Vector3(staticProps.staticPropInfo[i].Angles.x, staticProps.staticPropInfo[i].Angles.z, staticProps.staticPropInfo[i].Angles.y);
                staticProps.staticPropInfo[i].PropType = DataParser.ReadUShort(stream);     // index into model name dictionary
                staticProps.staticPropInfo[i].FirstLeaf = DataParser.ReadUShort(stream);    // index into leaf array
                staticProps.staticPropInfo[i].LeafCount = DataParser.ReadUShort(stream);
                staticProps.staticPropInfo[i].Solid = DataParser.ReadByte(stream);         // solidity type
                staticProps.staticPropInfo[i].Flags = DataParser.ReadByte(stream);
                staticProps.staticPropInfo[i].Skin = DataParser.ReadInt(stream);        // model skin numbers
                staticProps.staticPropInfo[i].FadeMinDist = DataParser.ReadFloat(stream);
                staticProps.staticPropInfo[i].FadeMaxDist = DataParser.ReadFloat(stream);
                staticProps.staticPropInfo[i].LightingOrigin = new Vector3(DataParser.ReadFloat(stream), DataParser.ReadFloat(stream), DataParser.ReadFloat(stream));  // for lighting
                                                                                                                                                                       // since v5
                staticProps.staticPropInfo[i].ForcedFadeScale = DataParser.ReadFloat(stream); // fade distance scale
                                                                                              // v6 and v7 only
                staticProps.staticPropInfo[i].MinDXLevel = DataParser.ReadUShort(stream);      // minimum DirectX version to be visible
                staticProps.staticPropInfo[i].MaxDXLevel = DataParser.ReadUShort(stream);      // maximum DirectX version to be visible
                                                                                               // since v8
                staticProps.staticPropInfo[i].MinCPULevel = DataParser.ReadByte(stream);
                staticProps.staticPropInfo[i].MaxCPULevel = DataParser.ReadByte(stream);
                staticProps.staticPropInfo[i].MinGPULevel = DataParser.ReadByte(stream);
                staticProps.staticPropInfo[i].MaxGPULevel = DataParser.ReadByte(stream);
                // since v7
                staticProps.staticPropInfo[i].DiffuseModulation = new Color32(DataParser.ReadByte(stream), DataParser.ReadByte(stream), DataParser.ReadByte(stream), DataParser.ReadByte(stream)); // per instance color and alpha modulation
                                                                                                                                                                                                   // since v10
                staticProps.staticPropInfo[i].unknown = DataParser.ReadFloat(stream);
                // since v9
                //staticProps.staticPropInfo[i].DisableX360 = Convert.ToBoolean(FileReader.readByte(stream));     // if true, don't show on XBox 360

                //largestIndex = staticProps.staticPropInfo[i].PropType > largestIndex ? staticProps.staticPropInfo[i].PropType : largestIndex;

                #region Full Debug
                /*Debug.Log(i +
                    " Origin: " + staticProps.staticPropInfo[i].Origin +
                    " Angle: " + staticProps.staticPropInfo[i].Angles +
                    " Prop Type: " + staticProps.staticPropInfo[i].PropType +
                    " First Leaf: " + staticProps.staticPropInfo[i].FirstLeaf +
                    " Leaf Count: " + staticProps.staticPropInfo[i].LeafCount + 
                    " Solid: " + staticProps.staticPropInfo[i].Solid +
                    " Flags: " + staticProps.staticPropInfo[i].Flags +
                    " Skin: " + staticProps.staticPropInfo[i].Skin +
                    " FadeMinDist: " + staticProps.staticPropInfo[i].FadeMinDist +
                    " FadeMaxDist: " + staticProps.staticPropInfo[i].FadeMaxDist +
                    " LightingOrigin: " + staticProps.staticPropInfo[i].LightingOrigin +
                    " ForcedFadeScale: " + staticProps.staticPropInfo[i].ForcedFadeScale +
                    " MinDXLevel: " + staticProps.staticPropInfo[i].MinDXLevel +
                    " MaxDXLevel: " + staticProps.staticPropInfo[i].MaxDXLevel +
                    " MinCPULevel: " + staticProps.staticPropInfo[i].MinCPULevel +
                    " MaxCPULevel: " + staticProps.staticPropInfo[i].MaxCPULevel +
                    " MinGPULevel: " + staticProps.staticPropInfo[i].MinGPULevel +
                    " MaxGPULevel: " + staticProps.staticPropInfo[i].MaxGPULevel +
                    " DiffuseModulation: " + staticProps.staticPropInfo[i].DiffuseModulation +
                    " Unknown: " + staticProps.staticPropInfo[i].unknown +
                    " DisableX360: " + staticProps.staticPropInfo[i].DisableX360);*/
                #endregion
            }
            //Debug.Log("Total Static Props: " + staticProps.staticPropInfo.Length + " Largest index into dict: " + largestIndex);
            #endregion
        }

        return staticProps;
    }
}

public class FaceMesh
{
    public Vector3 relativePosition;
    public Vector3 relativeRotation;

    public dface_t face;
    //public Mesh mesh;
    public MeshData meshData;
    public Vector3 s, t;
    public float xOffset, yOffset;
    public string rawTexture, textureLocation;
    public texflags textureFlag = texflags.SURF_NODRAW;
    //public Matrix4x4 localToWorldMatrix;
    //public string textureLocation, materialLocation;
}
public class MeshData
{
    public Vector3[] vertices = new Vector3[0];
    public int[] triangles = new int[0];
    public Vector3[] normals = new Vector3[0];
    public Vector2[] uv = new Vector2[0];
}
