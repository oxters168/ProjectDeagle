using UnityEngine;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityHelpers;
using UnitySourceEngine;

public class MapData
{
    private static Dictionary<string, MapData> cachedMaps = new Dictionary<string, MapData>();

    public string mapName { get; private set; }

    public static readonly MapTypeData[] MAP_TYPES = new MapTypeData[]
    {
        new MapTypeData("cs", "Hostage Rescue"),
        new MapTypeData("de", "Bomb Defusal"),
        new MapTypeData("as", "Assasination"),
        new MapTypeData("es", "Terrorist Escape"),
        new MapTypeData("tr", "Training Map"),
        new MapTypeData("ar", "Arms Race"),
        new MapTypeData("dz", "Danger Zone"),
        new MapTypeData("gd", "Guardian Scenario"),
        new MapTypeData("coop", "Co-op"),
        new MapTypeData("aim", "Aim Arena"),
        new MapTypeData("awp", "AWP Arena"),
        new MapTypeData("bhop", "Bunny Hop Map"),
        new MapTypeData("cp", "Control Points"),
        new MapTypeData("dm", "Deathmatch Arena"),
        new MapTypeData("dr", "Dead Run"),
        new MapTypeData("fy", "Frag Yard"),
        new MapTypeData("fun", "Fun Map"),
        new MapTypeData("mg", "Fun Map"),
        new MapTypeData("gg", "Gun Game"),
        new MapTypeData("he", "Grenade Arena"),
        new MapTypeData("nade", "Grenade Arena"),
        new MapTypeData("hg", "Hunger Game"),
        new MapTypeData("ka", "Knife Arena"),
        new MapTypeData("kz", "Kreedz Climbing"),
        new MapTypeData("pa", "Prepared Assault"),
        new MapTypeData("surf", "Surf Map"),
        new MapTypeData("texture", "Dev"),
        new MapTypeData("ze", "Zombie Escape"),
        new MapTypeData("zm", "Zombie Mod")
    };

    private BSPMap map;
    private Sprite preview;
    private OverviewData overview;

    public static bool ramCombine;
    private bool combine;

    //public string status;

    public bool IsBuilt { get { return map != null && map.isBuilt; } }
    public bool IsBuilding { get { return map != null && (map.isParsing || map.isBuilding); } }
    public bool IsDownloading3D { get { return TaskMaker.IsMainTask(download3DTask); } }
    public bool IsDownloading2D { get { return TaskMaker.IsMainTask(download2DTask); } }
    public bool IsDownloadingPreview { get { return TaskMaker.IsMainTask(downloadPreviewTask); } }
    public bool IsReadingDependencies { get { return TaskMaker.IsMainTask(readDependenciesTask); } }
    public bool IsDownloadingDependencies { get { return TaskMaker.IsMainTask(downloadDependenciesTask); } }
    public bool IsLoading3D { get { return TaskMaker.HasChainedTask(map3DChainTask); } }
    public bool IsLoading2D { get { return TaskMaker.HasChainedTask(map2DChainTask); } }

    private ChainedTask map3DChainTask, map2DChainTask;
    private TaskWrapper downloadPreviewTask, download2DTask, download3DTask, readDependenciesTask, downloadDependenciesTask, loadMapTask;

    public string[] dependenciesList { get; private set; }

    public static MapData loadedMap { get; private set; }

    private MapData(string _mapName)
    {
        mapName = _mapName;
        Application.lowMemory += Application_lowMemory;
    }

    private void Application_lowMemory()
    {
        if (IsLoading3D)
        {
            CancelLoading3D();
            SteamController.ShowErrorPopup("Loading Error", "Not enough ram to load this map. You can try tweaking the settings to maybe get it working. It would be best to restart the application to make sure the ram clears out properly.");
        }
    }

