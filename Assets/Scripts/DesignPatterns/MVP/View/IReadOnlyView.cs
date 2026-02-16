namespace MyToolz.DesignPatterns.MVP.View
{
    public interface IReadOnlyView<T>
    {
        public void Initialize(T model);
        public void Show();
        public void UpdateView(T model);
        public void Hide();
        public void Destroy(T model);
    }
}
