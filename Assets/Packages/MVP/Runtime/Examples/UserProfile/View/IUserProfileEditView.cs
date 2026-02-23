using System.Collections.Generic;
using MyToolz.DesignPatterns.MVP.View;

namespace MyToolz.DesignPatterns.MVP.Examples.UserProfile
{
    public interface IUserProfileEditView : IInteractableView<UserProfileModel>
    {
        void ShowValidationErrors(IReadOnlyList<string> errors);
        void ClearValidationErrors();
        void ShowSaveSuccess();
    }
}
