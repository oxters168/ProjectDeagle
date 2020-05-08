using Doozy.Engine.UI;
using UnityEngine;
using UnityHelpers;

public class LoginController : MonoBehaviour
{
    //private SteamController.AuthType authType;
    //public static string username, password, authCode;
    //public static bool rememberMe;

    private void Start()
    {
        SteamController.steamInScene.steam3.onPassRequired += RequestPassword;
        SteamController.steamInScene.steam3.on2faRequired += Request2faCode;
        SteamController.steamInScene.steam3.onAuthRequired += RequestAuthCode;
    }

    private void RequestPassword(SteamKit2.EResult result)
    {
        //SteamController.LogToConsole("Login Key expired");
        TaskManagerController.RunAction(() =>
        {
            SettingsController.ForgetUser();
            SteamController.ShowErrorPopup("Login Error", "Login key expired please log in again");
        });
    }
    private void Request2faCode(SteamKit2.EResult result)
    {
        TaskManagerController.RunAction(() =>
        {
            var authPopup = UIPopupManager.ShowPopup("AuthPopup", true, false);
            authPopup.Data.SetButtonsCallbacks(
                () => { authPopup.Hide(); },
                () =>
                {
                    var authItems = authPopup.GetComponent<AuthPopupItemsContainer>();
                    SteamController.steamInScene.steam3.SendTwoFactor(authItems.authField.text);
                    authPopup.Hide();
                });
        });
    }
    private void RequestAuthCode(SteamKit2.EResult result)
    {
        TaskManagerController.RunAction(() =>
        {
            var authPopup = UIPopupManager.ShowPopup("AuthPopup", true, false);
            authPopup.Data.SetButtonsCallbacks(
                () => { authPopup.Hide(); },
                () =>
                {
                    var authItems = authPopup.GetComponent<AuthPopupItemsContainer>();
                    SteamController.steamInScene.steam3.SendAuth(authItems.authField.text);
                    authPopup.Hide();
                });
        });
    }

}
