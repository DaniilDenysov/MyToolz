using MyToolz.AI.Platformer.Interfaces;
using MyToolz.AI.Platformer.Presenters;
using MyToolz.EditorToolz;
using MyToolz.ScriptableObjects.AI.Platformer;
using UnityEngine;
using Zenject;

namespace MyToolz.AI.Platformer.Installers
{
    public class EnemyCombatInstaller : MonoInstaller
    {
        [SerializeField, Required] private EnemyCombatSO enemyCombatSO;
        [SerializeField, Required] private EnemyMovementSO enemyMovementSO;
        [SerializeField, Required] private EnemyCombatPresenter enemyCombatPresenter;
        [SerializeField] private EnemyModel model = new();
        //TODO: refactor structure to divide into separate installers

        public override void InstallBindings()
        {
            model.Construct(enemyCombatSO, enemyMovementSO);
            Container.Bind<IEnemyModel>().FromInstance(model).AsSingle();
            Container.Bind<IReadOnlyEnemyModel>().FromInstance(model).AsSingle();
            Container.Bind<IEnemyCombatPresenter>().FromInstance(enemyCombatPresenter).AsSingle();
        }
    }
}
