using UnityEngine;

namespace MyToolz.GameSettings
{

    [System.Serializable]
    public class MultiplierDisplayFormat : ValueDisplayFormat
    {
        public override string Format(float value)
        {
            return string.Format("{0:0.0}x", value);
        }
    }

    [System.Serializable]
    public class CustomDisplayFormat : ValueDisplayFormat
    {
        [SerializeField] private string customFormat = "{0:0}";

        public override string Format(float value)
        {
            string pattern = string.IsNullOrWhiteSpace(customFormat) ? "{0}" : customFormat;
            return string.Format(pattern, value);
        }
    }

    [System.Serializable]
    public class PercentDisplayFormat : ValueDisplayFormat
    {
        public override string Format(float value)
        {
            return string.Format("{0:0}%", value);
        }
    }

    [System.Serializable]
    public class TwoDecimalDisplayFormat : ValueDisplayFormat
    {
        public override string Format(float value)
        {
            return string.Format("{0:0.00}", value);
        }
    }

    [System.Serializable]
    public class OneDecimalDisplayFormat : ValueDisplayFormat
    {
        public override string Format(float value)
        {
            return string.Format("{0:0.0}", value);
        }
    }

    [System.Serializable]
    public class IntegerDisplayFormat : ValueDisplayFormat
    {
        public override string Format(float value)
        {
            return string.Format("{0:0.##}", value);
        }
    }

    [System.Serializable]
    public abstract class ValueDisplayFormat
    {
        public abstract string Format(float value);
    }
}
