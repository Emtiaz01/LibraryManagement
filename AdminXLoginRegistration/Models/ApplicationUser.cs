using Microsoft.AspNetCore.Identity;
using System;

namespace LibraryManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsSubscribed { get; set; }                  
        public DateTime? SubscriptionEndDate { get; set; }
        public bool IsBlockedFromBorrowing { get; set; }
        public bool HasEverSubscribed { get; set; } 


    }
}
