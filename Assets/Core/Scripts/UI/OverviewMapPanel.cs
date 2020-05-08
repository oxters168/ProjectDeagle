using UnityEngine;
using UnityEngine.UI;
using UnityHelpers;

public class OverviewMapPanel : MonoBehaviour
{
    public RectTransform overviewBack;
    public Image overviewImage;
    public DraggableItem overviewDragger;
    public SizableItem overviewSizer;
    private OverviewData currentOverview;

    private void Start()
    {
        TaskMaker.mapReady3D += TaskMaker_mapReady3D;
        TaskMaker.mapReady2D += TaskMaker_mapReady2D;
    }

    private void TaskMaker_mapReady2D(MapData map, bool showMap, bool showReplayControls)
    {
        TaskManagerController.RunAction(() =>
        {
            overviewDragger.enabled = true;
            overviewSizer.enabled = true;
            overviewImage.gameObject.SetActive(true);
        });
    }
    private void TaskMaker_mapReady3D(MapData map, bool showMap, bool showReplayControls)
    {
        TaskManagerController.RunAction(() =>
        {
            overviewDragger.enabled = false;
            overviewDragger.enabled = false;
            overviewImage.gameObject.SetActive(SettingsController.showOverview);
        });
    }

    private void OnEnable()
    {
        MapData currentMap = null;
        if (MapLoaderController.mapLoaderInScene)
            currentMap = MapLoaderController.mapLoaderInScene.currentMap;
        if (currentMap != null && currentMap.IsOverviewAvailable())
            currentOverview = currentMap.GetOverviewData();
        if (currentOverview != null)
            overviewImage.sprite = currentOverview.GetRadar();

        overviewBack.gameObject.SetActive(currentOverview != null);
        overviewImage.rectTransform.SetParent(overviewBack, false);
        float size = Mathf.Min(overviewBack.rect.size.x, overviewBack.rect.size.y);
        overviewSizer.SetSize(Vector2.one * size);
        overviewImage.rectTransform.localRotation = Quaternion.identity;
        overviewImage.rectTransform.localPosition = Vector2.zero;
    }
    private void OnDisable()
    {
        currentOverview = null;
    }
}
