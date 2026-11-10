namespace Web_prog_server.Domain.Catalog;

public sealed class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public Category(string name, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = RequireName(name);
        Description = description?.Trim();
    }

    public void Rename(string name)
    {
        Name = RequireName(name);
    }

    private static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty.", nameof(name));
        }

        return name.Trim();
    }
}
