using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;

namespace ABCRetailers.Controllers
{
    // Handles customer-related actions in the web app
    public class CustomerController : Controller
    {
        private readonly IFunctionsApi _api;

        // Injects the API service used to call Azure Functions
        public CustomerController(IFunctionsApi api) => _api = api;

        // Displays a list of all customers
        public async Task<IActionResult> Index()
        {
            var customers = await _api.GetCustomersAsync();
            return View(customers);
        }

        // Shows the empty form to create a new customer
        public IActionResult Create() => View();

        // Handles form submission for creating a customer
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer); // If form is invalid, redisplay it

            try
            {
                await _api.CreateCustomerAsync(customer); // Call backend to save customer
                TempData["Success"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index)); // Go back to list
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                return View(customer); // Show error on form
            }
        }

        // Loads the customer data into the edit form
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound(); // No ID provided

            var customer = await _api.GetCustomerAsync(id);
            return customer is null ? NotFound() : View(customer); // Show form or 404
        }

        // Handles form submission for editing a customer
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer); // If form is invalid, redisplay it

            try
            {
                await _api.UpdateCustomerAsync(customer.Id, customer); // Save changes
                TempData["Success"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index)); // Go back to list
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                return View(customer); // Show error on form
            }
        }

        // Deletes a customer by ID
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _api.DeleteCustomerAsync(id); // Call backend to delete
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }

            return RedirectToAction(nameof(Index)); // Go back to list
        }
    }
}