using MyToolz.EditorToolz;

namespace MyToolz.UI.Management
{
    public class UISubScreen : UIScreenBase, ISelfValidator
    {
        public override void Close()
        {
            parent.ExitState(this);
        }

        public override void Open()
        {
            if (parent != null && !parent.IsActive)
                parent.Open();
            parent.ChangeState(this);
        }

        public void Validate(SelfValidationResult result)
        {
            if (parent == null)
                result.AddError("Sub-screen has no Parent. A UISubScreen is opened/closed through its parent and will throw without one.");
        }
    }
}
