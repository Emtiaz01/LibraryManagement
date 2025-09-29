using LibraryManagementSystem.Models;
using System.Collections.Generic;

namespace LibraryManagementSystem.ViewModel
{
    public class AdminNotificationViewModel
    {
        public int PendingCount { get; set; }
        public List<BookLoan> PendingRequests { get; set; }
        public List<Product> PendingDonations { get; set; }
    }
}
