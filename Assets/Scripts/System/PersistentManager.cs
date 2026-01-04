using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moyo;
using Moyo.Unity;
using System;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;

// 确保引用你的命名空间
using static TechTreeManager;





public class PersistentManager : MonoSingleton<PersistentManager>
{
    protected PersistentManager() { }


    public event Action OnDeleteGameSave;

    // --- 数据引用 ---
    [ShowInInspector]
    private AppSaveData currentAppData;

    public AppSaveData CurrentAppData { get { return currentAppData; } }    
    [ShowInInspector]
    private GameSaveData currentGameData;
    public GameSaveData GetCurrentGameSave()
    {
        if (currentGameData == null)
        {
            currentGameData = GameSaveData.CreateNew();
        }
        return currentGameData;
    }

    // --- 路径与常量定义 ---
    private const string AppDataFileName = "AppData.load";
    private const string GameSaveDirName = "GameSaves";

    // 数据文件包含完整的游戏状态
    private const string GameDataKey = "GameData";
    private const string GameFileExtension = ".logd";

    // [新] 元数据文件仅包含列表显示所需的信息
    private const string MetaDataKey = "MetaData";
    private const string MetaFileExtension = ".meta";

    private string GameSaveRootPath => Path.Combine(Application.persistentDataPath, GameSaveDirName);

    #region AppData (全局设置)
    // ... (保持原有的 SaveAppData 和 LoadAppData 不变) ...
    public void SaveAppData()
    {
        if (currentAppData == null) return;
        ES3.Save("currentAppData", currentAppData, AppDataFileName);
    }

    public string GetLastGameSaveId()
    {
        if (currentAppData == null)
        {
            LoadAppData();
        }
        return currentAppData?.lastGameSaveId;
    }
    /// <summary>
    /// [新增] 检查是否有可供“继续游戏”的存档
    /// </summary>
    public bool HasLastGameSave()
    {
        string id = GetLastGameSaveId();
        if (string.IsNullOrEmpty(id)) return false;

        // 进一步检查文件实际是否存在，防止玩家手动删除了文件但 AppData 没更新
        string metaPath = Path.Combine(GameSaveDirName, id + MetaFileExtension);
        string dataPath = Path.Combine(GameSaveDirName, id + GameFileExtension);

        return ES3.FileExists(metaPath) || ES3.FileExists(dataPath);
    }


    public event Action OnAppDataLoad;
    public void LoadAppData()
    {
        if (ES3.FileExists(AppDataFileName))
        {
            currentAppData = ES3.Load<AppSaveData>("currentAppData", AppDataFileName);
        }
        else
        {
            currentAppData = AppSaveData.GetDef();
            SaveAppData();
        }
        OnAppDataLoad?.Invoke();
    }
    #endregion

    #region GameData (游戏存档)

    // CollectCurrentGameSaveData 方法保持你修复后的样子，这里略去不写
    private GameSaveData CollectCurrentGameSaveData(string newName = null,bool newGuid = false)
    {
        if (currentGameData == null)
        {
            Debug.Log("自动创建存档");
            currentGameData = GameSaveData.CreateNew();
        }
        if (newGuid)
        {
            currentGameData.SetNewGuid();
        }
        if (!string.IsNullOrEmpty(newName))
        {
            currentGameData.saveName = newName;
        }


        //收集建筑数据
        List<BuildingInstance.BuildingSaveData> buildingSaveDatas = new();
        //遍历所有的激活的建筑
        foreach (var building in BuildingInstance.ActiveInstances)
        {
            buildingSaveDatas.Add(building.Save());
        }
        currentGameData.allBuildingData = buildingSaveDatas;

        //游戏上下文数据
        currentGameData.turnSystemSaveData = GameContext.Instance.Turn.Save();
        currentGameData.humanResourcesNetworkSaveData = GameContext.Instance.HumanResourcesNetwork.Save();
        currentGameData.techTreeSaveData = GameContext.Instance.TechTree.Save();
        currentGameData.resourceNetworkSaveData = GameContext.Instance.ResourceNetwork.Save();
        currentGameData.connectionManagerSaveData = ConnectionManager.Instance.Save();


        return currentGameData;
    }
    
    public void SaveGame(string newName = null,bool newGuid = false)
    {
        // 假设你已经修复了 CollectCurrentGameSaveData 中的赋值问题
        SaveGame(CollectCurrentGameSaveData(newName,newGuid));
    }

    public void SaveGame(GameSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("试图保存空的 GameSaveData！");
            return;
        }

