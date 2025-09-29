using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace LibraryManagementSystem.ViewModel
{
    public class ProductViewModel
    {
        public Product Product { get; set; }
        [BindNever]
        [ValidateNever]
        public IEnumerable<SelectListItem> CategoryList { get; set; }

        [BindNever]
        [ValidateNever]
        public List<BookLoan> LoanRecords { get; set; }
    }
}
