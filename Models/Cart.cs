using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    [Table("Cart")] // explicitly match the SQL table name
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CustomerUsername { get; set; } = string.Empty;

        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        public string Status { get; set; } = "Submitted";
    }
}
