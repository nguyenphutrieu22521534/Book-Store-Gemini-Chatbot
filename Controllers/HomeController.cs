using BookMart.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using BookMart.Repositories;
using System.Linq;

namespace BookMart.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeRepository _homeRepository;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserOrderRepository _userOrderRepo;
        private readonly ICartRepository _cartRepo;
        private readonly string _apiKey = "AIzaSyCeoFu8tHB8bW6Icpw0EL3Kj72aqO23yI4";

        public HomeController(ILogger<HomeController> logger, IHomeRepository homeRepository, ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserOrderRepository userOrderRepo, ICartRepository cartRepo)
        {
            _logger = logger;
            _homeRepository = homeRepository;
            _db = db;
            _userManager = userManager;
            _userOrderRepo = userOrderRepo;
            _cartRepo = cartRepo;
        }

        public async Task<IActionResult> Index(string sterm="",int genreId=0)
        {
            
            IEnumerable<Book> books =await _homeRepository.GetBooks(sterm, genreId);
            IEnumerable<Genre> genres = await _homeRepository.Genres();
            BookDisplayModel bookModel = new BookDisplayModel
            {
                Books = books,
                Genres = genres,
                STerm=sterm,
                GenreId= genreId

            };
            //IEnumerable<Book> books = await _homeRepository.GetBooks(sterm, genreId);
            return View(bookModel);
        }
        public IActionResult Starting()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Chat()
        {
            var historyJson = HttpContext.Session.GetString("ChatHistory");
            List<ChatMessage> history = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(historyJson))
            {
                history = JsonSerializer.Deserialize<List<ChatMessage>>(historyJson);
            }
            ViewBag.History = history;
            return View();
        }

        [HttpGet]
        public IActionResult ClearChat()
        {
            HttpContext.Session.Remove("ChatHistory");
            return RedirectToAction("Chat");
        }

        [HttpPost]
        public async Task<IActionResult> Chat(string userMessage)
        {
            var historyJson = HttpContext.Session.GetString("ChatHistory");
            List<ChatMessage> history = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(historyJson))
            {
                history = JsonSerializer.Deserialize<List<ChatMessage>>(historyJson);
            }

            history.Add(new ChatMessage { Sender = "user", Text = userMessage });

            string responseText = "";

            if (userMessage == "Show my orders")
            {
                if (User.Identity.IsAuthenticated)
                {
                    try
                    {
                        var orders = await _userOrderRepo.UserOrders();
                        if (!orders.Any())
                        {
                            responseText = "You have no orders yet.";
                        }
                        else
                        {
                            var orderInfo = new StringBuilder("Your Orders:\n");
                            foreach (var order in orders)
                            {
                                var total = order.OrderDetail.Sum(od => od.Quantity * od.UnitPrice);
                                orderInfo.AppendLine($" Date: {order.CreateDate:yyyy-MM-dd}, Status: {order.OrderStatus.StatusName}, Total: $.{total}");
                                if (order.OrderDetail.Any())
                                {
                                    orderInfo.Append("  Items: ");
                                    orderInfo.AppendLine(string.Join(", ", order.OrderDetail.Select(od => $"{od.Book.BookName} (Qty: {od.Quantity})")));
                                }
                                orderInfo.AppendLine();
                            }
                            responseText = orderInfo.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        responseText = $"Error fetching orders: {ex.Message}. Please try again.";
                    }
                }
                else
                {
                    responseText = "Please log in to view your orders. Go to the login page to sign in.";
                }
            }
            else if (userMessage == "Show my cart")
            {
                if (User.Identity.IsAuthenticated)
                {
                    try
                    {
                        var cart = await _cartRepo.GetUserCart();
                        if (!cart.CartDetails.Any())
                        {
                            responseText = "Your cart is empty.";
                        }
                        else
                        {
                            var cartInfo = new StringBuilder("Your Cart:\n");
                            var total = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);
                            foreach (var cd in cart.CartDetails)
                            {
                                cartInfo.AppendLine($" {cd.Book.BookName}, Quantity: {cd.Quantity}, Price: ${cd.UnitPrice * cd.Quantity}");
                            }
                            cartInfo.AppendLine($"\nTotal: ${total}");
                            responseText = cartInfo.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        responseText = $"Error fetching cart: {ex.Message}. Please try again.";
                    }
                }
                else
                {
                    responseText = "Please log in to view your cart. Go to the login page to sign in.";
                }
            }
            else if (userMessage == "Show my profile")
            {
                if (User.Identity.IsAuthenticated)
                {
                    try
                    {
                        var user = await _userManager.GetUserAsync(User);
                        responseText = $"Your Profile:\nEmail: {user.Email}";
                        // Add more fields if custom user properties exist
                    }
                    catch (Exception ex)
                    {
                        responseText = $"Error fetching profile: {ex.Message}. Please try again.";
                    }
                }
                else
                {
                    responseText = "Please log in to view your profile. Go to the login page to sign in.";
                }
            }
            else
            {
                // Fetch knowledge from DB (books, genres, etc.)
                var books = _db.Books.Select(b => $"{b.BookName} by {b.AuthorName} ,  Price {b.Price} , Quantity {b.Stock.Quantity}").ToList();
                var knowledgeBase = string.Join("\n", books);

                string faqs = @"
Common FAQs:
- How to order a book: Browse books on the home page, click 'Add to Cart' on a book, go to Cart via the navigation, click Checkout, fill in your details, and pay with Razorpay.
- How to pay: At the checkout page, enter your payment details using Razorpay for secure online payment.
- Track my order: Use the 'My Orders' button in the chat (after login) or visit the User Orders page.
- Returns and refunds: Contact the admin via email at support@bookmart.com for returns. We accept returns within 30 days.
- Shipping: Standard delivery within 5-7 business days. Free shipping on orders over $50.
- Account management: Log in to view profile, cart, and orders. Update details in your account settings.";

                string prompt = $@"
You are a helpful assistant for BookMart bookstore.
Here is knowledge from the database:
{knowledgeBase}

{faqs}

User asked: {userMessage}
Answer using the above knowledge when possible. If the user requests a book description, fetch a brief summary from the internet and provide it. Keep responses concise and helpful.";

                responseText = await CallGeminiApi(prompt);
            }

            history.Add(new ChatMessage { Sender = "bot", Text = responseText });

            HttpContext.Session.SetString("ChatHistory", JsonSerializer.Serialize(history));

            // For view: history except last two (current user and bot)
            ViewBag.History = history.Take(history.Count - 2).ToList();

            ViewBag.UserMessage = userMessage;
            ViewBag.Response = responseText;

            return View();
        }


        private async Task<string> CallGeminiApi(string prompt)
        {
            using var client = new HttpClient();
            // Use a valid model name, e.g. "gemini-2.5-flash"
            var modelName = "gemini-2.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
            new {
                role = "user",
                parts = new[] {
                    new { text = prompt }
                }
            }
        }
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            try
            {
                var parsed = JObject.Parse(result);
                var reply = parsed["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                if (!string.IsNullOrEmpty(reply))
                    return reply;
                var error = parsed["error"]?["message"]?.ToString();
                return error ?? "No response from Gemini.";
            }
            catch (Exception ex)
            {
                return $"Error parsing Gemini response: {ex.Message}\nRaw: {result}";
            }
        }





    }



}
