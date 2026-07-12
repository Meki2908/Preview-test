#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public static class GameAudioMixerSetup
{
    private const string MixerPath = GameAudioMixerConfig.MixerAssetPath;
    private const string ConfigPath = "Assets/Resources/GameAudioMixerConfig.asset";

    [MenuItem("Tools/Audio/Setup Game Audio Mixer")]
    public static void Setup()
    {
        SetupInternal(showDialog: true);
    }

    internal static void SetupSilent()
    {
        SetupInternal(showDialog: false);
    }

    private static void SetupInternal(bool showDialog)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Game Audio Mixer Setup ===");

        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            string message = $"Không tìm thấy mixer:\n{MixerPath}";
            Debug.LogWarning(message);
            if (showDialog)
                EditorUtility.DisplayDialog("Audio Mixer Setup", message, "OK");
            return;
        }

        EnsureResourcesFolder(report);
        EnsureConfigAsset(mixer, report);
        ExposeParameters(mixer, report);
        VerifyParameters(mixer, report);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(report.ToString());
        if (showDialog)
            EditorUtility.DisplayDialog("Audio Mixer Setup", report.ToString(), "OK");
    }

    private static void EnsureResourcesFolder(System.Text.StringBuilder report)
    {
        if (AssetDatabase.IsValidFolder("Assets/Resources"))
            return;

        AssetDatabase.CreateFolder("Assets", "Resources");
        report.AppendLine("Created Assets/Resources");
    }

    private static void EnsureConfigAsset(AudioMixer mixer, System.Text.StringBuilder report)
    {
        GameAudioMixerConfig config = AssetDatabase.LoadAssetAtPath<GameAudioMixerConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GameAudioMixerConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            report.AppendLine($"Created {ConfigPath}");
        }

        if (config.mixer != mixer)
        {
            config.mixer = mixer;
            EditorUtility.SetDirty(config);
            report.AppendLine("Assigned mixer to GameAudioMixerConfig");
        }
        else
        {
            report.AppendLine("GameAudioMixerConfig: OK");
        }
    }

    private static void ExposeParameters(AudioMixer mixer, System.Text.StringBuilder report)
    {
        object controller = AssetDatabase.LoadAllAssetsAtPath(MixerPath)
            .FirstOrDefault(asset => asset != null && asset.GetType().Name == "AudioMixerController");

        if (controller == null)
        {
            report.AppendLine("WARNING: Không tìm thấy AudioMixerController sub-asset.");
            return;
        }

        bool musicExposed = ExposeGroupAttenuation(
            controller,
            GameAudioMixerConfig.MusicGroupName,
            GameAudioMixerConfig.MusicVolumeParameter,
            report);
        bool sfxExposed = ExposeGroupAttenuation(
            controller,
            GameAudioMixerConfig.SfxGroupName,
            GameAudioMixerConfig.SfxVolumeParameter,
            report);

        if (musicExposed && sfxExposed)
            report.AppendLine("Exposed MusicVolume + SFXVolume");
    }

    private static bool ExposeGroupAttenuation(
        object controller,
        string groupName,
        string exposedName,
        System.Text.StringBuilder report)
    {
        object group = FindGroupController(controller, groupName);
        if (group == null)
        {
            report.AppendLine($"WARNING: Không tìm thấy group '{groupName}'");
            return false;
        }

        object attenuationParameter = FindAttenuationParameter(group);
        if (attenuationParameter == null)
        {
            report.AppendLine($"WARNING: Group '{groupName}' không có Attenuation parameter");
            return false;
        }

        RemoveExposedParameter(controller, exposedName);
        bool added = AddExposedParameter(controller, attenuationParameter, exposedName);
        if (added)
            report.AppendLine($"  Exposed {exposedName} <- {groupName}/Attenuation");

        EditorUtility.SetDirty(controller as UnityEngine.Object);
        return added;
    }

    private static object FindGroupController(object controller, string groupName)
    {
        Type controllerType = controller.GetType();
        MethodInfo getGroups = controllerType.GetMethod(
            "GetCurrentViewGroups",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (getGroups == null)
            return null;

        Array groups = getGroups.Invoke(controller, null) as Array;
        if (groups == null)
            return null;

        foreach (object group in groups)
        {
            if (group == null)
                continue;

            PropertyInfo nameProperty = group.GetType().GetProperty(
                "name",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            string name = nameProperty?.GetValue(group) as string;
            if (name == groupName)
                return group;
        }

        return null;
    }

    private static object FindAttenuationParameter(object group)
    {
        PropertyInfo effectsProperty = group.GetType().GetProperty(
            "effects",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Array effects = effectsProperty?.GetValue(group) as Array;
        if (effects == null)
            return null;

        foreach (object effect in effects)
        {
            if (effect == null)
                continue;

            PropertyInfo effectNameProperty = effect.GetType().GetProperty(
                "effectName",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            string effectName = effectNameProperty?.GetValue(effect) as string;
            if (effectName != "Attenuation")
                continue;

            MethodInfo getParam = effect.GetType().GetMethod(
                "GetParameter",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null);
            return getParam?.Invoke(effect, new object[] { "Attenuation" });
        }

        return null;
    }

    private static void RemoveExposedParameter(object controller, string exposedName)
    {
        PropertyInfo exposedProperty = controller.GetType().GetProperty(
            "exposedParameters",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (exposedProperty?.GetValue(controller) is not System.Collections.IList exposedList)
            return;

        for (int i = exposedList.Count - 1; i >= 0; i--)
        {
            object exposed = exposedList[i];
            PropertyInfo nameProperty = exposed.GetType().GetProperty(
                "name",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            string name = nameProperty?.GetValue(exposed) as string;
            if (name == exposedName)
                exposedList.RemoveAt(i);
        }
    }

    private static bool AddExposedParameter(object controller, object parameter, string exposedName)
    {
        MethodInfo addMethod = controller.GetType().GetMethod(
            "AddExposedParameter",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (addMethod == null)
            return false;

        addMethod.Invoke(controller, new[] { parameter, exposedName });
        return true;
    }

    private static void VerifyParameters(AudioMixer mixer, System.Text.StringBuilder report)
    {
        bool musicOk = mixer.GetFloat(GameAudioMixerConfig.MusicVolumeParameter, out _);
        bool sfxOk = mixer.GetFloat(GameAudioMixerConfig.SfxVolumeParameter, out _);
        report.AppendLine($"MusicVolume readable: {musicOk}");
        report.AppendLine($"SFXVolume readable: {sfxOk}");

        AudioMixerGroup[] musicGroups = mixer.FindMatchingGroups(GameAudioMixerConfig.MusicGroupName);
        AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups(GameAudioMixerConfig.SfxGroupName);
        report.AppendLine($"Music groups: {musicGroups.Length}");
        report.AppendLine($"SFX groups: {sfxGroups.Length}");
    }
}

[InitializeOnLoad]
internal static class GameAudioMixerSetupAuto
{
    static GameAudioMixerSetupAuto()
    {
        EditorApplication.delayCall += TryAutoSetup;
    }

    private static void TryAutoSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(GameAudioMixerConfig.MixerAssetPath);
        if (mixer == null)
            return;

        bool musicReady = mixer.GetFloat(GameAudioMixerConfig.MusicVolumeParameter, out _);
        bool sfxReady = mixer.GetFloat(GameAudioMixerConfig.SfxVolumeParameter, out _);
        if (musicReady && sfxReady)
            return;

        GameAudioMixerSetup.SetupSilent();
    }
}
#endif
