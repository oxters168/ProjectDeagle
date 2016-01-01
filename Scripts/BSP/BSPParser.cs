using UnityEngine;
using System;
using System.IO;

public class BSPParser
{
    public const string TEXTURE_STRING_DATA_SPLITTER = ":";

	Stream stream;

	int identifier;
	int version;
	int mapRevision;

	lump_t[] lumps;
	object[] lumpData;

	public BSPParser(Stream stream)
	{
		this.stream = stream;
		this.lumps = new lump_t[64];
		this.lumpData = new object[64];

		this.identifier = FileReader.readInt(stream);
		this.version = FileReader.readInt(stream);
		this.LoadLumps();
		this.mapRevision = FileReader.readInt(stream);

		Debug.Log("[BSPLoader] File loaded");//, ((BSPRenderer)GameView.instance.BSPRenderer).mapName);
        Debug.Log("[BSPLoader] Identifier: " + identifier);
		Debug.Log("[BSPLoader] Version: " + version);
		Debug.Log("[BSPLoader] Map Revision: " + mapRevision);
	}

	private void LoadLumps()
	{
		for (int i = 0; i < lumps.Length; i++)
		{
			lump_t lump = new lump_t();
			lump.fileofs = FileReader.readInt(stream);
			lump.filelen = FileReader.readInt(stream);
			lump.version = FileReader.readInt(stream);
			lump.fourCC = FileReader.readInt(stream);
			lumps[i] = lump;
		}
	}

    public string GetEntities()
    {
        lump_t lump = lumps[0];
        string allEntities = "";
        stream.Position = lump.fileofs;

        for (int i = 0; i < lump.filelen; i++)
        {
            char nextChar = FileReader.readChar(stream);
            allEntities += nextChar;
        }

        return allEntities;
    }

    public dbrush_t[] GetBrushes()
    {
        lump_t lump = lumps[18];
        dbrush_t[] brushes = new dbrush_t[lump.filelen / 12];
        stream.Position = lump.fileofs;

        for (int i = 0; i < brushes.Length; i++)
        {
            brushes[i].firstside = FileReader.readInt(stream);
            brushes[i].numsides = FileReader.readInt(stream);
            brushes[i].contents = FileReader.readInt(stream);
        }

        lumpData[18] = brushes;
        return brushes;
    }

    public dbrushside_t[] GetBrushSides()
    {
        lump_t lump = lumps[19];
        dbrushside_t[] brushSides = new dbrushside_t[lump.filelen / 8];
        stream.Position = lump.fileofs;

        for (int i = 0; i < brushSides.Length; i++)
        {
            brushSides[i].planenum = FileReader.readUShort(stream);
            brushSides[i].texinfo = FileReader.readShort(stream);
            brushSides[i].dispinfo = FileReader.readShort(stream);
            brushSides[i].bevel = FileReader.readShort(stream);
        }

        lumpData[19] = brushSides;
        return brushSides;
    }

    public ddispinfo_t[] GetDispInfo()
    {
        lump_t lump = lumps[26];
        ddispinfo_t[] displacementInfo = new ddispinfo_t[lump.filelen / 86];
        stream.Position = lump.fileofs;

        for (int i = 0; i < displacementInfo.Length; i++)
        {
            displacementInfo[i].startPosition = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
            displacementInfo[i].DispVertStart = FileReader.readInt(stream);
            displacementInfo[i].DispTriStart = FileReader.readInt(stream);
            displacementInfo[i].power = FileReader.readInt(stream);
            displacementInfo[i].minTess = FileReader.readInt(stream);
            displacementInfo[i].smoothingAngle = FileReader.readFloat(stream);
            displacementInfo[i].contents = FileReader.readInt(stream);
            displacementInfo[i].MapFace = FileReader.readUShort(stream);
            displacementInfo[i].LightmapAlphaStart = FileReader.readInt(stream);
            displacementInfo[i].LightmapSamplePositionStart = FileReader.readInt(stream);
            stream.Position += 90;
            displacementInfo[i].AllowedVerts = new uint[10] { FileReader.readUInt(stream), FileReader.readUInt(stream), FileReader.readUInt(stream), FileReader.readUInt(stream), FileReader.readUInt(stream), FileReader.readUInt(stream), FileReader.readUInt(stream), FileReader.readUInt(stream), FileReader.readUInt(stream), FileReader.readUInt(stream) };
        }

        lumpData[26] = displacementInfo;
        return displacementInfo;
    }

