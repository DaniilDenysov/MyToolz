using System.Collections.Generic;

namespace MyToolz.DesignPatterns.MVP.Model
{
    public interface IValidatable
    {
        bool IsValid();
        IReadOnlyList<string> GetValidationErrors();
    }
}
