using UnityEngine;

public class AdShower : MonoBehaviour
{
    private float startTime = -1;
    [Tooltip("The time in seconds an item is viewed for an ad to show up after leaving.")]
    public float viewTimeToAd = 30;

    public void StartTimer()
    {
        startTime = Time.time;
    }
    public void TryShowAd()
    {
        if (startTime >= 0 && Time.time - startTime > viewTimeToAd)
            AdMobController.ShowAd(0);
        startTime = -1;
    }
}
