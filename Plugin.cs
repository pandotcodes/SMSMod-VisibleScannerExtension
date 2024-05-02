using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MyBox;
using PortableScanner;
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace VisibleScannerExtension
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("PortableScanner")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static Texture2D Scanner { get; private set; }
        public static Texture2D ScanLine { get; private set; }
        public bool DoShowScanner = false;
        public bool DoShowScanLine = false;
        public static ManualLogSource StaticLogger { get; set; }
        public static ConfigEntry<float> ScannerTime { get; set; }
        public static ConfigEntry<bool> ShowScannerForever { get; set; }
        public static ConfigEntry<float> ScanLineTime { get; set; }
        public static ConfigEntry<bool> ShowScanLineForever { get; set; }
        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded! Applying patch...");
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Instance = this;

            Scanner = new Texture2D(1024, 1024);
            Scanner.LoadImage(Properties.Resources.Scanner);
            Logger.LogInfo("Texture Loaded.");

            ScanLine = new Texture2D(1, 1);
            ScanLine.SetPixels([Color.red.WithAlphaSetTo(0.5f)]);
            ScanLine.Apply();

            StaticLogger = Logger;

            ScannerTime = Config.Bind(PluginInfo.PLUGIN_GUID, "Time to show the scanner for", 0.5f, "The amount of seconds until the scanner disappears again.");
            ScanLineTime = Config.Bind(PluginInfo.PLUGIN_GUID, "Time to show the scan line for", 0.5f, "The amount of seconds until the scan line disappears again.");

            ShowScannerForever = Config.Bind(PluginInfo.PLUGIN_GUID, "Show Scanner forever", false, "If this is set to true, the scanner will be shown permanently, from the moment you start the game.");
            ShowScanLineForever = Config.Bind(PluginInfo.PLUGIN_GUID, "Show Scan Line forever", false, "If this is set to true, the scan line will be shown permanently, from the moment you start the game.");
        }
        private void OnGUI()
        {
            if (Singleton<DayCycleManager>.Instance == null) return;
            if (DoShowScanner || ShowScannerForever.Value)
            {
                float xValue = 0.4f;
                float yValue = 0.6f;
                var rect = new Rect(Screen.width * (1 - xValue), Screen.height * (1 - yValue), Screen.width * xValue, Screen.height * yValue);
                GUI.DrawTexture(rect, Scanner);
            }
            if (DoShowScanLine || ShowScanLineForever.Value) {
                float xValue = 0.025f;
                float yValue = 0.005f;
                var rect = new Rect((Screen.width - xValue * Screen.width) / 2, (Screen.height - yValue * Screen.height) / 2, Screen.width * xValue, Screen.height * yValue);
                GUI.DrawTexture(rect, ScanLine);
            }
        }
        public static void ShowScanner()
        {
            Instance.StartCoroutine(Instance.ShowScannerCoroutine());
            Instance.StartCoroutine(Instance.ShowScanLineCoroutine());
        }
        public IEnumerator ShowScannerCoroutine()
        {
            DoShowScanner = true;
            yield return new WaitForSeconds(ScannerTime.Value);
            DoShowScanner = false;
        }
        public IEnumerator ShowScanLineCoroutine()
        {
            DoShowScanLine = true;
            yield return new WaitForSeconds(ScanLineTime.Value);
            DoShowScanLine = false;
        }
    }
    [HarmonyPatch(typeof(PopupMessage), "ShowToast")]
    public static class PopupMessage_ShowToast_Patch
    {
        public static void Prefix(string message)
        {
            Plugin.StaticLogger.LogInfo(message);
            if (!message.StartsWith("Scan failed"))
            {
                Plugin.ShowScanner();
            }
        }
    }
}
