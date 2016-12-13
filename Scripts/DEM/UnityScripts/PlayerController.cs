using UnityEngine;
using ProjectDeagle;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public DemoController demoController;

    public GameObject physicalLook;

    public int playerKey;
    public PlayerInfo playerInfo;
    public PlayerResource playerResource;
	
	void Update ()
    {
        UpdatePlayerResource();
        if (playerResource != null)
        {
            UpdateSelf();
        }
	}

    private void UpdateSelf()
    {
        UpdatePhysicalLook();
        transform.position = playerResource.position;
        name = playerInfo.name;
    }
    private void UpdatePhysicalLook()
    {
        if (physicalLook == null)
        {
            ApplicationPreferences.UpdateVPKParser();
            SourceModel loadedModel = SourceModel.GrabModel(playerResource.model);
            physicalLook = loadedModel.InstantiateGameObject();
            physicalLook.transform.parent = transform;
            //physicalLook = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //physicalLook.transform.parent = transform;
            //physicalLook.transform.localScale = new Vector3(100, 100, 100);
        }
    }

    private void UpdatePlayerResource()
    {
        playerResource = demoController.demo.GetPlayerResource(demoController.tickIndex, playerInfo.entityID);
        //if (playerResource == null) RemoveSelf();
    }
    private void RemoveSelf()
    {
        Debug.Log("Destroy");
        demoController.players.Remove(playerKey);
        Destroy(gameObject);
    }
}
