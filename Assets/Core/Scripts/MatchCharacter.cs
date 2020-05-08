using UnityEngine;

public class MatchCharacter : MonoBehaviour
{
    public int entityId;
    public GameObject orbitModel, fpsModel;
    public Animator playerAnimator;
    public Renderer playerRenderer;
    public WeaponController orbitMainWeapon, orbitSecondaryWeapon, fpsMainWeapon, fpsSecondaryWeapon;
    public TMPro.TextMeshProUGUI playerNameLabel;
    public bool isFirstPersonView { get; private set; }
    public Vector2 lookDirection { get; private set; }

    private bool isCrouching;

    public void HideFirstPersonModel(bool orbit)
    {
        isFirstPersonView = !orbit;
        orbitModel.SetActive(orbit);
        fpsModel.SetActive(!orbit);
    }
    public void SetAnimationPlaying(bool isOn)
    {
        playerAnimator.speed = isOn ? 1 : 0;
    }
    public void SetColor(Color color)
    {
        playerRenderer.material.color = color;
    }
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
    public void SetRotation(Vector2 direction)
    {
        //transform.forward = Quaternion.AngleAxis(direction.y, Vector3.up) * Vector3.forward;

        //55 degrees is almost down
        //0 degrees about forward
        //0-90 forward-down
        //360-270 forward-up
        float directionXAngle = direction.x;
        if (directionXAngle > 90)
            directionXAngle -= 360;
        //directionXAngle = directionXAngle + 90;
        //playerNameLabel.text = "LookVector: " + direction + " FixedX: " + directionXAngle;
        playerAnimator.SetFloat("LookX", directionXAngle / 90);

        transform.rotation = Quaternion.Euler(new Vector2(0, direction.y));
        fpsModel.transform.localRotation = Quaternion.Euler(new Vector3(directionXAngle, 0));
        lookDirection = new Vector2(directionXAngle, direction.y);
    }
    public void SetVelocity(Vector3 velocity)
    {
        float magnitude = velocity.magnitude;

        //Walk: 109.2, 130 with bomb
        //Run: 250 with knife
        playerAnimator.SetBool("Sneaking", magnitude > 0 && magnitude <= 130);
        playerAnimator.SetBool("Running", magnitude > 130);

        Vector3 moveDirection = new Vector3(velocity.x, 0, velocity.z).normalized;
        Vector3 forwardProjection = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Debug.DrawRay(transform.position + transform.up * 10, transform.forward * 10, Color.black);
        Debug.DrawRay(transform.position + transform.up * 10, moveDirection * 10, Color.blue);
        float moveDirectionAngle = Vector3.SignedAngle(forwardProjection, moveDirection, Vector3.up);
        if (moveDirectionAngle < 0)
            moveDirectionAngle += 360;
        //playerNameLabel.text = "Speed: " + magnitude + " VelocityAngle: " + moveDirectionAngle;
        playerAnimator.SetFloat("MoveDirection", moveDirectionAngle / 360);
    }
    public void ToggleCrouching()
    {
        isCrouching = !isCrouching;
        playerAnimator.SetBool("Crouch", isCrouching);
    }
    public void SetCrouching(bool isCrouching)
    {
        //if (isCrouching)
        //    ToggleCrouching();
        playerAnimator.SetBool("Crouch", isCrouching);
    }
    public void SetWeapon(DemoInfo.EquipmentElement weapon)
    {
        orbitMainWeapon?.SetWeapon(weapon);
        fpsMainWeapon?.SetWeapon(weapon);
        if (weapon == DemoInfo.EquipmentElement.DualBarettas)
        {
            orbitSecondaryWeapon?.SetWeapon(weapon);
            fpsSecondaryWeapon?.SetWeapon(weapon);
        }
        else
        {
            orbitSecondaryWeapon?.SetAllWeaponsInvisible();
            fpsSecondaryWeapon?.SetAllWeaponsInvisible();
        }
    }
    public void SetGunShotVisibility(bool onOff)
    {
        orbitMainWeapon?.SetFlashVisibility(onOff);
        fpsMainWeapon?.SetFlashVisibility(onOff);
        orbitSecondaryWeapon?.SetFlashVisibility(onOff);
        fpsSecondaryWeapon?.SetFlashVisibility(onOff);
    }
    public void SetName(string name)
    {
        playerNameLabel.text = name;
    }
}
