namespace MyToolz.DesignPatterns.Adapter
{
    public interface IAdapter<From,To>
    {
        To Convert(From reference);
    }
}
