namespace Web_prog_server.Domain.Inventory;

public sealed class InventoryItem
{
    public Guid ProductId { get; private set; }
    public int QuantityOnHand { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => QuantityOnHand - ReservedQuantity;

    public InventoryItem(Guid productId, int quantityOnHand = 0)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        }

        if (quantityOnHand < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantityOnHand), "Quantity on hand cannot be negative.");
        }

        ProductId = productId;
        QuantityOnHand = quantityOnHand;
    }

    public void Reserve(int quantity)
    {
        RequirePositiveQuantity(quantity);
        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException("Cannot reserve more than the available quantity.");
        }

        ReservedQuantity += quantity;
    }

    public void ReleaseReservation(int quantity)
    {
        RequirePositiveQuantity(quantity);
        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException("Cannot release more than the reserved quantity.");
        }

        ReservedQuantity -= quantity;
    }

    public void FulfillReservation(int quantity)
    {
        RequirePositiveQuantity(quantity);
        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException("Cannot fulfill more than the reserved quantity.");
        }

        ReservedQuantity -= quantity;
        QuantityOnHand -= quantity;
    }

    public void AddStock(int quantity)
    {
        RequirePositiveQuantity(quantity);
        QuantityOnHand += quantity;
    }

    private static void RequirePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }
    }
}
