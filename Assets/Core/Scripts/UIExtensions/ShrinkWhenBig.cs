using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public class ShrinkWhenBig : MonoBehaviour
{
    private RectTransform _selfRectTransform;
    private RectTransform SelfRectTransform { get { if (!_selfRectTransform) _selfRectTransform = GetComponentInParent<RectTransform>(); return _selfRectTransform; } }
    public bool applyToWidth, applyToHeight, keepAspectRatio;
    public Vector2 percentWeights = Vector2.one;
    private Vector2 originalSize;

    private void Start()
    {
        originalSize = SelfRectTransform.rect.size;
    }
    private void Update()
    {
        if (applyToWidth || applyToHeight)
        {
            RectTransform parentRectTransform = SelfRectTransform.parent.GetComponentInParent<RectTransform>();
            Vector2 percentedParentSize = new Vector2(parentRectTransform.rect.size.x * percentWeights.x, parentRectTransform.rect.size.y * percentWeights.y);
            SelfRectTransform.sizeDelta = new Vector2((applyToWidth ? Mathf.Min(originalSize.x, percentedParentSize.x) : SelfRectTransform.rect.size.x), (applyToHeight ? Mathf.Min(originalSize.y, percentedParentSize.y) : SelfRectTransform.rect.size.y));
            if (keepAspectRatio)
            {
                float getXRatio = originalSize.x / originalSize.y;
                float getYRatio = originalSize.y / originalSize.x;
                float ratioedX = applyToHeight ? SelfRectTransform.rect.size.y * getXRatio : SelfRectTransform.rect.size.x;
                float ratioedY = applyToWidth ? SelfRectTransform.rect.size.x * getYRatio : SelfRectTransform.rect.size.y;
                SelfRectTransform.sizeDelta = new Vector2(Mathf.Min(SelfRectTransform.rect.size.x, ratioedX), Mathf.Min(SelfRectTransform.rect.size.y, ratioedY));
            }
        }
    }
}
