using Microsoft.AspNetCore.Http;

namespace LibraryManagementSystem.ViewModel
{
    public class DonateBookViewModel
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        public string ProductISBN { get; set; }
        public string ProductAuthor { get; set; }
        public int ProductQuantity { get; set; }
        public int CategoryId { get; set; }
        public IFormFile? ProductImageFile { get; set; }
        public string? DonorName { get; set; }
        public string? DonorEmail { get; set; }
    }
}
