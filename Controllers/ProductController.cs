using System.Net.Http;
using System.Text;
using System.Text.Json;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;
        private readonly HttpClient _httpClient;

        public ProductController(
            IAzureStorageService storageService,
            ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        // Helper to check if the current user is Admin
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        public IActionResult Create()
        {
            if (!IsAdmin())
                return Unauthorized();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (!IsAdmin())
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(product);

            product.PartitionKey = "Product";
            product.RowKey = Guid.NewGuid().ToString();

            // 🔹 DEBUG: print the price coming from the form
            Console.WriteLine($"[DEBUG] Create Product - Received Price: {product.Price}");
            _logger.LogInformation("Create Product - Received Price: {Price}", product.Price);

            try
            {
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    product.ImageURL = await UploadImageAsync(product.ImageFile);

                    var queuePayload = new
                    {
                        TransactionId = Guid.NewGuid().ToString(),
                        Type = "ImageUploaded",
                        ProductId = product.RowKey,
                        FileName = Path.GetFileName(product.ImageURL),
                        Timestamp = DateTime.UtcNow
                    };

                    string messageJson = JsonSerializer.Serialize(queuePayload);
                    await _storageService.SendMessageAsync("order-notifications", messageJson);
                }

                product.ImageFile = null;

                await _storageService.AddEntityAsync(product);

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {Message}", ex.Message);
                ModelState.AddModelError("", "Failed to create product.");
                return View(product);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (!IsAdmin())
                return Unauthorized();

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, string ETag)
        {
            if (!IsAdmin())
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(product);

            // 🔹 DEBUG: print the price coming from the form
            Console.WriteLine($"[DEBUG] Edit Product - Received Price: {product.Price}");
            _logger.LogInformation("Edit Product - Received Price: {Price}", product.Price);

            try
            {
                var existingProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                if (existingProduct == null)
                    return NotFound();

                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    product.ImageURL = await UploadImageAsync(product.ImageFile);

                    var queuePayload = new
                    {
                        TransactionId = Guid.NewGuid().ToString(),
                        Type = "ImageReplaced",
                        ProductId = product.RowKey,
                        FileName = Path.GetFileName(product.ImageURL),
                        Timestamp = DateTime.UtcNow
                    };

                    string messageJson = JsonSerializer.Serialize(queuePayload);
                    await _storageService.SendMessageAsync("order-notifications", messageJson);
                }
                else
                {
                    product.ImageURL = existingProduct.ImageURL;
                }

                product.ImageFile = null;

                product.ETag = new Azure.ETag(ETag);

                await _storageService.UpdateEntityAsync(product);

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                ModelState.AddModelError("", "Failed to update product.");
                return View(product);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string rowKey)
        {
            if (!IsAdmin())
                return Unauthorized();

            if (string.IsNullOrEmpty(rowKey))
                return BadRequest();

            try
            {
                await _storageService.DeleteEntityAsync<Product>("Product", rowKey);

                var queuePayload = new
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Type = "ProductDeleted",
                    ProductId = rowKey,
                    Timestamp = DateTime.UtcNow
                };

                string messageJson = JsonSerializer.Serialize(queuePayload);
                await _storageService.SendMessageAsync("order-notifications", messageJson);

                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Failed to delete product.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            using var memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);

            var payload = new
            {
                FileName = imageFile.FileName,
                ContentBase64 = Convert.ToBase64String(memoryStream.ToArray())
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:7251/api/products/upload", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("ProductFunction returned error: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"Error uploading image: {response.StatusCode}");
            }

            var imageUrl = await response.Content.ReadAsStringAsync();
            return imageUrl;
        }
    }
}
