using UnityEngine;
using DG.Tweening;

namespace Moyo.Unity
{
    public abstract class PanelBase : MonoBehaviour
    {
        public virtual bool SingletonPanel => true;
        public abstract UILayer Layer { get; }
        public virtual void Show(params object[] args)
        {
            gameObject.SetActive(true);
          
        }
        public virtual void Hide(params object[] args)
        {
            gameObject.SetActive(false);
            
        }
        public virtual bool Back(params object[] args)
        {
            return false;
        }
       
    }
}
