using LBG;
using MyToolz.EditorToolz;
using UnityEngine;
using Zenject;

namespace MyToolz.Networking.Relays
{
    public class RelayInstaller : MonoInstaller
    {
        [SerializeReference, SubclassSelector, Required] private Relay relay;

        public override void InstallBindings()
        {
            relay?.Initialize();
            Container.BindInstance(relay).AsSingle();
        }
    }
}
