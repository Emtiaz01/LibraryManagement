namespace LibraryManagementSystem.ViewModel
{
    public class ReturnRequestViewModel
    {
        public int BookLoanId { get; set; }
        public string ProductName { get; set; }
        public string UserName { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
