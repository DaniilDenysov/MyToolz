using MyToolz.DesignPatterns.MVP.Presenter;

namespace MyToolz.DesignPatterns.MVP.Examples.UserProfile
{
    public class UserProfileEditPresenter : PresenterBase<UserProfileModel, IUserProfileEditView>
    {
        private UserProfileModel _draft;

        public UserProfileEditPresenter(UserProfileModel model, IUserProfileEditView view)
            : base(model, view) { }

        protected override void OnInitialize()
        {
            _draft = Model.Clone();
            View.Initialize(_draft);
        }

        protected override void SubscribeEvents()
        {
            View.OnUserInput += HandleUserInput;
            View.OnSubmit += HandleSubmit;
            View.OnCancel += HandleCancel;
        }

        protected override void UnsubscribeEvents()
        {
            View.OnUserInput -= HandleUserInput;
            View.OnSubmit -= HandleSubmit;
            View.OnCancel -= HandleCancel;
        }

        private void HandleUserInput(UserProfileModel draft)
        {
            _draft = draft;
            View.ClearValidationErrors();
        }

        private void HandleSubmit()
        {
            if (!_draft.IsValid())
            {
                View.ShowValidationErrors(_draft.GetValidationErrors());
                return;
            }

            Model.SetUsername(_draft.Username);
            Model.SetEmail(_draft.Email);
            Model.SetAvatarUrl(_draft.AvatarUrl);
            View.ShowSaveSuccess();
        }

        private void HandleCancel()
        {
            _draft = Model.Clone();
            View.UpdateView(_draft);
            View.ClearValidationErrors();
        }
    }
}
