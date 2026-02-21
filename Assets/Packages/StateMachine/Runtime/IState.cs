namespace MyToolz.DesignPatterns.StateMachine
{
    public interface IState
    {
        public void OnEnter();
        public void OnExit();
    }
}
