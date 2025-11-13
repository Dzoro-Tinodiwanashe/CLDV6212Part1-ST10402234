using System.Text;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(IAzureStorageService storageService, ILogger<CustomerController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // Display customers
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            var customers = await _storageService.GetAllEntitiesAsync<Customer>();

            if (role != "Admin")
            {
                // Customers see only their own record
                customers = customers.Where(c => c.Username == username).ToList();
            }

            return View(customers);
        }

        // Show create form
        public IActionResult Create() => View();

        // Handle create form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            if (!ModelState.IsValid) return View(customer);

            // Customers can only create their own record
            if (role != "Admin" && customer.Username != username)
            {
                TempData["ErrorMessage"] = "You cannot create another customer's record.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Call CustomerFunction via HTTP
                using var client = new HttpClient();
                var json = JsonConvert.SerializeObject(new
                {
                    customer.Name,
                    customer.Surname,
                    customer.Username,
                    customer.Email,
                    customer.ShippingAddress
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("http://localhost:7251/api/customers/add", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Customer created successfully via Function!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Function error: {error}");
                    return View(customer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CustomerFunction: {Message}", ex.Message);
                ModelState.AddModelError("", "Failed to create customer via Function.");
                return View(customer);
            }
        }

        // GET: Show edit form
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var customer = await _storageService.GetEntityAsync<Customer>("CustomerPartition", id);
            if (customer == null) return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            // Customers can only edit their own record
            if (role != "Admin" && customer.Username != username)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this customer.";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        // POST: Handle edit submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer, string ETag)
        {
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            if (!ModelState.IsValid) return View(customer);

            // Customers can only edit their own record
            if (role != "Admin" && customer.Username != username)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this customer.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                customer.ETag = new Azure.ETag(ETag);
                await _storageService.UpdateEntityAsync(customer);
                TempData["SuccessMessage"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {Message}", ex.Message);
                ModelState.AddModelError("", "Failed to update customer. Another user may have modified this record.");
                return View(customer);
            }
        }

        // GET: Show delete confirmation
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var customer = await _storageService.GetEntityAsync<Customer>("CustomerPartition", id);
            if (customer == null) return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            // Customers can only delete their own record
            if (role != "Admin" && customer.Username != username)
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this customer.";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        // POST: Handle delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var customer = await _storageService.GetEntityAsync<Customer>("CustomerPartition", id);
            if (customer == null) return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            // Customers can only delete their own record
            if (role != "Admin" && customer.Username != username)
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this customer.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _storageService.DeleteEntityAsync<Customer>("CustomerPartition", id);
                TempData["SuccessMessage"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Failed to delete customer.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
