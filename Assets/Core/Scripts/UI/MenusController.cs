using UnityEngine;

public class MenusController : MonoBehaviour
{
    public static MenusController menusControllerInScene { get; private set; }
    public CanvasGroup[] allCanvasGroups;

    private void Awake()
    {
        menusControllerInScene = this;
    }

    public static void SetFocusTo(CanvasGroup focusedGroup)
    {
        foreach (CanvasGroup canvasGroup in menusControllerInScene.allCanvasGroups)
            canvasGroup.interactable = false;
        focusedGroup.interactable = true;
    }
    public static void ReturnFocusToAll()
    {
        foreach (CanvasGroup canvasGroup in menusControllerInScene.allCanvasGroups)
            canvasGroup.interactable = true;
    }
}
