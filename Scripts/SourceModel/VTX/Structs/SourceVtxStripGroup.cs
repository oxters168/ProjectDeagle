using System;

public class SourceVtxStripGroup
{
    public int vertexCount;
    public int vertexOffset;

    public int indexCount;
    public int indexOffset;

    public int stripCount;
    public int stripOffset;

    public byte flags;

    public int topologyIndexCount;
    public int topologyIndexOffset;

    public SourceVtxVertex[] theVtxVertices;
    public ushort[] theVtxIndices;
    public SourceVtxStrip[] theVtxStrips;
}

[Flags]
public enum StripGroupFlags_t
{
    STRIPGROUP_IS_FLEXED = 0x01,
    STRIPGROUP_IS_HWSKINNED = 0x02,
    STRIPGROUP_IS_DELTA_FIXED = 0x04,
    STRIPGROUP_SUPPRESS_HW_MORPH = 0x08,
}