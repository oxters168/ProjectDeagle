using UnityEngine;
using System.IO;

public class SourceModel
{
    public string modelName { get; private set; }
    public string modelLocation { get; private set; }

    GameObject modelGO;

    private MDLParser mdl;
    private VVDParser vvd;
    private VTXParser vtx;

    public SourceModel(string name, string location)
    {
        modelName = name;
        modelLocation = location;

        if(modelLocation.LastIndexOf("/") != modelLocation.Length - 1 && modelLocation.LastIndexOf("\\") != modelLocation.Length - 1)
        {
            if (modelLocation.IndexOf("/") > -1) modelLocation = modelLocation + "/";
            else modelLocation = modelLocation + "\\";
        }
    }

    public GameObject ParseModel()
    {
        modelGO = new GameObject(modelName);

        ReadFiles();

        if (mdl == null) { Debug.Log("MDL missing"); return null; }
        if (mdl.bodyParts == null) { Debug.Log("Body Parts missing"); return null; }

        #region Building
        for (int i = 0; i < mdl.bodyParts.Length; i++)
        {
            GameObject bodyPartRepresentation = new GameObject(mdl.bodyParts[i].name);
            bodyPartRepresentation.transform.parent = modelGO.transform;
            for (int j = 0; j < mdl.bodyParts[i].models.Length; j++)
            {
                GameObject modelRepresentation = new GameObject(new string(mdl.bodyParts[i].models[j].name));
                modelRepresentation.transform.parent = bodyPartRepresentation.transform;

                int currentPosition = 0;
                for (int k = 0; k < mdl.bodyParts[i].models[j].theMeshes.Length; k++)
                {
                    GameObject meshRepresentation = new GameObject(mdl.bodyParts[i].models[j].theMeshes[k].id.ToString());
                    meshRepresentation.transform.parent = modelRepresentation.transform;

                    Vector3[] vertices = new Vector3[mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[0]];
                    Vector3[] normals = new Vector3[mdl.bodyParts[i].models[j].theMeshes[k].vertexData.lodVertexCount[0]];
                    for (int l = 0; l < vertices.Length; l++)
                    {
                        if (currentPosition < vvd.vertices[0].Length)
                            vertices[l] = vvd.vertices[0][currentPosition].m_vecPosition;
                        if (currentPosition < vvd.vertices[0].Length)
                            normals[l] = vvd.vertices[0][currentPosition].m_vecNormal;
                        currentPosition++;
                        //Debug.Log("Index " + (currentPosition) + " Length " + vvd.vertices[0].Length);
                        //GameObject vertex = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        //vertex.transform.position = vertices[l];
                        //vertex.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                        //vertex.transform.parent = meshRepresentation.transform;
                    }

                    int[] triangles = new int[vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices.Length];
                    //Debug.Log("Body Parts: " + vtx.bodyParts.Length + " == " + mdl.bodyParts.Length + " Models: " + vtx.bodyParts[i].theVtxModels.Length + " == " + mdl.bodyParts[i].models.Length + " Meshes: " + vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes.Length + " == " + mdl.bodyParts[i].models[j].theMeshes.Length);
                    int largestIndex = -1;
                    for (int l = 0; l < vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices.Length; l++)
                    {
                        //Debug.Log(l + "/" + vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices.Length + ": " + vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices[l]);
                        //if (vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices[l] >= vertices.Length) Debug.Log(vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices[l] + " > " + vertices.Length);

                        triangles[l + 0] = vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxVertices[vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices[l + 0]].originalMeshVertexIndex;
                        //triangles[l + 1] = vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices[l + 1];
                        //triangles[l + 2] = vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].theVtxIndices[l + 2];

                        largestIndex = triangles[l + 0] > largestIndex ? triangles[l + 0] : largestIndex;
                        //largestIndex = triangles[l + 1] > largestIndex ? triangles[l + 1] : largestIndex;
                        //largestIndex = triangles[l + 2] > largestIndex ? triangles[l + 2] : largestIndex;
                    }
                    Debug.Log(largestIndex + " == " + (vertices.Length - 1));
                    //vtx.bodyParts[i].theVtxModels[j].theVtxModelLods[0].theVtxMeshes[k].theVtxStripGroups[0].

                    Mesh mesh = new Mesh();
                    mesh.name = "Custom Mesh";
                    mesh.vertices = vertices;
                    mesh.triangles = triangles;
                    mesh.normals = normals;

                    MeshFilter mesher = meshRepresentation.AddComponent<MeshFilter>();
                    mesher.sharedMesh = mesh;

                    Material mapAtlas = new Material(ApplicationPreferences.playerMaterial);
                    meshRepresentation.AddComponent<MeshRenderer>().material = mapAtlas;
                    meshRepresentation.AddComponent<MeshCollider>();
                }
            }
        }
        #endregion

        return modelGO;
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
}