    public dDispVert[] GetDispVerts()
    {
        lump_t lump = lumps[33];
        dDispVert[] displacementVertices = new dDispVert[lump.filelen / 20];
        stream.Position = lump.fileofs;

        for (int i = 0; i < displacementVertices.Length; i++)
        {
            displacementVertices[i].vec = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
            displacementVertices[i].dist = FileReader.readFloat(stream);
            displacementVertices[i].alpha = FileReader.readFloat(stream);
        }

        lumpData[33] = displacementVertices;
        return displacementVertices;
    }

	public dedge_t[] GetEdges()
	{
		lump_t lump = lumps[12];
        dedge_t[] edges = new dedge_t[lump.filelen / 4];
		stream.Position = lump.fileofs;

		for (int i = 0; i < edges.Length; i++)
		{
			edges[i].v = new ushort[2];
            edges[i].v[0] = FileReader.readUShort(stream);
            edges[i].v[1] = FileReader.readUShort(stream);
		}

		lumpData[12] = edges;
		return edges;
	}

	public Vector3[] GetVertices()
	{
		lump_t lump = lumps[3];
        Vector3[] vertices = new Vector3[lump.filelen / 12];
		stream.Position = lump.fileofs;

		for (int i = 0; i < vertices.Length; i++)
		{
            vertices[i] = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
		}

		lumpData[3] = vertices;
		return vertices;
	}

	public dface_t[] GetOriginalFaces()
	{
		lump_t lump = lumps[27];
        dface_t[] faces = new dface_t[lump.filelen / 56];
		stream.Position = lump.fileofs;

		for (int i = 0; i < faces.Length; i++)
		{
			faces[i].planenum = FileReader.readUShort(stream);
            faces[i].side = FileReader.readByte(stream);
            faces[i].onNode = FileReader.readByte(stream);
            faces[i].firstedge = FileReader.readInt(stream);
            faces[i].numedges = FileReader.readShort(stream);
            faces[i].texinfo = FileReader.readShort(stream);
            faces[i].dispinfo = FileReader.readShort(stream);
            faces[i].surfaceFogVolumeID = FileReader.readShort(stream);
            faces[i].styles = new byte[4] { FileReader.readByte(stream), FileReader.readByte(stream), FileReader.readByte(stream), FileReader.readByte(stream) };
            faces[i].lightofs = FileReader.readInt(stream);
            faces[i].area = FileReader.readFloat(stream);
            faces[i].LightmapTextureMinsInLuxels = new int[2] { FileReader.readInt(stream), FileReader.readInt(stream) };
            faces[i].LightmapTextureSizeInLuxels = new int[2] { FileReader.readInt(stream), FileReader.readInt(stream) };
            faces[i].origFace = FileReader.readInt(stream);
            faces[i].numPrims = FileReader.readUShort(stream);
            faces[i].firstPrimID = FileReader.readUShort(stream);
            faces[i].smoothingGroups = FileReader.readUInt(stream);
		}

		lumpData[27] = faces;
		return faces;
	}

