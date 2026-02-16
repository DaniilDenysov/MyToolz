using Mirror;
using MyToolz.DesignPatterns.MVP.View;
using MyToolz.Networking.GameModes.Presenter;
using MyToolz.UI.Management;
using UnityEngine;

namespace MyToolz.Networking.GameModes.View
{
    public abstract class GameModeView<T> : NetworkBehaviour, IReadOnlyView<T> where T : GameStateHandler
    {
        [SerializeField] protected UIScreenBase screen;

        public virtual void Destroy(T model)
        {

        }

        public virtual void Hide()
        {
            if (screen == null) return;
            screen.Close();
        }
        public virtual void Initialize(T model)
        {

        }
        public virtual void Show()
        {
            if (screen == null) return;
            screen.Open();
        }

        public abstract void UpdateView(T model);
    }
}
