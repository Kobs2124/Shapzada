using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shapzada.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public string Brand { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }

        public void DecrementQuantity()
        {
            if (Quantity > 0)
            {
                Quantity--;
            }
        }
    }
}
