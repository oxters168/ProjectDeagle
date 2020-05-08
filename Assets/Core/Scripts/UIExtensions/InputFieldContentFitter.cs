using UnityEngine;
using TMPro;

[ExecuteAlways]
public class InputFieldContentFitter : MonoBehaviour
{
    private RectTransform _selfRectTransform;
    public RectTransform SelfRectTransform { get { if (!_selfRectTransform) _selfRectTransform = GetComponent<RectTransform>(); return _selfRectTransform; } }

    public TMP_InputField inputField;
    public Vector2 padding = new Vector2(20, 12);
    public bool horizontalFitting;
    public bool verticalFitting;

    private void Update()
    {
        if (inputField)
            SelfRectTransform.sizeDelta = new Vector2(horizontalFitting ? (inputField.textComponent.preferredWidth + padding.x) : SelfRectTransform.rect.size.x, verticalFitting ? (inputField.textComponent.preferredHeight + padding.y) : SelfRectTransform.rect.size.y);
    }
}
