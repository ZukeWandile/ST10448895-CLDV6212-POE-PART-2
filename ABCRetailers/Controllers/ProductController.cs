using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    // Handles product-related actions in the web app
    public class ProductController : Controller
    {
        private readonly IFunctionsApi _api; // Service to call Azure Functions
        private readonly ILogger<ProductController> _logger; // Logger for error tracking

        // Inject dependencies via constructor
        public ProductController(IFunctionsApi api, ILogger<ProductController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // Displays a list of all products
        public async Task<IActionResult> Index()
        {
            var products = await _api.GetProductsAsync();
            return View(products);
        }

        // Shows the empty form to create a new product
        public IActionResult Create() => View();

        // Handles form submission for creating a product (with optional image)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(product); // If form is invalid, redisplay it

            try
            {
                var saved = await _api.CreateProductAsync(product, imageFile); // Save product via API
                TempData["Success"] = $"Product '{saved.ProductName}' created successfully with price {saved.Price:C}!";
                return RedirectToAction(nameof(Index)); // Go back to product list
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                return View(product); // Show error on form
            }
        }

        // Loads product data into the edit form
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound(); // No ID provided

            var product = await _api.GetProductAsync(id);
            return product is null ? NotFound() : View(product); // Show form or 404
        }

        // Handles form submission for editing a product (with optional image)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(product); // If form is invalid, redisplay it

            try
            {
                var updated = await _api.UpdateProductAsync(product.Id, product, imageFile); // Save changes
                TempData["Success"] = $"Product '{updated.ProductName}' updated successfully!";
                return RedirectToAction(nameof(Index)); // Go back to product list
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                return View(product); // Show error on form
            }
        }

        // Deletes a product by ID
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _api.DeleteProductAsync(id); // Call backend to delete
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index)); // Go back to product list
        }
    }
}