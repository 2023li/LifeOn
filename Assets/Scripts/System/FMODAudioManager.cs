using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using Sirenix.OdinInspector;
using Moyo.Unity;

public class FMODAudioManager : MonoSingleton<FMODAudioManager>
{
    [Header("Bus 路径配置 (需要在FMOD Studio中对应)")]
    // 建议在FMOD Studio中创建对应的Group: Master, Music, SFX, Ambience, Voice
    private const string PATH_MASTER = "bus:/";
    private const string PATH_MUSIC = "bus:/Music";
    private const string PATH_SFX = "bus:/SFX";
    private const string PATH_AMBIENCE = "bus:/Ambience";
    private const string PATH_VOICE = "bus:/Voice";

    // FMOD Bus 实例缓存
    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;
    private Bus ambienceBus;
    private Bus voiceBus;

    [Header("运行时数据 (只读)")]
    [ShowInInspector, ReadOnly] private float currentMasterVol = 1;
    [ShowInInspector, ReadOnly] private float currentMusicVol = 1;
    [ShowInInspector, ReadOnly] private float currentSFXVol = 1;
    [ShowInInspector, ReadOnly] private float currentAmbienceVol = 1;
    [ShowInInspector, ReadOnly] private float currentVoiceVol = 1;

    // 存储BGM和环境音实例
    private EventInstance musicEventInstance;
    private EventInstance ambienceEventInstance;

    protected override void Initialize()
    {
        base.Initialize();
        // 获取 Bus 实例
        //masterBus = RuntimeManager.GetBus(PATH_MASTER);
        //musicBus = RuntimeManager.GetBus(PATH_MUSIC);
        //sfxBus = RuntimeManager.GetBus(PATH_SFX);
        //ambienceBus = RuntimeManager.GetBus(PATH_AMBIENCE);
        //voiceBus = RuntimeManager.GetBus(PATH_VOICE);

        // 加载保存的音量设置 (默认值为 1 或 0.8)
        LoadVolumeSettings();
    }

    private void Start()
    {
        // 确保在Start时再次应用音量，防止FMOD初始化延迟导致设置被覆盖
        ApplyAllVolumes();




        CheckLoadedBanks();

    }
    private void CheckLoadedBanks()
    {
        // 1. 检查 Master Bank 是否被 FMOD 认为已加载
        bool isMasterLoaded = RuntimeManager.HaveMasterBanksLoaded;
        Debug.Log($"[FMOD Check] Master Bank Loaded? {isMasterLoaded}");

        // 2. 获取当前所有已加载 Bank 的列表
        RuntimeManager.StudioSystem.getBankList(out FMOD.Studio.Bank[] loadedBanks);

        Debug.Log($"[FMOD Check] 当前已加载 {loadedBanks.Length} 个 Bank:");
        foreach (var bank in loadedBanks)
        {
            bank.getPath(out string path);
            bank.getLoadingState(out FMOD.Studio.LOADING_STATE state);
            Debug.Log($" - Path: {path} | State: {state}");
        }

        // 3. 专门检查 "UI_换页" 是否存在于这些 Bank 中
        // 注意：如果 Strings Bank 没加载，GetEventDescription 也会失败
        string eventPath = "event:/test";
        FMOD.RESULT result = RuntimeManager.StudioSystem.getEvent(eventPath, out FMOD.Studio.EventDescription eventDesc);

        if (result == FMOD.RESULT.OK)
        {
            Debug.Log($"[FMOD Check] 成功找到事件: {eventPath}");
        }
        else
        {
            Debug.LogError($"[FMOD Check] 无法找到事件 {eventPath}. 错误代码: {result}");
            // 常见错误：ERR_EVENT_NOTFOUND (事件不在已加载的Bank里) 
            // 或 ERR_NET_CONNECT (如果使用了 Live Update)
        }
    }

    #region 音量控制 (供UI调用)

    public void SetMasterVolume(float value)
    {
        currentMasterVol = value;
        masterBus.setVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        currentMusicVol = value;
        musicBus.setVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        currentSFXVol = value;
        sfxBus.setVolume(value);
    }

    public void SetAmbienceVolume(float value)
    {
        currentAmbienceVol = value;
        ambienceBus.setVolume(value);
    }

    public void SetVoiceVolume(float value)
    {
        currentVoiceVol = value;
        voiceBus.setVolume(value);
    }

    #endregion

    #region 数据获取 (供UI初始化显示)

    public float GetMasterVolume() => currentMasterVol;
    public float GetMusicVolume() => currentMusicVol;
    public float GetSFXVolume() => currentSFXVol;
    public float GetAmbienceVolume() => currentAmbienceVol;
    public float GetVoiceVolume() => currentVoiceVol;

    #endregion

    #region 内部逻辑

    private void LoadVolumeSettings()
    {
        currentMasterVol = GetMasterVolume();
        currentMusicVol = GetMusicVolume();
        currentSFXVol = GetSFXVolume();
        currentAmbienceVol = GetAmbienceVolume();
        currentVoiceVol = GetVoiceVolume();

        ApplyAllVolumes();
    }

    private void ApplyAllVolumes()
    {
        masterBus.setVolume(currentMasterVol);
        musicBus.setVolume(currentMusicVol);
        sfxBus.setVolume(currentSFXVol);
        ambienceBus.setVolume(currentAmbienceVol);
        voiceBus.setVolume(currentVoiceVol);
    }

    #endregion

    #region 播放逻辑 (保留你原有的部分)
   
    public void PlayOneShot(EventReference soundReference, Vector3 worldPos = default)
    {
        if (!soundReference.IsNull)
        {
            // 如果 worldPos 是默认值(0,0,0)，通常意味着2D声音，FMOD会自动处理
            RuntimeManager.PlayOneShot(soundReference, worldPos);
        }
    }

    public void InitializeMusic(EventReference musicReference)
    {
        StopMusic(true);
        musicEventInstance = RuntimeManager.CreateInstance(musicReference);
        musicEventInstance.start();
    }

    public void StopMusic(bool allowFadeOut)
    {
        // 检查实例是否有效
        if (musicEventInstance.isValid())
        {
            PLAYBACK_STATE state;
            musicEventInstance.getPlaybackState(out state);
            if (state != PLAYBACK_STATE.STOPPED)
            {
                musicEventInstance.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
                musicEventInstance.release();
            }
        }
    }

    // ... 环境音逻辑同理 ...

    #endregion
}
