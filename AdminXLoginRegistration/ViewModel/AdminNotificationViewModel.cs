using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModel
{
    public class AdminNotificationViewModel
    {
        public int PendingCount { get; set; }
        public List<BookLoan> PendingRequests { get; set; }
    }
}
