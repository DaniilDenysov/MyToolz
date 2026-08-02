using System;
using UnityEngine;

namespace MyToolz.Localization
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class LocalizationLanguageAttribute : PropertyAttribute
    {
        public readonly string DatabaseField;

        public LocalizationLanguageAttribute(string databaseField = "database")
        {
            DatabaseField = databaseField;
        }
    }
}
