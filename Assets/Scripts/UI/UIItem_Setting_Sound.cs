using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Moyo.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItem_Setting_Sound : MonoBehaviour
{
    [Header("UI 组件绑定")]
    [SerializeField] private Slider sld_主音量;
    [SerializeField] private TMP_Text txt_主音量;

    [SerializeField] private Slider sld_SFX音量;
    [SerializeField] private TMP_Text txt_SFX音量;

    [SerializeField] private Slider sld_BGM音量;
    [SerializeField] private TMP_Text txt_BGM音量;

    [SerializeField] private Slider sld_环境音量;
    [SerializeField] private TMP_Text txt_环境音量;

    [SerializeField] private Slider sld_语言音量;
    [SerializeField] private TMP_Text txt_语言音量;
    private void Start()
    {
        // 1. 初始化显示：从 Manager 获取当前存储的数值
        RefreshVolumeUI();

        // 2. 绑定事件：当滑块拖动时通知 Manager
        BindEvents();
    }

    /// <summary>
    /// 初始化滑块值和对应文本显示
    /// </summary>
    private void RefreshVolumeUI()
    {
        if (AudioManager.Instance == null) return;

        // 主音量
        if (sld_主音量)
        {
            sld_主音量.value = AudioManager.Instance.GetMasterVolume();
            UpdateVolumeText(sld_主音量, txt_主音量);
        }

        // SFX音量
        if (sld_SFX音量)
        {
            sld_SFX音量.value = AudioManager.Instance.GetSFXVolume();
            UpdateVolumeText(sld_SFX音量, txt_SFX音量);
        }

        // BGM音量
        if (sld_BGM音量)
        {
            sld_BGM音量.value = AudioManager.Instance.GetMusicVolume();
            UpdateVolumeText(sld_BGM音量, txt_BGM音量);
        }

        // 环境音量
        if (sld_环境音量)
        {
            sld_环境音量.value = AudioManager.Instance.GetEnvironmentVolume();
            UpdateVolumeText(sld_环境音量, txt_环境音量);
        }

        // 语言音量
        if (sld_语言音量)
        {
            sld_语言音量.value = AudioManager.Instance.GetVoiceVolume();
            UpdateVolumeText(sld_语言音量, txt_语言音量);
        }
    }

    /// <summary>
    /// 绑定滑块事件
    /// </summary>
    private void BindEvents()
    {
        if (AudioManager.Instance == null) return;

        // 主音量
        if (sld_主音量)
            sld_主音量.onValueChanged.AddListener(val =>
            {
                AudioManager.Instance.SetMasterVolume(val);
                UpdateVolumeText(sld_主音量, txt_主音量);
            });

        // SFX音量
        if (sld_SFX音量)
            sld_SFX音量.onValueChanged.AddListener(val =>
            {
                AudioManager.Instance.SetSFXVolume(val);
                UpdateVolumeText(sld_SFX音量, txt_SFX音量);
            });

        // BGM音量
        if (sld_BGM音量)
            sld_BGM音量.onValueChanged.AddListener(val =>
            {
                AudioManager.Instance.SetMusicVolume(val);
                UpdateVolumeText(sld_BGM音量, txt_BGM音量);
            });

        // 环境音量
        if (sld_环境音量)
            sld_环境音量.onValueChanged.AddListener(val =>
            {
                AudioManager.Instance.SetEnvironmentVolume(val);
                UpdateVolumeText(sld_环境音量, txt_环境音量);
            });

        // 语言音量
        if (sld_语言音量)
            sld_语言音量.onValueChanged.AddListener(val =>
            {
                AudioManager.Instance.SetVoiceVolume(val);
                UpdateVolumeText(sld_语言音量, txt_语言音量);
            });
    }


    /// <summary>
    /// 通用方法：更新音量文本显示（转成百分比，保留0位小数）
    /// </summary>
    /// <param name="slider">音量滑块</param>
    /// <param name="text">显示文本</param>
    private void UpdateVolumeText(Slider slider, TMP_Text text)
    {
        // 空引用保护
        if (slider == null || text == null) return;

        // 将0~1的滑块值转为0~100的百分比，保留0位小数（如需小数可改为"F1"）
        float volumePercent = slider.value * 100;
        text.text = volumePercent.ToString("F0"); // F0 = 无小数位，F1 = 1位小数，依需求调整
    }

  

}
