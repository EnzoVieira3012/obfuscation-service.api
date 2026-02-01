namespace Domain.ValueObjects;

public readonly struct EncryptedId
{
    public string Value { get; }

    public EncryptedId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("EncryptedId inválido.");

        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EncryptedId id) => id.Value;
    public static implicit operator EncryptedId(string value) => new(value);
}