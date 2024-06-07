using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Shapzada.Models;

namespace Shapzada.Controllers
{
    public class HomeController : Controller
    {
        private string connStr = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\mangu\source\repos\Shapzada\Shapzada\App_Data\Database1.mdf;Integrated Security=True";

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Error()
        {
            return View();
        }
    

    public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        private bool IsAdmin()
        {
            return Session["IsAdmin"] != null && (bool)Session["IsAdmin"];
        }

        public ActionResult Customer()
        {
            return View();
        }

        public ActionResult Allproduct()
        {
            List<Product> products = GetProductsFromDatabase();

            // Check if the user is an admin
            if (IsAdmin())
            {
                // If the user is an admin, return all products
                return View(products);
            }
            else
            {
                // If the user is not an admin, filter out products with quantity zero
                List<Product> filteredProducts = products.Where(p => p.Quantity > 0).ToList();
                return View(filteredProducts);
            }
        }

        public ActionResult Allproductuser()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login");
            }

            // Filter products with quantity greater than zero
            List<Product> products = GetProductsFromDatabase().Where(p => p.Quantity > 0).ToList();
            return View(products);
        }
        public ActionResult Login()
        {
            return View();  
        }


        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Check if the user is an admin
                    string adminQuery = "SELECT Id FROM [Admin] WHERE Admin_email = @Email AND Admin_pass = @Password";
                    SqlCommand adminCmd = new SqlCommand(adminQuery, conn);
                    adminCmd.Parameters.AddWithValue("@Email", email);
                    adminCmd.Parameters.AddWithValue("@Password", password); // Note: Replace with hashed password

                    var adminResult = adminCmd.ExecuteScalar();

                    if (adminResult != null)
                    {
                        // Set session variable to indicate admin is logged in
                        Session["IsAdmin"] = true;
                        Session["UserId"] = (int)adminResult;

                        // Redirect admin to AuthPage
                        return RedirectToAction("Allproduct", "Home");
                    }

                    // Check if the user is a regular user
                    string userQuery = "SELECT Id FROM [User] WHERE Email = @Email AND Password = @Password";
                    SqlCommand userCmd = new SqlCommand(userQuery, conn);
                    userCmd.Parameters.AddWithValue("@Email", email);
                    userCmd.Parameters.AddWithValue("@Password", password); // Note: Replace with hashed password

                    var userResult = userCmd.ExecuteScalar();

                    if (userResult != null)
                    {
                        // Set session variable to indicate user is logged in
                        Session["IsAdmin"] = false;
                        Session["UserId"] = (int)userResult;

                        // Redirect user to the UserPage
                        return RedirectToAction("Userview", "Home");
                    }

                    ViewBag.ErrorMessage = "Invalid email or password";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View();
            }
        }

        public ActionResult Addcart(int productId, int quantity)
        {
            try
            {
                int? userId = GetUserIdFromSession();
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not logged in." });
                }

                if (string.IsNullOrEmpty(connStr))
                {
                    return Json(new { success = false, message = "Connection string is not set." });
                }

                using (var db = new SqlConnection(connStr))
                {
                    db.Open();

                    using (var cmd = db.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "BEGIN TRANSACTION; " +
                                          "DECLARE @AvailableQuantity INT; " +
                                          "SELECT @AvailableQuantity = Quantity FROM Product WHERE Id = @ProductId; " +
                                          "IF @AvailableQuantity >= @Quantity " +
                                          "BEGIN " +
                                          "   INSERT INTO Cart (UserId, ProductId, Quantity) VALUES (@UserId, @ProductId, @Quantity); " +
                                          "   UPDATE Product SET Quantity = Quantity - @Quantity WHERE Id = @ProductId; " +
                                          "   COMMIT TRANSACTION; " +
                                          "   SELECT 1 AS Success; " +
                                          "END " +
                                          "ELSE " +
                                          "BEGIN " +
                                          "   ROLLBACK TRANSACTION; " +
                                          "   SELECT 0 AS Success, 'Insufficient quantity available.' AS Message; " +
                                          "END";
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@Quantity", quantity);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int success = (int)reader["Success"];
                                if (success == 1)
                                {
                                    return Json(new { success = true, message = "Product added to cart successfully." });
                                }
                                else
                                {
                                    string errorMessage = reader["Message"].ToString();
                                    return Json(new { success = false, message = errorMessage });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while adding the product to the cart: " + ex.Message });
            }

            return Json(new { success = false, message = "An error occurred while adding the product to the cart." });
        }




        private int? GetUserIdFromSession()
        {
            // Assuming the user ID is stored in session as an integer
            return Session["UserId"] as int?;
        }

        public ActionResult Viewcart()
        {
            int? userId = GetUserIdFromSession();
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            List<CartItemViewModel> cartItems = new List<CartItemViewModel>();

            try
            {
                // Fetch cart items for the user
                using (var db = new SqlConnection(connStr))
                {
                    db.Open();

                    using (var cmd = db.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT ci.Id, ci.ProductId, p.Name AS ProductName, p.Price, p.Image AS Picture, ci.Quantity, (ci.Quantity * p.Price) AS TotalPrice " +
                                          "FROM Cart ci " +
                                          "JOIN Product p ON ci.ProductId = p.Id " +
                                          "WHERE ci.UserId = @UserId";
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cartItems.Add(new CartItemViewModel
                                {
                                    ProductName = reader["ProductName"].ToString(),
                                    ProductPrice = (int)reader["Price"],
                                    Quantity = (int)reader["Quantity"],
                                    TotalPrice = (int)reader["TotalPrice"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while fetching cart items: " + ex.Message;
                // Redirect to an error page or display error message directly
                return RedirectToAction("Error");
            }

            return View(cartItems);
        }

        // Helper method to get user ID from session (replace this with your actual implementation)


        [HttpPost]
        public ActionResult CheckOut(int productId)
        {
            try
            {
                // Get the user ID from session
                int? userId = GetUserIdFromSession();
                if (userId == null)
                {
                    return Json(new { success = false, message = "User not logged in." });
                }

                // Check if connStr is null or empty
                if (string.IsNullOrEmpty(connStr))
                {
                    return Json(new { success = false, message = "Connection string is not set." });
                }

                // Fetch cart items for the user
                List<CartItemViewModel> cartItems = new List<CartItemViewModel>();
                using (var db = new SqlConnection(connStr))
                {
                    db.Open();

                    using (var cmd = db.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT ci.Id, ci.ProductId, p.Name AS ProductName, p.Price, p.Image, ci.Quantity, (ci.Quantity * p.Price) AS TotalPrice " +
                                          "FROM Cart ci " +
                                          "JOIN Product p ON ci.ProductId = p.Id " +
                                          "WHERE ci.UserId = @UserId AND ci.ProductId = @ProductId";
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@ProductId", productId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cartItems.Add(new CartItemViewModel
                                {
                                    ProductId = (int)reader["ProductId"],
                                    ProductName = reader["ProductName"].ToString(),
                                    ProductPrice = (int)(decimal)reader["ProductPrice"],
                                    Quantity = (int)reader["Quantity"],
                                    TotalPrice = (int)(decimal)reader["TotalPrice"]
                                });
                            }
                        }
                    }
                }

                // Proceed with the checkout logic here...
                // For example, you could mark the items as purchased, etc.

                return Json(new { success = true, message = "Checkout successful!" });
            }
            catch (Exception ex)
            {
                // Log the exception
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }


        /*private List<CartItemViewModel> GetCartItems()
        {
            // Retrieve cart items from session or database
            var cartItems = Session["CartItems"] as List<CartItemViewModel> ?? new List<CartItemViewModel>();
            // Log the current cart items
            System.Diagnostics.Debug.WriteLine("Current Cart Items:");
            foreach (var item in cartItems)
            {
                System.Diagnostics.Debug.WriteLine($"ProductId: {item.ProductId}, ProductName: {item.ProductName}, Quantity: {item.Quantity}");
            }
            return cartItems;
        }

        private void SaveCartItems(List<CartItemViewModel> cartItems)
        {
            // Save cart items to session or database
            Session["CartItems"] = cartItems;
            // Log the cart items after saving
            System.Diagnostics.Debug.WriteLine("Cart Items after Save:");
            foreach (var item in cartItems)
            {
                System.Diagnostics.Debug.WriteLine($"ProductId: {item.ProductId}, ProductName: {item.ProductName}, Quantity: {item.Quantity}");
            }
        }*/

        /*private static List<CartItemViewModel> cartItems = new List<CartItemViewModel>();
        [HttpPost]
        public ActionResult Cancel(int productId)
        {
            var item = cartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (item != null)
            {
                cartItems.Remove(item);
            }
            return RedirectToAction("Viewcart");
        }*/
        [HttpPost]
        public ActionResult Cancel(int productId)
        {
            try
            {
                // Get the user ID from session
                int? userId = GetUserIdFromSession();
                if (userId == null)
                {
                    return RedirectToAction("Login"); // Redirect to login if the user is not logged in
                }

                // Check if connStr is null or empty
                if (string.IsNullOrEmpty(connStr))
                {
                    TempData["CancelMessage"] = "Connection string is not set.";
                    return RedirectToAction("ViewCart"); // Redirect to the view cart page
                }

                // Remove the product from the user's cart
                using (var db = new SqlConnection(connStr))
                {
                    db.Open();

                    using (var cmd = db.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "DELETE FROM Cart WHERE UserId = @UserId AND ProductId = @ProductId";
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@ProductId", productId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["CancelMessage"] = "Product cancelled successfully.";
                        }
                        else
                        {
                            TempData["CancelMessage"] = "Product not found in the cart.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                TempData["CancelMessage"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("ViewCart"); // Redirect to the view cart page
        }

        private List<Product> GetProductsFromDatabase()
        {
            List<Product> products = new List<Product>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "SELECT Id, Name, Price, Brand, Quantity, Description, Image FROM Product";
                SqlCommand cmd = new SqlCommand(query, conn);


                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Product product = new Product
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = reader["Name"].ToString(),
                        Price = Convert.ToInt32(reader["Price"]),
                        Brand = reader["Brand"].ToString(),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        Description = reader["Description"].ToString(),
                        Image = reader["Image"].ToString()
                    };

                    products.Add(product);
                }

                reader.Close();
            }

            return products;
        }

        private void DecrementProductQuantity(int productId)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "UPDATE Product SET Quantity = Quantity - 1 WHERE Id = @ProductId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProductId", productId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private Product GetProductById(int productId)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "SELECT Id, Name, Price, Brand, Quantity, Description, Image FROM Product WHERE Id = @ProductId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProductId", productId);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    Product product = new Product
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = reader["Name"].ToString(),
                        Price = Convert.ToInt32(reader["Price"]),
                        Brand = reader["Brand"].ToString(),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        Description = reader["Description"].ToString(),
                        Image = reader["Image"].ToString()
                    };

                    reader.Close();
                    return product;
                }
            }

            return null;
        }

        public ActionResult cartlogo()
        {
            return View();
        }

        public ActionResult Adduser()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddUserProcess(User user)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Check if email already exists
                    string checkQuery = "SELECT COUNT(*) FROM [User] WHERE Email = @Email";
                    SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@Email", user.Email);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        return Json(new { success = false, message = "Email already exists." });
                    }

                    // Hash the password
                    string hashedPassword = HashPassword(user.Password);

                    string insertQuery = "INSERT INTO [User] (Email, Password) VALUES (@Email, @Password); SELECT SCOPE_IDENTITY()";
                    SqlCommand cmd = new SqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);

                    int userId = Convert.ToInt32(cmd.ExecuteScalar());
                    user.Id = userId;
                }

                // Set session variable to indicate user is logged in
                Session["UserId"] = user.Id;

                return Json(new { success = true, userId = user.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error adding user: " + ex.Message });
            }
        }



        public ActionResult Createproduct()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        public ActionResult Createproduct(Product product, HttpPostedFileBase img)
        {
            if (!IsAdmin())
            {
                // Redirect non-admin users to Userview
                return RedirectToAction("Userview");
            }

            if (ModelState.IsValid)
            {
                // Check if a product with the same name, brand, and description already exists
                using (var db = new SqlConnection(connStr))
                {
                    db.Open();
                    using (var cmd = db.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT * FROM PRODUCT WHERE Name = @Name AND Brand = @Brand AND Description = @Description";
                        cmd.Parameters.AddWithValue("@Name", product.Name);
                        cmd.Parameters.AddWithValue("@Brand", product.Brand);
                        cmd.Parameters.AddWithValue("@Description", product.Description);

                        SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            // Product already exists, update the quantity
                            int existingQuantity = (int)reader["Quantity"];
                            int newQuantity = existingQuantity + product.Quantity;
                            int productId = (int)reader["Id"];

                            reader.Close(); // Close the DataReader

                            using (var updateCmd = db.CreateCommand())
                            {
                                updateCmd.CommandType = CommandType.Text;
                                updateCmd.CommandText = "UPDATE PRODUCT SET Quantity = @Quantity WHERE Id = @Id";
                                updateCmd.Parameters.AddWithValue("@Quantity", newQuantity);
                                updateCmd.Parameters.AddWithValue("@Id", productId);

                                updateCmd.ExecuteNonQuery();
                            }

                            return RedirectToAction("Allproduct");
                        }
                        else
                        {
                            reader.Close(); // Close the DataReader
                        }
                    }
                }

                // If no product with the same name, brand, and description exists, create a new one
                if (img != null && img.ContentLength > 0)
                {
                    string imageName = Path.GetFileName(img.FileName);
                    string logpath = Server.MapPath("~/Uploads");

                    if (!Directory.Exists(logpath))
                    {
                        Directory.CreateDirectory(logpath);
                    }

                    string filepath = Path.Combine(logpath, imageName);
                    img.SaveAs(filepath);
                    product.Image = imageName;
                }
                else
                {
                    ModelState.AddModelError("", "Please upload a valid image file.");
                    return View(product);
                }

                using (var db = new SqlConnection(connStr))
                {
                    db.Open();
                    using (var cmd = db.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "INSERT INTO PRODUCT (Name, Price, Brand, Quantity, Description, Image)"
                                        + "VALUES (@Name, @Price, @Brand, @Quantity, @Description, @Image)";

                        cmd.Parameters.AddWithValue("@Name", product.Name);
                        cmd.Parameters.AddWithValue("@Price", product.Price);
                        cmd.Parameters.AddWithValue("@Brand", product.Brand);
                        cmd.Parameters.AddWithValue("@Quantity", product.Quantity);
                        cmd.Parameters.AddWithValue("@Description", product.Description);
                        cmd.Parameters.AddWithValue("@Image", product.Image);

                        var ctr = cmd.ExecuteNonQuery();
                        if (ctr > 0)
                        {
                            return RedirectToAction("Allproduct");
                        }
                        else
                        {
                            ModelState.AddModelError("", "An error occurred while saving the product.");
                        }
                    }
                }
            }

            return View(product);
        }




        public ActionResult Admin()
        {
            return View();
        }

        [HttpPost]
        public JsonResult CreateAdminProcess(string admin_email, string admin_pass)
        {
            try
            {
                if (string.IsNullOrEmpty(admin_email) || string.IsNullOrEmpty(admin_pass))
                {
                    throw new ArgumentException("Email and Password cannot be empty.");
                }

                // Hash the password
                string hashedPassword = HashPassword(admin_pass);

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "INSERT INTO Admin (Admin_email, Admin_pass) VALUES (@admin_email, @admin_pass)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@admin_email", admin_email);
                    cmd.Parameters.AddWithValue("@admin_pass", hashedPassword);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Admin created successfully." });
                    }
                    else
                    {
                        throw new Exception("No rows were inserted into the database.");
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

   


        public ActionResult produpdate(int id)
        {
            if (!IsAdmin())
            {
                // Redirect non-admin users to Userview
                return RedirectToAction("Userview");
            }
            Product product = null;

            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT * FROM PRODUCT WHERE Id = @Id";
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            product = new Product
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Price = (int)reader["Price"],
                                Brand = reader["Brand"].ToString(),
                                Quantity = (int)reader["Quantity"],
                                Description = reader["Description"].ToString(),
                                Image = reader["Image"].ToString()
                            };
                        }
                    }
                }
            }

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // This action method is for handling the form submission and updating the product.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult produpdate(Product product)
        {
            if (!IsAdmin())
            {
                // Redirect non-admin users to Userview
                return RedirectToAction("Userview");
            }
            if (product.Price < 0)
            {
                ModelState.AddModelError("Price", "Price cannot be negative.");
            }
            if (product.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Quantity cannot be negative.");
            }
            if (ModelState.IsValid)
            {
                using (var db = new SqlConnection(connStr))
                {
                    db.Open();
                    using (var cmd = db.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "UPDATE PRODUCT SET Name = @Name, Price = @Price, Brand = @Brand, Quantity = @Quantity, Description = @Description, Image = @Image WHERE Id = @Id";

                        cmd.Parameters.AddWithValue("@Name", product.Name);
                        cmd.Parameters.AddWithValue("@Price", product.Price);
                        cmd.Parameters.AddWithValue("@Brand", product.Brand);
                        cmd.Parameters.AddWithValue("@Quantity", product.Quantity);
                        cmd.Parameters.AddWithValue("@Description", product.Description);
                        cmd.Parameters.AddWithValue("@Image", product.Image);
                        cmd.Parameters.AddWithValue("@Id", product.Id);

                        cmd.ExecuteNonQuery();
                    }
                }

                return RedirectToAction("Allproduct");
            }

            return View(product);
        }

        public ActionResult Search(string searchTerm)
        {
            bool isAdmin = Session["IsAdmin"] != null && (bool)Session["IsAdmin"]; // Check if the user is an admin

            var products = GetProductsFromDatabase();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Filter products by name or brand containing the search term
                products = products.Where(p => p.Name.ToLower().Contains(searchTerm.ToLower()) || p.Brand.ToLower().Contains(searchTerm.ToLower())).ToList();
            }

            // Filter out products with negative quantity or price
            products = products.Where(p => p.Quantity >= 0 && p.Price >= 0).ToList();

            if (products.Any())
            {
                if (isAdmin)
                {
                    return View("Allproduct", products); // Return admin view
                }
                else
                {
                    return View("Userview", products); // Return user view
                }
            }
            else
            {
                ViewBag.Message = "No products exist.";
                if (isAdmin)
                {
                    return View("Allproduct", Enumerable.Empty<Shapzada.Models.Product>());
                }
                else
                {
                    return View("Userview", Enumerable.Empty<Shapzada.Models.Product>());
                }
            }
        }

        public ActionResult DeleteProduct(int id)
        {
            using (var db = new SqlConnection(connStr))
            {
                db.Open();

                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = db.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandType = CommandType.Text;

                            // Check if the product quantity is zero
                            cmd.CommandText = "SELECT Quantity FROM Product WHERE Id = @Id";
                            cmd.Parameters.AddWithValue("@Id", id);
                            int productQuantity = (int)cmd.ExecuteScalar();

                            if (productQuantity > 0)
                            {
                                // If the product quantity is greater than zero, do not delete and redirect with a message
                                transaction.Rollback();
                                TempData["ErrorMessage"] = "Cannot delete product with quantity greater than zero.";
                                return RedirectToAction("AllProduct");
                            }

                            // Clear parameters before reuse
                            cmd.Parameters.Clear();

                            // Delete all cart items that reference the product
                            cmd.CommandText = "DELETE FROM Cart WHERE ProductId = @ProductId";
                            cmd.Parameters.AddWithValue("@ProductId", id);
                            cmd.ExecuteNonQuery();

                            // Clear parameters before reuse
                            cmd.Parameters.Clear();

                            // Delete the product
                            cmd.CommandText = "DELETE FROM Product WHERE Id = @Id";
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return RedirectToAction("AllProduct");
        }







        public ActionResult Userview()
        {
            try
            {
                // Fetch products from the database or wherever they are stored
                var products = GetProductsFromDatabase(); // Replace this with your actual method to fetch products

                if (products != null && products.Any())
                {
                    // Filter products with quantity greater than zero
                    var filteredProducts = products.Where(p => p.Quantity > 0).ToList();
                    return View(filteredProducts);
                }
                else
                {
                    ViewBag.Message = "No products found.";
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during data retrieval
                ViewBag.Message = "Error: " + ex.Message;
            }

            // If no products were found or an error occurred, return the view with an empty model
            return View(new List<Shapzada.Models.Product>());
        }


        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }







    }
}







    