    public string GetStatus3D()
    {
        string status;

        if (map3DChainTask != null && map3DChainTask.cancelled)
            status = "Cancelling";
        else if (IsDownloading3D)
            status = "Downloading Map";
        else if (IsReadingDependencies)
            status = "Reading Dependencies";
        else if (IsDownloadingDependencies)
            status = "Downloading Dependencies";
        else if (IsBuilding)
            status = map.currentMessage;
        else
            status = "Waiting to Start";

        return status;
    }
    public string GetStatus2D()
    {
        string status;
        if (map2DChainTask != null && map2DChainTask.cancelled)
            status = "Cancelling";
        else if (IsDownloading2D)
            status = "Downloading Overview";
        else
            status = "Waiting to Start";

        return status;
    }

    public static MapData FindOrCreateMap(string mapName)
    {
        MapData requestedMap;
        if (!cachedMaps.ContainsKey(mapName))
        {
            requestedMap = new MapData(mapName);
            cachedMaps[mapName] = requestedMap;
        }
        else
            requestedMap = cachedMaps[mapName];

        return requestedMap;
    }
    public bool IsSameMap(string otherName)
    {
        return GetMapName().Equals(GetDerivedName(otherName), StringComparison.OrdinalIgnoreCase);
    }

    public static string GetMapPath(string mapName)
    {
        return Path.Combine(SettingsController.GetMapsLoc(), mapName + ".bsp");
    }
    public static string GetPreviewPath(string mapName)
    {
        return Path.Combine(SettingsController.GetMapsLoc(), mapName + ".jpg");
    }

    public static MapTypeData TypeFromPrefix(string prefix)
    {
        MapTypeData mapType = default;
        for (int i = 0; i < MAP_TYPES.Length; i++)
        {
            if (prefix.Equals(MAP_TYPES[i].prefix, StringComparison.OrdinalIgnoreCase))
            {
                mapType = MAP_TYPES[i];
                break;
            }
        }
        return mapType;
    }

    public OverviewData GetOverviewData()
    {
        if (overview == null && IsOverviewAvailable())
            overview = new OverviewData(mapName);
        return overview;
    }
    public Sprite GetPreview()
    {
        if (preview == null && IsPreviewAvailable())
        {
            Texture2D previewTexture = LoadMapPreview(mapName);
            preview = previewTexture?.ToSprite();
        }
        return preview;
    }
    public bool IsMapAvailable()
    {
        return File.Exists(GetMapPath(mapName));
    }
    public bool IsOverviewAvailable()
    {
        return OverviewData.IsOverviewAvailable(mapName);
    }
    public bool IsPreviewAvailable()
    {
        return File.Exists(GetPreviewPath(mapName));
    }
    public float GetPercentBuilt()
    {
        return map != null ? map.PercentLoaded : 0;
    }
    /*public bool IsMainTask()
    {
        return TaskMaker.IsMainTask(downloadPreviewTask) || TaskMaker.IsMainTask(download2DTask) || TaskMaker.IsMainTask(download3DTask) || TaskMaker.IsMainTask(readDependenciesTask) || TaskMaker.IsMainTask(downloadDependenciesTask) || TaskMaker.IsMainTask(loadMapTask);
    }*/

    public static string GetDerivedName(string mapName)
    {
        string derivedName = mapName;
        int underscoreIndex = derivedName.IndexOf("_");
        if (underscoreIndex > -1)
        {
            derivedName = derivedName.Substring(underscoreIndex + 1);
        }
        return derivedName;
    }
    public string GetMapName()
    {
        return GetDerivedName(mapName);
    }
    public static string GetPrefix(string mapName)
    {
        string mapPrefix = string.Empty;
        int underscoreIndex = mapName.IndexOf("_");
        if (underscoreIndex > -1)
            mapPrefix = mapName.Substring(0, underscoreIndex);
        return mapPrefix;
    }
    public MapTypeData GetMapType()
    {
        MapTypeData mapType = default;
        string mapPrefix = GetPrefix(mapName);
        if (!string.IsNullOrEmpty(mapPrefix))
            mapType = TypeFromPrefix(mapPrefix);
        return mapType;
    }

