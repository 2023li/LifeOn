using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Moyo.Unity;
public class AssetsManager : MonoSingleton<AssetsManager>
{
    [System.Serializable]
    public class LoadOptions
    {
        public bool AutoRelease = true;
        [LabelText("超时延迟")]
        public int Timeout = 1000;
        public int Priority = 0;
    }

    private Dictionary<string, AsyncOperationHandle> handles;
    private Dictionary<string, int> referenceCount;

    protected override void Initialize()
    {
        handles = new Dictionary<string, AsyncOperationHandle>();
        referenceCount = new Dictionary<string, int>();
    }


    // 异步加载资源
    public async Task<T> LoadAssetAsync<T>(string address, LoadOptions options = null) where T : class
    {
        if (referenceCount.ContainsKey(address))
        {
            referenceCount[address]++;
            return handles[address].Result as T;
        }

        var operation = Addressables.LoadAssetAsync<T>(address);
        handles[address] = operation;
        referenceCount[address] = 1;

        // 设置超时
        var timeoutTask = Task.Delay(options?.Timeout ?? 1000);
        var completedTask = await Task.WhenAny(operation.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Debug.LogError($"加载资源超时: {address}");
            Addressables.Release(operation);
            return null;
        }

        if (operation.Status == AsyncOperationStatus.Succeeded)
        {
            return operation.Result;
        }
        else
        {
            // 输出具体的异常信息
            if (operation.OperationException != null)
            {
                Debug.LogError($"加载资源失败: {address}。错误: {operation.OperationException}");
            }
            else
            {
                Debug.LogError($"加载资源失败: {address}，状态: {operation.Status}");
            }
            // 确保失败时释放句柄并清理字典
            if (handles.ContainsKey(address))
            {
                handles.Remove(address);
                referenceCount.Remove(address);
            }
            Addressables.Release(operation);
            return null;
        }
    }

    // 实例化游戏对象
    public async Task<GameObject> InstantiateAsync(string address, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var operation = Addressables.InstantiateAsync(address, position, rotation, parent);
        handles[address] = operation;

        if (!referenceCount.ContainsKey(address))
            referenceCount[address] = 0;
        referenceCount[address]++;

        return await operation.Task;
    }

    // 释放资源
    public void ReleaseAsset(string address)
    {
        if (!referenceCount.ContainsKey(address)) return;

        referenceCount[address]--;
        if (referenceCount[address] <= 0)
        {
            if (handles.ContainsKey(address))
            {
                Addressables.Release(handles[address]);
                handles.Remove(address);
            }
            referenceCount.Remove(address);
        }
    }

    // 检查资源更新
    public async Task<bool> CheckForUpdates()
    {
        var catalogUpdates = await Addressables.CheckForCatalogUpdates().Task;

        if (catalogUpdates.Count > 0)
        {
            await Addressables.UpdateCatalogs(catalogUpdates).Task;

            // CRIWARE资源需要特殊处理:cite[9]
#if CRIWARE
            CriWare.Assets.CriAddressables.ModifyLocators();
#endif

            return true;
        }
        return false;
    }

    // 预加载资源组
    public async Task PreloadGroup(string groupLabel)
    {
        var size = await Addressables.GetDownloadSizeAsync(groupLabel).Task;
        if (size > 0)
        {
            var downloadOperation = Addressables.DownloadDependenciesAsync(groupLabel);
            await downloadOperation.Task;
        }
    }
}
