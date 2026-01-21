using BookMart.Models;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public HomeController(ILogger<HomeController> logger, IHomeRepository homeRepository, ApplicationDbContext db, UserManager<IdentityUser> userManager, IUserOrderRepository userOrderRepo, ICartRepository cartRepo, IConfiguration configuration)
        {
            _logger = logger;
            _homeRepository = homeRepository;
            _db = db;
            _userManager = userManager;
            _userOrderRepo = userOrderRepo;
            _cartRepo = cartRepo;
            _configuration = configuration;
            _apiKey = _configuration["Gemini:ApiKey"];
            _groqApiKey = _configuration["Groq:ApiKey"];
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




        // ===== Chatbot Widget API (Groq) =====
        
        // ===== Chatbot Widget API (Groq) =====
        
        private readonly string _groqApiKey;

        public class ChatRequest
        {
            public string Message { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ChatbotWidget([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Message))
            {
                return Json(new { response = "Vui lòng nhập tin nhắn." });
            }

            try
            {
                // Build knowledge base from database
                var books = _db.Books.Select(b => $"- {b.BookName} của {b.AuthorName}, giá {b.Price:N0}đ").ToList();
                var genres = _db.Genres.Select(g => g.GenreName).ToList();
                var knowledgeBase = string.Join("\n", books);
                var genreList = string.Join(", ", genres);

                // Vietnamese system prompt
                string systemPrompt = $@"Bạn là trợ lý ảo của cửa hàng sách BookMart. Hãy trả lời bằng tiếng Việt, thân thiện và hữu ích.

THÔNG TIN CỬA HÀNG:
- Tên: BookMart
- Địa chỉ: Thủ Đức, TP. Hồ Chí Minh
- Điện thoại: +84 858 976 459
- Email: 22521534@gm.uit.edu.vn

DANH SÁCH SÁCH HIỆN CÓ:
{knowledgeBase}

THỂ LOẠI: {genreList}

CHÍNH SÁCH:
- Thanh toán: Hỗ trợ thanh toán qua Razorpay (thẻ tín dụng, thẻ ghi nợ)
- Giao hàng: 5-7 ngày làm việc, miễn phí cho đơn trên 500.000đ
- Đổi trả: Trong vòng 30 ngày nếu sách bị lỗi
- Khuyến mãi: Giảm 10% cho thành viên mới

HƯỚNG DẪN THANH TOÁN:
1. Thêm sách vào giỏ hàng
2. Vào trang Giỏ hàng (Cart)
3. Nhấn Checkout
4. Điền thông tin giao hàng
5. Thanh toán qua Razorpay

Nếu không biết câu trả lời, hãy nói rằng bạn sẽ chuyển cho nhân viên hỗ trợ.
Trả lời ngắn gọn, không quá 3-4 câu.";

                var response = await CallGroqApi(systemPrompt, request.Message);
                return Json(new { response = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatbotWidget");
                return Json(new { response = "Xin lỗi, đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        private async Task<string> CallGroqApi(string systemPrompt, string userMessage)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqApiKey}");

            var url = "https://api.groq.com/openai/v1/chat/completions";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.7,
                max_tokens = 500
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            try
            {
                var parsed = JObject.Parse(result);
                var reply = parsed["choices"]?[0]?["message"]?["content"]?.ToString();
                if (!string.IsNullOrEmpty(reply))
                    return reply;
                var error = parsed["error"]?["message"]?.ToString();
                return error ?? "Không thể xử lý yêu cầu.";
            }
            catch (Exception ex)
            {
                return $"Lỗi: {ex.Message}";
            }
        }

    }



}
