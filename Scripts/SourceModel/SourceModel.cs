using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SourceModel
{
    private static Dictionary<string, SourceModel> loadedModels = new Dictionary<string, SourceModel>();
    private static GameObject staticPropLibrary;

    public string modelName { get; private set; }
    public string modelLocation { get; private set; }

    private GameObject modelPrefab;
    public FaceMesh[] preloadedFaces { get; private set; }
    public Mesh[] modelMeshes { get; private set; }
    public SourceTexture[] modelTextures { get; private set; }

    private MDLParser mdl;
    private VVDParser vvd;
    private VTXParser vtx;

    private SourceModel(string name, string location)
    {
        modelName = name;
        modelLocation = location;

        preloadedFaces = new FaceMesh[0];
        modelMeshes = new Mesh[0];
        modelTextures = new SourceTexture[0];

        loadedModels.Add(modelLocation + modelName, this);
    }

    public static SourceModel GrabModel(string fullModelPath)
    {
        SourceModel model = null;

        string modelName = "";
        string modelLocation = fullModelPath.Replace("\\", "/").ToLower();

        if(modelLocation.IndexOf("/") > -1)
        {
            modelName = modelLocation.Substring(modelLocation.LastIndexOf("/") + 1);
            modelLocation = modelLocation.Substring(0, modelLocation.LastIndexOf("/") + 1);

            model = GrabModel(modelName, modelLocation);
        }

        return model;
    }
    public static SourceModel GrabModel(string name, string location)
    {
        SourceModel model = null;

        string fixedModelName = name.ToLower();
        string fixedModelLocation = location.Replace("\\", "/").ToLower();

        if (fixedModelName.LastIndexOf(".") == fixedModelName.Length - 4) fixedModelName = fixedModelName.Substring(0, fixedModelName.LastIndexOf("."));
        if (fixedModelLocation.LastIndexOf("/") != fixedModelLocation.Length - 1) fixedModelLocation = fixedModelLocation + "/";
        if (fixedModelLocation.IndexOf("models/") > -1) fixedModelLocation = fixedModelLocation.Substring(fixedModelLocation.IndexOf("models/") + "models/".Length);

        if (loadedModels.ContainsKey(fixedModelLocation + fixedModelName))
        {
            model = loadedModels[fixedModelLocation + fixedModelName];
        }
        else
        {
            model = new SourceModel(fixedModelName, fixedModelLocation);

            model.ReadFiles();

            //model.modelPrefab.name += " V(" + model.mdl.header1.version + ", " + model.vvd.header.version + ", " + model.vtx.header.version + ")";

            if (model.mdl == null) { Debug.Log("MDL missing"); return null; }
            if (model.mdl.bodyParts == null) { Debug.Log("Body Parts missing"); return null; }

            #region Grabbing Textures
            model.modelTextures = new SourceTexture[model.mdl.textures.Length];
            for(int i = 0; i < model.modelTextures.Length; i++)
            {
                string texturePath = "", textureName = "";
                if (model.mdl.texturePaths != null && model.mdl.texturePaths.Length > 0 && model.mdl.texturePaths[0] != null) texturePath = model.mdl.texturePaths[0].Replace("\\", "/").ToLower();
                if (model.mdl.textures[i] != null) textureName = model.mdl.textures[i].name.Replace("\\", "/").ToLower();
                if (textureName.IndexOf(texturePath) > -1) texturePath = "";
                model.modelTextures[i] = SourceTexture.GrabTexture(texturePath + textureName);
                //Debug.Log("Attempted to grab texture: " + model.modelTextures[i].location);
            }
            #endregion

            model.ReadFaceMeshes();
        }

        return model;
    }
    private void ReadFiles()
    {
        bool usingFileSystem = false;
        Stream mdlFile = null, vvdFile = null, vtxFile = null;
        #region Read From FileSystem
        if (ApplicationPreferences.useModels && File.Exists(ApplicationPreferences.modelsDir + modelLocation + modelName + ".mdl"))
        {
            bool fileSystemIntact = true;
            try { mdlFile = new FileStream(ApplicationPreferences.modelsDir + modelLocation + modelName + ".mdl", FileMode.Open); } catch (System.Exception e) { Debug.Log("Could not open mdl file: " + e.Message); fileSystemIntact = false; }

            if (fileSystemIntact && File.Exists(ApplicationPreferences.modelsDir + modelLocation + modelName + ".vvd"))
            {
                try { vvdFile = new FileStream(ApplicationPreferences.modelsDir + modelLocation + modelName + ".vvd", FileMode.Open); } catch (System.Exception e) { Debug.Log("Could not open vvd file: " + e.Message); fileSystemIntact = false; }

                if (fileSystemIntact)
                {
                    string vtxExtension = ".vtx";
                    if (!File.Exists(ApplicationPreferences.modelsDir + modelLocation + modelName + vtxExtension)) vtxExtension = ".dx90.vtx";
                    if (File.Exists(ApplicationPreferences.modelsDir + modelLocation + modelName + vtxExtension))
                    {
                        try { vtxFile = new FileStream(ApplicationPreferences.modelsDir + modelLocation + modelName + vtxExtension, FileMode.Open); usingFileSystem = true; } catch (System.Exception e) { Debug.Log("Could not open vtx file: " + e.Message); }
                    }
                }
            }
        }
        #endregion

        #region Read From VPK File
        if(!usingFileSystem && ApplicationPreferences.useVPK)
        {
            string modelsVPKDir = ((modelLocation.IndexOf("/") == 0) ? "/models" : "/models/");

            byte[] mdlData = ApplicationPreferences.vpkParser.LoadFile(modelsVPKDir + modelLocation + modelName + ".mdl");
            if (mdlData != null) mdlFile = new MemoryStream(mdlData);

            byte[] vvdData = ApplicationPreferences.vpkParser.LoadFile(modelsVPKDir + modelLocation + modelName + ".vvd");
            if (vvdData != null) vvdFile = new MemoryStream(vvdData);

            byte[] vtxData = ApplicationPreferences.vpkParser.LoadFile(modelsVPKDir + modelLocation + modelName + ".vtx");
            if (vtxData == null) vtxData = ApplicationPreferences.vpkParser.LoadFile(modelsVPKDir + modelLocation + modelName + ".dx90.vtx");
            if (vtxData != null) vtxFile = new MemoryStream(vtxData);

            //if (mdlData == null || vvdData == null || vtxData == null) Debug.Log("Not Found in VPK: " + modelLocation + modelName);
            //Debug.Log("Loaded: " + (mdlData != null) + ", " + (vvdData != null) + ", " + (vtxData != null));
        }
        #endregion

        if (mdlFile != null && vvdFile != null && vtxFile != null)
        {
            mdl = new MDLParser(mdlFile);
            mdl.ParseHeader();
            mdl.ParseBones();
            mdl.ParseBodyParts();
            mdl.ParseTextures();
            mdl.ParseTexturePaths();

            vvd = new VVDParser(vvdFile);
            vvd.ParseHeader();
            vvd.ParseVertices();
            vvd.ParseFixupTable();

            vtx = new VTXParser(vtxFile);
            vtx.ReadSourceVtxHeader();
            vtx.ReadSourceVtxBodyParts();
        }
        else Debug.Log("One or more files is missing. MDL, VVD, and VTX files are required");

        if(mdlFile != null)
        {
            mdlFile.Dispose();
            mdlFile.Close();
            mdlFile = null;
        }
        if(vvdFile != null)
        {
            vvdFile.Dispose();
            vvdFile.Close();
            vvdFile = null;
        }
        if(vtxFile != null)
        {
            vtxFile.Dispose();
            vtxFile.Close();
            vtxFile = null;
        }
    }
    private void ReadFaceMeshes()
    {
        int textureIndex = 0;
        List<FaceMesh> faces = new List<FaceMesh>();
        for (int i = 0; i < mdl.bodyParts.Length; i++)
        {
            for (int j = 0; j < mdl.bodyParts[i].models.Length; j++)
            {
                int currentPosition = 0;
                for (int k = 0; k < mdl.bodyParts[i].models[j].theMeshes.Length; k++)
                {
                    FaceMesh currentFace = new FaceMesh();

                    Vector3[] vertices = new Vector3[mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[mdl.header1.rootLod]];
                    Vector3[] normals = new Vector3[mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[mdl.header1.rootLod]];
                    Vector2[] uv = new Vector2[mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[mdl.header1.rootLod]];
                    for (int l = 0; l < vertices.Length; l++)
                    {
                        if (currentPosition < vvd.vertices[mdl.header1.rootLod].Length)
                            vertices[l] = vvd.vertices[mdl.header1.rootLod][currentPosition].m_vecPosition;
                        if (currentPosition < vvd.vertices[mdl.header1.rootLod].Length)
                            normals[l] = vvd.vertices[mdl.header1.rootLod][currentPosition].m_vecNormal;
                        if (currentPosition < vvd.vertices[mdl.header1.rootLod].Length)
                            uv[l] = vvd.vertices[mdl.header1.rootLod][currentPosition].m_vecTexCoord;
                        currentPosition++;
                    }

                    int[] triangles = new int[vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices.Length];
                    for (int l = 0; l < vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices.Length; l++)
                    {
                        triangles[l + 0] = vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxVertices[vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices[l + 0]].originalMeshVertexIndex;
                    }

                    MeshData meshData = new MeshData();
                    meshData.vertices = vertices;
                    meshData.triangles = triangles;
                    meshData.normals = normals;
                    meshData.uv = uv;

                    currentFace.meshData = meshData;
                    if (modelTextures != null && textureIndex < modelTextures.Length) { currentFace.textureLocation = modelTextures[textureIndex].location; textureIndex++; }

                    faces.Add(currentFace);
                }
            }
        }
        preloadedFaces = faces.ToArray();
    }

    /*private void BuildPrefab()
    {
        #region Building
        modelPrefab = new GameObject(modelName);
        modelPrefab.transform.parent = staticPropLibrary.transform;
        modelPrefab.SetActive(false);

        int textureIndex = 0;
        List<Mesh> meshes = new List<Mesh>();
        for (int i = 0; i < mdl.bodyParts.Length; i++)
        {
            GameObject bodyPartRepresentation = new GameObject(mdl.bodyParts[i].name);
            bodyPartRepresentation.transform.parent = modelPrefab.transform;
            for (int j = 0; j < mdl.bodyParts[i].models.Length; j++)
            {
                GameObject modelRepresentation = new GameObject(new string(mdl.bodyParts[i].models[j].name));
                modelRepresentation.transform.parent = bodyPartRepresentation.transform;

                int currentPosition = 0;
                for (int k = 0; k < mdl.bodyParts[i].models[j].theMeshes.Length; k++)
                {
                    GameObject meshRepresentation = new GameObject(mdl.bodyParts[i].models[j].theMeshes[k].id.ToString());
                    meshRepresentation.transform.parent = modelRepresentation.transform;

                    Vector3[] vertices = new Vector3[mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[mdl.header1.rootLod]];
                    Vector3[] normals = new Vector3[mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[mdl.header1.rootLod]];
                    Vector2[] uv = new Vector2[mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[mdl.header1.rootLod]];
                    for (int l = 0; l < vertices.Length; l++)
                    {
                        if (currentPosition < vvd.vertices[mdl.header1.rootLod].Length)
                            vertices[l] = vvd.vertices[mdl.header1.rootLod][currentPosition].m_vecPosition;
                        if (currentPosition < vvd.vertices[mdl.header1.rootLod].Length)
                            normals[l] = vvd.vertices[mdl.header1.rootLod][currentPosition].m_vecNormal;
                        if (currentPosition < vvd.vertices[mdl.header1.rootLod].Length)
                            uv[l] = vvd.vertices[mdl.header1.rootLod][currentPosition].m_vecTexCoord;
                        currentPosition++;
                    }

                    int[] triangles = new int[vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices.Length];
                    for (int l = 0; l < vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices.Length; l++)
                    {
                        triangles[l + 0] = vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxVertices[vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices[l + 0]].originalMeshVertexIndex;
                    }

                    Mesh mesh = new Mesh();
                    mesh.name = "Custom Mesh";
                    mesh.vertices = vertices;
                    mesh.triangles = triangles;
                    mesh.normals = normals;
                    mesh.uv = uv;
                    meshes.Add(mesh);

                    MeshFilter mesher = meshRepresentation.AddComponent<MeshFilter>();
                    mesher.sharedMesh = mesh;

                    Material meshMaterial = new Material(ApplicationPreferences.playerMaterial);
                    if (modelTextures != null && textureIndex < modelTextures.Length) { meshMaterial.mainTexture = modelTextures[textureIndex].GetTexture(); textureIndex++; }
                    meshRepresentation.AddComponent<MeshRenderer>().material = meshMaterial;
                    meshRepresentation.AddComponent<MeshCollider>();
                }
            }
        }
        modelMeshes = meshes.ToArray();
        #endregion
    }*/
    private void BuildPrefab()
    {
        #region Building
        modelPrefab = new GameObject(modelName);
        modelPrefab.transform.parent = staticPropLibrary.transform;
        modelPrefab.SetActive(false);

        List<Mesh> meshes = new List<Mesh>();
        foreach(FaceMesh faceMesh in preloadedFaces)
        {
            GameObject meshRepresentation = new GameObject("Custom Object");
            meshRepresentation.transform.parent = modelPrefab.transform;

            Mesh mesh = new Mesh();
            mesh.name = "Custom Mesh";
            mesh.vertices = faceMesh.meshData.vertices;
            mesh.triangles = faceMesh.meshData.triangles;
            mesh.normals = faceMesh.meshData.normals;
            mesh.uv = faceMesh.meshData.uv;
            meshes.Add(mesh);

            MeshFilter mesher = meshRepresentation.AddComponent<MeshFilter>();
            mesher.sharedMesh = mesh;

            Material meshMaterial = new Material(ApplicationPreferences.playerMaterial);
            SourceTexture texture = SourceTexture.GrabTexture(faceMesh.textureLocation);
            if (texture != null) { meshMaterial.mainTexture = texture.GetTexture(); }
            meshRepresentation.AddComponent<MeshRenderer>().material = meshMaterial;
            meshRepresentation.AddComponent<MeshCollider>();
        }
        modelMeshes = meshes.ToArray();
        #endregion
    }
    public GameObject InstantiateGameObject()
    {
        if (!staticPropLibrary) staticPropLibrary = new GameObject("StaticPropPrefabs");
        if (!modelPrefab) BuildPrefab();

        GameObject cloned = Object.Instantiate(modelPrefab);
        cloned.SetActive(true);
        return cloned;
    }
}
