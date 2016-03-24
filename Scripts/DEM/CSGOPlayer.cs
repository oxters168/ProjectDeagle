using UnityEngine;
//using System.Collections;
using DemoInfo;
using RootMotion.FinalIK;

public class CSGOPlayer : MonoBehaviour {

    public long steamID;
    public int entityID;

    public Transform chestPosition, weaponRHPosition, weaponLHPosition;
    public Vector3 aimPosition = new Vector3(0f, 66f, 20f);
    //public Vector3 aimDifference;
    //public bool headAimSet = false;
    private Animator animator;
    public Demo replay;
    public DemoEntity playerInfo;
    public int currentTeam = -1;
    public Vector2 rawDirection = Vector2.zero;
    public float maxAimX = 5f, maxAimY = 15f;
    private AimIK aimIK;
    public Vector2 aimDirection = Vector2.zero;
    public Vector3 aimForward = Vector3.zero;
    public Vector3 horizontalVelocity = Vector3.zero, verticalVelocity = Vector3.zero;
    public float movementDirection = 0;
    public string origWeaponString = "", weaponClass = "", weaponElement = "";
    public GameObject weapon, aimIKTarget;
    
    private bool invisible;
	
	void FixedUpdate ()
    {
        if (replay != null && playerInfo != null && replay.seekIndex > -1)
        {
            name = playerInfo.statsInTick[replay.seekIndex].name;
            steamID = playerInfo.statsInTick[replay.seekIndex].steamID;
            entityID = playerInfo.statsInTick[replay.seekIndex].entityID;
            if(animator == null) animator = GetComponent<Animator>();
            AddAimIK();


            UpdateBodyTransforms();

            UpdateWeapon();

            ConfigureAimIK();

            UpdateAim();

            UpdateTransform();

            UpdateVelocity();

            UpdateAnimator();

            DrawDebugStuff();

            UpdateVisibility();
            UpdateGameObject();
        }
	}

    void OnDestroy()
    {
        DestroyImmediate(aimIKTarget);
    }

