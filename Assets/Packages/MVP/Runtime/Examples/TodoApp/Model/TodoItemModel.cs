using System;
using System.Collections.Generic;
using MyToolz.DesignPatterns.MVP.Model;

namespace MyToolz.DesignPatterns.MVP.Examples.TodoApp
{
    [Serializable]
    public class TodoItemModel : ModelBase<TodoItemModel>, IValidatable
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public bool IsCompleted { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public TodoItemModel()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public TodoItemModel(string title, string description)
        {
            Title = title;
            Description = description;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetTitle(string title)
        {
            Title = title;
            NotifyChanged();
        }

        public void SetDescription(string description)
        {
            Description = description;
            NotifyChanged();
        }

        public void SetCompleted(bool completed)
        {
            IsCompleted = completed;
            NotifyChanged();
        }

        public override TodoItemModel Clone()
        {
            return new TodoItemModel(Title, Description)
            {
                IsCompleted = IsCompleted,
                CreatedAt = CreatedAt
            };
        }

        public override void Reset()
        {
            Title = string.Empty;
            Description = string.Empty;
            IsCompleted = false;
            NotifyChanged();
        }

        public bool IsValid()
        {
            return GetValidationErrors().Count == 0;
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Title))
                errors.Add("Title is required.");

            if (Title != null && Title.Length > 100)
                errors.Add("Title must be 100 characters or fewer.");

            return errors;
        }
    }
}
