using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

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

        // Display all customers
        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            return View(customers);
        }

        // Show create form
        public IActionResult Create() => View();

        // Handle create form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);

            customer.PartitionKey = "Customer";
            customer.RowKey = Guid.NewGuid().ToString();

            try
            {
                await _storageService.AddEntityAsync(customer);
                TempData["SuccessMessage"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer: {Message}", ex.Message);
                ModelState.AddModelError("", "Failed to create customer.");
                return View(customer);
            }
        }

        // Show edit form
        [HttpGet]
        public async Task<IActionResult> Edit(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();

            var customer = await _storageService.GetEntityAsync<Customer>("Customer", rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer, string ETag)
        {
            if (!ModelState.IsValid) return View(customer);

            // Convert string to Azure.ETag
            customer.ETag = new Azure.ETag(ETag);

            try
            {
                await _storageService.UpdateEntityAsync(customer);
                TempData["SuccessMessage"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {Message}", ex.Message);
                ModelState.AddModelError("", "Failed to update customer.");
                return View(customer);
            }
        }

        // GET: Show confirmation page
        [HttpGet]
        public async Task<IActionResult> Delete(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();

            var customer = await _storageService.GetEntityAsync<Customer>("Customer", rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Handle actual delete action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return BadRequest();

            try
            {
                await _storageService.DeleteEntityAsync<Customer>("Customer", rowKey);
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
