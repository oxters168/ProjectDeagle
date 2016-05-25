using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class MDLParser {

    public string name;
    public mstudiobone_t[] bones;
    public mstudiobodyparts_t[] bodyParts;
    public mstudioattachment_t[] attachments;
    public mstudioanimdesc_t[] animDescs;
    public mstudiotexture_t[] textures;
    public string[] texturePaths;

    Stream stream;
    public studiohdr_t header1;
    public studiohdr2_t header2;

    public MDLParser(Stream stream)
    {
        this.stream = stream;
    }

    public void ParseHeader()
    {
        ParseHeader1();
        ParseHeader2();
    }
    public studiohdr_t ParseHeader1()
    {
        header1 = new studiohdr_t();

        header1.id = FileReader.readInt(stream); // Model format ID, such as "IDST" (0x49 0x44 0x53 0x54)
        header1.version = FileReader.readInt(stream); // Format version number, such as 48 (0x30,0x00,0x00,0x00)
        header1.checkSum = FileReader.readInt(stream); // this has to be the same in the phy and vtx files to load!
        char[] name = new char[64];
        for (int i = 0; i < name.Length; i++)
        {
            name[i] = FileReader.readChar(stream);
        }
        header1.name = name; // The internal name of the model, padding with null bytes.
        // Typically "my_model.mdl" will have an internal name of "my_model"
        this.name = new String(name);
        header1.dataLength = FileReader.readInt(stream);	// Data size of MDL file in bytes.

        // A vector is 12 bytes, three 4-byte float-values in a row.
        header1.eyeposition = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream)); // Position of player viewpoint relative to model origin
        header1.illumposition = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream)); // ?? Presumably the point used for lighting when per-vertex lighting is not enabled.
        header1.hull_min = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream)); // Corner of model hull box with the least X/Y/Z values
        header1.hull_max = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream)); // Opposite corner of model hull box
        header1.view_bbmin = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream)); // View Bounding Box Minimum Position
        header1.view_bbmax = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream)); // View Bounding Box Maximum Position

        header1.flags = FileReader.readInt(stream); // Binary flags in little-endian order. 
        // ex (00000001,00000000,00000000,11000000) means flags for position 0, 30, and 31 are set. 
        // Set model flags section for more information

        //Debug.Log("ID: " + header1.id + ", \nVersion: " + header1.version + ", \nCheckSum: " + header1.checkSum + ", \nName: " + this.name + ", \nLength: " + header1.dataLength);
        //Debug.Log("EyePos: " + header1.eyeposition + ", \nIllumPos: " + header1.illumposition + ", \nHullMin: " + header1.hull_min + ", \nHullMax: " + header1.hull_max + ", \nViewBBMin: " + header1.view_bbmin + ", \nViewBBMax: " + header1.view_bbmax);

        /*
         * After this point, the header contains many references to offsets
         * within the MDL file and the number of items at those offsets.
         *
         * Offsets are from the very beginning of the file.
         * 
         * Note that indexes/counts are not always paired and ordered consistently.
         */

        // mstudiobone_t
        header1.bone_count = FileReader.readInt(stream);	// Number of data sections (of type mstudiobone_t)
        header1.bone_offset = FileReader.readInt(stream);	// Offset of first data section

        // mstudiobonecontroller_t
        header1.bonecontroller_count = FileReader.readInt(stream);
        header1.bonecontroller_offset = FileReader.readInt(stream);

        // mstudiohitboxset_t
        header1.hitbox_count = FileReader.readInt(stream);
        header1.hitbox_offset = FileReader.readInt(stream);

        // mstudioanimdesc_t
        header1.localanim_count = FileReader.readInt(stream);
        header1.localanim_offset = FileReader.readInt(stream);

        // mstudioseqdesc_t
        header1.localseq_count = FileReader.readInt(stream);
        header1.localseq_offset = FileReader.readInt(stream);

        header1.activitylistversion = FileReader.readInt(stream); // ??
        header1.eventsindexed = FileReader.readInt(stream);	// ??

        // VMT texture filenames
        // mstudiotexture_t
        header1.texture_count = FileReader.readInt(stream);
        header1.texture_offset = FileReader.readInt(stream);

        // This offset points to a series of ints.
        // Each int value, in turn, is an offset relative to the start of this header/the-file,
        // At which there is a null-terminated string.
        header1.texturedir_count = FileReader.readInt(stream);
        header1.texturedir_offset = FileReader.readInt(stream);

        // Each skin-family assigns a texture-id to a skin location
        header1.skinreference_count = FileReader.readInt(stream);
        header1.skinrfamily_count = FileReader.readInt(stream);
        header1.skinreference_index = FileReader.readInt(stream);

        // mstudiobodyparts_t
        header1.bodypart_count = FileReader.readInt(stream);
        header1.bodypart_offset = FileReader.readInt(stream);

        // Local attachment points		
        // mstudioattachment_t
        header1.attachment_count = FileReader.readInt(stream);
        header1.attachment_offset = FileReader.readInt(stream);

        // Node values appear to be single bytes, while their names are null-terminated strings.
        header1.localnode_count = FileReader.readInt(stream);
        header1.localnode_index = FileReader.readInt(stream);
        header1.localnode_name_index = FileReader.readInt(stream);

        // mstudioflexdesc_t
        header1.flexdesc_count = FileReader.readInt(stream);
        header1.flexdesc_index = FileReader.readInt(stream);

        // mstudioflexcontroller_t
        header1.flexcontroller_count = FileReader.readInt(stream);
        header1.flexcontroller_index = FileReader.readInt(stream);

        // mstudioflexrule_t
        header1.flexrules_count = FileReader.readInt(stream);
        header1.flexrules_index = FileReader.readInt(stream);

        // IK probably referse to inverse kinematics
        // mstudioikchain_t
        header1.ikchain_count = FileReader.readInt(stream);
        header1.ikchain_index = FileReader.readInt(stream);

        // Information about any "mouth" on the model for speech animation
        // More than one sounds pretty creepy.
        // mstudiomouth_t
        header1.mouths_count = FileReader.readInt(stream);
        header1.mouths_index = FileReader.readInt(stream);

        // mstudioposeparamdesc_t
        header1.localposeparam_count = FileReader.readInt(stream);
        header1.localposeparam_index = FileReader.readInt(stream);

        /*
         * For anyone trying to follow along, as of this writing,
         * the next "surfaceprop_index" value is at position 0x0134 (308)
         * from the start of the file.
         */
        //stream.Position = 308;

        // Surface property value (single null-terminated string)
        header1.surfaceprop_index = FileReader.readInt(stream);

        // Unusual: In this one index comes first, then count.
        // Key-value data is a series of strings. If you can't find
        // what you're interested in, check the associated PHY file as well.
        header1.keyvalue_index = FileReader.readInt(stream);
        header1.keyvalue_count = FileReader.readInt(stream);

        // More inverse-kinematics
        // mstudioiklock_t
        header1.iklock_count = FileReader.readInt(stream);
        header1.iklock_index = FileReader.readInt(stream);


        header1.mass = FileReader.readFloat(stream); // Mass of object (4-bytes)
        header1.contents = FileReader.readInt(stream); // ??

        // Other models can be referenced for re-used sequences and animations
        // (See also: The $includemodel QC option.)
        // mstudiomodelgroup_t
        header1.includemodel_count = FileReader.readInt(stream);
        header1.includemodel_index = FileReader.readInt(stream);

        header1.virtualModel = FileReader.readInt(stream); // Placeholder for mutable-void*

        // mstudioanimblock_t
        header1.animblocks_name_index = FileReader.readInt(stream);
        header1.animblocks_count = FileReader.readInt(stream);
        header1.animblocks_index = FileReader.readInt(stream);

        header1.animblockModel = FileReader.readInt(stream); // Placeholder for mutable-void*

        // Points to a series of bytes?
        header1.bonetablename_index = FileReader.readInt(stream);

        header1.vertex_base = FileReader.readInt(stream); // Placeholder for void*
        header1.offset_base = FileReader.readInt(stream); // Placeholder for void*

        // Used with $constantdirectionallight from the QC 
        // Model should have flag #13 set if enabled
        header1.directionaldotproduct = FileReader.readByte(stream);

        header1.rootLod = FileReader.readByte(stream); // Preferred rather than clamped

        // 0 means any allowed, N means Lod 0 -> (N-1)
        header1.numAllowedRootLods = FileReader.readByte(stream);

        //header.unused; // ??
        FileReader.readByte(stream);
        //header.unused; // ??
        FileReader.readInt(stream);

        // mstudioflexcontrollerui_t
        header1.flexcontrollerui_count = FileReader.readInt(stream);
        header1.flexcontrollerui_index = FileReader.readInt(stream);

        header1.vertAnimFixedPointScale = FileReader.readFloat(stream);
        header1.surfacePropLookup = FileReader.readInt(stream);

        /**
         * Offset for additional header information.
         * May be zero if not present, or also 408 if it immediately 
         * follows this studiohdr_t
         */
        // studiohdr2_t
        header1.studiohdr2index = FileReader.readInt(stream);

        //header.unused; // ??
        FileReader.readInt(stream);

        return header1;
    }
    public studiohdr2_t ParseHeader2()
    {
        header2 = new studiohdr2_t();

        header2.srcbonetransform_count = FileReader.readInt(stream);
        header2.srcbonetransform_index = FileReader.readInt(stream);
        header2.illumpositionattachmentindex = FileReader.readInt(stream);
        header2.flMaxEyeDeflection = FileReader.readFloat(stream);
        header2.linearbone_index = FileReader.readInt(stream);

        header2.sznameindex = FileReader.readInt(stream);
        header2.m_nBoneFlexDriverCount = FileReader.readInt(stream);
        header2.m_nBoneFlexDriverIndex = FileReader.readInt(stream);

        int[] reserved = new int[56];
        for (int i = 0; i < reserved.Length; i++)
        {
            reserved[i] = FileReader.readInt(stream);
        }
        header2.reserved = reserved;

        return header2;
    }

    public mstudiobone_t[] ParseBones()
    {
        if (header1.bone_count >= 0)
        {
            long savePosition = header1.bone_offset;
            
            bones = new mstudiobone_t[header1.bone_count];
            for (int i = 0; i < bones.Length; i++)
            {
                stream.Position = savePosition;
                long bonePosition = savePosition;

                bones[i] = new mstudiobone_t();

                bones[i].nameOffset = FileReader.readInt(stream);
                bones[i].parentBoneIndex = FileReader.readInt(stream);
                //stream.Position += 150;
                bones[i].boneControllerIndex = new int[6];
                for (int j = 0; j < bones[i].boneControllerIndex.Length; j++)
                {
                    bones[i].boneControllerIndex[j] = FileReader.readInt(stream);
                }
                //FileReader.readInt(stream);
                bones[i].position = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                bones[i].quat = new Quaternion(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                if (header1.version != 2531)
                {
                    bones[i].rotation = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                    bones[i].positionScale = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                    bones[i].rotationScale = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                }
                //FileReader.readInt(stream);
                float[] columnExes = new float[4] { FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream) };
                float[] columnWise = new float[4] { FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream) };
                float[] columnZees = new float[4] { FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream) };

                bones[i].poseToBoneColumn0 = new Vector3(columnExes[0], columnWise[0], columnZees[0]);
                bones[i].poseToBoneColumn1 = new Vector3(columnExes[1], columnWise[1], columnZees[1]);
                bones[i].poseToBoneColumn2 = new Vector3(columnExes[2], columnWise[2], columnZees[2]);
                bones[i].poseToBoneColumn3 = new Vector3(columnExes[3], columnWise[3], columnZees[3]);

                if(header1.version != 2531) bones[i].qAlignment = new Quaternion(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));

                bones[i].flags = FileReader.readInt(stream);

                bones[i].proceduralRuleType = FileReader.readInt(stream);
                bones[i].proceduralRuleOffset = FileReader.readInt(stream);
                bones[i].physicsBoneIndex = FileReader.readInt(stream);
                bones[i].surfacePropNameOffset = FileReader.readInt(stream);
                bones[i].contents = FileReader.readInt(stream);

                if (header1.version != 2531)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        FileReader.readInt(stream);
                    }
                }

                savePosition = stream.Position;

                if (bones[i].nameOffset != 0)
                {
                    stream.Position = bonePosition + bones[i].nameOffset;
                    bones[i].name = FileReader.readNullTerminatedString(stream);
                }
                else bones[i].name = "";

                if (bones[i].surfacePropNameOffset != 0)
                {
                    stream.Position = bonePosition + bones[i].surfacePropNameOffset;
                    bones[i].theSurfacePropName = FileReader.readNullTerminatedString(stream);
                }
                else bones[i].theSurfacePropName = "";
            }
        }

        return bones;
    }

    public mstudiobodyparts_t[] ParseBodyParts()
    {
        if (header1.bodypart_count >= 0)
        {
            long nextBodyPartPosition = header1.bodypart_offset;

            bodyParts = new mstudiobodyparts_t[header1.bodypart_count];
            for (int i = 0; i < bodyParts.Length; i++)
            {
                stream.Position = nextBodyPartPosition;
                long bodyPartPosition = nextBodyPartPosition;

                bodyParts[i] = new mstudiobodyparts_t();

                bodyParts[i].nameOffset = FileReader.readInt(stream);
                bodyParts[i].modelCount = FileReader.readInt(stream);
                bodyParts[i].theBase = FileReader.readInt(stream);
                bodyParts[i].modelOffset = FileReader.readInt(stream);

                nextBodyPartPosition = stream.Position;

                if (bodyParts[i].nameOffset != 0)
                {
                    stream.Position = bodyPartPosition + bodyParts[i].nameOffset;
                    bodyParts[i].name = FileReader.readNullTerminatedString(stream);
                }
                else bodyParts[i].name = "";

                ParseModels(bodyPartPosition, bodyParts[i]);
            }
        }

        return bodyParts;
    }
    private void ParseModels(long bodyPartPosition, mstudiobodyparts_t bodyPart)
    {
        if (bodyPart.modelCount >= 0)
        {
            long nextModelPosition = bodyPartPosition + bodyPart.modelOffset;
            bodyPart.models = new mstudiomodel_t[bodyPart.modelCount];
            for (int i = 0; i < bodyPart.models.Length; i++)
            {
                stream.Position = nextModelPosition;
                long modelPosition = nextModelPosition;

                bodyPart.models[i] = new mstudiomodel_t();

                bodyPart.models[i].name = new char[64];
                for (int j = 0; j < bodyPart.models[i].name.Length; j++)
                {
                    bodyPart.models[i].name[j] = FileReader.readChar(stream);
                }
                bodyPart.models[i].type = FileReader.readInt(stream);
                bodyPart.models[i].boundingRadius = FileReader.readFloat(stream);
                bodyPart.models[i].meshCount = FileReader.readInt(stream);
                bodyPart.models[i].meshOffset = FileReader.readInt(stream);
                bodyPart.models[i].vertexCount = FileReader.readInt(stream);
                bodyPart.models[i].vertexOffset = FileReader.readInt(stream);
                bodyPart.models[i].tangentOffset = FileReader.readInt(stream);
                bodyPart.models[i].attachmentCount = FileReader.readInt(stream);
                bodyPart.models[i].attachmentOffset = FileReader.readInt(stream);
                bodyPart.models[i].eyeballCount = FileReader.readInt(stream);
                bodyPart.models[i].eyeballOffset = FileReader.readInt(stream);

                bodyPart.models[i].vertexData = new mstudio_modelvertexdata_t();
                bodyPart.models[i].vertexData.vertexDataP = FileReader.readInt(stream);
                bodyPart.models[i].vertexData.tangentDataP = FileReader.readInt(stream);

                bodyPart.models[i].unused = new int[8];
                for (int j = 0; j < bodyPart.models[i].unused.Length; j++)
                {
                    bodyPart.models[i].unused[j] = FileReader.readInt(stream);
                }

                nextModelPosition = stream.Position;

                ParseEyeballs(modelPosition, bodyPart.models[i]);
                ParseMeshes(modelPosition, bodyPart.models[i]);
            }
        }
    }
    private void ParseEyeballs(long modelPosition, mstudiomodel_t model)
    {
        if (model.eyeballCount >= 0 && model.eyeballOffset != 0)
        {
            model.theEyeballs = new mstudioeyeball_t[model.eyeballCount];

            long nextEyeballPosition = modelPosition + model.eyeballOffset;
            for (int i = 0; i < model.theEyeballs.Length; i++)
            {
                stream.Position = nextEyeballPosition;
                long eyeballPosition = nextEyeballPosition;

                model.theEyeballs[i] = new mstudioeyeball_t();

                model.theEyeballs[i].nameOffset = FileReader.readInt(stream);
                model.theEyeballs[i].boneIndex = FileReader.readInt(stream);
                model.theEyeballs[i].org = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                model.theEyeballs[i].zOffset = FileReader.readFloat(stream);
                model.theEyeballs[i].radius = FileReader.readFloat(stream);
                model.theEyeballs[i].up = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                model.theEyeballs[i].forward = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                model.theEyeballs[i].texture = FileReader.readInt(stream);

                model.theEyeballs[i].unused1 = FileReader.readInt(stream);
                model.theEyeballs[i].irisScale = FileReader.readFloat(stream);
                model.theEyeballs[i].unused2 = FileReader.readInt(stream);

                model.theEyeballs[i].upperFlexDesc = new int[3];
                model.theEyeballs[i].lowerFlexDesc = new int[3];
                model.theEyeballs[i].upperTarget = new double[3];
                model.theEyeballs[i].lowerTarget = new double[3];

                model.theEyeballs[i].upperFlexDesc[0] = FileReader.readInt(stream);
                model.theEyeballs[i].upperFlexDesc[1] = FileReader.readInt(stream);
                model.theEyeballs[i].upperFlexDesc[2] = FileReader.readInt(stream);
                model.theEyeballs[i].lowerFlexDesc[0] = FileReader.readInt(stream);
                model.theEyeballs[i].lowerFlexDesc[1] = FileReader.readInt(stream);
                model.theEyeballs[i].lowerFlexDesc[2] = FileReader.readInt(stream);
                model.theEyeballs[i].upperTarget[0] = FileReader.readFloat(stream);
                model.theEyeballs[i].upperTarget[1] = FileReader.readFloat(stream);
                model.theEyeballs[i].upperTarget[2] = FileReader.readFloat(stream);
                model.theEyeballs[i].lowerTarget[0] = FileReader.readFloat(stream);
                model.theEyeballs[i].lowerTarget[1] = FileReader.readFloat(stream);
                model.theEyeballs[i].lowerTarget[2] = FileReader.readFloat(stream);

                model.theEyeballs[i].upperLidFlexDesc = FileReader.readInt(stream);
                model.theEyeballs[i].lowerLidFlexDesc = FileReader.readInt(stream);

                model.theEyeballs[i].unused = new int[4];
                for (int j = 0; j < model.theEyeballs[i].unused.Length; j++)
                {
                    model.theEyeballs[i].unused[j] = FileReader.readInt(stream);
                }

                model.theEyeballs[i].eyeballIsNonFacs = FileReader.readByte(stream);

                model.theEyeballs[i].unused3 = new char[3];
                for (int j = 0; j < model.theEyeballs[i].unused3.Length; j++)
                {
                    model.theEyeballs[i].unused3[j] = FileReader.readChar(stream);
                }
                model.theEyeballs[i].unused4 = new int[7];
                for (int j = 0; j < model.theEyeballs[i].unused4.Length; j++)
                {
                    model.theEyeballs[i].unused4[j] = FileReader.readInt(stream);
                }

                //Set the default value to -1 to distinguish it from value assigned to it by ReadMeshes()
                model.theEyeballs[i].theTextureIndex = -1;

                nextEyeballPosition = stream.Position;

                if (model.theEyeballs[i].nameOffset != 0)
                {
                    stream.Position = eyeballPosition + model.theEyeballs[i].nameOffset;

                    model.theEyeballs[i].name = FileReader.readNullTerminatedString(stream);
                }
                else model.theEyeballs[i].name = "";
            }
        }
    }
    private void ParseMeshes(long modelPosition, mstudiomodel_t model)
    {
        if (model.meshCount >= 0)
        {
            long nextMeshPosition = modelPosition + model.meshOffset;
            model.theMeshes = new mstudiomesh_t[model.meshCount];

            for (int i = 0; i < model.theMeshes.Length; i++)
            {
                stream.Position = nextMeshPosition;
                long meshPosition = nextMeshPosition;

                model.theMeshes[i] = new mstudiomesh_t();

                model.theMeshes[i].materialIndex = FileReader.readInt(stream);
                model.theMeshes[i].modelOffset = FileReader.readInt(stream);
                model.theMeshes[i].vertexCount = FileReader.readInt(stream);
                model.theMeshes[i].vertexIndexStart = FileReader.readInt(stream);
                model.theMeshes[i].flexCount = FileReader.readInt(stream);
                model.theMeshes[i].flexOffset = FileReader.readInt(stream);
                model.theMeshes[i].materialType = FileReader.readInt(stream);
                model.theMeshes[i].materialParam = FileReader.readInt(stream);
                model.theMeshes[i].id = FileReader.readInt(stream);
                model.theMeshes[i].center = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));

                model.theMeshes[i].vertexData = new mstudio_meshvertexdata_t();
                model.theMeshes[i].vertexData.modelVertexDataP = FileReader.readInt(stream);
                model.theMeshes[i].vertexData.lodVertexCount = new int[8];
                for (int j = 0; j < model.theMeshes[i].vertexData.lodVertexCount.Length; j++)
                {
                    model.theMeshes[i].vertexData.lodVertexCount[j] = FileReader.readInt(stream);
                }

                model.theMeshes[i].unused = new int[8];
                for (int j = 0; j < model.theMeshes[i].unused.Length; j++)
                {
                    model.theMeshes[i].unused[j] = FileReader.readInt(stream);
                }

                if (model.theMeshes[i].materialType == 1)
                {
                    model.theEyeballs[model.theMeshes[i].materialParam].theTextureIndex = model.theMeshes[i].materialIndex;
                }

                nextMeshPosition = stream.Position;

                if (model.theMeshes[i].flexCount > 0 && model.theMeshes[i].flexOffset != 0)
                {
                    ParseFlexes(meshPosition, model.theMeshes[i]);
                }

                //stream.Position = model.theMeshes[i].vertexData.modelVertexDataP + model.theMeshes[i].vertexIndexStart;
                //model.theMeshes[i].vertices = new Vector3[model.theMeshes[i].vertexCount];
                //for (int j = 0; j < model.theMeshes[i].vertices.Length; j++)
                //{
                //    model.theMeshes[i].vertices[j] = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                //    if (j >= 0 && j < 100) Debug.Log("Mesh " + i + ": V" + j + " " + model.theMeshes[i].vertices[j]);
                //}
            }
        }
    }
    private void ParseFlexes(long meshPosition, mstudiomesh_t mesh)
    {

    }

    public mstudioattachment_t[] ParseAttachments()
    {
        if (header1.attachment_count >= 0)
        {
            long nextAttachmentPosition = header1.attachment_offset;

            attachments = new mstudioattachment_t[header1.attachment_count];
            for (int i = 0; i < attachments.Length; i++)
            {
                stream.Position = nextAttachmentPosition;
                long attachmentPosition = nextAttachmentPosition;

                if (header1.version == 10)
                {
                    attachments[i].builtName = new char[32];
                    for (int j = 0; j < attachments[i].builtName.Length; j++)
                    {
                        attachments[i].builtName[j] = FileReader.readChar(stream);
                    }
                    attachments[i].type = FileReader.readInt(stream);
                    attachments[i].bone = FileReader.readInt(stream);

                    attachments[i].attachmentPoint = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                    attachments[i].vectors = new Vector3[3];
                    for (int j = 0; j < attachments[i].vectors.Length; j++)
                    {
                        attachments[i].vectors[j] = new Vector3(FileReader.readFloat(stream), FileReader.readFloat(stream), FileReader.readFloat(stream));
                    }
                }
                else
                {
                    attachments[i].nameOffset = FileReader.readInt(stream);
                    attachments[i].flags = FileReader.readInt(stream);
                    attachments[i].localBoneIndex = FileReader.readInt(stream);
                    attachments[i].localM11 = FileReader.readFloat(stream);
                    attachments[i].localM12 = FileReader.readFloat(stream);
                    attachments[i].localM13 = FileReader.readFloat(stream);
                    attachments[i].localM14 = FileReader.readFloat(stream);
                    attachments[i].localM21 = FileReader.readFloat(stream);
                    attachments[i].localM22 = FileReader.readFloat(stream);
                    attachments[i].localM23 = FileReader.readFloat(stream);
                    attachments[i].localM24 = FileReader.readFloat(stream);
                    attachments[i].localM31 = FileReader.readFloat(stream);
                    attachments[i].localM32 = FileReader.readFloat(stream);
                    attachments[i].localM33 = FileReader.readFloat(stream);
                    attachments[i].localM34 = FileReader.readFloat(stream);
                    attachments[i].unused = new int[8];
                    for (int j = 0; j < attachments[i].unused.Length; j++)
                    {
                        attachments[i].unused[j] = FileReader.readInt(stream);
                    }
                }

                nextAttachmentPosition = stream.Position;

                if (attachments[i].nameOffset != 0)
                {
                    stream.Position = attachmentPosition + attachments[i].nameOffset;
                    attachments[i].name = FileReader.readNullTerminatedString(stream);
                }
            }
        }

        return attachments;
    }

    public mstudioanimdesc_t[] ParseAnimationDescs()
    {
        if (header1.localanim_count >= 0)
        {
            long animDescFileByteSize = 0;
            long nextAnimDescPosition = header1.localanim_offset;

            animDescs = new mstudioanimdesc_t[header1.localanim_count];
            for (int i = 0; i < animDescs.Length; i++)
            {
                stream.Position = nextAnimDescPosition;
                long animDescPosition = nextAnimDescPosition;

                animDescs[i].baseHeaderOffset = FileReader.readInt(stream);
                animDescs[i].nameOffset = FileReader.readInt(stream);
                animDescs[i].fps = FileReader.readFloat(stream);
                animDescs[i].flags = FileReader.readInt(stream);
                animDescs[i].frameCount = FileReader.readInt(stream);
                animDescs[i].movementCount = FileReader.readInt(stream);
                animDescs[i].movementOffset = FileReader.readInt(stream);

                animDescs[i].ikRuleZeroFrameOffset = FileReader.readInt(stream);

                animDescs[i].unused1 = new int[5];
                for (int j = 0; j < animDescs[i].unused1.Length; j++)
                {
                    animDescs[i].unused1[j] = FileReader.readInt(stream);
                }

                animDescs[i].animBlock = FileReader.readInt(stream);
                animDescs[i].animOffset = FileReader.readInt(stream);
                animDescs[i].ikRuleCount = FileReader.readInt(stream);
                animDescs[i].ikRuleOffset = FileReader.readInt(stream);
                animDescs[i].animblockIkRuleOffset = FileReader.readInt(stream);
                animDescs[i].localHierarchyCount = FileReader.readInt(stream);
                animDescs[i].localHierarchyOffset = FileReader.readInt(stream);
                animDescs[i].sectionOffset = FileReader.readInt(stream);
                animDescs[i].sectionFrameCount = FileReader.readInt(stream);

                animDescs[i].spanFrameCount = FileReader.readShort(stream);
                animDescs[i].spanCount = FileReader.readShort(stream);
                animDescs[i].spanOffset = FileReader.readInt(stream);
                animDescs[i].spanStallTime = FileReader.readFloat(stream);

                nextAnimDescPosition = stream.Position;
                if (i == 0) animDescFileByteSize = nextAnimDescPosition - animDescPosition;

                if (animDescs[i].nameOffset != 0)
                {
                    stream.Position = animDescPosition + animDescs[i].nameOffset;
                    animDescs[i].name = FileReader.readNullTerminatedString(stream);
                }
                else animDescs[i].name = "";
            }

            for (int i = 0; i < animDescs.Length; i++)
            {
                long animDescPosition = header1.localanim_offset + (i * animDescFileByteSize);
                stream.Position = animDescPosition;

                if ((((animdesc_flags) animDescs[i].flags) & animdesc_flags.STUDIO_ALLZEROS) == 0)
                {
                    animDescs[i].sectionsOfAnimations = new List<List<mstudioanim_t>>();
                    //List<mstudioanim_t> animationSection = new List<mstudioanim_t>();
                    //animDescs[i].sectionsOfAnimations.Add(animationSection);
                    animDescs[i].sectionsOfAnimations.Add(new List<mstudioanim_t>());

                    if ((((animdesc_flags)animDescs[i].flags) & animdesc_flags.STUDIO_FRAMEANIM) != 0)
                    {
                        //if (animDescs[i].sectionOffset != 0 && animDescs[i].sectionFrameCount > 0) ;
                        //else if (animDescs[i].animBlock == 0) ;
                    }
                    else
                    {
                        if (animDescs[i].sectionOffset != 0 && animDescs[i].sectionFrameCount > 0)
                        {
                            int sectionCount = (animDescs[i].frameCount / animDescs[i].sectionFrameCount) + 2;

                            for (int j = 1; j < sectionCount; j++)
                            {
                                animDescs[i].sectionsOfAnimations.Add(new List<mstudioanim_t>());
                            }

                            animDescs[i].sections = new List<mstudioanimsections_t>();
                            for (int j = 0; j < sectionCount; j++)
                            {
                                ParseMdlAnimationSection(animDescPosition + animDescs[i].sectionOffset, animDescs[i]);
                            }

                            if (animDescs[i].animBlock == 0)
                            {
                                for (int j = 0; j < sectionCount; j++)
                                {
                                    int sectionFrameCount = 0;
                                    if (j < sectionCount - 2)
                                    {
                                        sectionFrameCount = animDescs[i].sectionFrameCount;
                                    }
                                    else
                                    {
                                        sectionFrameCount = animDescs[i].frameCount - ((sectionCount - 2) * animDescs[i].sectionFrameCount);
                                    }

                                    ParseMdlAnimation(animDescPosition + animDescs[i].sections[j].animOffset, animDescs[i], sectionFrameCount, animDescs[i].sectionsOfAnimations[j]);
                                }
                            }
                        }
                    }
                }
            }
        }

        return animDescs;
    }
    private void ParseMdlAnimationSection(long sectionPosition, mstudioanimdesc_t animDesc)
    {
        stream.Position = sectionPosition;

        mstudioanimsections_t animSection = new mstudioanimsections_t();
        animSection.animBlock = FileReader.readInt(stream);
        animSection.animOffset = FileReader.readInt(stream);
        animDesc.sections.Add(animSection);
    }
    private void ParseMdlAnimation(long animPosition, mstudioanimdesc_t animDesc, int sectionFrameCount, List<mstudioanim_t> sectionOfAnims)
    {

    }

    public mstudiotexture_t[] ParseTextures()
    {
        if (header1.texture_count >= 0)
        {
            long nextTexturePosition = header1.texture_offset;

            textures = new mstudiotexture_t[header1.texture_count];
            for (int i = 0; i < textures.Length; i++)
            {
                stream.Position = nextTexturePosition;
                long texturePosition = nextTexturePosition;

                textures[i] = new mstudiotexture_t();

                textures[i].nameOffset = FileReader.readInt(stream);
                textures[i].flags = FileReader.readInt(stream);
                textures[i].used = FileReader.readInt(stream);
                textures[i].unused1 = FileReader.readInt(stream);
                textures[i].materialP = FileReader.readInt(stream);
                textures[i].clientMaterialP = FileReader.readInt(stream);

                textures[i].unused = new int[10];
                for (int j = 0; j < textures[i].unused.Length; j++)
                {
                    textures[i].unused[j] = FileReader.readInt(stream);
                }

                nextTexturePosition = stream.Position;

                if (textures[i].nameOffset != 0)
                {
                    stream.Position = texturePosition + textures[i].nameOffset;
                    textures[i].name = FileReader.readNullTerminatedString(stream);
                }
                else textures[i].name = "";
            }
        }

        return textures;
    }
    public string[] ParseTexturePaths()
    {
        if (header1.texturedir_count >= 0)
        {
            long nextTextureDirPosition = header1.texturedir_offset;

            texturePaths = new string[header1.texturedir_count];
            for (int i = 0; i < texturePaths.Length; i++)
            {
                stream.Position = nextTextureDirPosition;
                int texturePathPosition = FileReader.readInt(stream);

                nextTextureDirPosition = stream.Position;

                if (texturePathPosition != 0)
                {
                    stream.Position = texturePathPosition;
                    texturePaths[i] = FileReader.readNullTerminatedString(stream);
                }
                else texturePaths[i] = "";
            }
        }

        return texturePaths;
    }
}
