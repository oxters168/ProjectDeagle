using System;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
using DemoInfo;

[Serializable]
public class WeaponDictionary : SerializableDictionaryBase<DemoInfo.EquipmentElement, GameObject> { }

public class WeaponController : MonoBehaviour
{
    public EquipmentElement currentWeapon;
    public WeaponDictionary weapons;

    public GameObject gunFlash;

    private void Start()
    {
        SetAllWeaponsInvisible();
    }

    public void SetAllWeaponsInvisible()
    {
        foreach (var weapon in weapons)
            weapon.Value.SetActive(false);
    }
    public void SetWeapon(EquipmentElement key)
    {
        currentWeapon = key;
        EquipmentElement weaponKey = key;
        if (!weapons.ContainsKey(weaponKey))
            weaponKey = EquipmentElement.Unknown;

        GameObject weaponGO = null;
        foreach (var weapon in weapons)
        {
            weapon.Value.gameObject.SetActive(weapon.Key == weaponKey);
            if (weapon.Key == weaponKey)
                weaponGO = weapon.Value;
        }
        if (weaponGO)
        {
            foreach(Transform child in weaponGO.transform)
            {
                if (child.CompareTag("GunFlash"))
                    gunFlash = child.gameObject;
            }
        }
    }
    public void SetFlashVisibility(bool onOff)
    {
        if (gunFlash)
            gunFlash.SetActive(onOff);
    }
}
