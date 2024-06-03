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
            return View(products);
        }

        public ActionResult Allproductuser()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login");
            }

            List<Product> products = GetProductsFromDatabase();
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





        [HttpPost]
        public ActionResult AddToCart(int productId)
        {
            // Check if the user is logged in
            if (Session["UserId"] == null)
            {
                // User is not logged in, redirect to the Login page
                return RedirectToAction("Login");
            }

            // User is logged in, proceed with adding the product to the cart
            Product product = GetProductById(productId);

            if (product != null && product.Quantity > 0)
            {
                // Decrement the quantity
                DecrementProductQuantity(productId);

                // You can add additional logic here, like updating the cart in the database
            }

            return RedirectToAction("Allproduct");
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
                    // Hash the password
                    string hashedPassword = HashPassword(user.Password);

                    string query = "INSERT INTO [User] (Email, Password) VALUES (@Email, @Password); SELECT SCOPE_IDENTITY()";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);

                    conn.Open();
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



        public ActionResult Userview()
        {
            try
            {
                // Fetch products from the database or wherever they are stored
                var products = GetProductsFromDatabase(); // Replace this with your actual method to fetch products

                if (products != null && products.Any())
                {
                    return View(products);
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






    }
}







    

