using System;

namespace MyToolz.DesignPatterns.MVP.Model
{
    public abstract class ModelBase<T> : IModel<T> where T : ModelBase<T>
    {
        public event Action<T> OnChanged;

        public abstract T Clone();
        public abstract void Reset();

        protected void NotifyChanged()
        {
            OnChanged?.Invoke((T)this);
        }
    }
}
