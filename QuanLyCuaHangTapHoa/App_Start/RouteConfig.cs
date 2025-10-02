using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace QuanLyCuaHangTapHoa
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Thêm route cho XemDonHang
            routes.MapRoute(
                name: "XemDonHang",
                url: "XemDonHang/Index",
                defaults: new { controller = "XemDonHang", action = "Index" }
            );

            // Các route khác của bạn...
            routes.MapRoute(
                name: "Product_Indexadminsp",
                url: "Product/Indexadminsp",
                defaults: new { controller = "Product", action = "Indexadminsp" }
            );

            routes.MapRoute(
                name: "User_TrangChu",
                url: "",
                defaults: new { controller = "User", action = "TrangChu" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "User", action = "TrangChu", id = UrlParameter.Optional }
            );
        }
    }
}