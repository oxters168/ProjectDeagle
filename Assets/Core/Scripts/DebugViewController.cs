using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class DebugViewController : MonoBehaviour
{
    public static DebugViewController debugViewInScene { get; private set; }
    public ScrollRect debugScrollRect;
    private RectTransform _rectTransform;
    public RectTransform rectTransform { get { if (!_rectTransform) _rectTransform = GetComponent<RectTransform>(); return _rectTransform; } }
    public TMPro.TextMeshProUGUI debugTextBlock;

    public uint logCharCapacity = 750;
    private static StringBuilder logged = new StringBuilder();

    private float previousButtonY;

    private void Awake()
    {
        debugViewInScene = this;
    }
    private void Update()
    {
        SteamController.steamInScene.suppressDebugLog = !SettingsController.showDebugLog;
    }

    private static void RefreshLogArray()
    {
        int removeAmount = (int)(logged.Length - debugViewInScene.logCharCapacity);
        if (removeAmount > 0)
            logged.Remove(0, removeAmount);
    }
    private static void PushToLog(string pushed)
    {
        logged.AppendLine(pushed);
        RefreshLogArray();
    }
    public static void Log(string log)
    {
        bool scrollToBottom = debugViewInScene.debugScrollRect.verticalScrollbar.value == 0;
        bool scrollToLeft = debugViewInScene.debugScrollRect.horizontalScrollbar.value == 0;

        PushToLog(log);
        debugViewInScene.debugTextBlock.text = logged.ToString();

        if (scrollToBottom)
            debugViewInScene.debugScrollRect.verticalScrollbar.value = 0;
        if (scrollToLeft)
            debugViewInScene.debugScrollRect.horizontalScrollbar.value = 0;
    }
}
