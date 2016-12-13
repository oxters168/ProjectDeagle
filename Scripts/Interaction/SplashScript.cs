using System.Collections;
using UnityEngine;

public class SplashScript : MonoBehaviour
{
    GameObject userInterface;
	
    public void StartSplash(GameObject userInterface)
    {
        if (userInterface) { userInterface.SetActive(false); this.userInterface = userInterface; }

        StartCoroutine(WaitForSplash());
    }

    private IEnumerator WaitForSplash()
    {
        while (Application.isShowingSplashScreen) yield return null;

        Animator splashAnimator = GetComponent<Animator>();
        splashAnimator.SetTrigger("Splash");
    }

	public void AnimationEnded ()
    {
        if (!Application.isShowingSplashScreen)
        {
            if (userInterface) userInterface.SetActive(true);
            Destroy(transform.root.gameObject);
        }
    }
}
