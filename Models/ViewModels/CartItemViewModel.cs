namespace ABCRetailers.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int Id { get; set; } // Add this
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
    }
}
