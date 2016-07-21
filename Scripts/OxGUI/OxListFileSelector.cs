using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace OxGUI
{
    public class OxListFileSelector : OxMenu
    {
        private bool firstTimeLoad = true;
        private string previousDirectory = "";
        public string currentDirectory;
        public bool directorySelection = false;
        private List<string> extensions = new List<string>();
        private List<float> savedScroll = new List<float>();

        public OxListFileSelector() : this(Vector2.zero, Vector2.zero, "") { }
        public OxListFileSelector(string startingDir) : this(Vector2.zero, Vector2.zero, startingDir) { }
        public OxListFileSelector(Vector2 position, Vector2 size) : this(position, size, "") { }
        public OxListFileSelector(Vector2 position, Vector2 size, string startingDir) : base(position, size)
        {
            currentDirectory = startingDir;
            enableItemSelection = true;
        }

        public override void Draw()
        {
            if (firstTimeLoad || !previousDirectory.Equals(currentDirectory)) { PopulateList(); previousDirectory = currentDirectory; firstTimeLoad = false; }
            base.Draw();
        }

        private void PopulateList()
        {
            currentDirectory = OxHelpers.PathConvention(currentDirectory);
            FindDirectory();

            ClearItems();
            if(currentDirectory.Length > 0)
            {
                AddBackButton();
                AddDirectories();
                if (!directorySelection)
                {
                    AddFiles();
                }
            }
            else
            {
                AddDrives();             
            }
        }

        private void FindDirectory()
        {
            while(currentDirectory.Length > 0 && !Directory.Exists(currentDirectory))
            {
                if (currentDirectory.LastIndexOf("/") == currentDirectory.Length - 1)
                {
                    currentDirectory = currentDirectory.Substring(0, currentDirectory.Length - 1);
                    if (currentDirectory.IndexOf("/") > -1) currentDirectory = currentDirectory.Substring(0, currentDirectory.LastIndexOf("/") + 1);
                    else currentDirectory = "";
                }
                else currentDirectory = "";
            }
        }
        private void AddBackButton()
        {
            OxButton backButton = new OxButton("..");
            backButton.clicked += BackButton_clicked;
            AddItems(backButton);
        }
        private void AddDirectories()
        {
            string[] directories = Directory.GetDirectories(currentDirectory);
            foreach (string directory in directories)
            {
                string shortenedDirectory = OxHelpers.GetLastPartInAbsolutePath(directory);

                OxButton dirButton = new OxButton(shortenedDirectory + "/");
                dirButton.clicked += DirectoryButton_clicked;
                AddItems(dirButton);
            }
        }
        private void AddFiles()
        {
            string searchPattern = "";
            for(int i = 0; i < extensions.Count; i++)
            {
                searchPattern += "*." + extensions[i];
                if(i < extensions.Count - 1)
                {
                    searchPattern += "|";
                }
            }
            string[] files = new string[0];
            if (searchPattern.Length > 0) files = Directory.GetFiles(currentDirectory, searchPattern, SearchOption.TopDirectoryOnly);
            else files = Directory.GetFiles(currentDirectory);
            foreach (string file in files)
            {
                string shortednedFile = OxHelpers.GetLastPartInAbsolutePath(file);
                OxButton fileButton = new OxButton(shortednedFile);
                AddItems(fileButton);
            }
        }
        private void AddDrives()
        {
            string[] drives = Directory.GetLogicalDrives();
            foreach (string drive in drives)
            {
                OxButton driveButton = new OxButton(OxHelpers.PathConvention(drive));
                driveButton.clicked += DirectoryButton_clicked;
                AddItems(driveButton);
            }
        }

        /// <summary>
        /// This method lets you make an item in a directory
        /// be selected.
        /// </summary>
        /// <param name="selection">The absolute path of the item with the extension if directorySelection is false</param>
        public void SetSelection(string selection)
        {
            if (selection != null && selection.Length > 0)
            {
                string directory = selection.Replace("\\", "/");
                string itemName = (directory.LastIndexOf("/") == directory.Length - 1) ? directory.Substring(0, directory.Length - 1) : directory;
                itemName = (itemName.LastIndexOf("/") > -1) ? itemName.Substring(itemName.LastIndexOf("/") + 1) : itemName;
                directory = (directory.LastIndexOf("/") == directory.Length - 1) ? directory.Substring(0, directory.Length - 1) : directory;
                directory = (directory.LastIndexOf("/") > -1) ? directory.Substring(0, directory.LastIndexOf("/")) : "";
                if (directorySelection) itemName += "/";

                currentDirectory = directory;
                PopulateList();
                previousDirectory = currentDirectory;
                firstTimeLoad = false;
                foreach (OxBase item in items)
                {
                    if (item.text.Equals(itemName, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        selectedItems.Add(item);
                        if (enableItemSelection) item.Select(true);
                        FireSelectionChangedEvent(item, true);
                        break;
                    }
                }
            }
        }

        private void BackButton_clicked(OxBase obj)
        {
            if (!dragging)
            {
                currentDirectory = OxHelpers.ParentPath(currentDirectory);
                if (savedScroll.Count > 0) { scrollProgress = savedScroll[savedScroll.Count - 1]; savedScroll.RemoveAt(savedScroll.Count - 1); }
                else scrollProgress = 0;
            }
        }
        private void DirectoryButton_clicked(OxBase obj)
        {
            if (!dragging && (!directorySelection || obj.isSelected))
            {
                string nextDirectory = currentDirectory + obj.text;
                if (OxHelpers.CanBrowseDirectory(nextDirectory)) currentDirectory = nextDirectory;
                savedScroll.Add(scrollProgress);
                scrollProgress = 0;
            }
        }

        #region Extensions
        /// <summary>
        /// Adds an extension to the extensions list. When files are listed
        /// only the ones which have the extensions listed will show on the
        /// list.
        /// </summary>
        /// <param name="extension">The file extension. If you want to add the txt extension, just put "txt"</param>
        public void AddExtensions(params string[] extension)
        {
            extensions.AddRange(extension);
        }
        public void RemoveExtensions(params string[] extension)
        {
            foreach(string ext in extension)
            {
                extensions.Remove(ext);
            }
        }
        public string[] GetExtensions()
        {
            return extensions.ToArray();
        }
        #endregion
    }
}