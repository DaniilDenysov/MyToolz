using Mirror;
using MyToolz.Utilities.Debug;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Networking
{
    public interface IOwnershipSetter
    {
        public void SetTeamGuid(string guid);
        public string GetTeamGuid();
    }
}

namespace MyToolz.Networking.Ownership
{

    [System.Serializable]
    public class Cluster
    {
        [SerializeField] private List<GameObject> ownershipObjects = new List<GameObject>();
        public void SetOwnershipGuid(string guid)
        {
            foreach (var ownershipObject in ownershipObjects)
            {
                if (ownershipObject.TryGetComponent(out IOwnershipSetter setter))
                {
                    setter.SetTeamGuid(guid);
                }
            }
        }
    }

    public class OwnershipManager : MonoBehaviour
    {
        [SerializeField] private List<Cluster> clusters = new List<Cluster>();

        private void Start() 
        {
            if (NetworkServer.active)
            {
                var networkManager = (CustomNetworkManager)NetworkManager.singleton;
                if (networkManager == null) return;
                //if (networkManager.GameModeSO.EnableCommunism) return;
                var teamGuids = networkManager.TeamGuids;

                int teamIndex = 0;

                foreach (var cluster in clusters)
                {
                    if (teamIndex < teamGuids.Count)
                    {
                        cluster.SetOwnershipGuid(teamGuids[teamIndex]);
                        teamIndex++;
                        DebugUtility.Log(this, "Created cluster");
                    }
                    else
                    {
                        DebugUtility.LogWarning(this, "Not enough TeamGuids to assign all ownershipSetters.");
                        return;
                    }
                }
            }
        }
    }

}
