using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LibraryManagementSystem.ViewModel
{
    public class BookLoanViewModel
    {
        [ValidateNever]
        public Product Product { get; set; }
        public BookLoan? BookLoan { get; set; } = new BookLoan();
        public DateTime? NextAvailableDate { get; set; }
    }
}
