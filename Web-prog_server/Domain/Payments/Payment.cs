namespace Web_prog_server.Domain.Payments;

public sealed class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public PaymentStatus Status { get; private set; }

    public Payment(Guid orderId, decimal amount, string currency)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be empty.", nameof(currency));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        Status = PaymentStatus.Pending;
    }

    public void MarkSucceeded()
    {
        Transition(PaymentStatus.Pending, PaymentStatus.Succeeded);
    }

    public void MarkFailed()
    {
        Transition(PaymentStatus.Pending, PaymentStatus.Failed);
    }

    public void Refund()
    {
        Transition(PaymentStatus.Succeeded, PaymentStatus.Refunded);
    }

    private void Transition(PaymentStatus expectedStatus, PaymentStatus newStatus)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(
                $"Payment in status {Status} cannot transition to {newStatus}.");
        }

        Status = newStatus;
    }
}
