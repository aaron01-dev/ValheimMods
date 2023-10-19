using WxAxW.PinAssistant.Utils;

namespace WxAxW.PinAssistant.Core
{
    public abstract class Component
    {
        private bool m_enabled = true;
        public bool enabled 
        { 
            get => m_enabled; 
            
            set
            {
                m_enabled = value;

                if (m_enabled) OnEnable();
                else OnDisable();
            }
        }
        public abstract void Start();
        public abstract void Destroy();
        public abstract void OnEnable();
        public abstract void OnDisable();
    }
}
