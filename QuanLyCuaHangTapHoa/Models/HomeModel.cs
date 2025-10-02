using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLyCuaHangTapHoa.Models
{
    public class HomeModel
    {
        public List<Product> ListProduct { get; set; }
        public List<Catalog> ListCategory { get; set; }

        public HomeModel()
        {
            ListProduct = new List<Product>();
            ListCategory = new List<Catalog>();
        }
    }
}