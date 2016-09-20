using UnityEngine;
//using System.Collections;
using System.Collections.Generic;
using System.IO;
//using System.Linq;

public class BSPMap : UnityThreadJob
{
    public readonly string[] undesiredTextures = new string[] { "TOOLS/TOOLSTRIGGER", "TOOLS/TOOLSBLACK", "TOOLS/CLIMB", "TOOLS/CLIMB_ALPHA", "TOOLS/FOGVOLUME", "TOOLS/TOOLSAREAPORTAL-DX10", "TOOLS/TOOLSBLACK", "TOOLS/TOOLSBLOCK_LOS",
                "TOOLS/TOOLSBLOCK_LOS-DX10", "TOOLS/TOOLSBLOCKBOMB", "TOOLS/TOOLSBLOCKBULLETS", "TOOLS/TOOLSBLOCKBULLETS-DX10", "TOOLS/TOOLSBLOCKLIGHT", "TOOLS/TOOLSCLIP", "TOOLS/TOOLSCLIP-DX10", "TOOLS/TOOLSDOTTED", "TOOLS/TOOLSFOG", "TOOLS/TOOLSFOG-DX10",
                "TOOLS/TOOLSHINT", "TOOLS/TOOLSHINT-DX10", "TOOLS/TOOLSINVISIBLE", "TOOLS/TOOLSINVISIBLE-DX10", "TOOLS/TOOLSINVISIBLELADDER", "TOOLS/TOOLSNODRAW", "TOOLS/TOOLSNPCCLIP", "TOOLS/TOOLSOCCLUDER", "TOOLS/TOOLSOCCLUDER-DX10", "TOOLS/TOOLSORIGIN",
                "TOOLS/TOOLSPLAYERCLIP", "TOOLS/TOOLSPLAYERCLIP-DX10", "TOOLS/TOOLSSKIP", "TOOLS/TOOLSSKIP-DX10", "TOOLS/TOOLSSKYBOX2D", "TOOLS/TOOLSSKYFOG", "TOOLS/TOOLSTRIGGER", "TOOLS/TOOLSTRIGGER-DX10" };

    #region Map Variables
    public static Dictionary<string, BSPMap> loadedMaps = new Dictionary<string, BSPMap>();
    public string mapName;
    private BSPParser bsp;
    private FileStream mapFile = null;
    public GameObject mapGameObject;

    public string mapLocation;
    //public static bool averageTextures = false, decreaseTextureSizes = true, combineMeshes = true;
    //public static int maxSizeAllowed = 128;
    //public static string mapsDir = "D:/Steam/SteamApps/common/Counter-Strike Global Offensive/csgo/maps/";
    //public static string mapsLocation = "/storage/emulated/0/Download/CSGO/Maps/";
    //public static string texturesDir = "D:/CSGOModels/Textures/";
    //public static string texturesDir = "/storage/emulated/0/Download/CSGO/Textures/";
    private static List<SourceTexture> mapTextures = new List<SourceTexture>();
    private static List<string> textureLocations = new List<string>();
    //private static Dictionary<string, SourceTexture> mapTextures = new Dictionary<string, SourceTexture>();

    private Material mainSurfaceMaterial = Resources.Load<Material>("Materials/MapMaterial");

    private Vector3[] vertices;
    //private dplane_t[] planes;
    private dedge_t[] edges;
    //private dface_t[] origFaces;
    private dface_t[] faces;
    private int[] surfedges;

    //private dbrush_t[] brushes;
    //private dbrushside_t[] brushSides;
    private ddispinfo_t[] dispInfo;
    private dDispVert[] dispVerts;

    private texinfo_t[] texInfo;
    private dtexdata_t[] texData;
    private int[] texStringTable;
    private string textureStringData;

    private StaticProps_t staticProps;

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

    protected override void ThreadFunction()
    {
        BuildMap();
    }

    public void BuildMap()
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
                currentFace.rawTexture = currentFace.rawTexture.Substring(0, currentFace.rawTexture.IndexOf(BSPParser.TEXTURE_STRING_DATA_SPLITTER));
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
    }
    private void ReadFile()
    {
        bsp = new BSPParser(mapFile);

        string entities = bsp.GetEntities();
        //Debug.Log("Map Entities: " + entities);
        vertices = bsp.GetVertices();

        //vertices = bsp.lumpData[3];
        //planes = bsp.GetPlanes();
        edges = bsp.GetEdges();
        //origFaces = bsp.GetOriginalFaces();
        faces = bsp.GetFaces();
        surfedges = bsp.GetSurfedges();

        //brushes = bsp.GetBrushes();
        //brushSides = bsp.GetBrushSides();
        dispInfo = bsp.GetDispInfo();
        dispVerts = bsp.GetDispVerts();

        texInfo = bsp.GetTextureInfo();
        texData = bsp.GetTextureData();
        texStringTable = bsp.GetTextureStringTable();
        textureStringData = bsp.GetTextureStringData();

        staticProps = bsp.GetStaticProps();

        mapFile.Close();
    }

    public static Texture2D[] GetTexturesAsArray()
    {
        Texture2D[] textures = new Texture2D[mapTextures.Count];
        for (int i = 0; i < textures.Length; i++)
        {
            //textures[i] = mapTextures.ElementAt(i).Value.texture;
            textures[i] = mapTextures[i].texture;
        }
        return textures;
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

        #region Apply Displacement
        if (face.dispinfo > -1)
        {
            ddispinfo_t disp = dispInfo[face.dispinfo];
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

                    Vector3 dispDirectionA = dispVerts[disp.DispVertStart + orderNum].vec;
                    dispDirectionA = new Vector3(dispDirectionA.x, dispDirectionA.z, dispDirectionA.y);
                    dispVertices.Add(pointA + (dispDirectionA * dispVerts[disp.DispVertStart + orderNum].dist));
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
            ddispinfo_t disp = dispInfo[face.dispinfo];
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
        mesh.RecalculateNormals();
        #endregion

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

public class FaceMesh
{
    public dface_t face;
    public Mesh mesh;
    public Vector3 s, t;
    public float xOffset, yOffset;
    public string rawTexture, textureLocation;
    public texflags textureFlag = texflags.SURF_NODRAW;
    public Matrix4x4 localToWorldMatrix;
    //public string textureLocation, materialLocation;
}
