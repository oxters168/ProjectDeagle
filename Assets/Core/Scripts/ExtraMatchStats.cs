using System;
using System.Collections.Generic;

[Serializable]
public class ExtraMatchStats
{
    public const int STATS_VERSION = 1;

    [UnityEngine.SerializeField]
    private int _version = STATS_VERSION;
    public int version { get { return _version; } }

    public List<uint> accountIds = new List<uint>();
    public List<string> playerNames = new List<string>();
    public List<int> roundStartTicks = new List<int>();
    public List<int> roundEndTicks = new List<int>();
    public List<int> roundWinner = new List<int>();
}
