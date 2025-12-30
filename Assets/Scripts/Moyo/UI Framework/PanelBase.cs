using UnityEngine;
using DG.Tweening;

namespace Moyo.Unity
{
    public abstract class PanelBase : MonoBehaviour
    {
        public UIManager.UILayer CurrentLayer { get; set; }
        public virtual void Show(UIManager manager,params object[] args)
        {
            gameObject.SetActive(true);
            OnShow();
        }
        public virtual void Hide(UIManager manager, params object[] args)
        {
            gameObject.SetActive(false);
            OnHide();
        }

        /// <summary>
        /// 处理显示
        /// </summary>
        protected virtual void OnShow()
        {

        }
        /// <summary>
        /// 处理隐藏
        /// </summary>
        protected virtual void OnHide()
        {

        }
    }
}
