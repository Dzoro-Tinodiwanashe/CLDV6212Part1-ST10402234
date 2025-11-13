using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class CartController : Controller
    {
        private readonly AuthDbContext _context;

        public CartController(AuthDbContext context)
        {
            _context = context;
        }

        // GET: Cart/Index - view cart items
        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            List<Cart> cartItems;

            if (role == "Admin")
            {
                cartItems = await _context.Carts.ToListAsync();
            }
            else
            {
                cartItems = await _context.Carts
                    .Where(c => c.CustomerUsername == username)
                    .ToListAsync();
            }

            var model = cartItems.Select(c => new CartItemViewModel
            {
                Id = c.Id, // add this
                ProductId = c.ProductId,
                Quantity = c.Quantity,
                Status = c.Status
            }).ToList();


            // Pass role to view
            ViewBag.Role = role;

            return View(model);
        }


        // GET: Cart/Add?productId=XXX
        public IActionResult Add(string productId)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Simple view to add product quantity
            ViewBag.ProductId = productId;
            return View();
        }

        // POST: Cart/Add
        [HttpPost]
        public async Task<IActionResult> Add(string productId, int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var cartItem = new Cart
            {
                CustomerUsername = username,
                ProductId = productId,
                Quantity = quantity,
                Status = "Submitted"
            };

            _context.Carts.Add(cartItem);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Product added to cart!";
            return RedirectToAction("Index");
        }

        // POST: Cart/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return Unauthorized();

            var cartItem = await _context.Carts.FindAsync(id);
            if (cartItem == null)
                return NotFound();

            cartItem.Status = status;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Status updated!";
            return RedirectToAction("Index");
        }

        // POST: Cart/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            var cartItem = await _context.Carts.FindAsync(id);
            if (cartItem == null)
                return NotFound();

            // Only admin or owner can remove
            if (role != "Admin" && cartItem.CustomerUsername != username)
                return Unauthorized();

            _context.Carts.Remove(cartItem);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Item removed from cart!";
            return RedirectToAction("Index");
        }

        // GET: Cart/Confirmation
        public async Task<IActionResult> Confirmation()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Get all user's cart items with status Submitted
            var cartItems = await _context.Carts
                .Where(c => c.CustomerUsername == username && c.Status == "Submitted")
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Message"] = "Your cart is empty!";
                return RedirectToAction("Index");
            }

            // Optionally: mark them as Pending or leave as Submitted
            foreach (var item in cartItems)
            {
                item.Status = "Pending"; // customer submitted order
            }
            await _context.SaveChangesAsync();

            return View(cartItems);
        }

    }
}
