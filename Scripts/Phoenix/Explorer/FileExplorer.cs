using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class FileExplorer : Explorer
{
    public const string DRIVES = "Drives";

    public Sprite fileSprite, directorySprite;

    public List<string> extensions;
    public string currentDirectory;
    private List<string> directoryHistory;
    public VisibleItems explorerType = VisibleItems.Files | VisibleItems.Directories;

	protected override void Start ()
    {
        base.Start();
        //directoryHistory = new List<string>();
        //extensions = new List<string>();
        ChangeDirectory(currentDirectory);
        itemDoubleClickedEvent += FileExplorer_itemDoubleClickedEvent;
	}

    private void FileExplorer_itemDoubleClickedEvent(ListableButton item)
    {
        if(item.listableItem.itemType == "Folder")
        {
            ChangeDirectory((string)item.listableItem.value);
            selectedItem = null;
        }
    }

    public void UpDirectory()
    {
        string upDirectory = DRIVES;
        DirectoryInfo parentInfo = Directory.GetParent(currentDirectory);
        if (parentInfo != null) upDirectory = parentInfo.FullName;
        ChangeDirectory(upDirectory);
    }
    public void BackDirectory()
    {
        if(directoryHistory.Count > 0)
        {
            currentDirectory = directoryHistory[directoryHistory.Count - 1];
            directoryHistory.RemoveAt(directoryHistory.Count - 1);
            PopulateItems();
        }
        Refresh();
    }
    public void ChangeDirectory(string directory)
    {
        string nextDirectory = directory;
        if (nextDirectory != DRIVES && !Directory.Exists(nextDirectory)) nextDirectory = currentDirectory;

        if (nextDirectory != currentDirectory)
        {
            AddToHistory(currentDirectory);
            currentDirectory = nextDirectory;
        }
        else if(currentDirectory == null || nextDirectory.Length <= 0)
        {
            currentDirectory = DRIVES;
        }

        PopulateItems();
        Refresh();
    }
    private void PopulateItems()
    {
        if (items == null) items = new List<ListableItem>();
        items.Clear();

        if (currentDirectory != DRIVES)
        {
            DirectoryInfo parentInfo = null;
            try { parentInfo = Directory.GetParent(currentDirectory); } catch (System.Exception) { }
            items.Add(new ListableItem("..", "Folder", directorySprite, parentInfo != null ? DirPathConvention(parentInfo.Parent.FullName) : DRIVES));

            if ((explorerType & VisibleItems.Directories) != 0)
            {
                string[] subdirectories = Directory.GetDirectories(currentDirectory);
                foreach (string subdirectory in subdirectories)
                {
                    string conventional = DirPathConvention(subdirectory);
                    items.Add(new ListableItem(GetLastPartInAbsolutePath(conventional), "Folder", directorySprite, conventional));
                }
            }
            if ((explorerType & VisibleItems.Files) != 0)
            {
                string[] files = new string[0];
                string searchPattern = GenerateSearchPattern(extensions.ToArray());
                if (searchPattern.Length > 0) files = Directory.GetFiles(currentDirectory, searchPattern, SearchOption.TopDirectoryOnly);
                else files = Directory.GetFiles(currentDirectory);

                foreach (string file in files)
                {
                    string conventional = FilePathConvention(file);
                    items.Add(new ListableItem(GetLastPartInAbsolutePath(conventional), "File", fileSprite, conventional));
                }
            }
        }
        else
        {
            string[] drives = Directory.GetLogicalDrives();
            foreach (string drive in drives)
            {
                string conventional = FilePathConvention(drive);
                items.Add(new ListableItem(conventional, "Folder", directorySprite, conventional));
            }
        }
    }
    private void AddToHistory(string directory)
    {
        if (directoryHistory == null) directoryHistory = new List<string>();
        if (directory != null && directory.Length > 0) directoryHistory.Add(directory);
    }

    public static string GenerateSearchPattern(string[] extensions)
    {
        string searchPattern = "";
        if (extensions != null)
        {
            for (int i = 0; i < extensions.Length; i++)
            {
                searchPattern += "*." + extensions[i];
                if (i < extensions.Length - 1) searchPattern += "|";
            }
        }
        return searchPattern;
    }
    public static string GetLastPartInAbsolutePath(string input)
    {
        string output = DirPathConvention(input);
        if (output.LastIndexOf("/") == output.Length - 1) output = output.Substring(0, output.Length - 1);
        if (output.LastIndexOf("/") > -1) output = output.Substring(output.LastIndexOf("/") + 1);
        return output;
    }
    public static string DirPathConvention(string input)
    {
        if (input == null) input = "";

        string output = input.Replace("\\", "/");
        if (output.LastIndexOf("/") < output.Length - 1) output += "/";
        return output;
    }
    public static string FilePathConvention(string input)
    {
        if (input == null) input = "";

        string output = input.Replace("\\", "/");
        return output;
    }

    [System.Flags]
    public enum VisibleItems { Files = 0x2, Directories = 0x4, }
}
