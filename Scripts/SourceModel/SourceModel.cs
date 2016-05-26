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
    public Mesh[] modelMeshes;
    public SourceTexture[] modelTextures;

    private MDLParser mdl;
    private VVDParser vvd;
    private VTXParser vtx;

    private SourceModel(string name, string location)
    {
        modelName = name;
        modelLocation = location;

        if(modelLocation.LastIndexOf("/") != modelLocation.Length - 1 && modelLocation.LastIndexOf("\\") != modelLocation.Length - 1)
        {
            if (modelLocation.IndexOf("/") > -1) modelLocation = modelLocation + "/";
            else modelLocation = modelLocation + "\\";
        }

        loadedModels.Add(location + name, this);
    }

    public static SourceModel GrabModel(string name, string location)
    {
        SourceModel model = null;

        if (loadedModels.ContainsKey(location + name))
        {
            model = loadedModels[location + name];
        }
        else
        {
            model = new SourceModel(name, location);
            model.modelPrefab = new GameObject(model.modelName);
            model.modelPrefab.transform.parent = staticPropLibrary.transform;
            model.modelPrefab.SetActive(false);

            model.ReadFiles();

            if (model.mdl == null) { Debug.Log("MDL missing"); return null; }
            if (model.mdl.bodyParts == null) { Debug.Log("Body Parts missing"); return null; }

            #region Grabbing Textures
            //if(model.mdl.texturePaths.Length == model.mdl.textures.Length)
            //{
                model.modelTextures = new SourceTexture[model.mdl.textures.Length];
                for(int i = 0; i < model.modelTextures.Length; i++)
                {
                    string texturePath = "", textureName = "";
                    if (model.mdl.texturePaths != null && model.mdl.texturePaths.Length > 0 && model.mdl.texturePaths[0] != null) texturePath = model.mdl.texturePaths[0];
                    if (model.mdl.textures[i] != null) textureName = model.mdl.textures[i].name;
                    model.modelTextures[i] = SourceTexture.GrabTexture(texturePath + textureName);
                    //Debug.Log("Attempted to grab texture: " + model.modelTextures[i].location);
                }
            //}
            /*for (int i = 0; i < model.mdl.texturePaths.Length; i++)
            {
                Debug.Log(model.mdl.texturePaths[i]);
            }
            for (int i = 0; i < model.mdl.textures.Length; i++)
            {
                Debug.Log(model.mdl.textures[i].name);
            }*/
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

                        Vector3[] vertices = new Vector3[model.mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[0]];
                        Vector3[] normals = new Vector3[model.mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[0]];
                        Vector2[] uv = new Vector2[model.mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[0]];
                        for (int l = 0; l < vertices.Length; l++)
                        {
                            if (currentPosition < model.vvd.vertices[0].Length)
                                vertices[l] = model.vvd.vertices[0][currentPosition].m_vecPosition;
                            if (currentPosition < model.vvd.vertices[0].Length)
                                normals[l] = model.vvd.vertices[0][currentPosition].m_vecNormal;
                            if (currentPosition < model.vvd.vertices[0].Length)
                                uv[l] = model.vvd.vertices[0][currentPosition].m_vecTexCoord;
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
        string vtxExtension = ".vtx";
        if (!File.Exists(modelLocation + modelName + vtxExtension)) vtxExtension = ".dx90.vtx";

        if (File.Exists(modelLocation + modelName + ".mdl") && File.Exists(modelLocation + modelName + ".vvd") && File.Exists(modelLocation + modelName + vtxExtension))
        {
            FileStream currentFile = null;

            try { currentFile = new FileStream(modelLocation + modelName + ".mdl", FileMode.Open); } catch (System.Exception e) { Debug.Log("Could not open mdl file: " + e.Message); }
            if (currentFile != null)
            {
                mdl = new MDLParser(currentFile);
                mdl.ParseHeader();
                mdl.ParseBones();
                mdl.ParseBodyParts();
                mdl.ParseTextures();
                mdl.ParseTexturePaths();
                currentFile.Close();

                try { currentFile = new FileStream(modelLocation + modelName + ".vvd", FileMode.Open); } catch (System.Exception e) { Debug.Log("Could not open vvd file: " + e.Message); }
                if (currentFile != null)
                {
                    vvd = new VVDParser(currentFile);
                    vvd.ParseHeader();
                    vvd.ParseVertices();
                    vvd.ParseFixupTable();
                    currentFile.Close();

                    try { currentFile = new FileStream(modelLocation + modelName + vtxExtension, FileMode.Open); } catch (System.Exception e) { Debug.Log("Could not open vtx file: " + e.Message); }
                    if(currentFile != null)
                    {
                        vtx = new VTXParser(currentFile);
                        vtx.ReadSourceVtxHeader();
                        vtx.ReadSourceVtxBodyParts();
                        currentFile.Close();
                    }
                }
            }
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
