namespace MyToolz.UI.Management
{
    public class UISubScreen : UIScreenBase
    {
        public override void Close()
        {
            parent.ExitState(this);
        }

        public override void Open()
        {
            parent.ChangeState(this);
        }
    }
}
