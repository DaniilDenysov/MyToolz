using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.InventorySystem.Models;
using MyToolz.Networking.Events;
using MyToolz.UI.Labels;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MyToolz.Networking.Events
{
    public struct TargetGotShotEvent : IEvent
    {
        public bool IsHeadShot;
    }

    public struct PlayerShot : IEvent
    {
        public ItemSO WeaponSO;
    }
}

namespace MyToolz.Networking.Scoreboards
{
    public class WeaponStats
    {
        public int HeadShots;
        public int TotalShots;
        public int Hits;
    }

    public class TrainingGroundsScoreboard : ScoreboardBackend
    {
        [SerializeField] protected Label scoreboardLabel;
        public TMP_Text headshotsDisplay
        {
            get;
            set;
        }
        public TMP_Text totalHitsDisplay
        {
            get;
            set;
        }
        public TMP_Text totalShotsDisplay
        {
            get;
            set;
        }
        public TMP_Text totalAccuracyDisplay
        {
            get;
            set;
        }

        private int headShots;
        private int totalHits;
        private int totalShots;

        private int HeadShots
        {
            get => headShots;
            set
            {
                headShots = value;
                headshotsDisplay.text = $"{headShots.ToString()}";
            }
        }
        private int TotalHits
        {
            get => totalHits;
            set
            {
                totalHits = value;
                totalHitsDisplay.text = $"{totalHits.ToString()}";
                UpdateTotalAccuracy();
            }
        }
        private int TotalShots
         {
            get => totalShots;
            set
            {
                totalShots = value;
                totalShotsDisplay.text = $"{totalShots.ToString()}";
                UpdateTotalAccuracy();
            }
        }
        private Dictionary<ItemSO, WeaponStats> weaponStatistics = new Dictionary<ItemSO, WeaponStats>();
        private ItemSO currentWeapon;
        private EventBinding<PlayerShot> playerShotEventBinding;
        private EventBinding<TargetGotShotEvent> targetGotShotEventBinding;
        public override void RegisterEvents()
        {
            base.RegisterEvents();
            playerShotEventBinding = new EventBinding<PlayerShot>(OnPlayerShot);
            targetGotShotEventBinding = new EventBinding<TargetGotShotEvent>(OnTargetGotShot);

            EventBus<PlayerShot>.Register(playerShotEventBinding);
            EventBus<TargetGotShotEvent>.Register(targetGotShotEventBinding);
        }

        public void UpdateTotalAccuracy()
        {
            totalAccuracyDisplay.text = $"{Mathf.RoundToInt((totalHits / (float)totalShots)*100f)}%";
        }

        public override void UnregisterEvents()
        {
            base.UnregisterEvents();
            EventBus<PlayerShot>.Deregister(playerShotEventBinding);
            EventBus<TargetGotShotEvent>.Deregister(targetGotShotEventBinding);
        }

        private void OnPlayerShot(PlayerShot @event)
        {
            Debug.Log("Player shot");
            currentWeapon = @event.WeaponSO;
            TotalShots++;
            if (weaponStatistics.TryGetValue(currentWeapon, out WeaponStats weaponStats))
            {
                weaponStats.TotalShots++;
            }
            else
            {
                weaponStatistics.TryAdd(currentWeapon, new WeaponStats() { TotalShots = 1 });
            }
            SetIsDirty();
        }

        private void OnTargetGotShot(TargetGotShotEvent @event)
        {
            if (currentWeapon == null) return;
            TotalHits++;
            if (@event.IsHeadShot) HeadShots++;
            if (weaponStatistics.TryGetValue(currentWeapon,out WeaponStats weaponStats))
            {
                weaponStats.Hits++;
                if (@event.IsHeadShot) weaponStats.HeadShots++;
            }
            SetIsDirty();
        }

        public override void Refresh() 
        {
            isDirty = false;
            ClearContainer();
            foreach (var weapon in weaponStatistics.Keys)
            {
                if (weaponStatistics.TryGetValue(weapon, out var stat)) CreateLabel(weapon, stat);
            }
        }

        private void ResetScoreboard()
        {
            headShots = 0;
            totalHits = 0;
            totalShots = 0;
            currentWeapon = null;
            weaponStatistics.Clear();
        }

        public override void OnDisable()
        {
            ResetScoreboard();
            base.OnDisable();
        }
        public override void OnDestroy()
        {
            ResetScoreboard();
            base.OnDestroy();
        }

        public override void ClearContainer() 
        {
            foreach (Label child in labelContainer.GetComponentsInChildren<Label>())
            {
                EventBus<ReleaseRequest<Label>>.Raise(new ReleaseRequest<Label>()
                {
                    PoolObject = child,
                });
            }
        }

        public virtual void CreateLabel(ItemSO weaponSO, WeaponStats weaponStats)
        {
            EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
            {
                Prefab = scoreboardLabel,
                Callback = 
                (label) =>
                {
                    label.transform.SetParent(labelContainer);
                    //((TrainingGroundsLabel)label).Construct(weaponSO, weaponStats);
                    label.transform.localScale = Vector3.one;
                }
            });
        }
    }

    public class MonoTrainingGroundsScoreboard : MonoScoreboard
    {
        [SerializeField] private TMP_Text headshotsDisplay, totalHitsDisplay, totalShotsDisplay, totalAccuracyDisplay;

        protected TrainingGroundsScoreboard trainingGroundsScoreboard
        {
            get => (TrainingGroundsScoreboard)scoreboard;
        }

        protected override void Awake()
        {
            trainingGroundsScoreboard.headshotsDisplay = headshotsDisplay;
            trainingGroundsScoreboard.totalShotsDisplay = totalShotsDisplay;
            trainingGroundsScoreboard.totalHitsDisplay = totalHitsDisplay;
            trainingGroundsScoreboard.totalAccuracyDisplay = totalAccuracyDisplay;
            base.Awake();
        }
    }
}
