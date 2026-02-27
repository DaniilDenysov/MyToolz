namespace MyToolz.DesignPatterns.ObjectPool
{
    public interface IPoolable
    {
        public void OnSpawned();
        public void OnDespawned();
    }
}