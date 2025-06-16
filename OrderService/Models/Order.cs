// OrderService/Models/Order.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace OrdersService.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required]
        public decimal Amount { get; set; }

        public string Description { get; set; } = default!;
        public string Status { get; set; } = "NEW";
    }
}