        if (!Directory.Exists(GameSaveRootPath))
        {
            Directory.CreateDirectory(GameSaveRootPath);
        }

        // 1. 处理 ID 和 时间
        if (string.IsNullOrEmpty(data.saveid))
        {
            data.saveid = System.Guid.NewGuid().ToString();
        }
        data.lastSaveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 2. 保存完整数据 (.logd)
        string contentFileName = $"{data.saveid}{GameFileExtension}";
        string contentPath = Path.Combine(GameSaveDirName, contentFileName);
        ES3.Save(GameDataKey, data, contentPath);

        // 3. [新] 保存元数据 (.meta)
        // 提取元数据
        SaveMetadata meta = new SaveMetadata
        {
            saveid = data.saveid,
            saveName = data.saveName,
            lastSaveDate = data.lastSaveDate,
            versionNumber = data.versionNumber
        };

        string metaFileName = $"{data.saveid}{MetaFileExtension}";
        string metaPath = Path.Combine(GameSaveDirName, metaFileName);
        ES3.Save(MetaDataKey, meta, metaPath);

        currentGameData = data;


        if (currentAppData != null)
        {
            currentAppData.lastGameSaveId = data.saveid;
            SaveAppData(); // 立即保存全局设置，确保记录生效
        }

        Debug.Log($"[PersistentManager] Game saved: {contentPath} & {metaPath}");
    }

    #region 加载
    [Button]
    public void LoadGame(string saveid)
    {
        // 1. 读取存档文件到内存 (currentGameData)
        GameSaveData data = LoadGameData(saveid);
        if (data == null)
        {
            Debug.LogError($"[PersistentManager] 无法加载存档: {saveid}");
            return;
        }

        Debug.Log($"[PersistentManager] 开始加载游戏: {data.saveName} ({data.saveid})");


        // [新增] 既然加载了这个存档，它就变成了“最后一次游玩的存档”
        if (currentAppData != null && currentAppData.lastGameSaveId != saveid)
        {
            currentAppData.lastGameSaveId = saveid;
            SaveAppData();
        }

        // 3. 构建加载上下文，完全接管加载流程
        // 我们不再使用 AppManager.Instance.LoadGameScene()，因为它包含默认的 Init 逻辑
        var loadContext = new AppManager.SceneLoadContext()
        {
            TargetSceneName = LOConstant.SceneName.Game,
            UseTransition = true, // 使用过渡场景
            PreloadAddresses = new List<string>()
            {
                "UIPanel_GameMain" // 预加载游戏主界面资源
            },
            // 【核心修复】：所有的逻辑都在这个回调中顺序执行
            OnComplete = async () =>
            {
                // A. 显示游戏主界面 (但在数据恢复前，它可能显示为空数据，所以其实可以放在后面，或者由 Restore 刷新)
                await UIManager.Instance.ShowPanel<UIPanel_GameMain>(UIManager.UILayer.Main);

                // B. 执行数据恢复 (替代原本的 RestoreGameRoutine)
                // 注意：这里不需要协程等待，因为 OnComplete 触发时，场景已经加载完毕且 Awake/Start 已执行
                RestoreGameState();

                // C. 触发事件
                AppEventArgs.Tiggle(AppEventEnum.场景加载完成);

                Debug.Log("[PersistentManager] 存档加载流程全部结束，Loading 界面已关闭。");
            }
        };


        AppManager.Instance.LoadGameScene(loadContext); // true 表示使用过渡页

        // 3. 开启协程等待场景加载完成，然后恢复数据
    }


    //恢复游戏状态
    private void RestoreGameState()
    {
        Debug.Log("RestoreGameState...开始恢复数据");

        if (currentGameData == null)
        {
            Debug.LogError("[PersistentManager] 数据恢复失败：currentGameData 为空");
            return;
        }

        try
        {
            // 1. 重建建筑
            ReconstructBuildings();

            // 2. 恢复子系统数据
            RecoverGameContext();

            Debug.Log("[PersistentManager] 数据恢复成功！");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PersistentManager] 恢复数据时发生严重错误: {e}");
            // 如果出错，建议弹窗提示或返回主菜单，防止玩家玩坏档
        }
    }

    //恢复建筑
    private void ReconstructBuildings()
    {
       
        BuildingInstance.ClearAll();

        if (currentGameData.allBuildingData == null) return;

        foreach (BuildingInstance.BuildingSaveData bData in currentGameData.allBuildingData)
        {
            BuildingBuilder.Instance.TryCreateBuildingByData(bData,out BuildingInstance ins);
            
        }
    }

    //恢复GameContext
    private void RecoverGameContext()
    {
        GameContext.Instance.Init();

        // 1. 恢复回合与时间
        if (GameContext.Instance.Turn != null && currentGameData.turnSystemSaveData != null)
            GameContext.Instance.Turn.Load(currentGameData.turnSystemSaveData);

        // 2. 恢复资源网络 (库存、上限等)
        if (GameContext.Instance.ResourceNetwork != null && currentGameData.resourceNetworkSaveData != null)
            GameContext.Instance.ResourceNetwork.Load(currentGameData.resourceNetworkSaveData);

        // 3. 恢复科技树
        if (GameContext.Instance.TechTree != null && currentGameData.techTreeSaveData != null)
            GameContext.Instance.TechTree.Load(currentGameData.techTreeSaveData);

        // 4. 恢复人力资源
        if (GameContext.Instance.HumanResourcesNetwork != null && currentGameData.humanResourcesNetworkSaveData != null)
            GameContext.Instance.HumanResourcesNetwork.Load(currentGameData.humanResourcesNetworkSaveData);

        // 5. 恢复连接管理器
        if (ConnectionManager.Instance != null && currentGameData.connectionManagerSaveData != null)
            ConnectionManager.Instance.Load(currentGameData.connectionManagerSaveData);

    }
    #endregion


    /// <summary>
    /// 获取所有存档列表（高性能版）
    /// 优先读取 .meta 小文件。
    /// </summary>
    public List<GameSaveData> GetAllSaves()
    {
        List<GameSaveData> saves = new List<GameSaveData>();

        if (!Directory.Exists(GameSaveRootPath))
        {
            Directory.CreateDirectory(GameSaveRootPath);
            return saves;
        }

        // 1. 获取所有 .logd 文件（主数据文件），以此为基准
        string[] dataFiles = Directory.GetFiles(GameSaveRootPath, "*" + GameFileExtension);

        foreach (var fullDataPath in dataFiles)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(fullDataPath); // 获取 saveid
                string saveid = fileName; // 文件名即ID

                // 构建对应的 .meta 文件路径
                string metaRelativePath = Path.Combine(GameSaveDirName, saveid + MetaFileExtension);

                GameSaveData headerData = null;

                // 2. 检查是否存在对应的 .meta 文件
                if (ES3.FileExists(metaRelativePath))
                {
                    // [快路径] 只加载极小的元数据文件
                    SaveMetadata meta = ES3.Load<SaveMetadata>(MetaDataKey, metaRelativePath);

                    // 将元数据转换为 GameSaveData (仅填充头部信息)
                    headerData = new GameSaveData
                    {
                        saveid = meta.saveid,
                        saveName = meta.saveName,
                        lastSaveDate = meta.lastSaveDate,
                        versionNumber = meta.versionNumber
                        // 注意：其他字段如 allBuildingData 此时为 null
                    };
                }
                else
                {
                    // [慢路径/兼容路径] 只有数据文件，没有元数据（通常是旧版本的存档）
                    // 此时我们不得不加载完整文件，但顺便生成一个 .meta 方便下次快速读取
                    Debug.LogWarning($"[PersistentManager] 存档 {saveid} 缺少元数据，正在执行自动修复...");

                    string dataRelativePath = Path.Combine(GameSaveDirName, saveid + GameFileExtension);
                    headerData = ES3.Load<GameSaveData>(GameDataKey, dataRelativePath);

                    if (headerData != null)
                    {
                        // 立即补救：生成 .meta 文件
                        SaveMetadata newMeta = new SaveMetadata
                        {
                            saveid = headerData.saveid,
                            saveName = headerData.saveName,
                            lastSaveDate = headerData.lastSaveDate,
                            versionNumber = headerData.versionNumber
                        };
                        ES3.Save(MetaDataKey, newMeta, metaRelativePath);
                    }
                }

                if (headerData != null)
                {
                    saves.Add(headerData);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PersistentManager] 读取存档列表失败: {fullDataPath}, Error: {e.Message}");
            }
        }

        return saves.OrderByDescending(x => x.lastSaveDate).ToList();
    }

    /// <summary>
    /// 读取实际游戏数据
    /// </summary>
    public GameSaveData LoadGameData(string saveid)
    {
        string fileName = $"{saveid}{GameFileExtension}";
        string relativePath = Path.Combine(GameSaveDirName, fileName);

        if (ES3.FileExists(relativePath))
        {
            try
            {
                // 这里仍然加载完整的 .logd 文件
                GameSaveData data = ES3.Load<GameSaveData>(GameDataKey, relativePath);
                currentGameData = data;
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PersistentManager] Load failed for ID {saveid}: {e.Message}");
                return null;
            }
        }
        else
        {
            Debug.LogWarning($"[PersistentManager] Save file not found: {relativePath}");
            return null;
        }
    }

    public void DeleteGame(string saveid)
    {
        // --- 1. 构建绝对路径 (使用 GameSaveRootPath 确保和 GetAllSaves 逻辑一致) ---
        // GameSaveRootPath = Application.persistentDataPath + "/GameSaves"
        string dataFullPath = Path.Combine(GameSaveRootPath, $"{saveid}{GameFileExtension}");
        string metaFullPath = Path.Combine(GameSaveRootPath, $"{saveid}{MetaFileExtension}");

        // --- 2. 使用 System.IO 直接删除物理文件 (比 ES3.DeleteFile 更可控) ---
        try
        {
            if (File.Exists(dataFullPath))
            {
                File.Delete(dataFullPath);
                Debug.Log($"[PersistentManager] 已删除存档数据: {dataFullPath}");
            }
            else
            {
                // 如果是用 ES3 保存的，可能会有缓存 key，顺便尝试清理 ES3 缓存（可选）
                if (ES3.FileExists(Path.Combine(GameSaveDirName, $"{saveid}{GameFileExtension}")))
                    ES3.DeleteFile(Path.Combine(GameSaveDirName, $"{saveid}{GameFileExtension}"));
            }

            if (File.Exists(metaFullPath))
            {
                File.Delete(metaFullPath);
                Debug.Log($"[PersistentManager] 已删除存档元数据: {metaFullPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PersistentManager] 删除存档文件失败: {e.Message}");
        }

        // --- 3. [重要] 逻辑状态清理 ---

        // A. 如果删除的是当前内存中挂载的存档，将其置空
        if (currentGameData != null && currentGameData.saveid == saveid)
        {
            currentGameData = null;
        }

        // B. 如果删除的是“最后一次游玩”的记录，更新 AppData
        // 必须先加载 AppData 确保不为空
        if (currentAppData == null) LoadAppData();

        if (currentAppData != null && currentAppData.lastGameSaveId == saveid)
        {
            Debug.Log("[PersistentManager] 检测到删除了 LastGameSaveId，正在重置...");
            currentAppData.lastGameSaveId = null; // 置空

            // 尝试寻找一个新的“最新存档”作为替补（可选，提升体验）
            var allSaves = GetAllSaves();
            if (allSaves.Count > 0)
            {
                // GetAllSaves 已经是按时间倒序排列的，直接取第一个
                currentAppData.lastGameSaveId = allSaves[0].saveid;
            }

            SaveAppData(); // 保存全局设置
        }

        // --- 4. 触发事件 ---
        OnDeleteGameSave?.Invoke();
    }



    #endregion
}







