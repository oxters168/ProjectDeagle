using System;
using System.Collections.Generic;
using DemoInfo;

public class WeaponInfo
{
    public int weaponID { get; private set; }
    public int magazineAmmo { get; private set; }
    public int reserveAmmo { get; private set; }
    public int ammoType { get; private set; }
    public EquipmentClass equipmentClass { get; private set; }
    public int entityID { get; private set; }
    public string originalString { get; private set; }
    public string skinID { get; private set; }
    public Player owner { get; private set; }
    public EquipmentElement weapon { get; private set; }

    public WeaponInfo(int id, Equipment info)
    {
        weaponID = id;

        magazineAmmo = info.AmmoInMagazine;
        ammoType = info.AmmoType;
        equipmentClass = info.Class;
        entityID = info.EntityID;
        originalString = info.OriginalString;
        owner = info.Owner;
        reserveAmmo = info.ReserveAmmo;
        skinID = info.SkinID;
        weapon = info.Weapon;
    }
}
