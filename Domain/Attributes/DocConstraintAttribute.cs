namespace ATBS.Domain.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DocConstraintAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}