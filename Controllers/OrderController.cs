using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IAzureStorageService storageService, ILogger<OrderController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
                ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
                return View(order);
            }

            try
            {
                // Fetch product details
                var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                if (product != null)
                {
                    order.ProductName = product.ProductName;

                    // ✅ Use numeric Price field instead of PriceString
                    order.UnitPrice = product.Price;
                    order.TotalPrice = product.Price * order.Quantity;
                }
                else
                {
                    ModelState.AddModelError("ProductId", "Selected product not found.");
                    ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
                    ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
                    return View(order);
                }

                // Fetch customer details
                var customer = await _storageService.GetEntityAsync<Customer>("Customer", order.CustomerId);
                if (customer != null)
                {
                    order.Username = customer.Name;
                }
                else
                {
                    ModelState.AddModelError("CustomerId", "Selected customer not found.");
                    ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
                    ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
                    return View(order);
                }

                order.PartitionKey = "Order";
                order.RowKey = Guid.NewGuid().ToString();

                // Ensure OrderDate is UTC
                order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);

                await _storageService.AddEntityAsync(order);

                // 🚀 Structured queue message
                var queuePayload = new
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Type = "OrderCreated",
                    OrderId = order.RowKey,
                    CustomerId = order.CustomerId,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    TotalPrice = order.TotalPrice,
                    Timestamp = DateTime.UtcNow
                };

                string messageJson = JsonSerializer.Serialize(queuePayload);
                await _storageService.SendMessageAsync("order-notifications", messageJson);

                TempData["SuccessMessage"] = "Order created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order: {Message}", ex.Message);
                ModelState.AddModelError("", "Failed to create order. Please try again.");
                ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
                ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
                return View(order);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", rowKey);
            if (order == null) return NotFound();

            ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order, string ETag)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
                ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
                return View(order);
            }

            try
            {
                order.ETag = new Azure.ETag(ETag);

                if (order.OrderDate != DateTime.MinValue)
                    order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);

                // ✅ Recalculate TotalPrice in case Quantity changed
                var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                if (product != null)
                {
                    order.UnitPrice = product.Price;
                    order.TotalPrice = product.Price * order.Quantity;
                }

                await _storageService.UpdateEntityAsync(order);

                TempData["SuccessMessage"] = "Order updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {Message}", ex.Message);
                ModelState.AddModelError("", "Failed to update order.");
                ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
                ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
                return View(order);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
                return BadRequest();

            try
            {
                await _storageService.DeleteEntityAsync<Order>("Order", rowKey);

                // 🚀 Queue log for deletion
                var queuePayload = new
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Type = "OrderDeleted",
                    OrderId = rowKey,
                    Timestamp = DateTime.UtcNow
                };

                string messageJson = JsonSerializer.Serialize(queuePayload);
                await _storageService.SendMessageAsync("order-notifications", messageJson);

                TempData["SuccessMessage"] = "Order deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Failed to delete order. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", rowKey);
            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}