    public GameObject GetGameObject()
    {
        return map.gameObject;
    }
    private bool IsReadyToMake()
    {
        return map != null && map.isParsed && !map.isBuilding && !map.isBuilt;
    }
    private void CreateMapObjectIfNotExist()
    {
        if (map == null)
        {
            string fullMapPath = GetMapPath(mapName);

            if (!string.IsNullOrEmpty(fullMapPath) && File.Exists(fullMapPath))
                map = new BSPMap(fullMapPath);
        }
    }

    /*public void Download3DMapPrompt(Action<bool, ChainedTask> onDecisionMade = null)
    {
        SteamController.ShowPromptPopup(mapName, "Would you like to download the map dependencies as well (textures & models, usually more than 1GB)?", (response) =>
        {
            var task = TaskMaker.DownloadMap3D(this, response, response, true);
            onDecisionMade?.Invoke(response, task);
        }, "Yes", "No");
    }*/

    public void LoadMap2D(bool showMap, bool showControls)
    {
        if (!IsOverviewAvailable())
        {
            if (SteamController.steamInScene.IsLoggedIn)
                map2DChainTask = TaskMaker.DownloadMap2D(this, showMap, showControls);
            else
                SteamController.ShowErrorPopup("Download Error", "You must be logged in to download a map");
        }
        else
        {
            map2DChainTask = TaskMaker.InvokeMapReady2D(this, showMap, showControls);
        }
    }
    public void LoadMap3D(bool showMap, bool showControls)
    {
        if (!IsMapAvailable())
        {
            if (SteamController.steamInScene.IsLoggedIn)
                map3DChainTask = TaskMaker.DownloadMap3D(this, SettingsController.autoResourcePerMap, SettingsController.autoResourcePerMap, true, showMap, showControls);
            else
                SteamController.ShowErrorPopup("Download Error", "You must be logged in to download a map");
        }
        else if (IsBuilt)
        {
            map3DChainTask = TaskMaker.InvokeMapReady3D(this, showMap, showControls);
        }
        else
        {
            map3DChainTask = TaskMaker.LoadMap(this, showMap, showControls);
        }
    }
    public void CancelLoading2D()
    {
        map2DChainTask.Cancel();
    }
    public void CancelLoading3D()
    {
        map3DChainTask.Cancel();
        UnloadMap();
    }
    /*public void CancelDownloadPreview()
    {
        if (downloadPreviewTask != null)
            TaskMaker.CancelTask(downloadPreviewTask);
    }
    public void CancelDownloadOverview()
    {
        if (download2DTask != null)
            TaskMaker.CancelTask(download2DTask);
    }
    public void CancelLoading3D()
    {
        if (download3DTask != null)
            TaskMaker.CancelTask(download3DTask);
        if (readDependenciesTask != null)
            TaskMaker.CancelTask(readDependenciesTask);
        if (downloadDependenciesTask != null)
            TaskMaker.CancelTask(downloadDependenciesTask);
        if (loadMapTask != null)
            TaskMaker.CancelTask(loadMapTask);
    }*/

