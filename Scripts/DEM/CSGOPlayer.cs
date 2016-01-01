using UnityEngine;
using System.Collections;
using DemoInfo;

public class CSGOPlayer : MonoBehaviour {

    public Transform headPosition, chestPosition, weaponRHPosition, weaponLHPosition;
    public Vector3 aimPosition = new Vector3(0f, 66f, 20f);
    //public Vector3 aimDifference;
    //public bool headAimSet = false;
    private Animator animator;
    public Demo replay;
    public DemoEntity playerInfo;
    public int currentTeam = -1;
    public Vector2 rawDirection = Vector2.zero;
    public float maxAimX = 5f, maxAimY = 15f;
    public Vector2 aimDirection = Vector2.zero;
    public Vector3 horizontalVelocity = Vector3.zero, verticalVelocity = Vector3.zero;
    public float movementDirection = 0;
    public string origWeaponString = "", weaponClass = "", weaponElement = "";
    public GameObject weapon;

    private bool invisible;
	
	void FixedUpdate ()
    {
        if (replay != null && playerInfo != null && replay.seekIndex > -1)
        {
            name = playerInfo.statsInTick[replay.seekIndex].name;
            if(animator == null) animator = GetComponent<Animator>();

            UpdateBodyTransforms();

            UpdateWeapon();

            UpdateAim();

            UpdateTransform();

            UpdateVelocity();

            UpdateAnimator();

            DrawDebugStuff();

            UpdateVisibility();
            UpdateGameObject();
        }
	}

    private void DrawDebugStuff()
    {
        if (playerInfo.statsInTick[replay.seekIndex].isAlive)
        {
            Debug.DrawRay(transform.position + (Vector3.up * 10f), horizontalVelocity, Color.blue);
            Debug.DrawRay(transform.position + (transform.forward * aimPosition.z) + (transform.up * aimPosition.y), transform.forward * 1000f, Color.green);
        }
    }
    private void UpdateBodyTransforms()
    {
        if (animator != null)
        {
            if (headPosition == null)
            {
                headPosition = animator.GetBoneTransform(HumanBodyBones.Head).FindChild("CamPosition");
            }
            if(chestPosition == null) chestPosition = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (weaponRHPosition == null)
            {
                weaponRHPosition = animator.GetBoneTransform(HumanBodyBones.RightHand).FindChild("weapon_hand_R");
            }
        }

        //if (!headAimSet && headPosition != null)
        //{
        //    aimDifference = headPosition.position - transform.position;
        //    headAimSet = true;
        //}
    }
    private void UpdateAim()
    {
        rawDirection = playerInfo.statsInTick[replay.seekIndex].aimDirection;
        float aimX = (360f - playerInfo.statsInTick[replay.seekIndex].aimDirection.x) + 90f, aimY = (playerInfo.statsInTick[replay.seekIndex].aimDirection.y) + 90f;
        while (aimX < 0) aimX = 360 - aimX;
        while (aimX > 360) aimX -= 360;
        while (aimY < 0) aimY = 360 - aimY;
        while (aimY > 360) aimY -= 360;
        aimDirection = new Vector2(aimX, aimY);
    }
    private void UpdateTransform()
    {
        transform.position = playerInfo.statsInTick[replay.seekIndex].position;
        transform.rotation = Quaternion.Euler(new Vector3(0, aimDirection.x, 0));

        //float distanceToDirection = aimDirection.x - transform.rotation.eulerAngles.y, addedRotation = 0;
        //if (distanceToDirection < 0) addedRotation = distanceToDirection + maxAimX;
        //else if (distanceToDirection > 0) addedRotation = distanceToDirection - maxAimX;
        //if(Mathf.Abs(distanceToDirection) > maxAimX) transform.rotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y + addedRotation, 0));
    }
    private void UpdateWeapon()
    {
        string weaponString = "";
        string equipmentClassString = "";
        string weaponElementString = "";
        if (playerInfo.statsInTick[replay.seekIndex].activeWeapon != null)
        {
            weaponString = playerInfo.statsInTick[replay.seekIndex].activeWeapon.originalString;
            equipmentClassString = playerInfo.statsInTick[replay.seekIndex].activeWeapon.equipmentClass.ToString();
            weaponElementString = playerInfo.statsInTick[replay.seekIndex].activeWeapon.weapon.ToString();
        }
        else weaponString = "Null";
        //if (equipmentClassString == null) equipmentClassString = "";
        //if (weaponElementString == null) weaponElementString = "";

        #region Update GameObject
        if ((playerInfo.statsInTick[replay.seekIndex].activeWeapon != null) && (!weaponClass.Equals(equipmentClassString, System.StringComparison.InvariantCultureIgnoreCase) || !weaponElement.Equals(weaponElementString, System.StringComparison.InvariantCultureIgnoreCase)))
        {
            if (weapon != null) GameObject.DestroyImmediate(weapon);

            GameObject prefabWeapon = FindWeapon(playerInfo.statsInTick[replay.seekIndex].teamID, replay.demoTicks[replay.seekIndex].ctID, replay.demoTicks[replay.seekIndex].tID, playerInfo.statsInTick[replay.seekIndex].activeWeapon.equipmentClass, playerInfo.statsInTick[replay.seekIndex].activeWeapon.weapon);
            if (weaponRHPosition != null && prefabWeapon != null)
            {
                weapon = GameObject.Instantiate(prefabWeapon) as GameObject;
                
                if (weapon != null)
                {
                    weapon.transform.parent = weaponRHPosition;
                    weapon.transform.localPosition = new Vector3(0.02f, 0, -0.04f);
                    weapon.transform.localRotation = Quaternion.Euler(350, 280, 270);
                }
            }
        }
        #endregion

        #region Update Strings
        origWeaponString = weaponString;
        weaponClass = equipmentClassString;
        weaponElement = weaponElementString;
        #endregion
    }
    private void UpdateVelocity()
    {
        horizontalVelocity = new Vector3(playerInfo.statsInTick[replay.seekIndex].velocity.x, 0, playerInfo.statsInTick[replay.seekIndex].velocity.z);
        verticalVelocity = new Vector3(0, playerInfo.statsInTick[replay.seekIndex].velocity.y, 0);
        //movementDirection = Mathf.Atan(Mathf.Min(horizontalVelocity.normalized.x, horizontalVelocity.normalized.z) / Mathf.Max(horizontalVelocity.normalized.x, horizontalVelocity.normalized.z)) * Mathf.Rad2Deg;
        movementDirection = Vector3.Angle(Vector3.forward, horizontalVelocity.normalized);
        if (horizontalVelocity.x < 0) movementDirection = 360f - movementDirection;
        
        movementDirection -= aimDirection.x;
        
        while (movementDirection < 0) movementDirection += 360f;
        while (movementDirection > 360) movementDirection -= 360f;
    }
    private void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetFloat("MovementDirection", movementDirection);

