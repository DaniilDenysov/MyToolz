using System;

namespace MyToolz.DesignPatterns.MVP.Model
{
    public interface IModel<T> where T : IModel<T>
    {
        event Action<T> OnChanged;
        T Clone();
        void Reset();
    }
}
