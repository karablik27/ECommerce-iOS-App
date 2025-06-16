using System;
using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Models
{
    public class OutboxMessage
    {
        [Key]
        public Guid Id { get; set; }
        public string Type { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
