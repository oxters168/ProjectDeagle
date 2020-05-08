using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public class TwoPartButton : MonoBehaviour
{
    private RectTransform _selfRectTransform;
    private RectTransform SelfRectTransform { get { if (!_selfRectTransform) _selfRectTransform = GetComponent<RectTransform>(); return _selfRectTransform; } }

    public RectTransform leftHalf, rightHalf;
    public Image icon;
    public TextMeshProUGUI buttonText;
    public Button button;

    [Space(10)]
    public float topPadding;
    public float bottomPadding;
    public float leftPadding;
    public float rightPadding;
    [Space(5)]
    public float spacing;

    private void Update()
    {
        RectTransform parentRectTransform = SelfRectTransform.GetComponentInParent<RectTransform>();

        Vector2 paddedParentSize = new Vector2(parentRectTransform.rect.size.x - leftPadding - rightPadding, parentRectTransform.rect.size.y - topPadding - bottomPadding);

        Vector2 iconSize = new Vector2(Mathf.Min(paddedParentSize.x, paddedParentSize.y), Mathf.Min(paddedParentSize.x, paddedParentSize.y));
        if (leftHalf && leftHalf.gameObject.activeSelf && buttonText && rightHalf.gameObject.activeSelf)
        {
            Vector2 iconPosition = new Vector2(-paddedParentSize.x / 2f + iconSize.x / 2f, 0);
            leftHalf.sizeDelta = iconSize;
            leftHalf.localPosition = iconPosition;

            Vector2 textSize = new Vector2(paddedParentSize.x - iconSize.x - spacing, paddedParentSize.y);
            rightHalf.sizeDelta = textSize;
            rightHalf.localPosition = new Vector2(iconPosition.x + iconSize.x / 2f + spacing + textSize.x / 2f, 0);
        }
        else if (leftHalf && leftHalf.gameObject.activeSelf)
        {
            leftHalf.sizeDelta = iconSize;
            leftHalf.localPosition = Vector2.zero;
        }
        else if (rightHalf && rightHalf.gameObject.activeSelf)
        {
            rightHalf.sizeDelta = paddedParentSize;
            rightHalf.localPosition = Vector2.zero;
        }
    }
}
