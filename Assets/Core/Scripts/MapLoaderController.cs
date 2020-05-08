using UnityEngine;
using UnityHelpers;

public class MapLoaderController : MonoBehaviour
{
    public static MapLoaderController mapLoaderInScene;
    public MeshCombineStudio.MeshCombiner combiner;

    public MapData currentMap { get; private set; }

    void Awake()
    {
        mapLoaderInScene = this;
    }
    private void OnEnable()
    {
        Doozy.Engine.Message.AddListener<Doozy.Engine.GameEventMessage>(OnGameEventReceived);
        TaskMaker.mapReady3D += TaskMaker_mapReady3D;
        TaskMaker.mapReady2D += TaskMaker_mapReady2D;
    }
    private void OnDisable()
    {
        Doozy.Engine.Message.RemoveListener<Doozy.Engine.GameEventMessage>(OnGameEventReceived);
        TaskMaker.mapReady3D -= TaskMaker_mapReady3D;
        TaskMaker.mapReady2D -= TaskMaker_mapReady2D;
    }

    private void OnGameEventReceived(Doozy.Engine.GameEventMessage message)
    {
        if (message.EventName.Equals("Show3DMap"))
        {
            ExploreMap(true);
        }
        else if (message.EventName.Equals("Hide3DMap"))
        {
            ExploreMap(false);
        }
    }

    private void TaskMaker_mapReady2D(MapData map, bool showMap, bool showControls)
    {
        if (showMap)
        {
            Debug.Log("Viewing " + map.mapName + " overview");
            View2DMap(map);

            if (showControls && MatchInfoPanel.match != null)
                TaskManagerController.RunAction(() =>
                {
                    MatchPlayer.OpenMatch(MatchInfoPanel.match, true);
                });
        }
    }
    private void TaskMaker_mapReady3D(MapData map, bool showMap, bool showControls)
    {
        if (showMap)
        {
            var viewMode = showControls ? UserViewController.ViewMode.firstPerson : UserViewController.ViewMode.freeLook;
            View3DMap(map, viewMode);

            if (showControls && MatchInfoPanel.match != null)
                TaskManagerController.RunAction(() =>
                {
                    MatchPlayer.OpenMatch(MatchInfoPanel.match, false);
                });
            //else if (!showControls)
            //    View3DMap(map, viewMode);
        }
    }

    public void View2DMap(MapData map)
    {
        SetCurrentMap(map);
        TaskManagerController.RunAction(() =>
        {
            Doozy.Engine.GameEventMessage.SendEvent("GotoOverview");
        });
    }
    public void View3DMap(MapData map, UserViewController.ViewMode viewMode)
    {
        SetCurrentMap(map);
        //SettingsController.settingsInScene.viewingCamera.currentMode = viewMode;
        TaskManagerController.RunAction(() =>
        {
            SettingsController.settingsInScene.viewingCamera.SetViewMode(viewMode);
            Doozy.Engine.GameEventMessage.SendEvent("GotoExploreMap");
        });
    }

    private void ExploreMap(bool onOff)
    {
        //MapData currentMap = MapLoaderController.mapLoaderInScene.currentMap;
        if ((bool)MapData.loadedMap?.IsBuilt)
        {
            UserViewController viewingCamera = SettingsController.settingsInScene.viewingCamera;
            SettingsController.settingsInScene.SetViewingCamera(onOff);

            //GameObject mapObj = MapData.loadedMap.GetGameObject();
            //mapObj.SetActive(onOff);

            if (onOff)
            {
                Bounds mapBounds = MapData.loadedMap.GetBounds();
                Rect cameraRect = CameraHelpers.Aspectify(mapBounds.min.xz(), mapBounds.max.xz(), viewingCamera.viewingCamera.aspect);
                float cameraHeight = Camera.main.PerspectiveDistanceFromWidth(cameraRect.width);
                viewingCamera.transform.position = new Vector3(0, cameraHeight * 0.1f, 0);
                viewingCamera.transform.rotation = Quaternion.LookRotation(Vector3.down);
            }
        }
    }

    public void SetCurrentMap(MapData map)
    {
        currentMap = map;
    }
}
