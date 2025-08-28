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
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Upload the file to Azure File Share using existing method
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
