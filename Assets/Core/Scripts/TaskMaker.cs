using UnityEngine;
using UnityHelpers;
using System.Collections.Generic;
using System;
using System.Linq;

public class TaskMaker : MonoBehaviour
{
    public static TaskMaker taskMakerInScene;
    private static ChainedTask currentTask;
    private static Queue<ChainedTask> queuedTasks = new Queue<ChainedTask>();

    public static event MapActionDelegate mapDownloaded3D, mapDependenciesRead, mapDependenciesDownloaded;
    public delegate void MapActionDelegate(MapData map);
    public static event MapLoadedDelegate mapReady3D, mapReady2D;
    public delegate void MapLoadedDelegate(MapData map, bool showMap, bool showReplayControls);

    private void OnEnable()
    {
        taskMakerInScene = this;
    }
    private void Update()
    {
        if (!IsBusy())
        {
            currentTask?.PopCurrentTask();
            RunNextTask();
        }
    }

    public static bool IsBusy()
    {
        var currentTaskWrapper = currentTask?.GetCurrentTask();
        return currentTaskWrapper != null && !currentTask.cancelled && TaskManagerController.HasTask(currentTaskWrapper);
    }

    private static void QueueTask(ChainedTask task)
    {
        if (!queuedTasks.Contains(task))
            queuedTasks.Enqueue(task);
    }
    private static void RunNextTask()
    {
        var nextTaskWrapper = currentTask?.GetCurrentTask();
        if (nextTaskWrapper == null || currentTask.cancelled)
        {
            currentTask = null;
            nextTaskWrapper = null;
            if (queuedTasks.Count > 0)
            {
                currentTask = queuedTasks.Dequeue();
                nextTaskWrapper = currentTask?.GetCurrentTask();
            }
        }

        if (nextTaskWrapper != null)
            TaskManagerController.QueueTask(nextTaskWrapper);
    }

    public static ChainedTask DownloadManifest(Action onManifestDownloaded, ChainedTask chain = null)
    {
        TaskWrapper task = SteamController.steamInScene.GetDownloadManifestTask(onManifestDownloaded);

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        QueueTask(chain);

        return chain;
    }
    public static ChainedTask DownloadPreview(MapData map, Action onPreviewDownloaded, ChainedTask chain = null)
    {
        TaskWrapper task = null;
        task = map.GetDownloadPreviewTask(() =>
        {
            if (task != null && !task.cancelled && !chain.cancelled)
            {
                onPreviewDownloaded?.Invoke();
            }
        });

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        QueueTask(chain);

        return chain;
    }

    public static ChainedTask DownloadMatch(MatchInfo match, bool load2DMap, bool load3DMap, bool showMap, bool showControls, ChainedTask chain = null)
    {
        if (chain == null)
            chain = new ChainedTask();

        TaskWrapper task = match.GetDownloadTask((success) =>
        {
            if (!success)
                chain.Cancel();
        });

        chain.AddTask(task);

        GenerateMatchInfo(match, load2DMap, load3DMap, showMap, showControls, chain);

        QueueTask(chain);

        return chain;
    }
    public static ChainedTask GenerateMatchInfo(MatchInfo match, bool load2DMap, bool load3DMap, bool showMap, bool showControls, ChainedTask chain = null)
    {
        TaskWrapper task = match.GetMakeMatchInfoTask();

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        GetMapLoadTask(match, load2DMap, load3DMap, showMap, showControls, chain);

        QueueTask(chain);

        return chain;
    }
    private static ChainedTask GetMapLoadTask(MatchInfo match, bool load2DMap, bool load3DMap, bool showMap, bool showControls, ChainedTask chain = null)
    {
        if (chain == null)
            chain = new ChainedTask();

        string mapName = match.GetMapName();
        Debug.Assert(!string.IsNullOrEmpty(mapName), "TaskMaker: Could not determine map name from match");
        if (!string.IsNullOrEmpty(mapName))
        {
            MapData map = MapData.FindOrCreateMap(mapName);
            if (load2DMap && !map.IsLoading2D)
            {
                if (!map.IsOverviewAvailable())
                {
                    DownloadMap2D(map, showMap, showControls, chain);
                }
                else
                {
                    InvokeMapReady2D(map, showMap, showControls, chain);
                }
            }
            else if (load3DMap && !map.IsLoading3D)
            {
                if (!map.IsMapAvailable())
                {
                    DownloadMap3D(map, SettingsController.autoResourcePerMap, SettingsController.autoResourcePerMap, true, showMap, showControls, chain);
                }
                else if (!map.IsBuilt)
                {
                    LoadMap(map, showMap, showControls, chain);
                }
                else
                {
                    InvokeMapReady3D(map, showMap, showControls, chain);
                }
            }
        }

        QueueTask(chain);

        return chain;
    }

    public static ChainedTask DownloadMap2D(MapData map, bool showMap, bool showControls, ChainedTask chain = null)
    {
        TaskWrapper task = map.GetDownloadOverviewTask();

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        InvokeMapReady2D(map, showMap, showControls, chain);

        QueueTask(chain);

        return chain;
    }
    public static ChainedTask InvokeMapReady2D(MapData map, bool showMap, bool showControls, ChainedTask chain = null)
    {
        TaskWrapper task = TaskManagerController.CreateTask((cts) =>
        {
            if (!chain.cancelled)
                mapReady2D?.Invoke(map, showMap, showControls);
        });

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        QueueTask(chain);

        return chain;
    }

