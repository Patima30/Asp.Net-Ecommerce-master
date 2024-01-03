using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ecommerce_Mvc.Models;
using Ecommerce_Mvc.ViewModel;

namespace Ecommerce_Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        // Constructor for initializing UserManager, SignInManager, and JWT secret key
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Registration action - displays the registration view
        public IActionResult Register()
        {
            return View();
        }

        // POST method for user registration
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create a new ApplicationUser based on the registration model
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                };

                // Attempt to create the user in the Identity system
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Sign in the user after successful registration
                    await _signInManager.SignInAsync(user, isPersistent: false);


                    // Redirect to the login page after successful registration
                    return RedirectToAction("Login");
                }

                // Display registration errors if the creation was not successful
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // Login action - displays the login view
        public IActionResult Login()
        {
            return View();
        }

        // POST method for user login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to sign in the user using the provided credentials
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {

                    Response.Cookies.Append("Email", model.Email, new CookieOptions
                    {
                        HttpOnly = true,
                        // Other cookie options as needed
                    });

                    // Redirect to the product page after successful login
                    return RedirectToAction("Index", "Product");
                }
                else if (result.RequiresTwoFactor)
                {
                    // Handle two-factor authentication if it's enabled for the user
                    // You may redirect to a two-factor authentication page
                }
                else if (result.IsLockedOut)
                {
                    // Handle account lockout
                    // You may redirect to a lockout page
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt");
                }
            }

            return View(model);
        }

        // POST method for user logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await _signInManager.SignOutAsync();

            // Remove the JWT token cookie
            Response.Cookies.Delete("JwtToken");

            // Redirect to the home page or another desired page after logout
            return RedirectToAction("Index", "Product");
        }


     
    }

}