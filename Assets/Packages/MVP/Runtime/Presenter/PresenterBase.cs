namespace MyToolz.DesignPatterns.MVP.Presenter
{
    public abstract class PresenterBase<TModel, TView> : IPresenter<TModel, TView>
    {
        public TModel Model { get; private set; }
        public TView View { get; private set; }

        private bool isEnabled;
        private bool isDisposed;

        protected PresenterBase(TModel model, TView view)
        {
            Model = model;
            View = view;
        }

        public virtual void Initialize()
        {
            OnInitialize();
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
