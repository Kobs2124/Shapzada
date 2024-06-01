using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
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
        public ActionResult Login(User user)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string query = "SELECT Id FROM [User] WHERE Email = @Email AND Password = @Password";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password);

                    conn.Open();
                    var result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        // Set session variable to indicate user is logged in
                        Session["UserId"] = (int)result;

                        // Redirect user to the Allproduct page
                        return RedirectToAction("Allproduct", "Home");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Invalid email or password";
                        return View();
                    }
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
                    string query = "INSERT INTO [User] (Email, Password) VALUES (@Email, @Password); SELECT SCOPE_IDENTITY()";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Password", user.Password);

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
            return View();
        }




    }
}
