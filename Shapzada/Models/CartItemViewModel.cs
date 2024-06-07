using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shapzada.Models
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; } // Add the missing ProductId property
        public string ProductName { get; set; }
        public int ProductPrice { get; set; }
        public int Quantity { get; set; }
        public int TotalPrice { get; set; }

        
    }
}