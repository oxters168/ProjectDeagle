using UnityEngine;
using UnityEngine.UI;
using UnitySourceEngine;

using TMPro;

public class SettingsPanel : MonoBehaviour
{
    public Toggle showFrameRateToggle;
    public TMP_InputField frameRateField;
    public Toggle debugLogToggle;
    public TMP_InputField matchesSaveLocField, gameSaveLocField;
    public Toggle autoResourcePerMap, showOverviewToggle, flatTextures;
    public TMP_InputField maxTextureResolution;
    public Slider faceLoadPercentSlider, modelLoadPercentSlider, renderPercentSlider, modelDecimationSlider;


    private void Start()
    {
        RevertSettings();
    }

    public void ApplySettings()
    {
        //if (!SettingsController.gameLocation.Equals(gameSaveLocField.text))
        //    MapsPanel.mapsPanelInScene.ClearList();

        SettingsController.showFrameRate = showFrameRateToggle.isOn;
        SettingsController.showDebugLog = debugLogToggle.isOn;
        SettingsController.matchesLocation = matchesSaveLocField.text;
        SettingsController.gameLocation = gameSaveLocField.text;
        SettingsController.autoResourcePerMap = autoResourcePerMap.isOn;
        SettingsController.showOverview = showOverviewToggle.isOn;
        BSPMap.FaceLoadPercent = faceLoadPercentSlider.value;
        BSPMap.ModelLoadPercent = modelLoadPercentSlider.value;
        SourceTexture.averageTextures = flatTextures.isOn;
        SettingsController.renderPercent = renderPercentSlider.value;
        SourceModel.decimationPercent = modelDecimationSlider.value;

        int frameRate = 30;
        try
        {
            frameRate = System.Convert.ToInt32(frameRateField.text);
        }
        catch
        {
            Debug.LogError("SettingsPanel: Could not convert " + nameof(frameRateField) + " value to number.");
        }
        SettingsController.targetFrameRate = frameRate;

        int maxResSize = SourceTexture.maxTextureSize;
        try
        {
            maxResSize = System.Convert.ToInt32(maxTextureResolution.text);
        }
        catch (System.Exception)
        {
            Debug.LogError("SettingsPanel: Could not convert " + nameof(maxTextureResolution) + " value to number.");
        }
        SourceTexture.maxTextureSize = maxResSize;

        SettingsController.SaveSettings();
    }
    public void RevertSettings()
    {
        showFrameRateToggle.isOn = SettingsController.showFrameRate;
        frameRateField.text = SettingsController.targetFrameRate.ToString();
        debugLogToggle.isOn = SettingsController.showDebugLog;
        matchesSaveLocField.text = SettingsController.matchesLocation;
        gameSaveLocField.text = SettingsController.gameLocation;

        autoResourcePerMap.isOn = SettingsController.autoResourcePerMap;
        showOverviewToggle.isOn = SettingsController.showOverview;
        faceLoadPercentSlider.value = BSPMap.FaceLoadPercent;
        modelLoadPercentSlider.value = BSPMap.ModelLoadPercent;
        flatTextures.isOn = SourceTexture.averageTextures;
        maxTextureResolution.text = SourceTexture.maxTextureSize.ToString();
        renderPercentSlider.value = SettingsController.renderPercent;
        modelDecimationSlider.value = SourceModel.decimationPercent;
    }
    public void RestoreDefaults()
    {
        SettingsController.SetDefaults();
        RevertSettings();
    }
}
