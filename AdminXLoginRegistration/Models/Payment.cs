using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public int? BookLoanId { get; set; }
        [ForeignKey("BookLoanId")]
        public BookLoan BookLoan { get; set; }

        public double Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentType { get; set; } // e.g. "Fine", "Membership"
        public string Status { get; set; } // "Pending", "Paid", "Failed"
        public string TransactionId { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
