using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using Sirenix.OdinInspector;
using Moyo.Unity;
using System;

public class FMODAudioManager : MonoSingleton<FMODAudioManager>
{
    #region 所有的FMOD音效 暂时放在这里
    [SerializeField]
    public EventReference sfx_翻页;
    #endregion


    [Header("设置")]
    [Range(0f, 1f),LabelText("主音量")] public float masterVolume = 1f;
    [Range(0f, 1f),LabelText("音乐音量")] public float musicVolume = 1f;
    [Range(0f, 1f),LabelText("音效音量")] public float sfxVolume = 1f;

    // 存储当前的背景音乐实例，以便我们需要停止或更改它
    private EventInstance musicEventInstance;

    // 存储当前的环境音实例 (Ambience)
    private EventInstance ambienceEventInstance;

 

    private void Start()
    {
        // 初始化时设置音量（如果需要）
        // SetBusVolume("bus:/", masterVolume);
        // SetBusVolume("bus:/Music", musicVolume);
        // SetBusVolume("bus:/SFX", sfxVolume);
    }

    #region One Shot Sounds (SFX)

    /// <summary>
    /// 播放 2D 单次音效 (UI, 全局提示音)
    /// </summary>
    /// <param name="soundReference">FMOD Event Reference</param>
    public void PlayOneShot(EventReference soundReference)
    {
        if (!soundReference.IsNull)
        {
            RuntimeManager.PlayOneShot(soundReference);
        }
    }

    /// <summary>
    /// 播放 3D 单次音效 (爆炸, 脚步声, 枪声) - 指定位置
    /// </summary>
    /// <param name="soundReference">FMOD Event Reference</param>
    /// <param name="worldPos">世界坐标</param>
    public void PlayOneShot(EventReference soundReference, Vector3 worldPos)
    {
        if (!soundReference.IsNull)
        {
            RuntimeManager.PlayOneShot(soundReference, worldPos);
        }
    }

    #endregion

    #region Music & Ambience (Looping)

    /// <summary>
    /// 初始化并播放背景音乐
    /// </summary>
    public void InitializeMusic(EventReference musicReference)
    {
        // 如果当前有音乐在播放，先停止它
        StopMusic(true);

        musicEventInstance = RuntimeManager.CreateInstance(musicReference);
        musicEventInstance.start();
        // 如果你的BGM需要释放内存（通常BGM是在停止时释放），Release会在Stop时处理
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    /// <param name="allowFadeOut">是否允许FMOD Event中设置的淡出效果</param>
    public void StopMusic(bool allowFadeOut)
    {
        PLAYBACK_STATE state;
        musicEventInstance.getPlaybackState(out state);

        if (state != PLAYBACK_STATE.STOPPED)
        {
            musicEventInstance.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicEventInstance.release(); // 重要：释放实例以清理内存
        }
    }

    // 类似的逻辑可以用于环境音 (Ambience)
    public void InitializeAmbience(EventReference ambienceReference)
    {
        StopAmbience(true);
        ambienceEventInstance = RuntimeManager.CreateInstance(ambienceReference);
        ambienceEventInstance.start();
    }

    public void StopAmbience(bool allowFadeOut)
    {
        ambienceEventInstance.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        ambienceEventInstance.release();
    }

    #endregion

    #region Parameters

    /// <summary>
    /// 设置全局参数 (例如：游戏进程，时间，天气)
    /// </summary>
    public void SetGlobalParameter(string parameterName, float value)
    {
        RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
    }

    /// <summary>
    /// 设置特定实例的局部参数 (例如：汽车引擎声的RPM)
    /// 注意：这通常需要在其他脚本中持有EventInstance，这里仅作演示
    /// </summary>
    public void SetInstanceParameter(EventInstance instance, string parameterName, float value)
    {
        instance.setParameterByName(parameterName, value);
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// 设置总线音量 (Master, Music, SFX)
    /// </summary>
    /// <param name="busPath">例如 "bus:/", "bus:/Music", "bus:/SFX"</param>
    /// <param name="volume">0.0 到 1.0</param>
    public void SetBusVolume(string busPath, float volume)
    {
        Bus bus = RuntimeManager.GetBus(busPath);
        bus.setVolume(volume);
    }

    #endregion



    
}

