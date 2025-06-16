// Shared\Contracts\Messages.cs
using System;

namespace Shared.Contracts
{
    // отправляем из OrderService
    public class OrderCreatedMessage
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = default!;
        public decimal Amount { get; set; }
    }

    // отправляем из PaymentsService
    public class PaymentResultMessage
    {
        public Guid OrderId { get; set; }
        public bool IsSuccess { get; set; }
    }
}
