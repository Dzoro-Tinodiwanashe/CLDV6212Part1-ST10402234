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

        // ========================
        // INDEX
        // ========================
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");
            var orders = await _storageService.GetAllEntitiesAsync<Order>();

            if (role == "Customer")
            {
                // Only show this user's orders
                orders = orders.Where(o => o.Username == username).ToList();
            }

            return View(orders);
        }

        // ========================
        // CREATE (GET)
        // ========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
            var allCustomers = await _storageService.GetAllEntitiesAsync<Customer>();

            if (role == "Admin")
            {
                ViewBag.Customers = allCustomers;
            }
            else
            {
                // 🔹 FIXED: Match by Username, not Name
                var customer = allCustomers.FirstOrDefault(c => c.Username == username);

                if (customer == null)
                {
                    return Content("Customer record not found for the logged-in user.");
                }

                ViewBag.Customers = new List<Customer> { customer };
            }

            return View();
        }

        // ========================
        // CREATE (POST)
        // ========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
                ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
                return View(order);
            }

            var allCustomers = await _storageService.GetAllEntitiesAsync<Customer>();
            var allProducts = await _storageService.GetAllEntitiesAsync<Product>();

            Customer customer;
            if (role == "Admin")
            {
                customer = allCustomers.FirstOrDefault(c => c.RowKey == order.CustomerId);
            }
            else
            {
                // 🔹 FIXED: Match by Username
                customer = allCustomers.FirstOrDefault(c => c.Username == username);
            }

            if (customer == null)
            {
                ModelState.AddModelError("", "Customer record not found.");
                ViewBag.Products = allProducts;
                ViewBag.Customers = allCustomers;
                return View(order);
            }

            var product = allProducts.FirstOrDefault(p => p.RowKey == order.ProductId);
            if (product == null)
            {
                ModelState.AddModelError("", "Product not found.");
                ViewBag.Products = allProducts;
                ViewBag.Customers = allCustomers;
                return View(order);
            }

            // 🔹 Assign order details
            order.PartitionKey = "Order";
            order.RowKey = Guid.NewGuid().ToString();
            order.OrderDate = DateTime.UtcNow;
            order.ProductName = product.ProductName;
            order.UnitPrice = product.Price;
            order.TotalPrice = product.Price * order.Quantity;
            order.CustomerId = customer.RowKey;
            order.Username = username;
            order.Status = "Submitted";

            await _storageService.AddEntityAsync(order);
            TempData["SuccessMessage"] = "Order created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ========================
        // EDIT (GET)
        // ========================
        [HttpGet]
        public async Task<IActionResult> Edit(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", rowKey);
            if (order == null)
                return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            if (role != "Admin" && order.Username != username)
                return Unauthorized();

            ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
            var allCustomers = await _storageService.GetAllEntitiesAsync<Customer>();

            if (role == "Admin")
            {
                ViewBag.Customers = allCustomers;
            }
            else
            {
                var customer = allCustomers.FirstOrDefault(c => c.Username == username);
                ViewBag.Customers = new List<Customer> { customer };
            }

            return View(order);
        }

        // ========================
        // EDIT (POST)
        // ========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order, string ETag)
        {
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");
            var existingOrder = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);

            if (existingOrder == null)
                return NotFound();

            if (role != "Admin" && existingOrder.Username != username)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _storageService.GetAllEntitiesAsync<Product>();
                ViewBag.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
                return View(order);
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
            if (product != null)
            {
                order.UnitPrice = product.Price;
                order.TotalPrice = product.Price * order.Quantity;
            }

            order.ETag = new Azure.ETag(ETag);
            await _storageService.UpdateEntityAsync(order);

            TempData["SuccessMessage"] = "Order updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ========================
        // DELETE
        // ========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
                return BadRequest();

            var order = await _storageService.GetEntityAsync<Order>("Order", rowKey);
            if (order == null)
                return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            if (role != "Admin" && order.Username != username)
                return Unauthorized();

            await _storageService.DeleteEntityAsync<Order>("Order", rowKey);
            TempData["SuccessMessage"] = "Order deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ========================
        // DETAILS
        // ========================
        [HttpGet]
        public async Task<IActionResult> Details(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", rowKey);
            if (order == null)
                return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            if (role != "Admin" && order.Username != username)
                return Unauthorized();

            return View(order);
        }
    }
}
