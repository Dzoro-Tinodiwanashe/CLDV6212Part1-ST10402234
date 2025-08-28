using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ✅ Needed for [NotMapped]
using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name = "Product ID")]
        public string ProductId => RowKey;

        [Required(ErrorMessage = "Product name is required")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        public double Price { get; set; }  // ✅ Changed from decimal to double

        [Required(ErrorMessage = "Stock available is required")]
        [Display(Name = "Stock available")]
        public int StockAvailable { get; set; }

        [Display(Name = "Image URL")]
        public string ImageURL { get; set; } = string.Empty;

        // ✅ Not saved in Azure Table, only used for uploads
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
