using ProjectDeagle;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DemoController : MonoBehaviour
{
    public string demoLocation;
    public string demoFileName;
    public DemoParser demo;
    public ListableItem mapItem;
    public int tickIndex;
    public float tickRate { get { return demo != null ? demo.demoHeader.ticks / demo.demoHeader.playbackTime : 0; } }
    public float tickTime { get { return demo != null ? demo.demoHeader.playbackTime / demo.demoHeader.ticks : 0; } }
    public bool play;
    public float timePassed;

    public Dictionary<int, PlayerController> players = new Dictionary<int, PlayerController>();
    public Dictionary<int, WeaponController> weapons = new Dictionary<int, WeaponController>();
	
	void Update ()
    {
        if (demo != null && play)
        {
            if (tickIndex < 0) tickIndex = 0;
            if (tickIndex >= demo.TicksParsed()) tickIndex = demo.TicksParsed() - 1;

            if (timePassed > tickTime)
            {
                ProcessTick();

                int ticksPassed = (int)(timePassed / tickTime);
                tickIndex += ticksPassed;
                timePassed -= tickTime * ticksPassed;
            }
            timePassed += Time.deltaTime;
        }
        else timePassed = 0;
	}

    public void StartParsing(string demoLocation)
    {
        this.demoLocation = demoLocation.Replace("\\", "/");
        demoFileName = this.demoLocation;
        if (demoFileName.IndexOf("/") > -1) demoFileName = demoFileName.Substring(demoFileName.LastIndexOf("/") + 1);
        if (demoFileName.IndexOf(".") > -1) demoFileName = demoFileName.Substring(0, demoFileName.LastIndexOf("."));
        name = demoLocation;
        demo = new DemoParser(demoLocation);
        demo.ParseHeader();

        Debug.Log("Attempting to load " + ApplicationPreferences.mapsDir + demo.demoHeader.mapName + ".bsp");
        mapItem = Camera.main.GetComponent<ProgramInterface>().LoadMap(ApplicationPreferences.mapsDir + demo.demoHeader.mapName + ".bsp");
        StartCoroutine(WaitForMap());
        //Debug.Log(demo.demoHeader.mapName);
        //demo.Start();
    }
    private IEnumerator WaitForMap()
    {
        while(!((BSPMap)mapItem.value).IsDone)
        {
            yield return null;
        }
        demo.Start();
    }

    private void ProcessTick()
    {
        Tick tick = demo.GetTick(tickIndex);
        ProcessPlayers(tick);
    }
    private void ProcessPlayers(Tick tick)
    {
        foreach (KeyValuePair<int, PlayerInfo> rawPlayer in demo.playerInfo)
        {
            if (!players.ContainsKey(rawPlayer.Key))
            {
                GameObject playerGO = new GameObject("New Player");
                PlayerController playerController = playerGO.AddComponent<PlayerController>();

                players[rawPlayer.Key] = playerController;
                playerController.demoController = this;
                playerController.playerKey = rawPlayer.Key;
                playerController.playerInfo = rawPlayer.Value;
            }
        }
    }
}
