namespace VendingMachine.Common.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class FormatAsCurrencyAttribute(
    string format = "F2", string suffix = "lv") 
    : Attribute
{
    public string Format { get; } = format;
    public string Suffix { get; } = suffix;
}