    public TaskWrapper GetDownloadPreviewTask(Action onPreviewDownloaded = null)
    {
        if (downloadPreviewTask == null)
        {
            if (SteamController.steamInScene.ManifestHasPreview(mapName))
            {
                downloadPreviewTask = SteamController.steamInScene.GenerateMapPreviewDownloadTask(mapName, () =>
                {
                    //status = "Downloading Preview";
                }, () =>
                {
                    onPreviewDownloaded?.Invoke();
                });
            }
        }
        return downloadPreviewTask;
    }
    public TaskWrapper GetDownloadOverviewTask(Action onOverviewDownloaded = null)
    {
        if (download2DTask == null)
        {
            download2DTask = SteamController.steamInScene.GenerateMapOverviewDownloadTask(mapName, () =>
            {
                SteamController.LogToConsole("Attempting to download map overview files of " + mapName);
                //status = "Downloading Overview Files";
            }, () =>
            {
                onOverviewDownloaded?.Invoke();
            });
        }
        return download2DTask;
    }
    public TaskWrapper GetDownloadMap3DTask(Action onMapDownloaded = null)
    {
        if (download3DTask == null)
        {
            download3DTask = SteamController.steamInScene.GenerateDownloadMapTask(mapName, () =>
            {
                //status = "Downloading Map";
                SteamController.LogToConsole("Attempting to download map file of " + mapName);
            }, () =>
            {
                onMapDownloaded?.Invoke();
            });
        }

        return download3DTask;
    }
    public TaskWrapper GetReadDependenciesTask(Action onDependenciesRead = null)
    {
        if (readDependenciesTask == null)
        {
            readDependenciesTask = TaskManagerController.CreateTask("Reading " + mapName + " dependencies", (cancelToken) =>
            {
                CancellableReadDependencies(cancelToken, onDependenciesRead);
            });
        }
        return readDependenciesTask;
    }
    public TaskWrapper GetDownloadDependenciesTask(Action onDependenciesDownloaded = null)
    {
        if (downloadDependenciesTask == null)
        {
            downloadDependenciesTask = SteamController.steamInScene.GenerateDownloadVPKTask(this, () =>
            {
                //status = "Downloading Dependencies";
                SteamController.LogToConsole("Attempting to download dependencies of " + mapName);
            }, () =>
            {
                onDependenciesDownloaded?.Invoke();
            });
        }
        return downloadDependenciesTask;
    }
    public TaskWrapper GetStartLoadMapTask(Action onCompleted = null)
    {
        if (loadMapTask == null)
        {
            loadMapTask = TaskManagerController.CreateTask("Loading " + mapName, (cancelToken) =>
            {
                CancellableStartLoadMap(cancelToken, onCompleted);
            });
        }
        return loadMapTask;
    }

    private void CancellableReadDependencies(CancellationToken cancelToken, Action onDependenciesRead = null)
    {
        //status = "Reading Dependencies";
        CreateMapObjectIfNotExist();

        List<string> dependencies = map?.GetDependencies(cancelToken);
        if (!cancelToken.IsCancellationRequested && dependencies != null && dependencies.Count > 0)
        {
            string dependencyList = mapName + " dependencies:\n";
            foreach (string dependency in dependencies)
                dependencyList += dependency + "\n";
            SteamController.LogToConsole(dependencyList);

            dependenciesList = dependencies.ToArray();
        }

        onDependenciesRead?.Invoke();
    }
    private void CancellableStartLoadMap(CancellationToken cancelToken, Action onCompleted = null)
    {
        loadedMap?.UnloadMap();
        loadedMap = this;

        //status = "Loading Map";

        CreateMapObjectIfNotExist();

        if (map != null)
        {
            SteamController.LogToConsole("\nLoading " + map.mapName);
            try
            {
                map.ParseFile(cancelToken, null, null);
            }
            catch(Exception e)
            {
                Debug.LogError("MapData: Error while loading map " + e.ToString());
            }

            if (!cancelToken.IsCancellationRequested)
            {
                MakeGameObject(onCompleted);
            }
            else
                UnloadMap();
        }
    }

    public Bounds GetBounds()
    {
        Bounds mapBounds;
        if (combine)
            mapBounds = MapLoaderController.mapLoaderInScene.combiner.transform.GetTotalBounds();
        else
            mapBounds = map.gameObject.transform.GetTotalBounds();

        return mapBounds;
    }
    private void MakeGameObject(Action onCompleted = null)
    {
        combine = ramCombine;
        TaskManagerController.RunAction(() =>
        {
            //status = "Building Map";
            SteamController.LogToConsole("Building " + map.mapName);
            map.MakeGameObject(null, (go) =>
            {
                if (combine)
                {
                    MapLoaderController.mapLoaderInScene.combiner.searchOptions.parent = go;
                    MapLoaderController.mapLoaderInScene.combiner.CombineAll();
                }
                else
                    go.SetActive(true);

                onCompleted?.Invoke();
            });
            SteamController.LogToConsole("Finished building " + map.mapName);
        });
    }
    public void UnloadMap()
    {
        TaskManagerController.RunAction(() =>
        {
            map?.Unload();
            map = null;
            if (loadedMap == this)
                loadedMap = null;
            MapLoaderController.mapLoaderInScene.combiner.DestroyCombinedObjects();
            GC.Collect();
        });
    }

