using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
using Zenject;
using System.Threading;
using Sirenix.OdinInspector;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.Networking.ScriptableObjects;
using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using MyToolz.Networking.Relays;
using MyToolz.UI.Labels;
using MyToolz.Networking.Matchmaking.View;

namespace MyToolz.Networking.Matchmaking
{
    public class MatchmakingManager : Singleton<MatchmakingManager>
    {
        [SerializeField, Required] private string gameId = "NoSaints";
        [SerializeField, Range(1f, 60f)] private float listRefreshDelay = 3f;
        [SerializeField, Range(1, 12)] private int maxPlayersInLobby;
        [SerializeField] private List<GameModeSO> availableGameModes;
        [SerializeField] private Transform gameModesDisplayRoot;
        [SerializeField] private Label gameModeLabelPrefab;
        [SerializeField] private Label lobbyLabelPrefab;
        [SerializeField] private TMP_InputField searchBar;
        [SerializeField] private Transform lobbiesList;

        private Dictionary<string, GameModeSO> cachedFilter;

        private Dictionary<LobbyDTO, Label> lobbyLabels = new Dictionary<LobbyDTO, Label>();

        [SerializeReference] private Relay relay;
        private LobbyDTO activeLobby;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();



        [Inject]
        public void Construct(Relay relay)
        {
            this.relay = relay;
        }

        private void Start()
        {
            cachedFilter = new Dictionary<string, GameModeSO>();

            foreach (GameModeSO mode in availableGameModes)
            {
                if (mode == null) continue;
                cachedFilter.TryAdd(mode.Title, mode);
                CreateLabel(mode);
            }

            StartCoroutine(LobbyListUpdateRequestLoop());
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            StopAllCoroutines();
        }

        public void CreateLabel(GameModeSO gameModeSO)
        {
            EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
            {
                Prefab = gameModeLabelPrefab,
                Callback = (label) =>
                {
                    label.transform.SetParent(gameModesDisplayRoot.transform);
                    ((GameModeLabel)label).Construct(gameModeSO, () => JoinLobby(gameModeSO));
                    label.transform.localScale = Vector3.one;
                }
            });
        }

        public async void RefreshLobbyList()
        {
            var list = await relay.GetLobbyList();
            OnLobbyListUpdated(list);
        }

        private IEnumerator LobbyListUpdateRequestLoop()
        {
            while (true)
            {
                RefreshLobbyList();
                yield return new WaitForSeconds(listRefreshDelay);
            }
        }


        private void OnLobbyListUpdated(IReadOnlyList<LobbyDTO> lobbies)
        {
            ClearLobbyList();
            lobbyLabels.Clear();

            foreach (var lobbyDTO in lobbies)
            {
                EventBus<PoolRequest<Label>>.Raise(new PoolRequest<Label>()
                {
                    Prefab = lobbyLabelPrefab,
                    Callback = (label) =>
                    {
                        label.transform.SetParent(lobbiesList);
                        ((LobbyLabel)label).Construct(lobbyDTO, async () =>
                        {
                            var result = await relay.JoinLobby(lobbyDTO, cancellationTokenSource);
                            Debug.Log(result == ResultCode.Success_LobbyJoined
                                ? "Joined lobby successfully!"
                                : "Unable to join lobby!");
                        });

                        lobbyLabels[lobbyDTO] = label;
                        label.gameObject.SetActive(IsLobbyMatchesFilter(lobbyDTO, searchBar.text));
                        label.transform.localScale = Vector3.one;
                    }
                });
            }
        }

        private void ClearLobbyList()
        {
            foreach (var label in lobbyLabels.Values)
            {
                EventBus<ReleaseRequest<Label>>.Raise(new ReleaseRequest<Label>()
                {
                    PoolObject = label
                });
            }
        }

        public void Search(string searchStr)
        {
            if (string.IsNullOrEmpty(searchStr))
            {
                foreach (var pair in lobbyLabels)
                {
                    pair.Value.gameObject.SetActive(true);
                }
                return;
            }

            searchStr = searchStr.ToLower();

            foreach (var pair in lobbyLabels)
            {
                var dto = pair.Key;
                bool match = IsLobbyMatchesFilter(dto, searchStr);
                pair.Value.gameObject.SetActive(match);
            }
        }

        private bool IsLobbyMatchesFilter(LobbyDTO dto, string str)
        {
            return dto.GameModeName.ToLower().Contains(str)
                || dto.MapName.ToLower().Contains(str);
        }

        public async void JoinLobby(GameModeSO gameModeSO)
        {
            if (!TryGetRandomLobby(gameModeSO, out var lobbyDTO))
            {
                CreateLobby(gameModeSO);
                return;
            }
            var result = await relay.JoinLobbyGameMode(lobbyDTO, gameModeSO.Title, cancellationTokenSource);

            Debug.Log(result == ResultCode.Success_LobbyJoined
                ? "Joined lobby successfully!"
                : "Unable to join lobby!");
        }

        private bool TryGetRandomLobby(GameModeSO gameModeSO, out LobbyDTO lobbyDTO)
        {
            var filtered = lobbyLabels.Keys
                .Where(x => x.GameModeName == gameModeSO.Title)
                .ToList();

            if (filtered.Count == 0)
            {
                lobbyDTO = null;
                return false;
            }

            lobbyDTO = filtered[UnityEngine.Random.Range(0, filtered.Count)];
            return true;
        }

        public async void CreateLobby(GameModeSO gameModeSO)
        {
            var result = await relay.CreateLobby(gameModeSO, cancellationTokenSource);

            Debug.Log(result == ResultCode.Success_LobbyCreated
                ? "Lobby created successfully!"
                : "Unable to create lobby.");
        }

        public void ToggleGameMode(GameModeSO gameModeSO)
        {
            if (gameModeSO == null) return;

            if (cachedFilter.ContainsKey(gameModeSO.Title))
            {
                cachedFilter.Remove(gameModeSO.Title);
            }
            else
            {
                cachedFilter.Add(gameModeSO.Title, gameModeSO);
            }
        }

        private bool TryGetRandomAvailableMode(out GameModeSO gameModeSO)
        {
            gameModeSO = null;
            if (cachedFilter == null || cachedFilter.Count == 0) return false;

            int attempts = 10;
            while (attempts > 0)
            {
                string key = cachedFilter.Keys.ElementAt(UnityEngine.Random.Range(0, cachedFilter.Keys.Count));
                if (!cachedFilter.TryGetValue(key, out gameModeSO)) continue;
                if (gameModeSO != null) return true;
                attempts--;
            }
            return false;
        }

        public async void CancelSearch()
        {
            if (activeLobby != null)
            {
                await relay.LeaveLobby(activeLobby);
                Log($"Left lobby due to search cancellation.");
                activeLobby = null;
            }
        }

        public void CreateLobby()
        {
            if (!TryGetRandomAvailableMode(out var gameMode))
            {
                Debug.LogError("Failed to get available mode!");
                return;
            }
            CreateLobby(gameMode);
        }

        public override MatchmakingManager GetInstance()
        {
            return this;
        }
    }
}
