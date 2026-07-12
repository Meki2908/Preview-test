using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lưu volume và áp dụng cho AudioSource kể cả khi project chưa có AudioMixer.
/// AudioSource loop hoặc có tên music/theme/bgm được xem là nhạc; còn lại là SFX.
/// </summary>
public sealed class AudioVolumeService : MonoBehaviour
{
    private const string MusicPrefKey = "Audio.MusicVolume";
    private const string SfxPrefKey = "Audio.SfxVolume";
    private const float RefreshInterval = 0.5f;

    private static AudioVolumeService instance;
    private static float musicVolume = 1f;
    private static float sfxVolume = 1f;
    private static bool sourceScalingEnabled = true;

    private readonly Dictionary<int, SourceState> sources = new Dictionary<int, SourceState>();
    private float nextRefreshTime;
    private bool preferencesDirty;

    public static float MusicVolume => musicVolume;
    public static float SfxVolume => sfxVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInitialized();
    }

    public static void EnsureInitialized()
    {
        if (instance != null)
            return;

        musicVolume = PlayerPrefs.GetFloat(MusicPrefKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxPrefKey, 1f);

        GameObject host = new GameObject("[AUDIO_VOLUME_SERVICE]");
        instance = host.AddComponent<AudioVolumeService>();
        DontDestroyOnLoad(host);
    }

    public static void SetMusicVolume(float value)
    {
        EnsureInitialized();
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicPrefKey, musicVolume);
        instance.preferencesDirty = true;

        GameAudioMixerService.EnsureInitialized();
        if (!GameAudioMixerService.Instance.SetMusicVolume(musicVolume))
            instance.ApplyVolumes();
    }

    public static void SetSfxVolume(float value)
    {
        EnsureInitialized();
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxPrefKey, sfxVolume);
        instance.preferencesDirty = true;

        GameAudioMixerService.EnsureInitialized();
        if (!GameAudioMixerService.Instance.SetSfxVolume(sfxVolume))
            instance.ApplyVolumes();
    }

    public static void SetSourceScaling(bool enabled)
    {
        EnsureInitialized();

        if (sourceScalingEnabled == enabled)
            return;

        sourceScalingEnabled = enabled;
        if (enabled)
            instance.ApplyVolumes();
        else
            instance.RestoreBaseVolumes();
    }

    public static void Save()
    {
        if (instance == null || !instance.preferencesDirty)
            return;

        PlayerPrefs.Save();
        instance.preferencesDirty = false;
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
    }

    private void Start()
    {
        GameAudioMixerService.EnsureInitialized();
        RefreshSources();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + RefreshInterval;
        RefreshSources();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        Save();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameAudioMixerService.EnsureInitialized();
        RefreshSources();
    }

    private void RefreshSources()
    {
        AudioSource[] found = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < found.Length; i++)
        {
            AudioSource source = found[i];
            int id = source.GetInstanceID();

            if (!sources.ContainsKey(id))
            {
                sources.Add(id, new SourceState
                {
                    Source = source,
                    BaseVolume = source.volume,
                    IsMusic = IsMusicSource(source)
                });
            }

            GameAudioMixerService.EnsureInitialized();
            if (GameAudioMixerService.Instance != null)
                GameAudioMixerService.Instance.RouteSource(source);
        }

        ApplyVolumes();
        RemoveDestroyedSources();
    }

    private void ApplyVolumes()
    {
        if (!sourceScalingEnabled)
            return;

        foreach (SourceState state in sources.Values)
        {
            if (state.Source == null)
                continue;

            float categoryVolume = state.IsMusic ? musicVolume : sfxVolume;
            state.Source.volume = state.BaseVolume * categoryVolume;
        }
    }

    private void RestoreBaseVolumes()
    {
        foreach (SourceState state in sources.Values)
        {
            if (state.Source != null)
                state.Source.volume = state.BaseVolume;
        }
    }

    private void RemoveDestroyedSources()
    {
        List<int> destroyedIds = null;

        foreach (KeyValuePair<int, SourceState> pair in sources)
        {
            if (pair.Value.Source != null)
                continue;

            destroyedIds ??= new List<int>();
            destroyedIds.Add(pair.Key);
        }

        if (destroyedIds == null)
            return;

        for (int i = 0; i < destroyedIds.Count; i++)
            sources.Remove(destroyedIds[i]);
    }

    public static bool IsMusicSource(AudioSource source)
    {
        if (source == null)
            return false;

        if (source.loop)
            return true;

        string objectName = source.gameObject.name.ToLowerInvariant();
        if (objectName.Contains("music")
            || objectName.Contains("theme")
            || objectName.Contains("bgm"))
        {
            return true;
        }

        if (source.clip != null)
        {
            string clipName = source.clip.name.ToLowerInvariant();
            if (clipName.Contains("theme") || clipName.Contains("bgm") || clipName.Contains("music"))
                return true;
        }

        Transform parent = source.transform.parent;
        while (parent != null)
        {
            string parentName = parent.name.ToLowerInvariant();
            if (parentName.Contains("music") || parentName.Contains("theme") || parentName.Contains("bgm"))
                return true;

            parent = parent.parent;
        }

        return false;
    }

    private sealed class SourceState
    {
        public AudioSource Source;
        public float BaseVolume;
        public bool IsMusic;
    }
}
