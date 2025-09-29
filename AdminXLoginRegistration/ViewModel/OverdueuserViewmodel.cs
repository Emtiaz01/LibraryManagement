using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModel
{
    public class OverdueUserViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int OverdueBooks { get; set; }
        public double TotalFine { get; set; }
        public List<BookLoan> Loans { get; set; }
        public bool IsPremiumMember { get; set; }
        public bool IsBlockedFromBorrowing { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }
}
