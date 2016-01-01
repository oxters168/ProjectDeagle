using UnityEngine;
using System;
using System.Collections.Generic;
using DemoInfo;

public class DemoEntity
{
    //public string name, clantag;
    //public int entityID;
    //public long steamID;
    public List<EntityInfo> statsInTick;
    public Player key;

    public DemoEntity(Player pk, string n, string cT, WeaponInfo aW, int eID, long sID, Vector3 eP, float dX, float dY, Vector3 v, int hp, int k, int tID, bool iA, bool iD) : this(pk, new EntityInfo(n, cT, aW, eID, sID, eP, dX, dY, v, hp, k, tID, iA, iD)) { }
    public DemoEntity(Player k, EntityInfo info)
    {
        //entityID = eID;
        //steamID = sID;
        key = k;
        statsInTick = new List<EntityInfo>();
        AddTickInfo(info);
    }

    public void AddTickInfo(EntityInfo toAdd)
    {
        statsInTick.Add(toAdd);
    }
}
