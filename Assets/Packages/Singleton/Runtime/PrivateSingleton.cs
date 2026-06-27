namespace MyToolz.DesignPatterns.Singleton
{
    public class PrivateSingleton<T> : Singleton where T : PrivateSingleton<T>
    {
        private static T Instance { get; set; }

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
