using System.Collections.Generic;

namespace MyToolz.EditorToolz
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct ValidationMessage
    {
        public readonly string Message;
        public readonly ValidationSeverity Severity;

        public ValidationMessage(string message, ValidationSeverity severity)
        {
            Message = message;
            Severity = severity;
        }
    }

    /// <summary>
    /// Collects validation messages produced by an <see cref="ISelfValidator"/>. Deliberately
    /// editor-free so it can be populated from runtime code; <c>MyToolzInspector</c> renders the
    /// collected messages as inspector HelpBoxes.
    /// </summary>
    public sealed class SelfValidationResult
    {
        public readonly List<ValidationMessage> Messages = new List<ValidationMessage>();

        public void AddError(string message) => Messages.Add(new ValidationMessage(message, ValidationSeverity.Error));

        public void AddWarning(string message) => Messages.Add(new ValidationMessage(message, ValidationSeverity.Warning));

        public void AddInfo(string message) => Messages.Add(new ValidationMessage(message, ValidationSeverity.Info));
    }

    /// <summary>
    /// Implement on a MonoBehaviour or ScriptableObject to surface setup validation directly in the
    /// inspector. <c>MyToolzInspector</c> calls <see cref="Validate"/> on every repaint and draws a
    /// HelpBox for each collected message. Keep implementations editor-free (no UnityEditor
    /// references) so they compile into runtime assemblies.
    /// </summary>
    public interface ISelfValidator
    {
        void Validate(SelfValidationResult result);
    }
}
