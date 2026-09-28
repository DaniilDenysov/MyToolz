using Zenject;

namespace MyToolz.DesignPatterns.MVP.Presenter
{
    public abstract class PresenterBase<TModel, TView> : IPresenter<TModel, TView>
    {
        public TModel Model { get; private set; }
        public TView View { get; private set; }

        private bool isEnabled;
        private bool isDisposed;
        private bool isInitialized;

        public bool IsInitialized => isInitialized;
        public bool IsEnabled => isEnabled;
        public bool IsDisposed => isDisposed;

        protected PresenterBase(TModel model, TView view)
        {
            Model = model;
            View = view;
        }

        /// <summary>
        /// Runs <see cref="OnInitialize"/> once, then enables the presenter. Repeated calls (e.g. both
        /// Zenject's IInitializable and a manual call) only re-enable; a disposed presenter stays disposed.
        /// </summary>
        public virtual void Initialize()
        {
            if (isDisposed)
                return;

            if (!isInitialized)
            {
                isInitialized = true;
                OnInitialize();
            }

            Enable();
        }

        public void Enable()
        {
            if (isEnabled || isDisposed)
                return;

            isEnabled = true;
            SubscribeEvents();
            OnEnable();
        }

        public void Disable()
        {
            if (!isEnabled || isDisposed)
                return;

            isEnabled = false;
            UnsubscribeEvents();
            OnDisable();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            Disable();
            isDisposed = true;
            OnDispose();
        }

        protected abstract void SubscribeEvents();
        protected abstract void UnsubscribeEvents();

        protected virtual void OnInitialize() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        protected virtual void OnDispose() { }
    }
}