	public dface_t[] GetFaces()
	{
		lump_t lump = lumps[7];
        dface_t[] faces = new dface_t[lump.filelen / 56];
		stream.Position = lump.fileofs;

		for (int i = 0; i < faces.Length; i++)
		{
            faces[i].planenum = FileReader.readUShort(stream);
            faces[i].side = FileReader.readByte(stream);
            faces[i].onNode = FileReader.readByte(stream);
            faces[i].firstedge = FileReader.readInt(stream);
            faces[i].numedges = FileReader.readShort(stream);
            faces[i].texinfo = FileReader.readShort(stream);
            faces[i].dispinfo = FileReader.readShort(stream);
            faces[i].surfaceFogVolumeID = FileReader.readShort(stream);
            faces[i].styles = new byte[4] { FileReader.readByte(stream), FileReader.readByte(stream), FileReader.readByte(stream), FileReader.readByte(stream) };
            faces[i].lightofs = FileReader.readInt(stream);
            faces[i].area = FileReader.readFloat(stream);
            faces[i].LightmapTextureMinsInLuxels = new int[2] { FileReader.readInt(stream), FileReader.readInt(stream) };
            faces[i].LightmapTextureSizeInLuxels = new int[2] { FileReader.readInt(stream), FileReader.readInt(stream) };
            faces[i].origFace = FileReader.readInt(stream);
            faces[i].numPrims = FileReader.readUShort(stream);
            faces[i].firstPrimID = FileReader.readUShort(stream);
            faces[i].smoothingGroups = FileReader.readUInt(stream);
		}

		lumpData[7] = faces;
		return faces;
	}

	public dplane_t[] GetPlanes()
	{
		lump_t lump = lumps[1];
        dplane_t[] planes = new dplane_t[lump.filelen / 20];
		stream.Position = lump.fileofs;

		for (int i = 0; i < planes.Length; i++)
		{
            planes[i].normal = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
            planes[i].dist = FileReader.readFloat(stream);
            planes[i].type = FileReader.readInt(stream);
		}

		lumpData[1] = planes;
		return planes;
	}

	public int[] GetSurfedges()
	{
		
		lump_t lump = lumps[13];
		int[] surfedges = new int[lump.filelen / 4];
		stream.Position = lump.fileofs;

		for (int i = 0; i < lump.filelen / 4; i++)
		{
			surfedges[i] = FileReader.readInt(stream);
		}

		lumpData[13] = surfedges;
		return surfedges;
	}

	public texinfo_t[] GetTextureInfo()
	{
		lump_t lump = lumps[6];
        texinfo_t[] textureInfo = new texinfo_t[lump.filelen / 72];
		stream.Position = lump.fileofs;

		for (int i = 0; i < textureInfo.Length; i++)
		{
            textureInfo[i].textureVecs = new float[2][];
            textureInfo[i].textureVecs[0] = new float[4] { FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream) };
            textureInfo[i].textureVecs[1] = new float[4] { FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream) };
            textureInfo[i].lightmapVecs = new float[2][];
            textureInfo[i].lightmapVecs[0] = new float[4] { FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream) };
            textureInfo[i].lightmapVecs[1] = new float[4] { FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream) };
            textureInfo[i].flags = FileReader.readInt(stream);
            textureInfo[i].texdata = FileReader.readInt(stream);
		}

        lumpData[6] = textureInfo;
        return textureInfo;
	}

    public dtexdata_t[] GetTextureData()
    {
        lump_t lump = lumps[2];
        dtexdata_t[] textureData = new dtexdata_t[lump.filelen / 32];
        stream.Position = lump.fileofs;

        for (int i = 0; i < textureData.Length; i++)
        {
            Vector3 reflectivity = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
            textureData[i].reflectivity = reflectivity;
            textureData[i].nameStringTableID = FileReader.readInt(stream);
            textureData[i].width = FileReader.readInt(stream);
            textureData[i].height = FileReader.readInt(stream);
            textureData[i].view_width = FileReader.readInt(stream);
            textureData[i].view_height = FileReader.readInt(stream);
        }

        lumpData[2] = textureData;
        return textureData;
    }

    public int[] GetTextureStringTable()
    {
        lump_t lump = lumps[44];
        int[] textureStringTable = new int[lump.filelen / 4];
        stream.Position = lump.fileofs;

        for (int i = 0; i < textureStringTable.Length; i++)
        {
            textureStringTable[i] = FileReader.readInt(stream);
        }

        return textureStringTable;
    }

    public string GetTextureStringData()
    {
        lump_t lump = lumps[43];
        stream.Position = lump.fileofs;

        string textureStringData = "";
        for (int i = 0; i < lump.filelen; i++)
        {
            char nextChar = FileReader.readChar(stream);

            if (nextChar != '\0') textureStringData += nextChar;
            else textureStringData += TEXTURE_STRING_DATA_SPLITTER;
        }
        return textureStringData;
    }
}