            if (horizontalVelocity.magnitude > 0) animator.SetBool("Walk", true);
            else animator.SetBool("Walk", false);

            if (horizontalVelocity.magnitude > 150) animator.SetBool("Run", true);
            else animator.SetBool("Run", false);

            if (verticalVelocity.magnitude > 0) animator.SetBool("Jump", true);
            else animator.SetBool("Jump", false);

            animator.SetBool("Crouch", playerInfo.statsInTick[replay.seekIndex].isDucking);
            if(playerInfo.statsInTick[replay.seekIndex].activeWeapon != null) animator.SetInteger("EquipmentClass", (int) playerInfo.statsInTick[replay.seekIndex].activeWeapon.equipmentClass);
            //animator.SetFloat("AimX", (aimDirection.x - transform.rotation.eulerAngles.y) / maxAimX);
            animator.SetFloat("AimY", (90f - aimDirection.y) / maxAimY);
        }
    }
    private void UpdateVisibility()
    {
        if ((!replay.demoTicks[replay.seekIndex].playersInTick.Contains(playerInfo.key) && !invisible) || (replay.demoTicks[replay.seekIndex].playersInTick.Contains(playerInfo.key) && invisible))
        {
            Hide(!replay.demoTicks[replay.seekIndex].playersInTick.Contains(playerInfo.key));
        }
        if ((playerInfo.statsInTick[replay.seekIndex].isAlive && invisible) || (!playerInfo.statsInTick[replay.seekIndex].isAlive && !invisible))
        {
            Hide(!playerInfo.statsInTick[replay.seekIndex].isAlive);
        }
    }
    private void UpdateGameObject()
    {
        if (currentTeam != playerInfo.statsInTick[replay.seekIndex].teamID)
        {
            GameObject newAppearance = null;
            //currentTeam = playerInfo.statsInTick[replay.seekIndex].teamID;
            if (playerInfo.statsInTick[replay.seekIndex].teamID == replay.demoTicks[replay.seekIndex].ctID)
            {
                GameObject[] ct = Resources.LoadAll<GameObject>("Prefabs/CT");
                newAppearance = GameObject.Instantiate(ct[Random.Range(0, ct.Length)]) as GameObject;
            }
            else if (playerInfo.statsInTick[replay.seekIndex].teamID == replay.demoTicks[replay.seekIndex].tID)
            {
                GameObject[] t = Resources.LoadAll<GameObject>("Prefabs/T");
                newAppearance = GameObject.Instantiate(t[Random.Range(0, t.Length)]) as GameObject;
            }
            if (newAppearance != null)
            {
                //GameObject oldGameObject = gameObject;
                CSGOPlayer shiftSelf = newAppearance.AddComponent<CSGOPlayer>();
                shiftSelf.playerInfo = playerInfo;
                shiftSelf.replay = replay;
                shiftSelf.currentTeam = playerInfo.statsInTick[replay.seekIndex].teamID;
                replay.playerObjects[playerInfo.key] = shiftSelf;
                GameObject.DestroyImmediate(gameObject);
                //shiftSelf = this;
                //GameObject.DestroyImmediate(oldGameObject);
            }
        }
    }

    public void Hide(bool hide)
    {
        foreach (Transform child in transform)
        {
            Renderer childRend = child.gameObject.GetComponent<Renderer>();
            if (childRend != null)
            {
                childRend.enabled = !hide;
            }
        }
        if (weapon != null)
        {
            foreach (Transform child in weapon.transform)
            {
                Renderer childRend = child.gameObject.GetComponent<Renderer>();
                if (childRend != null)
                {
                    childRend.enabled = !hide;
                }
            }
        }
        invisible = hide;
    }

    public static GameObject FindWeapon(int teamID, int ctID, int tID, EquipmentClass weaponClass, EquipmentElement weaponElement)
    {
        string weaponsLocation = "Models/csgo/weapons/";

        if (weaponElement == EquipmentElement.AK47) return Resources.Load<GameObject>(weaponsLocation + "v_rif_ak47");
        if (weaponElement == EquipmentElement.AUG) return Resources.Load<GameObject>(weaponsLocation + "v_rif_aug");
        if (weaponElement == EquipmentElement.AWP) return Resources.Load<GameObject>(weaponsLocation + "v_snip_awp");
        if (weaponElement == EquipmentElement.Bizon) return Resources.Load<GameObject>(weaponsLocation + "v_smg_bizon");
        if (weaponElement == EquipmentElement.Bomb) return Resources.Load<GameObject>(weaponsLocation + "v_ied");
        if (weaponElement == EquipmentElement.CZ) return Resources.Load<GameObject>(weaponsLocation + "v_pist_cz_75");
        if (weaponElement == EquipmentElement.Deagle) return Resources.Load<GameObject>(weaponsLocation + "v_pist_deagle");
        if (weaponElement == EquipmentElement.Decoy) return Resources.Load<GameObject>(weaponsLocation + "v_eq_decoy");
        //if (weaponElement == EquipmentElement.DefuseKit) ;
        if (weaponElement == EquipmentElement.DualBarettas) return Resources.Load<GameObject>(weaponsLocation + "v_pist_elite");
        if (weaponElement == EquipmentElement.Famas) return Resources.Load<GameObject>(weaponsLocation + "v_rif_famas");
        if (weaponElement == EquipmentElement.FiveSeven) return Resources.Load<GameObject>(weaponsLocation + "v_pist_fiveseven");
        if (weaponElement == EquipmentElement.Flash) return Resources.Load<GameObject>(weaponsLocation + "v_eq_flashbang");
        if (weaponElement == EquipmentElement.G3SG1) return Resources.Load<GameObject>(weaponsLocation + "v_snip_g3sg1");
        if (weaponElement == EquipmentElement.Gallil) return Resources.Load<GameObject>(weaponsLocation + "v_rif_galilar");
        if (weaponElement == EquipmentElement.Glock) return Resources.Load<GameObject>(weaponsLocation + "v_pist_glock18");
        if (weaponElement == EquipmentElement.HE) return Resources.Load<GameObject>(weaponsLocation + "v_eq_fraggrenade");
        //if (weaponElement == EquipmentElement.Helmet) ;
        if (weaponElement == EquipmentElement.Incendiary) return Resources.Load<GameObject>(weaponsLocation + "v_eq_incendiarygrenade");
        //if (weaponElement == EquipmentElement.Kevlar) ;
        if (weaponElement == EquipmentElement.Knife)
        {
            if (teamID == ctID) return Resources.Load<GameObject>(weaponsLocation + "v_knife_default_ct");
            if (teamID == tID) return Resources.Load<GameObject>(weaponsLocation + "v_knife_default_t");
        }
        if (weaponElement == EquipmentElement.M249) return Resources.Load<GameObject>(weaponsLocation + "v_mach_m249para");
        if (weaponElement == EquipmentElement.M4A1) return Resources.Load<GameObject>(weaponsLocation + "v_rif_m4a1_s");
        if (weaponElement == EquipmentElement.M4A4) return Resources.Load<GameObject>(weaponsLocation + "v_rif_m4a1");
        if (weaponElement == EquipmentElement.Mac10) return Resources.Load<GameObject>(weaponsLocation + "v_smg_mac10");
        if (weaponElement == EquipmentElement.Molotov) return Resources.Load<GameObject>(weaponsLocation + "v_eq_molotov");
        if (weaponElement == EquipmentElement.MP7) return Resources.Load<GameObject>(weaponsLocation + "v_smg_mp7");
        if (weaponElement == EquipmentElement.MP9) return Resources.Load<GameObject>(weaponsLocation + "v_smg_mp9");
        if (weaponElement == EquipmentElement.Negev) return Resources.Load<GameObject>(weaponsLocation + "v_mach_negev");
        if (weaponElement == EquipmentElement.Nova) return Resources.Load<GameObject>(weaponsLocation + "v_shot_nova");
        if (weaponElement == EquipmentElement.P2000) return Resources.Load<GameObject>(weaponsLocation + "v_pist_hkp2000");
        if (weaponElement == EquipmentElement.P250) return Resources.Load<GameObject>(weaponsLocation + "v_pist_p250");
        if (weaponElement == EquipmentElement.P90) return Resources.Load<GameObject>(weaponsLocation + "v_smg_p90");
        if (weaponElement == EquipmentElement.SawedOff) return Resources.Load<GameObject>(weaponsLocation + "v_shot_sawedoff");
        if (weaponElement == EquipmentElement.Scar20) return Resources.Load<GameObject>(weaponsLocation + "v_snip_scar20");
        if (weaponElement == EquipmentElement.Scout) return Resources.Load<GameObject>(weaponsLocation + "v_snip_ssg08");
        if (weaponElement == EquipmentElement.SG556) return Resources.Load<GameObject>(weaponsLocation + "v_rif_sg556");
        if (weaponElement == EquipmentElement.Smoke) return Resources.Load<GameObject>(weaponsLocation + "v_eq_smokegrenade");
        if (weaponElement == EquipmentElement.Swag7) return Resources.Load<GameObject>(weaponsLocation + "v_shot_mag7");
        if (weaponElement == EquipmentElement.Tec9) return Resources.Load<GameObject>(weaponsLocation + "v_pist_tec9");
        if (weaponElement == EquipmentElement.UMP) return Resources.Load<GameObject>(weaponsLocation + "v_smg_ump45");
        //if (weaponElement == EquipmentElement.Unknown) ;
        if (weaponElement == EquipmentElement.USP) return Resources.Load<GameObject>(weaponsLocation + "v_pist_223");
        //if (weaponElement == EquipmentElement.World) return Resources.Load<GameObject>(weaponsLocation + "v_knife_default_ct");
        if (weaponElement == EquipmentElement.XM1014) return Resources.Load<GameObject>(weaponsLocation + "v_shot_xm1014");
        if (weaponElement == EquipmentElement.Zeus) return Resources.Load<GameObject>(weaponsLocation + "v_eq_taser");
        
        return null;
    }
}
