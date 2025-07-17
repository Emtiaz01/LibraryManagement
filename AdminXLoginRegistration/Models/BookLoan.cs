using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public enum LoanStatus
    {
        Nothing,Pending, Approved, Rejected,ReturnPending, Returned
    }
    public class BookLoan
    {
        [Key]
        public int BookLoanId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser User { get; set; }

        [Required]
        public DateTime BorrowDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }
        public LoanStatus Status { get; set; } = LoanStatus.Nothing;

        [NotMapped]
        public bool IsReturned => ReturnDate.HasValue;

        [NotMapped]
        public bool IsOverdue => !IsReturned && DueDate < DateTime.Now;
    }
}
