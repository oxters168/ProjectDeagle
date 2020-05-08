using SteamKit2;
using System;
using UnityEngine;

public class CallbackManagerController : MonoBehaviour
{
    private static bool quitting;
    private static CallbackManagerController callbackManagerInScene;
    private CallbackManager _manager;
    public static CallbackManager manager { get { return callbackManagerInScene?._manager; } set { var managerInScene = callbackManagerInScene; if (managerInScene) managerInScene._manager = value; } }

    private void Awake()
    {
        if (!callbackManagerInScene)
            callbackManagerInScene = this;
    }
    void Update()
    {
        if (_manager != null && !quitting)
            // in order for the callbacks to get routed, they need to be handled by the manager
            _manager.RunWaitCallbacks(TimeSpan.FromMilliseconds(1));
        //await Task.Run(() =>
        //{
            // in order for the callbacks to get routed, they need to be handled by the manager
        //    _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
        //});
    }
    private void OnApplicationQuit()
    {
        quitting = true;
    }
}
