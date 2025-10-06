using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IAzureStorageService storageService, ILogger<UploadController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            // Validate that a file was selected
            if (!ModelState.IsValid || model.ProofOfPayment == null || model.ProofOfPayment.Length == 0)
            {
                ModelState.AddModelError("ProofOfPayment", "Please select a valid file to upload.");
                return View(model);
            }

            try
            {
                // Upload the file safely to Azure File Share
                var fileName = await _storageService.UploadToFileShareAsync(
                    model.ProofOfPayment, "contracts", "payments");

                TempData["SuccessMessage"] = $"File uploaded successfully as {fileName}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {Message}", ex.Message);
                ModelState.AddModelError("", "Failed to upload file. Please try again.");
                return View(model);
            }
        }

        // Download file using existing method
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["ErrorMessage"] = "Invalid file name.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var fileBytes = await _storageService.DownloadFromFileShareAsync(
                    "contracts", fileName, "payments");

                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Failed to download file.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
