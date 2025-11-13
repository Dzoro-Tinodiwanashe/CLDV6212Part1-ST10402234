using ABCRetailers.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Redirect to Login if user is not logged in
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Login");
            }

            // Hardcoded featured products for simplicity
            var featuredProducts = new List<Product>
    {
        new Product { ProductName = "Product 1", Price = 100, ImageURL = "/images/product1.jpg" },
        new Product { ProductName = "Product 2", Price = 250, ImageURL = "/images/product2.jpg" },
        new Product { ProductName = "Product 3", Price = 75, ImageURL = "/images/product3.jpg" }
    };

            ViewBag.FeaturedProducts = featuredProducts;
            return View();
        }


        public IActionResult Contact() => View();
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ---------------------------
        // Role-based Dashboards
        // ---------------------------

        public IActionResult AdminDashboard()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                TempData["Error"] = "Access denied!";
                return RedirectToAction("Index");
            }

            // Load all orders or products for admin if needed
            ViewBag.Message = "Welcome, Admin!";
            return View();
        }

        public IActionResult CustomerDashboard()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Customer")
            {
                TempData["Error"] = "Access denied!";
                return RedirectToAction("Index");
            }

            // Load user-specific info, e.g., cart or past orders
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Message = "Welcome, Customer!";
            return View();
        }
    }
}
