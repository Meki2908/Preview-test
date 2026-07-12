using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Load Game audio.mixer, route AudioSource vào group Music/SFX và điều khiển volume qua exposed parameters.
/// </summary>
public sealed class GameAudioMixerService : MonoBehaviour
{
    private static GameAudioMixerService instance;

    private AudioMixer mixer;
    private AudioMixerGroup musicGroup;
    private AudioMixerGroup sfxGroup;
    private bool mixerParametersReady;
    private float nextRouteTime;

    public static GameAudioMixerService Instance => instance;
    public bool IsReady => mixer != null;
    public bool MixerParametersReady => mixerParametersReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInitialized();
    }

    public static void EnsureInitialized()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject("[GAME_AUDIO_MIXER_SERVICE]");
        instance = host.AddComponent<GameAudioMixerService>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        LoadMixer();
    }

    private void Start()
    {
        RouteAllSources();
        ApplySavedVolumes();
    }

    private void Update()
    {
        if (!IsReady || Time.unscaledTime < nextRouteTime)
            return;

        nextRouteTime = Time.unscaledTime + 0.5f;
        RouteAllSources();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RouteAllSources();
        ApplySavedVolumes();
    }

    private void LoadMixer()
    {
        GameAudioMixerConfig config = Resources.Load<GameAudioMixerConfig>(GameAudioMixerConfig.ResourceName);
        if (config != null && config.mixer != null)
            mixer = config.mixer;

#if UNITY_EDITOR
        if (mixer == null)
        {
            mixer = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioMixer>(
                GameAudioMixerConfig.MixerAssetPath);
        }
#endif

        if (mixer == null)
        {
            Debug.LogWarning(
                $"GameAudioMixerService: không tìm thấy mixer. Chạy Tools/Audio/Setup Game Audio Mixer.");
            return;
        }

        musicGroup = FindGroup(GameAudioMixerConfig.MusicGroupName);
        sfxGroup = FindGroup(GameAudioMixerConfig.SfxGroupName);

        if (musicGroup == null || sfxGroup == null)
        {
            Debug.LogWarning(
                "GameAudioMixerService: mixer thiếu group Music hoặc SFX. Kiểm tra Game audio.mixer.");
        }

        ApplySavedVolumes();
        AudioVolumeService.SetSourceScaling(!mixerParametersReady);
    }

    public void ApplySavedVolumes()
    {
        if (!IsReady)
            return;

        bool musicOk = SetMixerVolume(
            GameAudioMixerConfig.MusicVolumeParameter,
            AudioVolumeService.MusicVolume);
        bool sfxOk = SetMixerVolume(
            GameAudioMixerConfig.SfxVolumeParameter,
            AudioVolumeService.SfxVolume);

        mixerParametersReady = musicOk && sfxOk;
        AudioVolumeService.SetSourceScaling(!mixerParametersReady);
    }

    public bool SetMusicVolume(float linearVolume)
    {
        if (!IsReady)
            return false;

        bool ok = SetMixerVolume(GameAudioMixerConfig.MusicVolumeParameter, linearVolume);
        mixerParametersReady = ok && CanReadParameter(GameAudioMixerConfig.SfxVolumeParameter);
        AudioVolumeService.SetSourceScaling(!mixerParametersReady);
        return ok;
    }

    public bool SetSfxVolume(float linearVolume)
    {
        if (!IsReady)
            return false;

        bool ok = SetMixerVolume(GameAudioMixerConfig.SfxVolumeParameter, linearVolume);
        mixerParametersReady = ok && CanReadParameter(GameAudioMixerConfig.MusicVolumeParameter);
        AudioVolumeService.SetSourceScaling(!mixerParametersReady);
        return ok;
    }

    public void RouteAllSources()
    {
        if (!IsReady || musicGroup == null || sfxGroup == null)
            return;

        AudioSource[] sources = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < sources.Length; i++)
        {
            RouteSource(sources[i]);
        }
    }

    public void RouteSource(AudioSource source)
    {
        if (!IsReady || source == null || musicGroup == null || sfxGroup == null)
            return;

        AudioMixerGroup targetGroup = AudioVolumeService.IsMusicSource(source)
            ? musicGroup
            : sfxGroup;

        if (source.outputAudioMixerGroup != targetGroup)
            source.outputAudioMixerGroup = targetGroup;
    }

    private AudioMixerGroup FindGroup(string groupName)
    {
        if (mixer == null)
            return null;

        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && groups[i].name == groupName)
                return groups[i];
        }

        return groups.Length > 0 ? groups[0] : null;
    }

    private bool SetMixerVolume(string parameter, float linearVolume)
    {
        if (mixer == null)
            return false;

        float decibels = linearVolume <= 0.0001f
            ? -80f
            : Mathf.Log10(linearVolume) * 20f;

        return mixer.SetFloat(parameter, decibels);
    }

    private bool CanReadParameter(string parameter)
    {
        return mixer != null && mixer.GetFloat(parameter, out _);
    }
}
