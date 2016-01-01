public class mstudiomodel_t
{
    public char[] name;
    public int type;
    public float boundingRadius;
    public int meshCount;
    public int meshOffset;
    public int vertexCount;
    public int vertexOffset;
    public int tangentOffset;
    public int attachmentCount;
    public int attachmentOffset;
    public int eyeballCount;
    public int eyeballOffset;
    public mstudio_modelvertexdata_t vertexData;
    public int[] unused;
    public mstudiomesh_t[] theMeshes;
    public mstudioeyeball_t[] theEyeballs;
}