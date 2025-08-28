using ABCRetailers.Models; 
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required]
        public int CustomerId { get; set; }
        public List<Customer> Customers { get; set; } = new List<Customer>();

        [Required]
        public int ProductId { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();

        [Required]
        public int Quantity { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // default status

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;
    }
}
