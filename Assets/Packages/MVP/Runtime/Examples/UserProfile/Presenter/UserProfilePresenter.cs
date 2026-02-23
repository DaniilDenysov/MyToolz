using MyToolz.DesignPatterns.MVP.Presenter;

namespace MyToolz.DesignPatterns.MVP.Examples.UserProfile
{
    public class UserProfilePresenter : PresenterBase<UserProfileModel, IUserProfileView>
    {
        public UserProfilePresenter(UserProfileModel model, IUserProfileView view)
            : base(model, view) { }

        protected override void OnInitialize()
        {
            View.Initialize(Model);
            RefreshLevelDisplay();
        }

        protected override void SubscribeEvents()
        {
            Model.OnChanged += HandleModelChanged;
        }

        protected override void UnsubscribeEvents()
        {
            Model.OnChanged -= HandleModelChanged;
        }

        private void HandleModelChanged(UserProfileModel model)
        {
            View.UpdateView(model);
            RefreshLevelDisplay();
        }

        private void RefreshLevelDisplay()
        {
            int requiredXp = Model.Level * 100;
            View.ShowLevel(Model.Level, Model.ExperiencePoints, requiredXp);
        }
    }
}
