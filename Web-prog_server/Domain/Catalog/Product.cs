namespace Web_prog_server.Domain.Catalog;

public sealed class Product
{
    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public decimal Price { get; private set; }

    public Product(Guid categoryId, string name, decimal price, string? description = null)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));
        }

        Id = Guid.NewGuid();
        CategoryId = categoryId;
        Name = RequireName(name);
        Price = RequirePrice(price);
        Description = description?.Trim();
    }

    public void Rename(string name)
    {
        Name = RequireName(name);
    }

    public void ChangePrice(decimal price)
    {
        Price = RequirePrice(price);
    }

    private static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        }

        return name.Trim();
    }

    private static decimal RequirePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Product price must be greater than zero.");
        }

        return price;
    }
}