    public static ChainedTask DownloadMap3D(MapData map, bool readDependencies = false, bool downloadDependencies = false, bool loadMap = false, bool showMap = false, bool showControls = false, ChainedTask chain = null)
    {
        TaskWrapper task = null;
        task = map.GetDownloadMap3DTask(() =>
        {
            if (task != null && !task.cancelled && !chain.cancelled)
            {
                mapDownloaded3D?.Invoke(map);
            }
        });

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        if (readDependencies)
            ReadMapDependencies(map, downloadDependencies, loadMap, showMap, showControls, chain);
        else if (downloadDependencies)
            DownloadMapDependencies(map, loadMap, showMap, showControls, chain);
        else
            LoadMap(map, showMap, showControls, chain);

        QueueTask(chain);

        return chain;
    }
    public static ChainedTask ReadMapDependencies(MapData map, bool downloadDependencies = false, bool loadMap = false, bool showMap = false, bool showControls = false, ChainedTask chain = null)
    {
        TaskWrapper task = null;
        task = map.GetReadDependenciesTask(() =>
        {
            if (task != null && !task.cancelled && !chain.cancelled)
            {
                mapDependenciesRead?.Invoke(map);
            }
        });

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        if (downloadDependencies)
            DownloadMapDependencies(map, loadMap, showMap, showControls, chain);
        else if (loadMap)
            LoadMap(map, showMap, showControls, chain);

        QueueTask(chain);

        return chain;
    }
    public static ChainedTask DownloadMapDependencies(MapData map, bool loadMap = false, bool showMap = false, bool showControls = false, ChainedTask chain = null)
    {
        TaskWrapper task = null;
        task = map.GetDownloadDependenciesTask(() =>
        {
            if (task != null && !task.cancelled && !chain.cancelled)
            {
                mapDependenciesDownloaded?.Invoke(map);
            }
        });

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        if (loadMap)
            LoadMap(map, showMap, showControls, chain);

        QueueTask(chain);

        return chain;
    }
    public static ChainedTask DownloadFromSteam(string[] files, ChainedTask chain = null)
    {
        TaskWrapper task = SteamController.steamInScene.DownloadFromSteam(files);

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        QueueTask(chain);

        return chain;
    }
    public static ChainedTask LoadMap(MapData map, bool showMap, bool showControls, ChainedTask chain = null)
    {
        TaskWrapper task = map.GetStartLoadMapTask();

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        InvokeMapReady3D(map, showMap, showControls, chain);

        QueueTask(chain);

        return chain;
    }
    public static ChainedTask InvokeMapReady3D(MapData map, bool showMap, bool showControls, ChainedTask chain = null)
    {
        TaskWrapper task = TaskManagerController.CreateTask((cts) =>
        {
            if (!chain.cancelled)
                mapReady3D?.Invoke(map, showMap, showControls);
        });

        if (chain == null)
            chain = new ChainedTask(task);
        else
            chain.AddTask(task);

        QueueTask(chain);

        return chain;
    }

    public static bool IsMainTask(TaskWrapper task)
    {
        return task != null && currentTask?.GetCurrentTask() == task;
    }
    /*public static bool IsQueued(ChainedTask task)
    {
        return task != null && queuedTasks.Contains(task);
    }*/
    public static bool HasChainedTask(ChainedTask chainedTask)
    {
        return chainedTask != null && (queuedTasks.Contains(chainedTask) || chainedTask == currentTask);
    }
    /*public static ChainedTask FindTaskInChain(TaskWrapper task)
    {
        ChainedTask chainWithTask = currentTask;
        if (currentTask == null || !chainWithTask.Contains(task))
            chainWithTask = queuedTasks.FirstOrDefault(chain => chain.Contains(task));
        return chainWithTask;
    }*/
    /*public static void CancelTask(TaskWrapper task)
    {
        if (task != null)
            FindTaskInChain(task)?.Cancel();
        else
            Debug.LogError("TaskMaker: Task to be cancelled was null");
    }*/
}

public class ChainedTask
{
    private Queue<TaskWrapper> allTasks = new Queue<TaskWrapper>();
    public bool cancelled { get; private set; }

    public ChainedTask()
    {

    }
    public ChainedTask(TaskWrapper _startTask)
    {
        AddTask(_startTask);
    }

    public void AddTask(TaskWrapper task)
    {
        Debug.Assert(task != null, "ChainedTask: The given task was null, this will end the chain prematurely if there are more tasks after");
        allTasks.Enqueue(task);
    }
    public TaskWrapper GetCurrentTask()
    {
        TaskWrapper currentTask = null;
        if (allTasks.Count > 0)
            currentTask = allTasks.Peek();
        return currentTask;
    }
    public void PopCurrentTask()
    {
        if (allTasks.Count > 0)
            allTasks.Dequeue();
    }
    public bool Contains(TaskWrapper task)
    {
        return allTasks.Contains(task);
    }
    public void Cancel()
    {
        cancelled = true;
        var currentTask = GetCurrentTask();
        if (TaskManagerController.HasTask(currentTask))
            TaskManagerController.CancelTask(currentTask);
    }
}
