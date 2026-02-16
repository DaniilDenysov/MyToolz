using MyToolz.AI.Platformer.Presenters;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace MyToolz.AI.Platformer.Installers
{
    public class EnemyMovementInstaller : MonoInstaller
    {
        [SerializeField, Required] private EnemyMovementPresenter enemyMovementPresenter;

        public override void InstallBindings()
        {
            Container.Bind<IReadOnlyEnemyMovementModel>().FromInstance(enemyMovementPresenter).AsSingle();
            Container.Bind<IEnemyMovementPresenter>().FromInstance(enemyMovementPresenter).AsSingle();
        }
    }
}
