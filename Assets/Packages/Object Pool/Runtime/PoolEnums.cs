namespace MyToolz.DesignPatterns.ObjectPool
{
    public enum PoolContext
    {
        Project,
        Scene
    }

    public enum PoolCapacityMode
    {
        SoftLock,
        HardLock,
        QueueLock
    }
}
