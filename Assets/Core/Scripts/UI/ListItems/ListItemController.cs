using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ListItemController : MonoBehaviour
{
    protected object item;
    private RectTransform _selfRectTransform;
    public RectTransform SelfRectTransform { get { if (!_selfRectTransform) _selfRectTransform = GetComponent<RectTransform>(); return _selfRectTransform; } }
    public Image background;

    protected virtual void Awake()
    {
    }

    public virtual object GetItem() { return item; }
    public virtual void SetItem(object o) { item = o; }
    public virtual void UnsetItem() { item = null; }

    public virtual void SetBackground(Color color)
    {
        background.color = color;
    }
}
