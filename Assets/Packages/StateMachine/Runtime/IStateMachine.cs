namespace MyToolz.DesignPatterns.StateMachine
{
    public interface IStateMachine<T> where T : IState
    {
        public void ChangeState(T state);
    }
}
