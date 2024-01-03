﻿using Ecommerce_Mvc.Data;
using Microsoft.EntityFrameworkCore;

// Import necessary namespaces for the controller

using Ecommerce_Mvc.Models;
using Ecommerce_Mvc.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Ecommerce_Mvc.Controllers
{
   
    
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger; // ILogger for logging
        private readonly UserManager<ApplicationUser> _userManager; 
        private readonly IMemoryCache _memoryCache;

        // Constructor to initialize ApplicationDbContext and ILogger
        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger ,UserManager<ApplicationUser> userManager, IMemoryCache memoryCache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        // Action to display the list of products
      public async Task<IActionResult> Index(string search, string category)
{
    try
    {

        if (!_memoryCache.TryGetValue("key", out  var productListViewModels)){

        // Retrieve products from the database including category information
        IQueryable<Product> products = _context.Products.Include(p => p.Category);

        // Create a list of ProductListViewModel to display in the view
          productListViewModels = products
            .Select(p => new ProductListViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Color = p.Color,
                Image = p.Image,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.CategoryName,
                Quantity = 0
            })
            .ToList();

        _memoryCache.Set("key", productListViewModels, TimeSpan.FromMinutes(10));

        // Log successful product list retrieval
        _logger.LogInformation("\x1b[32m**********Product list retrieved successfully from database.**********\x1b[0m");

        }
        else if( !string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(category)) {

           IQueryable<Product> products = _context.Products.Include(p => p.Category);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            // Apply category filter if provided
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category.CategoryName == category);
            }

          //  Create a list of ProductListViewModel to display in the view
            productListViewModels = products
            .Select(p => new ProductListViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Color = p.Color,
                Image = p.Image,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.CategoryName,
                Quantity = 0
            })
            .ToList();

            // Log successful product list retrieval
            _logger.LogInformation("\x1b[32m**********Product list retrieved successfully  by search.**********\x1b[0m");

        }
        else {
            
           _logger.LogInformation("\x1b[32m**********Product list retrieved successfully from memory cache.**********\x1b[0m");

        }

        // Provide categories to the view
        ViewBag.Categories = _context.Categories.ToList();

        // Retrieve shopping cart from cookies or create a new one
        var shoppingCart = HttpContext.Request.Cookies.ContainsKey("ShoppingCart")
            ? JsonConvert.DeserializeObject<ShoppingCart>(HttpContext.Request.Cookies["ShoppingCart"])
            : new ShoppingCart();

        // Retrieve product IDs in the shopping cart
        var cartProductIds = shoppingCart.Items.Select(item => item.ProductId).ToList();

        // Retrieve products from the database based on the product IDs in the cart
        var cartProducts = _context.Products
            .Include(p => p.Category)
            .Where(p => cartProductIds.Contains(p.Id))
            .ToList();

        // Create a list of ProductListViewModel by joining products and shopping cart items
        var cartProductViewModels = cartProducts
            .Join(shoppingCart.Items,
                product => product.Id,
                cartItem => cartItem.ProductId,
                (product, cartItem) => new ProductListViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Color = product.Color,
                    Image = product.Image,
                    CategoryId = product.CategoryId,
                    Quantity = cartItem.Quantity,
                    CategoryName = product.Category.CategoryName
                })
            .ToList();

        // Set the CartProducts to ViewBag
        ViewBag.CartProducts = cartProductViewModels;

        // Update the shopping cart in session
        HttpContext.Session.SetString("ShoppingCart", JsonConvert.SerializeObject(shoppingCart));



        var Email = HttpContext.Request.Cookies["Email"];

        if (!string.IsNullOrEmpty(Email))
        {
          
                // Retrieve user information based on the email
                var normalizedEmail = _userManager.NormalizeEmail(Email);
                var currentUser = await _userManager.FindByEmailAsync(normalizedEmail);
               
                if (currentUser != null)
                { 
                    // Pass user information to the view
                    ViewBag.CurrentUser = currentUser;
                }
            
        }
        // Return the view with the list of products
        return View(productListViewModels);
    }
    catch (Exception ex)
    {
        // Log errors and redirect to the error page
        _logger.LogError("\x1b[31mError in Index action: {ErrorMessage}\x1b[0m", ex.Message);
        _logger.LogError("\x1b[31mStack Trace: {StackTrace}\x1b[0m", ex.StackTrace);
        TempData["ErrorMsg"] = "An error occurred while processing your request.";
        return RedirectToAction("Index");
    }
}


        // Action to display the form for creating a new product
        public IActionResult Create()
        {
            // Create a new ProductViewModel and provide categories to the view
            ProductViewModel productCreateViewModel = new ProductViewModel();
            productCreateViewModel.Category = (IEnumerable<SelectListItem>)_context.Categories.Select(c =>
                new SelectListItem()
                {
                    Text = c.CategoryName,
                    Value = c.CategoryId.ToString()
                });

            RafraichirCache();
            // Return the view with the form for creating a new product
            return View(productCreateViewModel);
        }

        // POST action to handle the creation of a new product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductViewModel productCreateViewModel)
        {
            try
            {
                // Provide categories to the view
                productCreateViewModel.Category = (IEnumerable<SelectListItem>)_context.Categories.Select(c =>
                    new SelectListItem()
                    {
                        Text = c.CategoryName,
                        Value = c.CategoryId.ToString()
                    });

                // Create a new Product instance from the form data
                var product = new Product()
                {
                    Name = productCreateViewModel.Name,
                    Description = productCreateViewModel.Description,
                    Price = productCreateViewModel.Price,
                    Color = productCreateViewModel.Color,
                    CategoryId = productCreateViewModel.CategoryId,
                    Image = productCreateViewModel.Image
                };

                // Remove "Category" from ModelState to avoid validation issues
                ModelState.Remove("Category");

                // Validate the model
                if (ModelState.IsValid)
                {
                    // Add the new product to the database and save changes
                    _context.Products.Add(product);
                    _context.SaveChanges();

                    // Provide a success message to be displayed
                    TempData["SuccessMsg"] = "Product (" + product.Name + ") added successfully.";

                    // Log the successful addition of a product
                    _logger.LogInformation("\x1b[32m**********Product added successfully: {ProductName}**********\x1b[0m", product.Name);

                    // Redirect to the product list page
                    return RedirectToAction("Index");
                }
                RafraichirCache();
                // Return the view with validation errors
                return View(productCreateViewModel);
            }
            catch (Exception ex)
            {
                // Log errors and redirect to the error page
                _logger.LogError(ex, "\x1b[31mError in Create action: {ErrorMessage}\x1b[0m", ex.Message);
                TempData["ErrorMsg"] = "An error occurred while creating the product.";
                return RedirectToAction("Create");
            }
        }

        // Action to display the form for editing an existing product
        public IActionResult Edit(int? id)
        {
            try
            {
                // Find the product to edit based on the provided ID
                var productToEdit = _context.Products.Find(id);

                // Check if the product exists
                if (productToEdit != null)
                {
                    // Create a new ProductViewModel for the product
                    var productViewModel = new ProductViewModel()
                    {
                        Id = productToEdit.Id,
                        Name = productToEdit.Name,
                        Description = productToEdit.Description,
                        Price = productToEdit.Price,
                        CategoryId = productToEdit.CategoryId,
                        Color = productToEdit.Color,
                        Image = productToEdit.Image,
                        // Provide categories to the view
                        Category = (IEnumerable<SelectListItem>)_context.Categories.Select(c => new SelectListItem()
                        {
                            Text = c.CategoryName,
                            Value = c.CategoryId.ToString()
                        })
                    };
                    RafraichirCache();
                    // Return the view with the form for editing the product
                    return View(productViewModel);
                }
                else
                {
                    // If the product does not exist, redirect to the product list page
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                // Log errors and redirect to the error page
                _logger.LogError(ex, "\x1b[31mError in Edit action: {ErrorMessage}\x1b[0m", ex.Message);
                TempData["ErrorMsg"] = "An error occurred while processing your request.";
                return RedirectToAction("Index");
            }
        }

        // POST action to handle the editing of an existing product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductViewModel productViewModel)
        {
            try
            {
                // Provide categories to the view
                productViewModel.Category = (IEnumerable<SelectListItem>)_context.Categories.Select(c =>
                    new SelectListItem()
                    {
                        Text = c.CategoryName,
                        Value = c.CategoryId.ToString()
                    });

                // Create a new Product instance from the form data
                var product = new Product()
                {
                    Id = productViewModel.Id,
                    Name = productViewModel.Name,
                    Description = productViewModel.Description,
                    Price = productViewModel.Price,
                    Color = productViewModel.Color,
                    CategoryId = productViewModel.CategoryId,
                    Image = productViewModel.Image
                };

                // Remove "Category" from ModelState to avoid validation issues
                ModelState.Remove("Category");

                // Validate the model
                if (ModelState.IsValid)
                {
                    // Update the product in the database and save changes
                    _context.Products.Update(product);
                    _context.SaveChanges();

                    // Provide a success message to be displayed
                    TempData["SuccessMsg"] = "Product (" + product.Name + ") updated successfully !";

                    // Log the successful update of a product
                    _logger.LogInformation("\x1b[32m**********Product updated successfully: {ProductName}**********\x1b[0m", product.Name);
                      RafraichirCache();
                    // Redirect to the product list page
                    return RedirectToAction("Index");
                }

                // Return the view with validation errors
                return View(productViewModel);
            }
            catch (Exception ex)
            {
                // Log errors and redirect to the error page
                _logger.LogError(ex, "\x1b[31mError in Edit action: {ErrorMessage}\x1b[0m", ex.Message);
                TempData["ErrorMsg"] = "An error occurred while processing your request.";
                return RedirectToAction("Index");
            }
        }

        // Action to display the form for deleting an existing product
        public IActionResult Delete(int? id)
        {
            // Find the product to delete based on the provided ID
            var productToEdit = _context.Products.Find(id);

            // Check if the product exists
            if (productToEdit != null)
            {
                // Create a new ProductViewModel for the product
                var productViewModel = new ProductViewModel()
                {
                    Id = productToEdit.Id,
                    Name = productToEdit.Name,
                    Description = productToEdit.Description,
                    Price = productToEdit.Price,
                    CategoryId = productToEdit.CategoryId,
                    Color = productToEdit.Color,
                    Image = productToEdit.Image,
                    // Provide categories to the view
                    Category = (IEnumerable<SelectListItem>)_context.Categories.Select(c => new SelectListItem()
                    {
                        Text = c.CategoryName,
                        Value = c.CategoryId.ToString()
                    })
                };
                RafraichirCache();
                // Return the view with the form for deleting the product
                return View(productViewModel);
            }
            else
            {
                // If the product does not exist, redirect to the product list page
                return RedirectToAction("Index");
            }
        }

        // POST action to handle the deletion of an existing product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int? id)
        {
            // Find the product to delete based on the provided ID
            var product = _context.Products.Find(id);

            // Check if the product exists
            if (product == null)
            {
                return NotFound();
            }

            // Remove the product from the database and save changes
            _context.Products.Remove(product);
            _context.SaveChanges();

            // Provide a success message to be displayed
            TempData["SuccessMsg"] = "Product (" + product.Name + ") deleted successfully.";

             RafraichirCache();
            // Redirect to the product list page
            return RedirectToAction("Index");
        }

        // POST action to handle adding a product to the shopping cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int quantity)
        {
            try
            {
                // Retrieve the shopping cart from cookies or create a new one
                var shoppingCart = HttpContext.Request.Cookies.ContainsKey("ShoppingCart")
                    ? JsonConvert.DeserializeObject<ShoppingCart>(HttpContext.Request.Cookies["ShoppingCart"])
                    : new ShoppingCart();

                // Add the selected product and quantity to the shopping cart
                shoppingCart.AddProduct(productId, quantity);

                // Serialize the shopping cart to JSON
                var shoppingCartJson = JsonConvert.SerializeObject(shoppingCart);

                // Set cookie options for the shopping cart
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(7),
                    HttpOnly = true,
                    Path = "/"
                };

                // Update the shopping cart in cookies and session
                Response.Cookies.Append("ShoppingCart", shoppingCartJson, cookieOptions);
                HttpContext.Session.SetString("ShoppingCart", shoppingCartJson);

                // Log the addition of products to the shopping cart
                _logger.LogInformation($"\x1b[32m**********Added {quantity} {(quantity > 1 ? "items" : "item")} to the shopping cart.**********\x1b[0m");

                // Provide a success message to be displayed
                TempData["SuccessMsg"] = $"Added {quantity} {(quantity > 1 ? "items" : "item")} to the shopping cart.";

                // Redirect to the shopping cart view
                return RedirectToAction("ViewCart");
            }
            catch (Exception ex)
            {
                // Log errors and redirect to the error page
                _logger.LogError(ex, "\x1b[31mError in AddToCart action: {ErrorMessage}\x1b[0m", ex.Message);
                TempData["ErrorMsg"] = "An error occurred while adding the product to the shopping cart.";
                return RedirectToAction("ViewCart");
            }
        }

        // Action to display the shopping cart
        public IActionResult ViewCart()
        {
            try
            {
                // Retrieve the shopping cart from cookies or create a new one
                var shoppingCart = HttpContext.Request.Cookies.ContainsKey("ShoppingCart")
                    ? JsonConvert.DeserializeObject<ShoppingCart>(HttpContext.Request.Cookies["ShoppingCart"])
                    : new ShoppingCart();

                // Retrieve product IDs in the shopping cart
                var cartProductIds = shoppingCart.Items.Select(item => item.ProductId).ToList();

                // Retrieve products from the database based on the product IDs in the cart
                var cartProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => cartProductIds.Contains(p.Id))
                    .ToList();

                // Create a list of ProductListViewModel by joining products and shopping cart items
                var cartProductViewModels = cartProducts
                    .Join(shoppingCart.Items,
                        product => product.Id,
                        cartItem => cartItem.ProductId,
                        (product, cartItem) => new ProductListViewModel
                        {
                            Id = product.Id,
                            Name = product.Name,
                            Description = product.Description,
                            Price = product.Price,
                            Color = product.Color,
                            Image = product.Image,
                            CategoryId = product.CategoryId,
                            Quantity = cartItem.Quantity,
                            CategoryName = product.Category.CategoryName
                        })
                    .ToList();

                // Set ViewBag.CartProducts to be used in the View
                ViewBag.CartProducts = cartProductViewModels;

                // Update the session with the latest shopping cart information
                HttpContext.Session.SetString("ShoppingCart", JsonConvert.SerializeObject(shoppingCart));

                // Log a success message indicating the shopping cart retrieval
                _logger.LogInformation("\x1b[32m**********Shopping cart retrieved successfully.**********\x1b[0m");

                // Get the product list view models to display in the Index view
                var productListViewModels = GetProductListViewModels();

                // Set ViewBag.Categories to be used in the View
                ViewBag.Categories = _context.Categories.ToList();

                // Return the Index view with the updated product list view models
                return View("Index", productListViewModels);
            }
            catch (Exception ex)
            {
                // Log an error message if an exception occurs during the ViewCart action
                _logger.LogError("\x1b[31mError in ViewCart action: {ErrorMessage}\x1b[0m", ex.Message);

                // Set an error message to be displayed in the View
                TempData["ErrorMsg"] = "An error occurred while retrieving the shopping cart.";

                // Return the Index view with the product list view models
                return View("Index", GetProductListViewModels());
            }
        }

