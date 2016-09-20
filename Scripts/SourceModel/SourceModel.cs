using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SourceModel
{
    private static Dictionary<string, SourceModel> loadedModels = new Dictionary<string, SourceModel>();
    private static GameObject staticPropLibrary = new GameObject("StaticPropPrefabs");

    public string modelName { get; private set; }
    public string modelLocation { get; private set; }

    private GameObject modelPrefab;
    public Mesh[] modelMeshes = new Mesh[0];
    public SourceTexture[] modelTextures = new SourceTexture[0];

    private MDLParser mdl;
    private VVDParser vvd;
    private VTXParser vtx;

    private SourceModel(string name, string location)
    {
        modelName = name;
        modelLocation = location;

        loadedModels.Add(modelLocation + modelName, this);
    }

    public static SourceModel GrabModel(string name, string location)
    {
        SourceModel model = null;

        string fixedModelName = name.ToLower();
        string fixedModelLocation = location.Replace("\\", "/").ToLower();

        if (fixedModelLocation.LastIndexOf("/") != fixedModelLocation.Length - 1) fixedModelLocation = fixedModelLocation + "/";
        if (fixedModelLocation.IndexOf("models/") > -1) fixedModelLocation = fixedModelLocation.Substring(fixedModelLocation.IndexOf("models/") + "models/".Length);
        if (loadedModels.ContainsKey(fixedModelLocation + fixedModelName))
        {
            model = loadedModels[fixedModelLocation + fixedModelName];
        }
        else
        {
            model = new SourceModel(fixedModelName, fixedModelLocation);
            model.modelPrefab = new GameObject(model.modelName);
            model.modelPrefab.transform.parent = staticPropLibrary.transform;
            model.modelPrefab.SetActive(false);

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

            #region Building
            int textureIndex = 0;
            List<Mesh> meshes = new List<Mesh>();
            for (int i = 0; i < model.mdl.bodyParts.Length; i++)
            {
                GameObject bodyPartRepresentation = new GameObject(model.mdl.bodyParts[i].name);
                bodyPartRepresentation.transform.parent = model.modelPrefab.transform;
                for (int j = 0; j < model.mdl.bodyParts[i].models.Length; j++)
                {
                    GameObject modelRepresentation = new GameObject(new string(model.mdl.bodyParts[i].models[j].name));
                    modelRepresentation.transform.parent = bodyPartRepresentation.transform;

                    int currentPosition = 0;
                    for (int k = 0; k < model.mdl.bodyParts[i].models[j].theMeshes.Length; k++)
                    {
                        GameObject meshRepresentation = new GameObject(model.mdl.bodyParts[i].models[j].theMeshes[k].id.ToString());
                        meshRepresentation.transform.parent = modelRepresentation.transform;

                        Vector3[] vertices = new Vector3[model.mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[model.mdl.header1.rootLod]];
                        Vector3[] normals = new Vector3[model.mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[model.mdl.header1.rootLod]];
                        Vector2[] uv = new Vector2[model.mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[model.mdl.header1.rootLod]];
                        for (int l = 0; l < vertices.Length; l++)
                        {
                            if (currentPosition < model.vvd.vertices[model.mdl.header1.rootLod].Length)
                                vertices[l] = model.vvd.vertices[model.mdl.header1.rootLod][currentPosition].m_vecPosition;
                            if (currentPosition < model.vvd.vertices[model.mdl.header1.rootLod].Length)
                                normals[l] = model.vvd.vertices[model.mdl.header1.rootLod][currentPosition].m_vecNormal;
                            if (currentPosition < model.vvd.vertices[model.mdl.header1.rootLod].Length)
                                uv[l] = model.vvd.vertices[model.mdl.header1.rootLod][currentPosition].m_vecTexCoord;
                            currentPosition++;
                        }

                        int[] triangles = new int[model.vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices.Length];
                        for (int l = 0; l < model.vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices.Length; l++)
                        {
                            triangles[l + 0] = model.vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxVertices[model.vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices[l + 0]].originalMeshVertexIndex;
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
                        if (model.modelTextures != null && textureIndex < model.modelTextures.Length) { meshMaterial.mainTexture = model.modelTextures[textureIndex].texture; textureIndex++; }
                        meshRepresentation.AddComponent<MeshRenderer>().material = meshMaterial;
                        meshRepresentation.AddComponent<MeshCollider>();
                    }
                }
            }
            model.modelMeshes = meshes.ToArray();
            #endregion
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
            mdlFile.Close();
            mdlFile = null;

            vvd = new VVDParser(vvdFile);
            vvd.ParseHeader();
            vvd.ParseVertices();
            vvd.ParseFixupTable();
            vvdFile.Close();
            vvdFile = null;

            vtx = new VTXParser(vtxFile);
            vtx.ReadSourceVtxHeader();
            vtx.ReadSourceVtxBodyParts();
            vtxFile.Close();
            vtxFile = null;
        }
        else Debug.Log("One or more files is missing. MDL, VVD, and VTX files are required");
    }

    public GameObject InstantiateGameObject()
    {
        GameObject cloned = Object.Instantiate(modelPrefab);
        cloned.SetActive(true);
        return cloned;
    }
}
