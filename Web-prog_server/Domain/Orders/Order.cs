namespace Web_prog_server.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(item => item.LineTotal);

    public Order(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        Status = OrderStatus.Pending;
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        EnsurePending();
        _items.Add(new OrderItem(productId, productName, unitPrice, quantity));
    }

    public void Confirm()
    {
        EnsureStatus(OrderStatus.Pending);
        if (_items.Count == 0)
        {
            throw new InvalidOperationException("An order must contain at least one item before it can be confirmed.");
        }

        Status = OrderStatus.Confirmed;
    }

    public void MarkPaid()
    {
        Transition(OrderStatus.Confirmed, OrderStatus.Paid);
    }

    public void Ship()
    {
        Transition(OrderStatus.Paid, OrderStatus.Shipped);
    }

    public void Complete()
    {
        Transition(OrderStatus.Shipped, OrderStatus.Completed);
    }

    public void Cancel()
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
        {
            throw new InvalidOperationException($"An order in status {Status} cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }

    private void EnsurePending()
    {
        EnsureStatus(OrderStatus.Pending);
    }

    private void EnsureStatus(OrderStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException($"Expected order status {expectedStatus}, but current status is {Status}.");
        }
    }

    private void Transition(OrderStatus expectedStatus, OrderStatus newStatus)
    {
        EnsureStatus(expectedStatus);
        Status = newStatus;
    }
}
