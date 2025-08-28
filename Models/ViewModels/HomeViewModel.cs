using ABCRetailers.Models;

namespace ABCRetailers.ViewModels
{
    public class HomeViewModel
    {
        public int CustomerCount { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }

        // Display some products on the homepage
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
    }
}

