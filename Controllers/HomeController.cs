using ABCRetailers.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
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

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
