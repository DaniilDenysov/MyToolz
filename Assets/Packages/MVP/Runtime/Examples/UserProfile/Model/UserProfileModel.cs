using System;
using System.Collections.Generic;
using MyToolz.DesignPatterns.MVP.Model;

namespace MyToolz.DesignPatterns.MVP.Examples.UserProfile
{
    [Serializable]
    public class UserProfileModel : ModelBase<UserProfileModel>, IValidatable
    {
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string AvatarUrl { get; private set; }
        public int Level { get; private set; }
        public int ExperiencePoints { get; private set; }

        public UserProfileModel() { }

        public UserProfileModel(string username, string email)
        {
            Username = username;
            Email = email;
            Level = 1;
        }

        public void SetUsername(string username)
        {
            Username = username;
            NotifyChanged();
        }

        public void SetEmail(string email)
        {
            Email = email;
            NotifyChanged();
        }

        public void SetAvatarUrl(string url)
        {
            AvatarUrl = url;
            NotifyChanged();
        }

        public void AddExperience(int amount)
        {
            ExperiencePoints += amount;
            while (ExperiencePoints >= Level * 100)
            {
                ExperiencePoints -= Level * 100;
                Level++;
            }
            NotifyChanged();
        }

        public override UserProfileModel Clone()
        {
            return new UserProfileModel(Username, Email)
            {
                AvatarUrl = AvatarUrl,
                Level = Level,
                ExperiencePoints = ExperiencePoints
            };
        }

        public override void Reset()
        {
            Username = string.Empty;
            Email = string.Empty;
            AvatarUrl = string.Empty;
            Level = 1;
            ExperiencePoints = 0;
            NotifyChanged();
        }

        public bool IsValid()
        {
            return GetValidationErrors().Count == 0;
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Username))
                errors.Add("Username is required.");

            if (Username != null && Username.Length < 3)
                errors.Add("Username must be at least 3 characters.");

            if (string.IsNullOrWhiteSpace(Email))
                errors.Add("Email is required.");

            if (Email != null && !Email.Contains("@"))
                errors.Add("Email must contain an @ symbol.");

            return errors;
        }
    }
}
