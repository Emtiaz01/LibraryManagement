using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class BookLoan
    {
        public int BookLoanId { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser User { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public LoanStatus Status { get; set; }

        // New property to store fine
        public double FineAmount { get; set; }
    }

    public enum LoanStatus
    {
        Pending,
        Approved,
        Rejected,
        Returned,
        ReturnPending
    }
}
