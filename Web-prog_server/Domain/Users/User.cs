namespace Web_prog_server.Domain.Users;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }

    public User(string name, string email)
    {
        Id = Guid.NewGuid();
        Name = RequireText(name, nameof(name));
        Email = RequireText(email, nameof(email));
    }

    public void Rename(string name)
    {
        Name = RequireText(name, nameof(name));
    }

    public void ChangeEmail(string email)
    {
        Email = RequireText(email, nameof(email));
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