// Cache
        public IActionResult RafraichirCache()
 {
     // Effacez le cache pour forcer une nouvelle récupération des données lors de la prochaine demande
     _memoryCache.Remove("key");
     _logger.LogInformation($"rafraichir les donnees pour un ajout");
     return RedirectToAction("Index");
 }

// Helper method to get a list of ProductListViewModel from the database
        private List<ProductListViewModel> GetProductListViewModels()
        {
            // Retrieve all products from the database with their associated categories
            IQueryable<Product> products = _context.Products.Include(p => p.Category);

            // Create a list of ProductListViewModel using the retrieved products
            var productListViewModelList = products.Select(item => new ProductListViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Color = item.Color,
                Price = item.Price,
                CategoryId = item.CategoryId,
                Image = item.Image,
                CategoryName = item.Category.CategoryName
            }).ToList();

            // Return the list of ProductListViewModel
            return productListViewModelList;
        }
        
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int productId)
        {
            try
            {
                // Retrieve the shopping cart from cookies or create a new one
                var shoppingCart = HttpContext.Request.Cookies.ContainsKey("ShoppingCart")
                    ? JsonConvert.DeserializeObject<ShoppingCart>(HttpContext.Request.Cookies["ShoppingCart"])
                    : new ShoppingCart();

                // Remove the selected product from the shopping cart
                shoppingCart.RemoveProduct(productId);

                // Serialize the shopping cart to JSON
                var shoppingCartJson = JsonConvert.SerializeObject(shoppingCart);

                // Set cookie options for the shopping cart
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(7),
                    HttpOnly = true,
                    Path = "/"
                };

                // Update the shopping cart in cookies and session
                Response.Cookies.Append("ShoppingCart", shoppingCartJson, cookieOptions);
                HttpContext.Session.SetString("ShoppingCart", shoppingCartJson);

                // Provide a success message to be displayed
                TempData["SuccessMsg"] = "Product removed from the shopping cart.";

                // Redirect to the shopping cart view
                return RedirectToAction("ViewCart");
            }
            catch (Exception ex)
            {
                // Log errors and redirect to the error page
                _logger.LogError(ex, "\x1b[31mError in RemoveFromCart action: {ErrorMessage}\x1b[0m", ex.Message);
                TempData["ErrorMsg"] = "An error occurred while removing the product from the shopping cart.";
                return RedirectToAction("ViewCart");
            }
        }

    }
    
}


