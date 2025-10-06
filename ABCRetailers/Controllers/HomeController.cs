using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;           // IFunctionsApi
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ABCRetailers.Controllers
{
    // Handles homepage and general site actions
    public class HomeController : Controller
    {
        private readonly IFunctionsApi _api; // Service to call Azure Functions
        private readonly ILogger<HomeController> _logger; // Logger for error tracking

        // Inject dependencies via constructor
        public HomeController(IFunctionsApi api, ILogger<HomeController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // Loads dashboard data for the homepage
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch products, customers, and orders in parallel
                var productsTask = _api.GetProductsAsync();
                var customersTask = _api.GetCustomersAsync();
                var ordersTask = _api.GetOrdersAsync();

                await Task.WhenAll(productsTask, customersTask, ordersTask);

                // Use fallback empty lists if any result is null
                var products = productsTask.Result ?? new List<Product>();
                var customers = customersTask.Result ?? new List<Customer>();
                var orders = ordersTask.Result ?? new List<Order>();

                // Build the view model for the dashboard
                var vm = new HomeViewModel
                {
                    FeaturedProducts = products.Take(8).ToList(), // Show top 8 products
                    ProductCount = products.Count,
                    CustomerCount = customers.Count,
                    OrderCount = orders.Count
                };

                return View(vm); // Render the dashboard view
            }
            catch (Exception ex)
            {
                // Log the error and show fallback view
                _logger.LogError(ex, "Failed to load dashboard data from Functions API.");
                TempData["Error"] = "Could not load dashboard data. Please try again.";
                return View(new HomeViewModel()); // Show empty dashboard
            }
        }

        // Static privacy page
        public IActionResult Privacy() => View();

        // Error page with request ID for diagnostics
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}