[Serializable]
public class AppSaveData
{
    //第一次启动程序
    public bool firstStartup = true;

    public bool firstGame = true;

    public string lastGameSaveId;

    public AppLanguage language = AppLanguage.简体中文;

    public AudioManager.AudioSaveData audioSaveData;

    public static AppSaveData GetDef()
    {
        return new AppSaveData
        {
            firstStartup = true,
            firstGame = true,
            lastGameSaveId = null,
            language = AppLanguage.简体中文,
            audioSaveData = AudioManager.AudioSaveData.GetDef()

        };
    }
}



[Serializable]
public class SaveMetadata
{
    public string saveid;
    public string saveName;
    public string lastSaveDate;
    public string versionNumber;
}



[Serializable]
public class GameSaveData
{
    // --- 元数据 ---
    public string saveid;       // 唯一ID，对应文件名 (GUID)
    public string saveName;     // 玩家给存档起的名字 (显示用)
    public string lastSaveDate; // 保存时间
    public string versionNumber;

    // --- 游戏内容数据 ---
    public ResourceNetworkSaveData resourceNetworkSaveData;
    public HumanResourcesNetworkSaveData humanResourcesNetworkSaveData;
    public TechSystemSaveData techTreeSaveData;
    public TurnSystemSaveData turnSystemSaveData;
    public ConnectionManagerSaveData connectionManagerSaveData;
    public List<BuildingInstance.BuildingSaveData> allBuildingData;


    // 创建新游戏的工厂方法
    public static GameSaveData CreateNew()
    {
        GameSaveData tData = new GameSaveData();
        tData.saveid = System.Guid.NewGuid().ToString(); // 初始化时就生成ID
        return tData;
    }

    public string SetNewGuid()
    {
        saveid = System.Guid.NewGuid().ToString();
        return saveid;
    }
}
