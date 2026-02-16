using Mirror;
using Mirror.Discovery;
using MyToolz.Networking.Messages;
using MyToolz.Utilities.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine.SceneManagement;

namespace MyToolz.Networking.Relays
{
    public class CustomNetworkDiscovery : NetworkDiscoveryBase<LanDiscoveryRequest, LanDiscoveryResponseWire>
    {
        readonly Dictionary<string, LanDiscoveryResponseWire> found = new();

        public event Action<LanDiscoveryResponseLite> OnServerFoundEvent;

        public List<LanDiscoveryResponseLite> GetAvailableHosts()
        {
            return found.Values.Select(w => w.ToLite()).ToList();
        }

        public void BeginSearching()
        {
            found.Clear();
            try
            {
                StartDiscovery();
                DebugUtility.Log(this, "LAN Discovery: searching…");
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning(this, $"LAN Discovery search failed: {e.Message}");
            }
        }

        public void EndSearching()
        {
            StopDiscovery();
            DebugUtility.Log(this, "LAN Discovery: search stopped.");
        }

        protected override LanDiscoveryRequest GetRequest()
        {
            DebugUtility.Log(this, "Discovery: building request");
            return new LanDiscoveryRequest { };
        }

        protected override LanDiscoveryResponseWire ProcessRequest(LanDiscoveryRequest request, IPEndPoint endpoint)
        {
            DebugUtility.Log(this, "Processing discovery request!");
            Uri uri = null;
            try
            {
                uri = Transport.active?.ServerUri();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(this, $"Transport.ServerUri() not available: {ex.Message}");
            }

            var nm = NetworkManager.singleton;
            int totalPlayers = nm ? nm.numPlayers : 0;

            var customNm = NetworkManager.singleton as CustomNetworkManager;
            var gm = customNm?.GameModeSO;
            var gameModeDto = new LanGameModeDTO
            {
                Id = gm ? gm.name : "",
                Name = gm ? gm.Title : "Unknown",
                MaxPlayers = gm ? gm.MaxPlayers : 0
            };

            return new LanDiscoveryResponseWire
            {
                URI = uri,
                IP = NetUtils.GetLocalIPv4Address(),
                Scene = SceneManager.GetActiveScene().name,
                GameState = GameState.Available.ToString(), //TODO: try adding functionality for handling it dynamically in Relay
                GameMode = gameModeDto,
                TotalPlayers = totalPlayers
            };
        }

        protected override void ProcessResponse(LanDiscoveryResponseWire response, IPEndPoint endpoint)
        {
            try
            {
                var builder = new UriBuilder(response.URI) { Host = endpoint.Address.ToString() };
                response.URI = builder.Uri;
            }
            catch (Exception e)
            {
                DebugUtility.LogWarning(this, $"Failed to normalize discovery URI: {e.Message}");
            }

            var key = response.URI != null ? response.URI.AbsoluteUri : $"udp://{endpoint.Address}:{endpoint.Port}";
            if (!found.ContainsKey(key))
            {
                found[key] = response;
                OnServerFoundEvent?.Invoke(response.ToLite());
                DebugUtility.Log(this, $"LAN host found: {response.URI}  players:{response.TotalPlayers}");
            }
            else
            {
                found[key] = response;
            }
        }
    }
}
