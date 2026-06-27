using MyToolz.UI.Management;
using Zenject;

namespace MyToolz.Installers
{
    public class UIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<UILayerStateManager>().FromInstance(new UILayerStateManager()).AsSingle();
        }
    }
}