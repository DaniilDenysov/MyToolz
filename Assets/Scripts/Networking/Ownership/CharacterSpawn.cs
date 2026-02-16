using UnityEngine;

namespace MyToolz.Networking.Ownership
{
    public class CharacterSpawn : MonoBehaviour, IOwnershipSetter
    {
        private string teamGuid;

        public string GetTeamGuid()
        {
            return teamGuid;
        }

        public void SetTeamGuid(string guid)
        {
           this.teamGuid = guid;
        }
    }
}