    public static Texture2D LoadMapPreview(string mapName)
    {
        Texture2D loadedPreview = null;
        if (!string.IsNullOrEmpty(mapName))
        {
            string previewPath = GetPreviewPath(mapName);
            if (File.Exists(previewPath))
            {
                Texture2D loadedFromFileSystem = new Texture2D(2, 2);
                loadedFromFileSystem.LoadImage(File.ReadAllBytes(previewPath));
                loadedPreview = loadedFromFileSystem;
            }
        }

        return loadedPreview;
    }

    public override bool Equals(object obj)
    {
        return obj != null && (obj is MapData) && ((MapData)obj).mapName.Equals(mapName, StringComparison.OrdinalIgnoreCase);
    }
    public override int GetHashCode()
    {
        return -1030726950 + EqualityComparer<string>.Default.GetHashCode(mapName);
    }
}

public struct MapTypeData
{
    public string prefix { get; private set; }
    public string name { get; private set; }

    public MapTypeData(string _prefix, string _typeName)
    {
        prefix = _prefix;
        name = _typeName;
    }
}
public class OverviewData
{
    public string mapName { get; private set; }

    public readonly Dictionary<Values, float> values = new Dictionary<Values, float>();
    private Dictionary<RadarLevel, Sprite> radars = new Dictionary<RadarLevel, Sprite>();
    private Dictionary<RadarLevel, VerticalSection> verticalsections = new Dictionary<RadarLevel, VerticalSection>();

    public enum Values { pos_x, pos_y, scale, rotate, zoom, CTSpawn_x, CTSpawn_y, TSpawn_x, TSpawn_y, bomb_x, bomb_y, bombA_x, bombA_y, bombB_x, bombB_y, Hostage1_x, Hostage1_y, Hostage2_x, Hostage2_y, Hostage3_x, Hostage3_y, Hostage4_x, Hostage4_y, Hostage5_x, Hostage5_y, Hostage6_x, Hostage6_y }
    public enum RadarLevel { main, lower, higher, }
    private static readonly string[] RadarNames = new string[] { "_radar", "_lower_radar", "_higher_radar" };

    private Regex quotationCatcher = new Regex("(?<=\")([A-Za-z0-9\\-_\\.])+(?=\")");
    private Regex verticalSectionsMatcher = new Regex("([^/])(\")([A-Za-z0-9\\-_\\.]*)(\")([A-Za-z0-9/\\.\\-_\\s]*)$(\\s*){([A-Za-z0-9\\.\\-_\\s\"]*)(\\s*)([/A-Za-z0-9\\.\\-_\"]*)(\\s*)$([A-Za-z0-9\\.\\-_\\s\"]*)(\\s*)([/A-Za-z0-9\\.\\-_\"]*)(\\s*)$(\\s*)}", RegexOptions.Multiline);

    public OverviewData(string _mapName)
    {
        mapName = _mapName;
        ReadData(GetData(mapName));

        Texture2D currentTexture = LoadMapOverview(RadarLevel.main);
        radars[RadarLevel.main] = currentTexture?.ToSprite();

        currentTexture = LoadMapOverview(RadarLevel.lower);
        radars[RadarLevel.lower] = currentTexture?.ToSprite();

        currentTexture = LoadMapOverview(RadarLevel.higher);
        radars[RadarLevel.higher] = currentTexture?.ToSprite();
    }