    private void DrawDebugStuff()
    {
        if (playerInfo.statsInTick[replay.seekIndex].isAlive)
        {
            Debug.DrawRay(transform.position + (Vector3.up * 10f), horizontalVelocity, Color.blue); //Movement Direction
            Debug.DrawRay(transform.position + (transform.forward * aimPosition.z) + (transform.up * aimPosition.y), aimForward * 1000f, Color.green); //Aim Direction
        }
    }
    private void UpdateBodyTransforms()
    {
        if (animator != null)
        {
            //if (headPosition == null)
            //{
            //    headPosition = animator.GetBoneTransform(HumanBodyBones.Head).FindChild("CamPosition");
            //}
            if(chestPosition == null) chestPosition = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (weaponRHPosition == null)
            {
                weaponRHPosition = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (weaponRHPosition != null && weaponRHPosition.FindChild("weapon_hand_R") != null) weaponRHPosition = weaponRHPosition.FindChild("weapon_hand_R");
                Debug.Log("Right Hand of " + name + " is " + weaponRHPosition.name);
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
        aimForward = Quaternion.Euler(aimDirection.y - 90f, aimDirection.x, 0) * Vector3.forward;
        
        if (aimIKTarget != null) aimIKTarget.transform.position = transform.position + (transform.forward * aimPosition.z) + (transform.up * aimPosition.y) + (aimForward * 100);
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
                weapon = Instantiate(prefabWeapon) as GameObject;
                
                if (weapon != null)
                {
                    if (weapon.transform.parent != weaponRHPosition)
                    {
                        Transform weaponHandPlacement = FindChildIn(weapon.transform, "weapon_hand_R", System.StringComparison.InvariantCultureIgnoreCase);
                        if (weaponHandPlacement != null)
                        {
                            Transform handPlacementParent = weaponHandPlacement.parent;

                            weaponHandPlacement.parent = weaponRHPosition;
                            weapon.transform.parent = weaponHandPlacement;

                            //weapon.transform.localPosition = Vector3.zero;
                            //weapon.transform.localRotation = Quaternion.Euler(Vector3.zero);

                            weaponHandPlacement.localPosition = Vector3.zero;
                            weaponHandPlacement.localRotation = Quaternion.Euler(Vector3.zero);
                            
                            weapon.transform.parent = weaponRHPosition;
                            weaponHandPlacement.parent = handPlacementParent;
                        }
                        else
                        {
                            weapon.transform.parent = weaponRHPosition;
                            weapon.transform.localPosition = Vector3.zero;
                            weapon.transform.rotation = Quaternion.Euler(Vector3.zero);
                        }
                    }
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
            if (replay.play && animator.speed != 1) animator.speed = 1;
            else if (!replay.play && animator.speed != 0) animator.speed = 0;

            animator.SetFloat("MovementDirection", movementDirection);

            if (horizontalVelocity.magnitude > 0) animator.SetBool("Walk", true);
            else animator.SetBool("Walk", false);

            if (horizontalVelocity.magnitude > 150) animator.SetBool("Run", true);
            else animator.SetBool("Run", false);

            if (verticalVelocity.magnitude > 0) animator.SetBool("Jump", true);
            else animator.SetBool("Jump", false);

            animator.SetBool("Crouch", playerInfo.statsInTick[replay.seekIndex].isDucking);
            //if(playerInfo.statsInTick[replay.seekIndex].activeWeapon != null) animator.SetInteger("EquipmentClass", (int) playerInfo.statsInTick[replay.seekIndex].activeWeapon.equipmentClass);
            //animator.SetFloat("AimX", (aimDirection.x - transform.rotation.eulerAngles.y) / maxAimX);
            //animator.SetFloat("AimY", (90f - aimDirection.y) / maxAimY);
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
            Material teamMaterial = new Material(ApplicationPreferences.playerMaterial);
            //currentTeam = playerInfo.statsInTick[replay.seekIndex].teamID;
            if (playerInfo.statsInTick[replay.seekIndex].teamID == replay.demoTicks[replay.seekIndex].ctID)
            {
                //GameObject[] ct = Resources.LoadAll<GameObject>("Prefabs/CT");
                //newAppearance = GameObject.Instantiate(ct[Random.Range(0, ct.Length)]) as GameObject;
                newAppearance = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Faceless")) as GameObject;
                teamMaterial.color = ApplicationPreferences.ctColor;
                Renderer facelessRenderer = newAppearance.GetComponentInChildren<Renderer>();
                if (facelessRenderer != null) facelessRenderer.material = teamMaterial;
                newAppearance.GetComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animation/AnimationControllers/CustomHumanoidController");
                newAppearance.GetComponent<Animator>().applyRootMotion = false;
            }
            else if (playerInfo.statsInTick[replay.seekIndex].teamID == replay.demoTicks[replay.seekIndex].tID)
            {
                //GameObject[] t = Resources.LoadAll<GameObject>("Prefabs/T");
                //newAppearance = GameObject.Instantiate(t[Random.Range(0, t.Length)]) as GameObject;
                newAppearance = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Faceless")) as GameObject;
                teamMaterial.color = ApplicationPreferences.tColor;
                Renderer facelessRenderer = newAppearance.GetComponentInChildren<Renderer>();
                if (facelessRenderer != null) facelessRenderer.material = teamMaterial;
                newAppearance.GetComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animation/AnimationControllers/CustomHumanoidController");
                newAppearance.GetComponent<Animator>().applyRootMotion = false;
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

    private void AddAimIK()
    {
        if(aimIK == null)
        {
            if(animator != null)
            {
                aimIK = gameObject.AddComponent<AimIK>();
                Debug.Log("Added aim IK to " + name);
                aimIK.solver.poleAxis = Vector3.right;
                aimIK.solver.poleWeight = 1;

                //aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightShoulder));
                aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
                aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
                aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightHand));

                if(aimIK.solver.bones.Length > 0) aimIK.solver.bones[0].weight = 0;

                aimIKTarget = new GameObject(name + " AimIK");
                aimIK.solver.target = aimIKTarget.transform;
            }
        }
    }
    private void ConfigureAimIK()
    {
        if (aimIK != null)
        {
            if (weapon != null)
            {
                Transform aim = FindChildIn(weapon.transform, "Aim", System.StringComparison.InvariantCultureIgnoreCase);
                Transform pole = FindChildIn(weapon.transform, "Pole", System.StringComparison.InvariantCultureIgnoreCase);

                if (aim != null) aimIK.solver.transform = aim;
                if (pole != null) aimIK.solver.poleTarget = pole;
            }
            else
            {
                aimIK.solver.IKPositionWeight = 0;
            }
        }
    }

    public T FindComponentIn<T>(GameObject go)
    {
        T theComponent = default(T);
        theComponent = go.GetComponent<T>();
        if (theComponent == null)
        {
            foreach (Transform t in go.transform)
            {
                theComponent = t.GetComponent<T>();
                if (theComponent != null) break;
            }
        }
        return theComponent;
    }
    public static Transform FindChildIn(Transform t, string searchName, System.StringComparison comparisonType)
    {
        if (t.name.Equals(searchName, comparisonType)) return t;

        foreach(Transform child in t)
        {
            Transform deeper = null;
            if (child.name.Equals(searchName, comparisonType)) return child;
            else deeper = FindChildIn(child, searchName, comparisonType);
            if (deeper != null) return deeper;
        }

        return null;
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
        string weaponsLocation = "Prefabs/CSGOWorldModels/", weaponFileName = ModelFileName(teamID, ctID, tID, weaponElement);
        GameObject loadedModel = null;

        if (weaponFileName != null) loadedModel = Resources.Load<GameObject>(weaponsLocation + "w_" + weaponFileName);
        if (loadedModel != null) return loadedModel;

        #region Defaults
        if(weaponClass == EquipmentClass.Pistol)
        {
            return Resources.Load<GameObject>(weaponsLocation + "w_" + ModelFileName(teamID, ctID, tID, EquipmentElement.USP));
        }
        else if(weaponClass == EquipmentClass.SMG)
        {
            return Resources.Load<GameObject>(weaponsLocation + "w_" + ModelFileName(teamID, ctID, tID, EquipmentElement.P90));
        }
        else if (weaponClass == EquipmentClass.Rifle)
        {
            return Resources.Load<GameObject>(weaponsLocation + "w_" + ModelFileName(teamID, ctID, tID, EquipmentElement.M4A1));
        }
        else if (weaponClass == EquipmentClass.Heavy)
        {
            return Resources.Load<GameObject>(weaponsLocation + "w_" + ModelFileName(teamID, ctID, tID, EquipmentElement.Negev));
        }
        else if (weaponClass == EquipmentClass.Grenade)
        {
            return Resources.Load<GameObject>(weaponsLocation + "w_" + ModelFileName(teamID, ctID, tID, EquipmentElement.HE));
        }
        else if (weaponClass == EquipmentClass.Equipment || weaponClass == EquipmentClass.Unknown)
        {
            return Resources.Load<GameObject>(weaponsLocation + "w_" + ModelFileName(teamID, ctID, tID, EquipmentElement.Knife));
        }
        #endregion

        return null;
    }
    public static string ModelFileName(int teamID, int ctID, int tID, EquipmentElement weaponElement)
    {
        if (weaponElement == EquipmentElement.AK47) return "rif_ak47";
        if (weaponElement == EquipmentElement.AUG) return "rif_aug";
        if (weaponElement == EquipmentElement.AWP) return "snip_awp";
        if (weaponElement == EquipmentElement.Bizon) return "smg_bizon";
        if (weaponElement == EquipmentElement.Bomb) return "ied";
        if (weaponElement == EquipmentElement.CZ) return "pist_cz_75";
        if (weaponElement == EquipmentElement.Deagle) return "pist_deagle";
        if (weaponElement == EquipmentElement.Decoy) return "eq_decoy";
        //if (weaponElement == EquipmentElement.DefuseKit) ;
        if (weaponElement == EquipmentElement.DualBarettas) return "pist_elite";
        if (weaponElement == EquipmentElement.Famas) return "rif_famas";
        if (weaponElement == EquipmentElement.FiveSeven) return "pist_fiveseven";
        if (weaponElement == EquipmentElement.Flash) return "eq_flashbang";
        if (weaponElement == EquipmentElement.G3SG1) return "snip_g3sg1";
        if (weaponElement == EquipmentElement.Gallil) return "rif_galilar";
        if (weaponElement == EquipmentElement.Glock) return "pist_glock18";
        if (weaponElement == EquipmentElement.HE) return "eq_fraggrenade";
        //if (weaponElement == EquipmentElement.Helmet) ;
        if (weaponElement == EquipmentElement.Incendiary) return "eq_incendiarygrenade";
        //if (weaponElement == EquipmentElement.Kevlar) ;
        if (weaponElement == EquipmentElement.Knife)
        {
            if (teamID == ctID) return "knife_default_ct";
            if (teamID == tID) return "knife_default_t";
        }
        if (weaponElement == EquipmentElement.M249) return "mach_m249para";
        if (weaponElement == EquipmentElement.M4A1) return "rif_m4a1_s";
        if (weaponElement == EquipmentElement.M4A4) return "rif_m4a1";
        if (weaponElement == EquipmentElement.Mac10) return "smg_mac10";
        if (weaponElement == EquipmentElement.Molotov) return "eq_molotov";
        if (weaponElement == EquipmentElement.MP7) return "smg_mp7";
        if (weaponElement == EquipmentElement.MP9) return "smg_mp9";
        if (weaponElement == EquipmentElement.Negev) return "mach_negev";
        if (weaponElement == EquipmentElement.Nova) return "shot_nova";
        if (weaponElement == EquipmentElement.P2000) return "pist_hkp2000";
        if (weaponElement == EquipmentElement.P250) return "pist_p250";
        if (weaponElement == EquipmentElement.P90) return "smg_p90";
        if (weaponElement == EquipmentElement.SawedOff) return "shot_sawedoff";
        if (weaponElement == EquipmentElement.Scar20) return "snip_scar20";
        if (weaponElement == EquipmentElement.Scout) return "snip_ssg08";
        if (weaponElement == EquipmentElement.SG556) return "rif_sg556";
        if (weaponElement == EquipmentElement.Smoke) return "eq_smokegrenade";
        if (weaponElement == EquipmentElement.Swag7) return "shot_mag7";
        if (weaponElement == EquipmentElement.Tec9) return "pist_tec9";
        if (weaponElement == EquipmentElement.UMP) return "smg_ump45";
        //if (weaponElement == EquipmentElement.Unknown) ;
        if (weaponElement == EquipmentElement.USP) return "pist_223";
        //if (weaponElement == EquipmentElement.World) return Resources.Load<GameObject>(weaponsLocation + "knife_default_ct");
        if (weaponElement == EquipmentElement.XM1014) return "shot_xm1014";
        if (weaponElement == EquipmentElement.Zeus) return "eq_taser";

        return null;
    }
}
