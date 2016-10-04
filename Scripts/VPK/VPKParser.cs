using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class VPKParser
{
    private const ushort DIR_PAK = 0x7fff, NO_PAK = ushort.MaxValue;
    public string directoryLocation { get; private set; }
    public string vpkPakName { get; private set; }
    public string location { get; private set; }
    private ushort currentlyOpenArchive = NO_PAK;
    private Stream currentStream;
    private VPKHeader header;
    private int headerSize;
    private Dictionary<string, Dictionary<string, Dictionary<string, VPKDirectoryEntry>>> tree;
    public bool parsed { get { return tree != null && tree.Count > 0; } private set { } }

    public VPKParser(string location)
    {
        if (location.Length > 0)
        {
            this.location = location.Replace("\\", "/").ToLower();
            if (this.location.IndexOf("/") > -1 && this.location.LastIndexOf("/") != this.location.Length - 1) directoryLocation = this.location.Substring(0, this.location.LastIndexOf("/") + 1);
            if (this.location.IndexOf("/") > -1 && this.location.LastIndexOf("/") != this.location.Length - 1) vpkPakName = this.location.Substring(this.location.LastIndexOf("/") + 1);
            if (vpkPakName != null && vpkPakName.IndexOf("_") > -1) vpkPakName = vpkPakName.Substring(0, vpkPakName.IndexOf("_"));
        }
    }

    private bool ParseHeader()
    {
        uint signature = DataParser.ReadUInt(currentStream);

        if (signature == VPKHeader.Signature)
        {
            header.Version = DataParser.ReadUInt(currentStream);
            header.TreeSize = DataParser.ReadUInt(currentStream);
            headerSize = 12;

            if (header.Version > 1)
            {
                header.FileDataSectionSize = DataParser.ReadUInt(currentStream);
                header.ArchiveMD5SectionSize = DataParser.ReadUInt(currentStream);
                header.OtherMD5SectionSize = DataParser.ReadUInt(currentStream);
                header.SignatureSectionSize = DataParser.ReadUInt(currentStream);
                headerSize += 16;
            }
        }
        else { Debug.Log("Signature Mismatch"); return false; }

        return true;
    }
    private void ParseTree()
    {
        tree = new Dictionary<string, Dictionary<string, Dictionary<string, VPKDirectoryEntry>>>();
        long readFromTree = 0;

        while (readFromTree < header.TreeSize)
        {
            string extension = DataParser.ReadNullTerminatedString(currentStream);
            readFromTree++;
            if (extension.Length <= 0) extension = tree.Keys.ElementAt(tree.Count - 1);
            else
            {
                if (!tree.ContainsKey(extension)) { tree.Add(extension, new Dictionary<string, Dictionary<string, VPKDirectoryEntry>>()); }
                readFromTree += extension.Length;
            }

            while (true)
            {
                string directory = DataParser.ReadNullTerminatedString(currentStream);
                readFromTree++;
                if (directory.Length <= 0) break;
                if (!tree[extension].ContainsKey(directory)) tree[extension].Add(directory, new Dictionary<string, VPKDirectoryEntry>());
                readFromTree += directory.Length;

                while (true)
                {
                    string file = DataParser.ReadNullTerminatedString(currentStream);
                    readFromTree++;
                    if (file.Length <= 0) break;
                    readFromTree += file.Length;

                    VPKDirectoryEntry dirEntry;
                    dirEntry.CRC = DataParser.ReadUInt(currentStream);
                    dirEntry.PreloadBytes = DataParser.ReadUShort(currentStream);
                    dirEntry.ArchiveIndex = DataParser.ReadUShort(currentStream);
                    dirEntry.EntryOffset = DataParser.ReadUInt(currentStream);
                    dirEntry.EntryLength = DataParser.ReadUInt(currentStream);
                    ushort terminator = DataParser.ReadUShort(currentStream);
                    readFromTree += (4 + 2 + 2 + 4 + 4 + 2);

                    dirEntry.PreloadData = new byte[dirEntry.PreloadBytes];
                    for (int i = 0; i < dirEntry.PreloadData.Length; i++)
                    {
                        dirEntry.PreloadData[i] = DataParser.ReadByte(currentStream);
                    }
                    readFromTree += dirEntry.PreloadBytes;

                    if (!tree[extension][directory].ContainsKey(file)) tree[extension][directory].Add(file, dirEntry);
                }
            }
        }

        //Debug.Log("Extensions: " + tree.Count);
        //for(int i = 0; i < tree.Count; i++)
        //{
        //    Debug.Log(tree.Keys.ElementAt(i) + ": " + tree.Values.ElementAt(i).Count);
        //    for(int j = 0; j < tree.Values.ElementAt(i).Count; j++)
        //    {
        //        Debug.Log(tree.Values.ElementAt(i).Keys.ElementAt(j) + ": " + tree.Values.ElementAt(i).Values.ElementAt(j).Count);
        //    }
        //}
    }

    public void Parse()
    {
        try { currentStream = new FileStream(location, FileMode.Open); currentlyOpenArchive = DIR_PAK; } catch (System.Exception) { currentlyOpenArchive = NO_PAK; }
        if (currentlyOpenArchive != NO_PAK && ParseHeader())
        {
            ParseTree();
        }
    }

    public byte[] LoadFile(string path)
    {
        string fixedPath = path.Replace("\\", "/").ToLower();
        string extension = fixedPath.Substring(fixedPath.LastIndexOf(".") + 1);
        string directory = fixedPath.Substring(0, fixedPath.LastIndexOf("/"));
        string fileName = fixedPath.Substring(fixedPath.LastIndexOf("/") + 1);
        fileName = fileName.Substring(0, fileName.LastIndexOf("."));

        return LoadFile(extension, directory, fileName);
    }
    public byte[] LoadFile(string extension, string directory, string fileName)
    {
        byte[] file = null;

        string extFixed = extension.ToLower();
        string dirFixed = directory.Replace("\\", "/").ToLower();
        string fileNameFixed = fileName.ToLower();

        if (extFixed.IndexOf(".") == 0) extFixed = extFixed.Substring(1);

        if (dirFixed.IndexOf("/") == 0) dirFixed = dirFixed.Substring(1);
        if (dirFixed.LastIndexOf("/") == dirFixed.Length - 1) dirFixed = dirFixed.Substring(0, dirFixed.Length - 1);

        if (tree != null && tree.ContainsKey(extFixed) && tree[extFixed].ContainsKey(dirFixed) && tree[extFixed][dirFixed].ContainsKey(fileNameFixed))
        {
            VPKDirectoryEntry entry = tree[extFixed][dirFixed][fileNameFixed];

            if (entry.EntryLength <= 0) return entry.PreloadData;

            #region Get Correct Pak and Full Path
            bool alreadyOpenPak = false;

            string vpkPakDir = "_";
            //if (entry.ArchiveIndex >= 1000) Debug.Log("Probably in Dir dir");
            if (entry.ArchiveIndex == DIR_PAK)
            {
                //Debug.Log("Switching to Dir dir");
                vpkPakDir += "dir";

                if (currentlyOpenArchive == DIR_PAK) alreadyOpenPak = true;
                else currentlyOpenArchive = DIR_PAK;
            }
            else if (entry.ArchiveIndex < 1000)
            {
                if (entry.ArchiveIndex >= 0 && entry.ArchiveIndex < 10) vpkPakDir += "00" + entry.ArchiveIndex;
                else if (entry.ArchiveIndex >= 10 && entry.ArchiveIndex < 100) vpkPakDir += "0" + entry.ArchiveIndex;
                else vpkPakDir += entry.ArchiveIndex;

                if (currentlyOpenArchive == entry.ArchiveIndex) alreadyOpenPak = true;
                else currentlyOpenArchive = entry.ArchiveIndex;
            }
            vpkPakDir += ".vpk";
            #endregion

            #region Open Correct Pak
            if(!alreadyOpenPak)
            {
                currentStream.Close();
                try { currentStream = new FileStream(directoryLocation + vpkPakName + vpkPakDir, FileMode.Open); } catch (System.Exception) { currentlyOpenArchive = NO_PAK; }
            }
            #endregion

            if (currentlyOpenArchive != NO_PAK)
            {
                #region Set Position in Stream
                if (currentlyOpenArchive == DIR_PAK) currentStream.Position = headerSize + header.TreeSize;
                else currentStream.Position = 0;
                currentStream.Position += entry.EntryOffset;
                #endregion

                #region Read File Bytes
                file = DataParser.ReadBytes(currentStream, entry.EntryLength);
                #endregion
            }
        }

        return file;
    }

    public bool FileExists(string path)
    {
        string fixedPath = path.Replace("\\", "/").ToLower();
        string extension = fixedPath.Substring(fixedPath.LastIndexOf(".") + 1);
        string directory = fixedPath.Substring(0, fixedPath.LastIndexOf("/"));
        string fileName = fixedPath.Substring(fixedPath.LastIndexOf("/") + 1);
        fileName = fileName.Substring(0, fileName.LastIndexOf("."));

        return FileExists(extension, directory, fileName);
    }
    public bool FileExists(string extension, string directory, string fileName)
    {
        string extFixed = extension.ToLower();
        string dirFixed = directory.Replace("\\", "/").ToLower();
        string fileNameFixed = fileName.ToLower();

        if (extFixed.IndexOf(".") == 0) extFixed = extFixed.Substring(1);

        if (dirFixed.IndexOf("/") == 0) dirFixed = dirFixed.Substring(1);
        if (dirFixed.LastIndexOf("/") == dirFixed.Length - 1) dirFixed = dirFixed.Substring(0, dirFixed.Length - 1);

        if (tree != null && tree.ContainsKey(extFixed) && tree[extFixed].ContainsKey(dirFixed) && tree[extFixed][dirFixed].ContainsKey(fileNameFixed))
        {
            return true;
        }

        return false;
    }
    public bool DirectoryExists(string directory)
    {
        string dirFixed = directory.Replace("\\", "/").ToLower();

        if (dirFixed.IndexOf("/") == 0) dirFixed = dirFixed.Substring(1);
        if (dirFixed.LastIndexOf("/") == dirFixed.Length - 1) dirFixed = dirFixed.Substring(0, dirFixed.Length - 1);

        if (tree != null)
        {
            for (int i = 0; i < tree.Count; i++)
            {
                if (tree.ContainsKey(tree.Keys.ElementAt(i)) && tree[tree.Keys.ElementAt(i)].ContainsKey(dirFixed))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Close()
    {
        currentStream.Close();
        currentlyOpenArchive = NO_PAK;
    }
}
