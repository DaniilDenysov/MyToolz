using System;

namespace MyToolz.DesignPatterns.MVP.Presenter
{
    public interface IPresenter : IDisposable
    {
        void Initialize();
        void Enable();
        void Disable();
    }

    public interface IPresenter<TModel, TView> : IPresenter
    {
        TModel Model { get; }
        TView View { get; }
    }
}
