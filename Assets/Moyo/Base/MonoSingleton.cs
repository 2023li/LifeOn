using UnityEngine;

namespace Moyo.Unity
{
    /// <summary>
    /// MonoBehaviour单例基类
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isApplicationQuitting = false;

        public static T Instance
        {
            get
            {
                if (_isApplicationQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] 应用程序正在退出，无法获取 {typeof(T)} 实例");
                    return null;
                }

                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // 在场景中查找已存在的实例
                            _instance = FindObjectOfType<T>();

                            if (_instance == null)
                            {
                                // 创建新的GameObject并添加组件
                                GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                                _instance = singletonObject.AddComponent<T>();

                                // 如果标记为持久化，在场景切换时不销毁
                                if (_instance.IsDontDestroyOnLoad)
                                {
                                    DontDestroyOnLoad(singletonObject);
                                }
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 是否在场景切换时不销毁（默认为true）
        /// </summary>
        protected virtual bool IsDontDestroyOnLoad => true;

        /// <summary>
        /// 是否自动初始化（默认为true）
        /// </summary>
        protected virtual bool AutoInitialize => true;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;

                if (IsDontDestroyOnLoad&transform.parent==null)
                {
                    DontDestroyOnLoad(gameObject);
                }

                if (AutoInitialize)
                {
                    Initialize();
                }
            }
            else if (_instance != this)
            {
                // 如果已存在实例，销毁新创建的实例
                Debug.LogWarning($"[MonoSingleton] 检测到重复的 {typeof(T)} 实例，销毁新实例");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _isApplicationQuitting = false;
            }
        }

        /// <summary>
        /// 初始化方法（子类可重写）
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// 销毁单例
        /// </summary>
        public static void DestroyInstance()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        /// <summary>
        /// 单例是否存在
        /// </summary>
        public static bool HasInstance => _instance != null && !_isApplicationQuitting;
    }
}
