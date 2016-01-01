using UnityEngine;
using System;

public class EntityInfo
{
    public string name, clantag;
    public WeaponInfo activeWeapon;
    public int entityID;
    public long steamID;
    public Vector3 position, velocity;
    public Vector2 aimDirection;
    public int health, kills, teamID;
    public bool isAlive, isDucking;

    public EntityInfo(string n, string cT, WeaponInfo aW, int eID, long sID, Vector3 eP, float dX, float dY, Vector3 v, int hp, int k, int tID, bool iA, bool iD)
    {
        name = n;
        clantag = cT;
        activeWeapon = aW;
        entityID = eID;
        steamID = sID;
        position = new Vector3(eP.x, eP.z, eP.y);
        velocity = new Vector3(v.x, v.z, v.y);
        aimDirection = new Vector2(dX, dY);
        health = hp;
        kills = k;
        teamID = tID;
        isAlive = iA;
        isDucking = iD;
    }
}
