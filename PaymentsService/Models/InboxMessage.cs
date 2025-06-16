using System;
using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Models
{
    // Приёмник сообщений: ключ — MessageId из RabbitMQ,
    // чтобы дубли не обрабатывались.
    public class InboxMessage
    {
        [Key]
        public Guid Id { get; set; }
        public string Type { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public DateTime ReceivedAt { get; set; }
        public bool Processed { get; set; }
    }
}
