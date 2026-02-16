using Mirror;
using MyToolz.Core;
using MyToolz.HealthSystem.Interfaces;
using MyToolz.Networking.ScriptableObjects;
using MyToolz.Player.Input;
using MyToolz.UI.Management;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.Networking.Scoreboards
{
    public abstract class ScoreboardBackend : ObjectPlus, IEventListener
    {
        [SerializeField] protected InputCommandSO openCloseInputCommandSO;

        protected bool isDirty;
        protected UIScreenBase scoreboardBody;
        protected Transform labelContainer;

        public void Awake(UIScreenBase scoreboardBody, Transform labelContainer)
        {
            this.scoreboardBody = scoreboardBody;
            this.labelContainer = labelContainer;
        }

        public virtual void OnEnable()
        {
            RegisterEvents();
        }

        public virtual void OnDisable()
        {
            UnregisterEvents();
        }

        public virtual void OnDestroy()
        {
            OnDisable();
        }

        public virtual void RegisterEvents()
        {
            openCloseInputCommandSO.Started += OnScoreboardOpened;
            openCloseInputCommandSO.Canceled += OnScoreboardClosed;
        }
        public virtual void UnregisterEvents()
        {
            openCloseInputCommandSO.Started -= OnScoreboardOpened;
            openCloseInputCommandSO.Canceled -= OnScoreboardClosed;
        }
        public abstract void Refresh();
        public abstract void ClearContainer();
        public void OpenScoreboard() => OnScoreboardOpened(default);
        public void CloseScoreboard() => OnScoreboardClosed(default);


        public void SetIsDirty()
        {
            if (!scoreboardBody.gameObject.activeInHierarchy) isDirty = true;
            else Refresh();
        }

        public virtual void OnScoreboardOpened(InputCommandSO obj)
        {
            if (isDirty) Refresh();
            scoreboardBody.Open();
        }

        public virtual void OnScoreboardClosed(InputCommandSO obj)
        {
            scoreboardBody.Close();
        }
    }

    public class MonoScoreboard : NetworkBehaviour
    {
        [SerializeField] protected UIScreenBase scoreboardBody;
        [SerializeField] protected Transform labelContainer;

        protected GameModeSO gameModeSO;
        protected ScoreboardBackend scoreboard
        {
            get => gameModeSO?.Scoreboard;
        }

        public virtual void OpenScoreboard()
        {
            scoreboard?.OpenScoreboard();
        }

        public virtual void CloseScoreboard()
        {
            scoreboard?.CloseScoreboard();
        }

        [Inject]
        private void Construct(GameModeSO gameModeSO)
        {
            this.gameModeSO = gameModeSO;
        }

        protected virtual void Awake()
        {
            scoreboard?.Awake(scoreboardBody,labelContainer);
        }

        protected virtual void OnEnable()
        {
            scoreboard?.OnEnable();
        }

        protected virtual void OnDisable()
        {
            scoreboard?.OnDisable();
        }

        protected virtual void OnDestroy()
        {
            scoreboard?.OnDestroy();
        }
    }
}