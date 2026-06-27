namespace MyToolz.DesignPatterns.Singleton
{
    public class PublicSingleton<T> : Singleton where T : PublicSingleton<T>
    {
        public static T Instance { get; private set; }

        protected override bool IsValid()
        {
            return Instance == null;
        }

        protected override void RemoveSelf()
        {
            if (Instance == this as T)
            {
                Instance = null;
            }
        }

        protected override void SetSelf()
        {
            Instance = this as T;
        }
    }
}
