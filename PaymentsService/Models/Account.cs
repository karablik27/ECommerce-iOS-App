// PaymentsService/Models/Account.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentsService.Models
{
    public class Account
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required]
        public decimal Balance { get; set; }

        // Сделал nullable, т.к. старые записи будут вставлены без значения,
        // БД заполнит его при INSERT/UPDATE
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
