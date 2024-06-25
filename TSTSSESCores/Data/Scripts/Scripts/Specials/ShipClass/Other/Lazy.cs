using System;
using Sandbox.ModAPI;

namespace MIG.SpecCores
{
    public class DecayingByFramesLazy<T> : Lazy<T>
    {
        private long lastUpdate = -1000;
        private long framesValid = 0;
        
        public DecayingByFramesLazy(int framesValid = 0) : base() { this.framesValid = framesValid;}
        protected override bool ShouldUpdate(){ return (MyAPIGateway.Session.GameplayFrameCounter - lastUpdate >= framesValid); }
    }
    
    public class Lazy<T>
    {
        private bool HasCorrectValue = false;
        private Func<T, T> getter;
        private T m_value = default(T);
        public event Action<T,T> Changed;
        
        public T Value
        { 
            get {
                if (ShouldUpdate())
                {
                    var newValue = getter(m_value);
                    if (!newValue.Equals(m_value))
                    {
                        var oldValue = m_value;
                        m_value = newValue;
                        Changed?.Invoke(oldValue, newValue);
                    }
                }
                return m_value;
            } 
        }

        
        public Lazy() { }

        public void SetGetter(Func<T, T> getter)
        {
            this.getter = getter;
        }
        
        protected virtual bool ShouldUpdate() { return !HasCorrectValue; }
    }
}