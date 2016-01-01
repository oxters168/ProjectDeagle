using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public abstract class OxListable : OxGUI
{
    public List<OxGUI> items = new List<OxGUI>();
    public int selectedIndex = -1;
    public event IndexChangedEventHandler indexChanged;

    public OxListable(Vector2 position, Vector2 size) : base(position, size)
    {
        selectable = false;
        //horizontal = horz;
    }

    public delegate void IndexChangedEventHandler(int itemIndex);

    public OxGUI GetItem(int index)
    {
        if (index >= 0 && index < items.Count) return items[index];

        return null;
    }
    public int SelectedIndex()
    {
        return selectedIndex;
    }
    public int Count()
    {
        return items.Count;
    }
    public void AddItem(params OxGUI[] newItems)
    {
        foreach (OxGUI item in newItems)
        {
            item.clicked += item_clicked;
            items.Add(item);
        }
    }

    protected void item_clicked(OxGUI sender)
    {
        selectedIndex = items.IndexOf(sender);
        if (indexChanged != null) { indexChanged(selectedIndex); }
    }
    public bool RemoveItem(OxGUI item)
    {
        if (selectedIndex == items.IndexOf(item))
        {
            //selectedIndex = -1;
            Deselect();
        }
        return items.Remove(item);
    }
    public void Clear()
    {
        Deselect();
        items.Clear();
    }

    private bool showFiles = false;
    private string extFilter = null;
    public void FillBrowserList(string currentDir, bool includeFiles)
    {
        FillBrowserList(currentDir, includeFiles, null);
    }
    public void FillBrowserList(string currentDir, bool includeFiles, string extension)
    {
        text = currentDir.Replace("\\", "/");
        string directoryView = text;
        if (directoryView.LastIndexOf(".") > directoryView.LastIndexOf("/")) directoryView = directoryView.Substring(0, directoryView.LastIndexOf("/"));
        if (!Directory.Exists(directoryView))
        {
            //if(Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            text = Directory.GetCurrentDirectory();
            directoryView = text;
        }
        showFiles = includeFiles;
        extFilter = extension;

        Clear();
        DirectoryInfo parent = null;
        try { parent = Directory.GetParent(directoryView); } catch(Exception) { }
        if (parent != null)
        {
            OxButton dirButton = new OxButton("..", "MenuButton");
            dirButton.clicked += BrowserButtonClicked;
            AddItem(dirButton);
        }
        foreach (string dir in Directory.GetDirectories(directoryView))
        {
            string relativeDir = dir;
            relativeDir = relativeDir.Replace("\\", "/");
            if (relativeDir.LastIndexOf("/") > -1) relativeDir = relativeDir.Substring(relativeDir.LastIndexOf("/") + 1);
            OxButton dirButton = new OxButton(relativeDir, "MenuButton");
            dirButton.clicked += BrowserButtonClicked;
            AddItem(dirButton);
        }

        if (includeFiles)
        {
            string[] files = null;
            if(extension != null && extension.Length > 0) files = Directory.GetFiles(directoryView, "*." + extension);
            else files = Directory.GetFiles(directoryView);

            foreach (string file in files)
            {
                string relativeFile = file;
                relativeFile = relativeFile.Replace("\\", "/");
                if (relativeFile.LastIndexOf("/") > -1) relativeFile = relativeFile.Substring(relativeFile.LastIndexOf("/") + 1);
                OxButton fileButton = new OxButton(relativeFile, "MenuButton");
                fileButton.clicked += BrowserButtonClicked;
                AddItem(fileButton);
            }
        }
    }
    void BrowserButtonClicked(OxGUI sender)
    {
        string currentDir = text;
        if (currentDir.LastIndexOf(".") > currentDir.LastIndexOf("/")) currentDir = currentDir.Substring(0, currentDir.LastIndexOf("/"));

        if (sender.text.Equals("..", StringComparison.InvariantCultureIgnoreCase))
        {
            DirectoryInfo parentDir = Directory.GetParent(currentDir);
            if (parentDir != null) currentDir = parentDir.FullName;
        }
        else
        {
            if(currentDir.LastIndexOf("/") != currentDir.Length - 1) currentDir += "/";
            currentDir += sender.text;
        }

        FillBrowserList(currentDir, showFiles, extFilter);
    }

    public void SelectNextItem()
    {
        SetIndex(selectedIndex + 1);
    }
    public void SelectPreviousItem()
    {
        if (selectedIndex > 0) SetIndex(selectedIndex - 1);
    }
    public void SetIndex(int index)
    {
        if (index >= -1 && index < items.Count)
        {
            selectedIndex = index;
            if (indexChanged != null) indexChanged(selectedIndex);
            for (int i = 0; i < items.Count; i++)
            {
                if (i == selectedIndex) items[i].highlighted = true;
                else items[i].highlighted = false;
            }
        }
    }
    public void Deselect()
    {
        selectedIndex = -1;
        if (indexChanged != null) indexChanged(selectedIndex);
    }
}
