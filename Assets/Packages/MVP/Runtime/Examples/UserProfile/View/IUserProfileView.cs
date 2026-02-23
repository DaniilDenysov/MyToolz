using MyToolz.DesignPatterns.MVP.View;

namespace MyToolz.DesignPatterns.MVP.Examples.UserProfile
{
    public interface IUserProfileView : IReadOnlyView<UserProfileModel>
    {
        void ShowLevel(int level, int currentXp, int requiredXp);
    }
}
