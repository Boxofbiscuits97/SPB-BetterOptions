using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetterOptions;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;

        Harmony.CreateAndPatchAll(typeof(OptionsScreenApplyGraphicsPatch));
        Harmony.CreateAndPatchAll(typeof(OptionsScreenStartPatch));

        SceneManager.sceneLoaded += MainMenuLoaded;

        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private void MainMenuLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "StartScene") return;

        if (PlayerPrefs.HasKey("VsyncEnabled"))
        {
            int vSyncCount = PlayerPrefs.GetInt("VsyncEnabled");
            bool vSyncEnabled = vSyncCount == 1;
            QualitySettings.vSyncCount = vSyncCount;
            Log.LogInfo($"VSync enabled: {vSyncEnabled}");
        }

        if (PlayerPrefs.HasKey("FullscreenEnabled"))
        {   
            int fullscreenCount = PlayerPrefs.GetInt("FullscreenEnabled");
            bool fullscreenEnabled = fullscreenCount == 1;
            Screen.fullScreen = fullscreenEnabled;
            Log.LogInfo($"Fullscreen enabled: {fullscreenEnabled}");
        }

        if (PlayerPrefs.HasKey("ResolutionWidth") && PlayerPrefs.HasKey("ResolutionHeight"))
        {
            int width = PlayerPrefs.GetInt("ResolutionWidth");
            int height = PlayerPrefs.GetInt("ResolutionHeight");
            Screen.SetResolution(width, height, Screen.fullScreen);
            Log.LogInfo($"Resolution set to: {width}x{height}");
        }
    }

    public static bool ResolutionExists(List<ResItem> resolutions, ResItem checkResolution)
    {
        foreach (ResItem resItem in resolutions)
        {
            if (resItem.horizontal == checkResolution.horizontal && resItem.vertical == checkResolution.vertical)
            {
                return true;
            }
        }
        return false;
    }
    
    [HarmonyPatch(typeof(OptionsScreen), nameof(OptionsScreen.Start))]
    class OptionsScreenStartPatch
    {
        static void Postfix(OptionsScreen __instance)
        {
            __instance.filteredResolutions.Clear();

            int systemWidth = Display.main.systemWidth;
            int systemHeight = Display.main.systemHeight;
            Log.LogInfo($"Found system resolution: {systemWidth}x{systemHeight}");

			ResItem resItem = new ResItem();
			resItem.horizontal = systemWidth;
			resItem.vertical = systemHeight;
            if (!ResolutionExists(__instance.resolutions, resItem))
            {
                __instance.resolutions.Add(resItem);
                __instance.selectedResolution = __instance.resolutions.Count - 1;
                __instance.UpdateResLabel();
            }

            foreach (ResItem resItem2 in __instance.resolutions)
            {
                if (resItem2.horizontal <= systemWidth && !ResolutionExists(__instance.filteredResolutions, resItem2))
                {
                    __instance.filteredResolutions.Add(resItem2);
                    Log.LogInfo($"Added resolution {resItem2.horizontal}x{resItem2.vertical} to filtered resolutions");
                }
            }
        }
    }

    [HarmonyPatch(typeof(OptionsScreen), nameof(OptionsScreen.ApplyGraphics))]
    class OptionsScreenApplyGraphicsPatch
    {
        static void Postfix(OptionsScreen __instance)
        {
            int vSyncCount = __instance.vsyncTog.isOn ? 1 : 0;
            int fullscreenCount = __instance.fullscreenTog.isOn ? 1 : 0;
            int resolutionWidth = __instance.filteredResolutions[__instance.selectedResolution].horizontal;
            int resolutionHeight = __instance.filteredResolutions[__instance.selectedResolution].vertical;

            Log.LogInfo($"Saving graphics settings to PlayerPrefs: vSync: {vSyncCount}, Fullscreen: {fullscreenCount}, Resolution: {resolutionWidth}x{resolutionHeight}");

            PlayerPrefs.SetInt("VsyncEnabled", vSyncCount);
            PlayerPrefs.SetInt("FullscreenEnabled", fullscreenCount);
            PlayerPrefs.SetInt("ResolutionWidth", resolutionWidth);
            PlayerPrefs.SetInt("ResolutionHeight", resolutionHeight);
        }
    }
}
