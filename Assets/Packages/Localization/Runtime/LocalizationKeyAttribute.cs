using System;
using UnityEngine;

namespace MyToolz.Localization
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class LocalizationKeyAttribute : PropertyAttribute
    {
        public readonly string DatabaseField;

        public LocalizationKeyAttribute(string databaseField = "database")
        {
            DatabaseField = databaseField;
        }
    }
}
