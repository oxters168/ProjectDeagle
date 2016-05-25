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

	public lump_t[] lumps;
	public object[] lumpData;
    public dgamelumpheader_t gameLumpHeader;
    //public dgamelump_t[] gameLumps;

    public BSPParser(Stream stream)
	{
		this.stream = stream;
		lumps = new lump_t[64];
		lumpData = new object[64];

		identifier = FileReader.readInt(stream);
		version = FileReader.readInt(stream);
		LoadLumps();
        LoadGameLumps();
		mapRevision = FileReader.readInt(stream);

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
    private void LoadGameLumps()
    {
        lump_t lump = lumps[35];
        stream.Position = lump.fileofs;

        //gameLumpHeader = new dgamelumpheader_t();
        gameLumpHeader.lumpCount = FileReader.readInt(stream);
        gameLumpHeader.gamelump = new dgamelump_t[gameLumpHeader.lumpCount];

        for(int i = 0; i < gameLumpHeader.gamelump.Length; i++)
        {
            gameLumpHeader.gamelump[i] = new dgamelump_t();
            gameLumpHeader.gamelump[i].id = FileReader.readInt(stream);
            gameLumpHeader.gamelump[i].flags = FileReader.readUShort(stream);
            gameLumpHeader.gamelump[i].version = FileReader.readUShort(stream);
            gameLumpHeader.gamelump[i].fileofs = FileReader.readInt(stream);
            gameLumpHeader.gamelump[i].filelen = FileReader.readInt(stream);
        }

        lumpData[35] = gameLumpHeader.gamelump;
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

    public StaticProps_t GetStaticProps()
    {
        dgamelump_t lump = null;

        //Debug.Log("# Game Lumps: " + gameLumpHeader.gamelump.Length);
        for(int i = 0; i < gameLumpHeader.gamelump.Length; i++)
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
            staticProps.staticPropDict.dictEntries = FileReader.readInt(stream);
            staticProps.staticPropDict.names = new string[staticProps.staticPropDict.dictEntries];

            for (int i = 0; i < staticProps.staticPropDict.names.Length; i++)
            {
                char[] nullPaddedName = new char[128];
                for (int j = 0; j < nullPaddedName.Length; j++)
                {
                    nullPaddedName[j] = FileReader.readChar(stream);
                }
                staticProps.staticPropDict.names[i] = new string(nullPaddedName);
                //Debug.Log(i + ": " + staticProps.staticPropDict.names[i]);
            }
            #endregion

            #region Leaf Lump
            staticProps.staticPropLeaf.leafEntries = FileReader.readInt(stream);
            staticProps.staticPropLeaf.leaf = new ushort[staticProps.staticPropLeaf.leafEntries];

            for(int i = 0; i < staticProps.staticPropLeaf.leaf.Length; i++)
            {
                staticProps.staticPropLeaf.leaf[i] = FileReader.readUShort(stream);
            }
            //Debug.Log("Leaf Entries: " + staticProps.staticPropLeaf.leaf.Length);
            #endregion

            #region Info Lump
            staticProps.staticPropInfo = new StaticPropLump_t[FileReader.readInt(stream)];
            //long currentSizeUsed = stream.Position - lump.fileofs;
            //Debug.Log("Used: " + currentSizeUsed + " Intended Length: " + lump.filelen + " BytesPerInfo: " + ((lump.filelen - currentSizeUsed) / staticProps.staticPropInfo.Length));
            //int largestIndex = -1;
            for (int i = 0; i < staticProps.staticPropInfo.Length; i++)
            {
                staticProps.staticPropInfo[i].Origin = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));       // origin
                staticProps.staticPropInfo[i].Origin = new Vector3(staticProps.staticPropInfo[i].Origin.x, staticProps.staticPropInfo[i].Origin.z, staticProps.staticPropInfo[i].Origin.y);
                staticProps.staticPropInfo[i].Angles = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));       // orientation (pitch roll yaw)
                //staticProps.staticPropInfo[i].Angles = new Vector3(staticProps.staticPropInfo[i].Angles.x, staticProps.staticPropInfo[i].Angles.z, staticProps.staticPropInfo[i].Angles.y);
                staticProps.staticPropInfo[i].PropType = FileReader.readUShort(stream);     // index into model name dictionary
                staticProps.staticPropInfo[i].FirstLeaf = FileReader.readUShort(stream);    // index into leaf array
                staticProps.staticPropInfo[i].LeafCount = FileReader.readUShort(stream);
                staticProps.staticPropInfo[i].Solid = FileReader.readByte(stream);         // solidity type
                staticProps.staticPropInfo[i].Flags = FileReader.readByte(stream);
                staticProps.staticPropInfo[i].Skin = FileReader.readInt(stream);        // model skin numbers
                staticProps.staticPropInfo[i].FadeMinDist = FileReader.readFloat(stream);
                staticProps.staticPropInfo[i].FadeMaxDist = FileReader.readFloat(stream);
                staticProps.staticPropInfo[i].LightingOrigin = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));  // for lighting
                                                              // since v5
                staticProps.staticPropInfo[i].ForcedFadeScale = FileReader.readFloat(stream); // fade distance scale
                                                              // v6 and v7 only
                staticProps.staticPropInfo[i].MinDXLevel = FileReader.readUShort(stream);      // minimum DirectX version to be visible
                staticProps.staticPropInfo[i].MaxDXLevel = FileReader.readUShort(stream);      // maximum DirectX version to be visible
                                                              // since v8
                staticProps.staticPropInfo[i].MinCPULevel = FileReader.readByte(stream);
                staticProps.staticPropInfo[i].MaxCPULevel = FileReader.readByte(stream);
                staticProps.staticPropInfo[i].MinGPULevel = FileReader.readByte(stream);
                staticProps.staticPropInfo[i].MaxGPULevel = FileReader.readByte(stream);
                // since v7
                staticProps.staticPropInfo[i].DiffuseModulation = new Color32(FileReader.readByte(stream), FileReader.readByte(stream), FileReader.readByte(stream), FileReader.readByte(stream)); // per instance color and alpha modulation
                                                                // since v10
                staticProps.staticPropInfo[i].unknown = FileReader.readFloat(stream);
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