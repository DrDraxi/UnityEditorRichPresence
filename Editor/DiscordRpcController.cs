using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[System.Serializable]
public class DiscordJoinEvent : UnityEngine.Events.UnityEvent<string> { }

[System.Serializable]
public class DiscordSpectateEvent : UnityEngine.Events.UnityEvent<string> { }

[System.Serializable]
public class DiscordJoinRequestEvent : UnityEngine.Events.UnityEvent<DiscordRpc.DiscordUser> { }

public class DiscordRpcController
{
    const string appID = "437403081144139776";


    DiscordRpc.EventHandlers handlers;

    public void Init()
    {
        Debug.Log("Initializing Discord Rich Presence");
        handlers = new DiscordRpc.EventHandlers();

        DiscordRpc.Initialize(appID, ref handlers, true, "");
    }

    public void Shutdown()
    {
        Debug.Log("Shutting down Discord Rich Presence");
        DiscordRpc.Shutdown();
    }
}

public static class DateTimeUtil
{
    public static double ConvertToUnixTimestamp(DateTime date)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan diff = date.ToUniversalTime() - origin;
        return Math.Floor(diff.TotalSeconds);
    }
}

class RichPresenceWindow : EditorWindow
{
    [MenuItem("Window/Discord Rich Presence")]
    public static void ShowWindow()
    {
        GetWindow(typeof(RichPresenceWindow));
    }

    DiscordRpc.RichPresence presence = new DiscordRpc.RichPresence();
    DiscordRpcController controller;

    int updateInterval = 15;
    bool updating = false;

    bool showProductName = true;
    bool showSceneName = true;

    DateTime lastUpdate;

    string lastScene = "";

    private void Awake()
    {
        controller = new DiscordRpcController();

        lastUpdate = DateTime.Now;

        presence.largeImageKey = "editor";
        presence.largeImageText = "Unity Editor";

        switch (EditorSettings.defaultBehaviorMode)
        {
            case EditorBehaviorMode.Mode2D:
                presence.smallImageKey = "2d";
                presence.smallImageText = "2D Project";
                break;
            case EditorBehaviorMode.Mode3D:
                presence.smallImageKey = "3d";
                presence.smallImageText = "3D Project";
                break;
        }
    }

    private void OnDestroy()
    {
        if (controller != null)
            controller.Shutdown();
    }

    private void Update()
    {
        if (controller == null)
            Close();

        if (updating)
        {
            DateTime now = DateTime.Now;

            TimeSpan dif = now - lastUpdate;

            if (dif.TotalSeconds > updateInterval)
            {
                presence.details = showProductName ? PlayerSettings.productName : "";
                presence.state = showSceneName ? EditorSceneManager.GetActiveScene().name : "";

                if (lastScene != EditorSceneManager.GetActiveScene().name)
                {
                    presence.startTimestamp = (long)DateTimeUtil.ConvertToUnixTimestamp(DateTime.Now);
                    lastScene = EditorSceneManager.GetActiveScene().name;
                }

                DiscordRpc.UpdatePresence(presence);

                lastUpdate = now;
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Discord Rich Presence", EditorStyles.boldLabel);

        //Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Initialize"))
        {
            controller.Init();

            lastUpdate = DateTime.Now;
            updating = true;
        }

        if (GUILayout.Button("Shutdown"))
        {
            updating = false;
            controller.Shutdown();
        }
        EditorGUILayout.EndHorizontal();

        //Update Interval
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Update Interval");
        updateInterval = EditorGUILayout.IntSlider(updateInterval, 15, 300);
        EditorGUILayout.EndHorizontal();


        GUILayout.Label("Running: " + updating);
        GUILayout.Label("Time till update: " + (updating ? (int)(updateInterval - (DateTime.Now - lastUpdate).TotalSeconds) + " seconds" : "Not running"));

        //Product name
        showProductName = GUILayout.Toggle(showProductName, "Show product name");
        if (showProductName)
            GUILayout.Label("Product name: " + PlayerSettings.productName);

        //Current scene
        showSceneName = GUILayout.Toggle(showSceneName, "Show current scene");
        if (showSceneName)
            GUILayout.Label("Current scene: " + EditorSceneManager.GetActiveScene().name);
    }
}
