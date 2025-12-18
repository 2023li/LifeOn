using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Moyo.Unity;
using UnityEngine;
using UnityEngine.UI;

public class UIItem_Setting_Sound : MonoBehaviour
{

    [SerializeField] private EventReference test;

    private void OnEnable()
    {
        FMODAudioManager.Instance.PlayOneShot(test);
    }

    [Header("UI 组件绑定")]
    [SerializeField] private Slider sld_主音量;
    [SerializeField] private Slider sld_SFX音量;
    [SerializeField] private Slider sld_BGM音量;
    [SerializeField] private Slider sld_环境音量;
    [SerializeField] private Slider sld_语言音量;

    private void Start()
    {
        // 1. 初始化显示：从 Manager 获取当前存储的数值
        InitSliderValues();

        // 2. 绑定事件：当滑块拖动时通知 Manager
        BindEvents();
    }

    private void InitSliderValues()
    {
        if (FMODAudioManager.Instance == null) return;

        if (sld_主音量) sld_主音量.value = FMODAudioManager.Instance.GetMasterVolume();
        if (sld_SFX音量) sld_SFX音量.value = FMODAudioManager.Instance.GetSFXVolume();
        if (sld_BGM音量) sld_BGM音量.value = FMODAudioManager.Instance.GetMusicVolume();
        if (sld_环境音量) sld_环境音量.value = FMODAudioManager.Instance.GetAmbienceVolume();
        if (sld_语言音量) sld_语言音量.value = FMODAudioManager.Instance.GetVoiceVolume();
    }

    private void BindEvents()
    {
        if (FMODAudioManager.Instance == null) return;

        // 使用 lambda 表达式将 slider 的 float 值传给 Manager
        if (sld_主音量)
            sld_主音量.onValueChanged.AddListener(val => FMODAudioManager.Instance.SetMasterVolume(val));

        if (sld_SFX音量)
            sld_SFX音量.onValueChanged.AddListener(val => FMODAudioManager.Instance.SetSFXVolume(val));

        if (sld_BGM音量)
            sld_BGM音量.onValueChanged.AddListener(val => FMODAudioManager.Instance.SetMusicVolume(val));

        if (sld_环境音量)
            sld_环境音量.onValueChanged.AddListener(val => FMODAudioManager.Instance.SetAmbienceVolume(val));

        if (sld_语言音量)
            sld_语言音量.onValueChanged.AddListener(val => FMODAudioManager.Instance.SetVoiceVolume(val));
    }
}
