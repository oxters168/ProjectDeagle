using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OverviewCharacter : MonoBehaviour
{
    private RectTransform _selfRectTransform;
    public RectTransform SelfRectTransform { get { if (!_selfRectTransform) _selfRectTransform = GetComponent<RectTransform>(); return _selfRectTransform; } }

    public Image characterImage;
    public Image gunShotImage;
    public RectTransform weaponRoot;
    public WeaponController mainWeapon, secondaryWeapon;
    public RectTransform rotatableCharacter;
    public TextMeshProUGUI nameText;
    private Vector3 origWeaponScale;
    public Vector2 origSize;
    private bool origSizeSet;

    public void SetColor(Color color)
    {
        characterImage.color = color;
    }
    public void SetRotation(float amount)
    {
        rotatableCharacter.localRotation = Quaternion.Euler(0, 0, amount);
    }
    public void SetName(string name)
    {
        nameText.text = name;
    }
    public void SetSizeOffset(float amount)
    {
        if (!origSizeSet)
        {
            origSize = SelfRectTransform.sizeDelta;
            origWeaponScale = weaponRoot.localScale;
            origSizeSet = true;
        }
        SelfRectTransform.sizeDelta = origSize + Vector2.one * amount;
        weaponRoot.localScale = origWeaponScale + Vector3.one * amount;
    }
    public void SetWeapon(DemoInfo.EquipmentElement weapon)
    {
        mainWeapon.SetWeapon(weapon);
        if (weapon == DemoInfo.EquipmentElement.DualBarettas)
            secondaryWeapon.SetWeapon(weapon);
        else
            secondaryWeapon.SetAllWeaponsInvisible();
    }
    public void SetGunShotVisibility(bool onOff)
    {
        mainWeapon.SetFlashVisibility(onOff);
        secondaryWeapon.SetFlashVisibility(onOff);
        //gunShotImage.color = new Color(gunShotImage.color.r, gunShotImage.color.g, gunShotImage.color.b, onOff ? 1 : 0);
    }
}
