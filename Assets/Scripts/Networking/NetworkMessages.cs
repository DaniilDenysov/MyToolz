using Mirror;
using MyToolz.Networking.Relays;
using MyToolz.Player.FPS.InteractionSystem.Model;
using System;
using UnityEngine;

namespace MyToolz.Networking.Extensions
{
    public static class NetworkIdentityExtensions
    {
        public static T ToNetworkInstance<T>(this uint instance) where T : MonoBehaviour
        {
            var dict = NetworkServer.active ? NetworkServer.spawned : NetworkClient.spawned;
            if (!dict.TryGetValue(instance, out NetworkIdentity networkIdentity)) return null;
            return networkIdentity.GetComponentInChildren<T>();
        }
    }
}

namespace MyToolz.Networking.Messages
{
    [System.Serializable]
    public struct LanDiscoveryRequest : NetworkMessage
    {

    }

    [System.Serializable]
    public struct LanDiscoveryResponseWire : NetworkMessage
    {
        public Uri URI;
        public string IP;
        public string Scene;
        public string GameState;
        public LanGameModeDTO GameMode;
        public int TotalPlayers;

        public LanDiscoveryResponseLite ToLite()
        {
            return new LanDiscoveryResponseLite
            {
                URI = URI,
                IP = IP,
                Scene = Scene,
                GameMode = GameMode,
                GameState = GameState,
                TotalPlayers = TotalPlayers
            };
        }
    }

    [System.Serializable]
    public struct LanDiscoveryResponseLite
    {
        public Uri URI;
        public string IP;
        public string Scene;
        public string GameState;
        public LanGameModeDTO GameMode;
        public GameState GameStateParsed => Enum.Parse<GameState>(GameState);
        public int TotalPlayers;
    }

    [System.Serializable]
    public struct LanGameModeDTO
    {
        public string Id;
        public string Name;
        public int MaxPlayers;
    }

    public static class CustomNetworkMessages
    {
        public static void WriteInteractionLink(this NetworkWriter w, InteractionConnection c)
        {
            if (c == null)
            {
                w.WriteBool(false);
                return;
            }

            w.WriteBool(true);
            w.WriteUInt(c.InteractableNetId);
            w.WriteUInt(c.InteractorNetId);
        }

        public static InteractionConnection ReadInteractionLink(this NetworkReader r)
        {
            if (!r.ReadBool())
                return null;

            var link = new InteractionConnection
            {
                InteractableNetId = r.ReadUInt(),
                InteractorNetId = r.ReadUInt()
            };
            return link;
        }


        //public static void WriteCameraShake(this NetworkWriter writer, CameraShakeSO shakeSO)
        //{
        //    writer.WriteBool(shakeSO == null);

        //    if (shakeSO == null)
        //        return;

        //    writer.WriteString(shakeSO.name);
        //}

        //public static CameraShakeSO ReadCameraShake(this NetworkReader reader)
        //{
        //    bool isNull = reader.ReadBool();
        //    if (isNull)
        //        return null;

        //    string name = reader.ReadString();

        //    return Resources.Load<CameraShakeSO>(name);
        //}


        //public static void WriteLethalEquipmentSO(this NetworkWriter w, LethalEquipmentSO so)
        //{
        //    w.WriteBool(so != null);
        //    if (so == null) return;
        //    w.WriteString(so.ResourcePath);
        //}

        //public static LethalEquipmentSO ReadLethalEquipmentSO(this NetworkReader r)
        //{
        //    if (!r.ReadBool()) return null;
        //    var path = r.ReadString();
        //    var so = Resources.Load<LethalEquipmentSO>(path);
        //    if (so == null)
        //        DebugUtility.LogError($"ReadLethalEquipmentSO: Resources.Load failed for '{path}'.");
        //    return so;
        //}

        //public static void WriteKillCamData(this NetworkWriter writer,KillCamData killCamData)
        //{
        //    writer.WriteVector3(killCamData.playerPosition);
        //    writer.WriteQuaternion(killCamData.playerCameraRotation);
        //    writer.WriteVector3(killCamData.playerCameraPosition);
        //    writer.WriteVector3(killCamData.killerPosition);
        //    writer.WriteBool(killCamData.undefinedKiller);
        //}

        //public static KillCamData ReadKillCamData(this NetworkReader reader)
        //{
        //    return new KillCamData(reader.ReadVector3(), reader.ReadQuaternion(), reader.ReadVector3(), reader.ReadVector3(), reader.ReadBool());
        //}

        public static void WritePlayerStats(this NetworkWriter writer, Player player)
        {
            writer.WriteString(player.Nickname);
            writer.WriteString(player.CharacterGUID);
            writer.WriteInt(player.ConnectionId);
            writer.WriteBool(player.IsPartyOwner);
            writer.WriteBool(player.IsReady);
        }

        public static Player ReadPlayerStats(this NetworkReader reader)
        {
            Player stats = new Player
            {
                Nickname = reader.ReadString(),
                CharacterGUID = reader.ReadString(),
                ConnectionId = reader.ReadInt(),
                IsPartyOwner = reader.ReadBool(),
                IsReady = reader.ReadBool()
            };
            return stats;
        }
    }
}