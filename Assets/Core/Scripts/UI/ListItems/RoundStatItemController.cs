using UnityEngine;
using UnityEngine.UI;

public class RoundStatItemController : MonoBehaviour
{
    private RectTransform _selfRectTransform;
    public RectTransform SelfRectTransform { get { if (!_selfRectTransform) _selfRectTransform = GetComponent<RectTransform>(); return _selfRectTransform; } }

    public Image ctWinImage, tWinImage;

    public void SetWinner(DemoInfo.Team team)
    {
        ctWinImage.gameObject.SetActive(team == DemoInfo.Team.CounterTerrorist);
        tWinImage.gameObject.SetActive(team == DemoInfo.Team.Terrorist);
    }
}
