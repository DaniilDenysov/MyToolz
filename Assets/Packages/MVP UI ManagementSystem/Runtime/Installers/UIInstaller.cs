using MyToolz.InputManagement;
using MyToolz.UI.Management;
using Zenject;

namespace MyToolz.Installers
{
    public class UIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            var inputManager = new InputStateManager();
            Container.Bind<InputStateManager>().FromInstance(inputManager).AsSingle();
            Container.Bind<UILayerStateManager>().FromInstance(new UILayerStateManager()).AsSingle();
        }
    }
}