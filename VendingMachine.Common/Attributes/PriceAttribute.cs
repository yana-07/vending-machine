namespace VendingMachine.Common.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class PriceAttribute(
    string format = "F2") 
    : Attribute
{
    public string Format { get; } = format;
}
