using UnityEngine;
using DG.Tweening;

namespace Moyo.Unity
{
    public abstract class PanelBase : MonoBehaviour
    {
        

        // --- 改进 ---：添加标志位用于跟踪面板是否至少显示过一次
        private bool hasBeenShown = false;
        protected Canvas canvas;
        
        protected virtual void Awake()
        {
            this.AutoBindFields();
            canvas = UIManager.Instance.GetMainCanvas();
        }

        #region 面板生命周期

        /// <summary>
        /// 面板实例化后由UIManager仅调用一次
        /// 用于一次性初始化设置
        /// </summary>
        public virtual void OnPanelCreated(params object[] args) { }

        /// <summary>
        /// 每次面板即将显示时调用
        /// 用于刷新数据
        /// </summary>
        public virtual void OnPanelShow() { }

        /// <summary>
        /// 每次面板完成隐藏动画时调用
        /// </summary>
        public virtual void OnPanelHide() { }

        #endregion

        public virtual void ImmediateHide()
        {
            gameObject.SetActive(false);
            OnPanelHide(); // --- 改进 ---：触发生命周期事件
        }

        public virtual void ImmediateShow()
        {
            OnPanelShow(); // --- 改进 ---：触发生命周期事件
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            

            transform.DOScale(Vector3.zero, 0.3f).From(Vector3.one).SetEase(Ease.InBack).OnComplete(() =>
            {
                gameObject.SetActive(false);
                OnPanelHide(); // --- 改进 ---：触发生命周期事件
            });
        }

        public virtual void Show()
        {
            // --- 改进 ---：在激活/执行动画前触发生命周期事件
            // 此处调用OnPanelShow确保面板可见前数据已刷新
            OnPanelShow();

            gameObject.SetActive(true);

          
            // --- 改进 ---：使用更具动态性的动画
            // 首次显示时从缩放为0开始动画，后续显示时快速弹出
            if (!hasBeenShown)
            {
                transform.DOScale(Vector3.one, 0.4f).From(Vector3.zero).SetEase(Ease.OutBack);
                hasBeenShown = true;
            }
            else
            {
                transform.localScale = Vector3.one; // 若之前隐藏过，确保缩放状态正确
            }
        }
    }
}
