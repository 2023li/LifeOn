using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BuildingView : MonoBehaviour
{
    [SerializeField, LabelText("动画控制器")]
    private Animator _animator;

    [SerializeField, LabelText("粒子父节点")]
    private Transform _particleRoot;

    [SerializeField, LabelText("子预制体父节点")]
    private Transform _childRoot;

    private readonly List<BuildingLevelViewConfig> _levelConfigs = new();

    private GameObject _persistentParticleInstance;
    private GameObject _childInstance;

    public void ConfigureLevels(IReadOnlyList<BuildingLevelViewConfig> configs)
    {
        _levelConfigs.Clear();
        if (configs == null)
        {
            return;
        }

        for (int i = 0; i < configs.Count; i++)
        {
            _levelConfigs.Add(configs[i]);
        }
    }

    public void ApplyLevelState(int levelIndex)
    {
        BuildingLevelViewConfig config = GetConfig(levelIndex);
        if (config == null)
        {
            ClearPersistentVisuals();
            return;
        }

        ApplyAnimatorDefaults(config);
        RefreshChildPrefab(config);
        RefreshPersistentParticle(config);
    }

    public void PlayUpgrade(int fromLevel, int toLevel)
    {
        BuildingLevelViewConfig config = GetConfig(toLevel);
        if (config == null)
        {
            return;
        }

        if (_animator != null && !string.IsNullOrEmpty(config.UpgradeTrigger))
        {
            _animator.SetTrigger(config.UpgradeTrigger);
        }

        if (config.UpgradeEffectPrefab != null)
        {
            Transform parent = _particleRoot != null ? _particleRoot : transform;
            GameObject effect = Instantiate(config.UpgradeEffectPrefab, parent);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.identity;
            effect.transform.localScale = Vector3.one;
        }
    }

    private BuildingLevelViewConfig GetConfig(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= _levelConfigs.Count)
        {
            return null;
        }

        return _levelConfigs[levelIndex];
    }

    private void ApplyAnimatorDefaults(BuildingLevelViewConfig config)
    {
        if (_animator == null || config == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(config.DefaultAnimatorTrigger))
        {
            _animator.SetTrigger(config.DefaultAnimatorTrigger);
        }

        if (!string.IsNullOrEmpty(config.DefaultAnimatorState))
        {
            _animator.Play(config.DefaultAnimatorState);
        }
    }

    private void RefreshChildPrefab(BuildingLevelViewConfig config)
    {
        if (_childInstance != null)
        {
            Destroy(_childInstance);
            _childInstance = null;
        }

        if (config == null || config.ChildPrefab == null)
        {
            return;
        }

        Transform parent = _childRoot != null ? _childRoot : transform;
        _childInstance = Instantiate(config.ChildPrefab, parent);
        _childInstance.transform.localPosition = Vector3.zero;
        _childInstance.transform.localRotation = Quaternion.identity;
        _childInstance.transform.localScale = Vector3.one;
    }

    private void RefreshPersistentParticle(BuildingLevelViewConfig config)
    {
        if (_persistentParticleInstance != null)
        {
            Destroy(_persistentParticleInstance);
            _persistentParticleInstance = null;
        }

        if (config == null || config.PersistentParticlePrefab == null)
        {
            return;
        }

        Transform parent = _particleRoot != null ? _particleRoot : transform;
        _persistentParticleInstance = Instantiate(config.PersistentParticlePrefab, parent);
        _persistentParticleInstance.transform.localPosition = Vector3.zero;
        _persistentParticleInstance.transform.localRotation = Quaternion.identity;
        _persistentParticleInstance.transform.localScale = Vector3.one;
    }

    private void ClearPersistentVisuals()
    {
        if (_childInstance != null)
        {
            Destroy(_childInstance);
            _childInstance = null;
        }

        if (_persistentParticleInstance != null)
        {
            Destroy(_persistentParticleInstance);
            _persistentParticleInstance = null;
        }
    }
}