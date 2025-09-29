namespace LibraryManagementSystem.ViewModel
{
    public class BookLoanWithFineViewModel
    {
        public string BookImageUrl { get; set; }

        public string BookName { get; set; }
        public string BookAuthor { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public double FineAmount { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsPremiumMember { get; set; }
        public bool IsReturned => ReturnDate.HasValue;
    }
}