    public static bool IsOverviewAvailable(string mapName)
    {
        return File.Exists(GetOverviewDataPath(mapName)) && File.Exists(GetOverviewTexturePath(mapName, RadarLevel.main));
    }
    public static string GetOverviewDataPath(string mapName)
    {
        return Path.Combine(SettingsController.GetOverviewLoc(), mapName + ".txt");
    }
    public static string GetOverviewTexturePath(string mapName, RadarLevel radarLevel)
    {
        return Path.Combine(SettingsController.GetOverviewLoc(), GetOverviewTextureFileName(mapName, radarLevel));
    }
    public static string GetOverviewTextureFileName(string mapName, RadarLevel radarLevel)
    {
        string overviewName = mapName + RadarNames[(int)radarLevel];
        return overviewName + ".dds";
    }

    public Sprite GetRadar()
    {
        Sprite returnedRadar = null;
        if (radars[RadarLevel.main] != null)
            returnedRadar = radars[RadarLevel.main];
        else if (radars[RadarLevel.lower] != null)
            returnedRadar = radars[RadarLevel.lower];
        else if (radars[RadarLevel.higher] != null)
            returnedRadar = radars[RadarLevel.higher];

        return returnedRadar;
    }
    public Sprite GetRadar(float height)
    {
        Sprite returnedRadar = GetRadar();
        foreach (var verticalSectionPair in verticalsections)
            if (height >= verticalSectionPair.Value.AltitudeMin && height <= verticalSectionPair.Value.AltitudeMax)
                returnedRadar = radars[verticalSectionPair.Key];

        return returnedRadar;
    }

    private string GetData(string mapName)
    {
        string text = string.Empty;
        if (!string.IsNullOrEmpty(mapName))
        {
            string textPath = GetOverviewDataPath(mapName);
            if (File.Exists(textPath))
                text = File.ReadAllText(textPath);
        }
        return text;
    }
    private void ReadData(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            #region VerticalSections
            MatchCollection sectionsCaught = verticalSectionsMatcher.Matches(text);
            for (int i = 0; i < sectionsCaught.Count; i++)
            {
                MatchCollection valuesCaught = quotationCatcher.Matches(sectionsCaught[i].Value);
                if (valuesCaught.Count == 5)
                {
                    string sectionName = valuesCaught[0].Value;
                    if (sectionName.Equals("default"))
                        sectionName = nameof(RadarLevel.main);
                    int radarLevelIndex = Array.IndexOf(Enum.GetNames(typeof(RadarLevel)), sectionName);

                    if (radarLevelIndex >= 0)
                    {
                        VerticalSection section = new VerticalSection();
                        try
                        {
                            section.AltitudeMax = Convert.ToSingle(valuesCaught[2].Value);
                            section.AltitudeMin = Convert.ToSingle(valuesCaught[4].Value);
                        }
                        catch (Exception) { }
                        verticalsections[(RadarLevel)radarLevelIndex] = section;
                    }
                }
            }
            #endregion

            #region Other Values
            string[] lines = text.Split('\n');
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    string nonComment = line.Split(new string[] { "//" }, StringSplitOptions.None)[0];

                    MatchCollection matched = quotationCatcher.Matches(nonComment);
                    if (matched.Count == 2)
                    {
                        string valueName = matched[0].Value;
                        int valueIndex = Array.IndexOf(Enum.GetNames(typeof(Values)), valueName);
                        //Debug.Log(nonComment + " | Matched" + valueIndex + ": " + valueName + " " + matched[1]);
                        if (valueIndex > -1)
                        {
                            float value = 0;
                            try
                            {
                                value = Convert.ToSingle(matched[1].Value);
                            }
                            catch (Exception) { }
                            values[(Values)valueIndex] = value;
                        }
                    }
                }
            }
            #endregion
        }
    }
    private Texture2D LoadMapOverview(RadarLevel radarLevel = RadarLevel.main)
    {
        Texture2D loadedOverview = null;
        if (!string.IsNullOrEmpty(mapName))
        {
            string overviewPath = GetOverviewTexturePath(mapName, radarLevel);
            if (File.Exists(overviewPath))
                loadedOverview = DDSLoader.LoadDDSFile(File.ReadAllBytes(overviewPath));
        }
        return loadedOverview;
    }

    public struct VerticalSection
    {
        public float AltitudeMax, AltitudeMin;
    }
}
