namespace LibraryManagementSystem.ViewModel
{
    public class AdminDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveLoans { get; set; }
        public int OverdueLoans { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal TodayPendingFines { get; set; }
        public decimal OverduePercent { get; set; }
        public decimal OverdueTrendToday { get; set; }
        public decimal TodayRevenueTrend { get; set; }
        public int PendingDonations { get; set; }
        public int MembersWithHighFine { get; set; }
        public int BooksIssuedToday { get; set; }
        public int BooksReturnedToday { get; set; }
        public int BooksReserved { get; set; }
        public int NewMembersToday { get; set; }
        public int DonatedBooksCount { get; set; }
        public List<string> RecentActivities { get; set; }
        public MostOverdueMemberViewModel MostOverdueMember { get; set; }
        public string MostBorrowedBook { get; set; }
    }
    public class MostOverdueMemberViewModel
    {
        public string Name { get; set; }
        public int OverdueBooks { get; set; }
        public double FineAmount { get; set; }
        public string Id { get; set; }
    }

}
