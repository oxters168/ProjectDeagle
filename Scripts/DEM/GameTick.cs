using UnityEngine;
using System.Collections;
using DemoInfo;
using System.Collections.Generic;

public class GameTick
{
    //public static Dictionary<Player, Entity> players;
    public List<Player> playersInTick;
    public int ctID, tID;
    //private List<EntityTickPosition> playerPositions;

    public GameTick()
    {
        playersInTick = new List<Player>();
        //if (players == null)
        //{
        //    players = new Dictionary<Player, Entity>();
        //}
    }

    public void AddPlayer(Player player)
    {
        //players.Add(player.Copy());
        //players.Add(player);
        playersInTick.Add(player);
        //if (!players.ContainsKey(player))
        //{
        //    players.Add(player, new Entity(player.Position, player.ViewDirectionX, player.ViewDirectionY, player.Velocity, player.HP, player.TeamID, player.IsAlive, player.IsDucking));
        //}
        //else
        //{
        //    players[player].AddTickInfo(new EntityInfo(player.Position, player.ViewDirectionX, player.ViewDirectionY, player.Velocity, player.HP, player.TeamID, player.IsAlive, player.IsDucking));
        //}
    }
    //public List<Entity> GetPlayers()
    //{
        //List<Player> clonedPlayers = new List<Player>();

        //foreach (Player p in players)
        //{
        //    clonedPlayers.Add(p.Copy());
        //}

        //return clonedPlayers;
    //    return players;
    //}
}

/*public struct EntityTick
{
    public Player player;
    public Vector3 playerPosition, playerVelocity;
    public float viewDirectionX, viewDirectionY;
    public int health, teamID;
    public bool isAlive, isDucking;

    public EntityTick(Player p, Vector3 eP, float dX, float dY, Vector3 v, int hp, int tID, bool iA, bool iD)
    {
        player = p;
        playerPosition = new Vector3(eP.x, eP.z, eP.y);
        viewDirectionX = dX;
        viewDirectionY = dY;
        playerVelocity = new Vector3(v.x, v.z, v.y);
        health = hp;
        teamID = tID;
        isAlive = iA;
        isDucking = iD;
    }
}*/
