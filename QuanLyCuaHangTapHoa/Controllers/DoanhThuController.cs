using QuanLyCuaHangTapHoa.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

public class DoanhThuController : Controller
{
    private quantaphoaEntities _db = new quantaphoaEntities();

    public ActionResult Index()
    {
        ViewBag.TongDoanhThu = ThongKeDoanhThu();

        // Lấy dữ liệu cho biểu đồ tổng quan
        var currentYear = DateTime.Now.Year;
        var doanhThuTheoThang = new Dictionary<string, decimal>();

        for (int i = 1; i <= 12; i++)
        {
            doanhThuTheoThang.Add($"T{i}", ThongKeDoanhThuThang(i, currentYear));
        }

        ViewBag.DoanhThuTheoThang = doanhThuTheoThang;

        // Thống kê top 5 sản phẩm bán chạy - Sửa để tránh lỗi casting decimal trong LINQ
        var orderDetails = _db.Order_Detail.ToList(); // Lấy dữ liệu về máy trước

        var topProducts = orderDetails
            .GroupBy(od => od.ID_Product)
            .Select(g => new
            {
                ProductID = g.Key,
                TotalQuantity = g.Sum(od => od.SoLuong ?? 0),
                TotalRevenue = g.Sum(od => (decimal)(od.SoLuong ?? 0) * (decimal)(od.Price ?? 0))
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(5)
            .ToList();

        var topProductsData = new List<object>();
        foreach (var item in topProducts)
        {
            var product = _db.Products.Find(item.ProductID);
            if (product != null)
            {
                topProductsData.Add(new
                {
                    ProductName = product.ProductName,
                    TotalQuantity = item.TotalQuantity,
                    TotalRevenue = item.TotalRevenue
                });
            }
        }

        ViewBag.TopProducts = topProductsData;

        return View();
    }

    public ActionResult DoanhThuNgay(FormCollection collection)
    {
        var ngaygiao = String.Format("{0:MM/dd/yyyy}", collection["NgayGiao"]);
        DateTime dDate;

        if (DateTime.TryParse(ngaygiao, out dDate))
        {
            // Tạo DateTime cho đầu ngày và cuối ngày
            var startDate = dDate.Date; // 00:00:00
            var endDate = startDate.AddDays(1); // 00:00:00 của ngày hôm sau

            ViewBag.Ngay = dDate.Day;
            ViewBag.Thang = dDate.Month;
            ViewBag.Nam = dDate.Year;

            // Lấy danh sách đơn hàng trong ngày đã hoàn thành
            var orders = _db.Orders
                        .Where(o => o.NgayDat.HasValue
                            && o.NgayDat >= startDate
                            && o.NgayDat < endDate
                            && o.Status == "Hoàn thành")
                        .ToList();

            // Tính tổng doanh thu và số lượng sản phẩm
            decimal tongDoanhThu = 0m;
            int tongSanPham = 0;

            foreach (var order in orders)
            {
                if (order.ThanhTien.HasValue)
                {
                    tongDoanhThu += Convert.ToDecimal(order.ThanhTien.Value);
                }
                if (order.TongSoLuong.HasValue)
                {
                    tongSanPham += order.TongSoLuong.Value;
                }
            }

            ViewBag.DoanhThuNgay = tongDoanhThu;
            ViewBag.DoanhThuNgayCount = tongSanPham;
            ViewBag.ListCountDTN = orders;

            // Thêm dữ liệu cho biểu đồ phân bố theo giờ
            var doanhThuTheoGio = new Dictionary<string, decimal>();
            for (int i = 0; i < 24; i++)
            {
                doanhThuTheoGio.Add($"{i}h", orders
                    .Where(o => o.NgayDat.HasValue && o.NgayDat.Value.Hour == i)
                    .Sum(o => (decimal)(o.ThanhTien ?? 0)));
            }
            ViewBag.DoanhThuTheoGio = doanhThuTheoGio;

            // Thêm dữ liệu cho biểu đồ sản phẩm bán ra
            var orderDetailIds = orders.Select(o => o.ID).ToList();
            var orderDetails = _db.Order_Detail.Where(od => orderDetailIds.Contains(od.ID_Order)).ToList();

            var sanPhamBanRa = orderDetails
                .GroupBy(od => od.ID_Product)
                .Select(g => new
                {
                    ProductID = g.Key,
                    TotalQuantity = g.Sum(od => od.SoLuong ?? 0),
                    TotalRevenue = g.Sum(od => (decimal)(od.SoLuong ?? 0) * (decimal)(od.Price ?? 0))
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(10)
                .ToList();

            var sanPhamData = new Dictionary<string, object>();
            foreach (var item in sanPhamBanRa)
            {
                var product = _db.Products.Find(item.ProductID);
                if (product != null)
                {
                    sanPhamData.Add(product.ProductName, new
                    {
                        Quantity = item.TotalQuantity,
                        Revenue = item.TotalRevenue
                    });
                }
            }
            ViewBag.SanPhamBanRa = sanPhamData;

            return View();
        }
        else
        {
            TempData["msgDate"] = "<script>alert('Không đúng định dạng ngày');</script>";
            return RedirectToAction("Index");
        }
    }

    public ActionResult DoanhThuThang(FormCollection collection)
    {
        var ngaygiao = String.Format("{0:MM/dd/yyyy}", collection["NgayGiao"]);
        DateTime dDate;

        if (DateTime.TryParse(ngaygiao, out dDate))
        {
            var month = dDate.Month;
            var year = dDate.Year;

            // Tạo datetime cho đầu tháng và cuối tháng
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            ViewBag.Thang = month;
            ViewBag.Nam = year;

            // Lấy danh sách đơn hàng trong tháng đã hoàn thành
            var orders = _db.Orders
                .Where(o => o.NgayDat.HasValue && o.NgayDat >= startDate && o.NgayDat < endDate)
                .Where(o => o.Status == "Hoàn thành")
                .ToList();


            // Tính tổng doanh thu
            decimal tongDoanhThu = 0m;
            int tongSanPham = 0;

            foreach (var order in orders)
            {
                if (order.ThanhTien.HasValue)
                {
                    tongDoanhThu += Convert.ToDecimal(order.ThanhTien.Value);
                }
                if (order.TongSoLuong.HasValue)
                {
                    tongSanPham += order.TongSoLuong.Value;
                }
            }

            ViewBag.DoanhThuThang = tongDoanhThu;
            ViewBag.DoanhThuThangCount = tongSanPham;
            ViewBag.ListCountDTN = orders;

            // Thêm dữ liệu cho biểu đồ phân bố theo ngày trong tháng
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var doanhThuTheoNgay = new Dictionary<string, decimal>();

            for (int i = 1; i <= daysInMonth; i++)
            {
                doanhThuTheoNgay.Add($"{i}", orders
                    .Where(o => o.NgayDat.HasValue && o.NgayDat.Value.Day == i)
                    .Sum(o => (decimal)(o.ThanhTien ?? 0)));
            }
            ViewBag.DoanhThuTheoNgay = doanhThuTheoNgay;

            // Thêm dữ liệu cho biểu đồ phân loại sản phẩm
            var orderIds = orders.Select(o => o.ID).ToList();
            var orderDetails = _db.Order_Detail.Where(od => orderIds.Contains(od.ID_Order)).ToList();

            var productIds = orderDetails.Select(od => od.ID_Product).Distinct().ToList();
            var products = _db.Products.Where(p => productIds.Contains(p.Id)).ToList();

            var doanhThuTheoDanhMuc = new Dictionary<string, decimal>();
            foreach (var product in products)
            {
                var catalogName = product.Catalog != null ? product.Catalog.CatalogName : "Không phân loại";
                var productRevenue = orderDetails
                    .Where(od => od.ID_Product == product.Id)
                    .Sum(od => (decimal)(od.SoLuong ?? 0) * (decimal)(od.Price ?? 0));

                if (doanhThuTheoDanhMuc.ContainsKey(catalogName))
                {
                    doanhThuTheoDanhMuc[catalogName] += productRevenue;
                }
                else
                {
                    doanhThuTheoDanhMuc.Add(catalogName, productRevenue);
                }
            }
            ViewBag.DoanhThuTheoDanhMuc = doanhThuTheoDanhMuc;

            return View();
        }
        else
        {
            TempData["msgDate"] = "<script>alert('Không đúng định dạng ngày');</script>";
            return RedirectToAction("Index");
        }
    }

    public ActionResult DoanhThuNam(FormCollection collection)
    {
        var ngaygiao = String.Format("{0:MM/dd/yyyy}", collection["NgayGiao"]);
        DateTime dDate;

        if (DateTime.TryParse(ngaygiao, out dDate))
        {
            var year = dDate.Year;

            // Tạo datetime cho đầu năm và cuối năm
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year + 1, 1, 1);

            ViewBag.Nam = year;

            // Lấy danh sách đơn hàng trong năm đã hoàn thành
            var orders = _db.Orders
                .Where(o => o.NgayDat.HasValue)
                .Where(o => o.NgayDat >= startDate && o.NgayDat < endDate)
                .Where(o => o.Status == "Hoàn thành")
                .ToList();

            // Tính tổng doanh thu
            decimal tongDoanhThu = 0m;
            int tongSanPham = 0;

            foreach (var order in orders)
            {
                if (order.ThanhTien.HasValue)
                {
                    tongDoanhThu += Convert.ToDecimal(order.ThanhTien.Value);
                }
                if (order.TongSoLuong.HasValue)
                {
                    tongSanPham += order.TongSoLuong.Value;
                }
            }

            ViewBag.DoanhThuNam = tongDoanhThu;
            ViewBag.DoanhThuNamCount = tongSanPham;
            ViewBag.ListCountDTN = orders;

            // Thêm dữ liệu cho biểu đồ phân bố theo tháng
            var doanhThuTheoThang = new Dictionary<string, decimal>();

            for (int i = 1; i <= 12; i++)
            {
                doanhThuTheoThang.Add($"T{i}", orders
                    .Where(o => o.NgayDat.HasValue && o.NgayDat.Value.Month == i)
                    .Sum(o => (decimal)(o.ThanhTien ?? 0)));
            }
            ViewBag.DoanhThuTheoThang = doanhThuTheoThang;

            // Thêm dữ liệu cho biểu đồ so sánh với năm trước
            var lastYearStartDate = new DateTime(year - 1, 1, 1);
            var lastYearEndDate = new DateTime(year, 1, 1);

            var lastYearOrders = _db.Orders
                .Where(o => o.NgayDat.HasValue)
                .Where(o => o.NgayDat >= lastYearStartDate && o.NgayDat < lastYearEndDate)
                .Where(o => o.Status == "Hoàn thành")
                .ToList();

            var soSanhTheoThang = new Dictionary<string, object>();

            for (int i = 1; i <= 12; i++)
            {
                decimal currentYearRevenue = orders
                    .Where(o => o.NgayDat.HasValue && o.NgayDat.Value.Month == i)
                    .Sum(o => (decimal)(o.ThanhTien ?? 0));

                decimal lastYearRevenue = lastYearOrders
                    .Where(o => o.NgayDat.HasValue && o.NgayDat.Value.Month == i)
                    .Sum(o => (decimal)(o.ThanhTien ?? 0));

                decimal tyLeTang = 0;
                if (lastYearRevenue > 0)
                {
                    tyLeTang = Math.Round((currentYearRevenue - lastYearRevenue) * 100m / lastYearRevenue, 2);
                }
                else
                {
                    tyLeTang = 100;
                }

                soSanhTheoThang.Add($"T{i}", new
                {
                    NamNay = currentYearRevenue,
                    NamTruoc = lastYearRevenue,
                    TyLeTang = tyLeTang
                });
            }
            ViewBag.SoSanhTheoThang = soSanhTheoThang;

            return View();
        }
        else
        {
            TempData["msgDate"] = "<script>alert('Không đúng định dạng ngày');</script>";
            return RedirectToAction("Index");
        }
    }

    private decimal ThongKeDoanhThu()
    {
        // Join Order và Order_Detail trước khi thực hiện phép tính
        var result = from od in _db.Order_Detail
                     join o in _db.Orders on od.ID_Order equals o.ID
                     where o.Status == "Hoàn thành"
                     select new { od.SoLuong, od.Price };

        // Tính tổng
        decimal tongTien = 0m;
        foreach (var item in result)
        {
            if (item.SoLuong.HasValue && item.Price.HasValue)
            {
                tongTien += (decimal)(item.SoLuong.Value) * (decimal)(item.Price.Value);
            }
        }
        return tongTien;
    }

    private decimal ThongKeDoanhThuNgay(int ngay, int thang, int nam)
    {
        // Tạo datetime cho đầu ngày và cuối ngày
        var startDate = new DateTime(nam, thang, ngay);
        var endDate = startDate.AddDays(1);

        var result = from od in _db.Order_Detail
                     join o in _db.Orders on od.ID_Order equals o.ID
                     where o.Status == "Hoàn thành"
                     && o.NgayDat >= startDate
                     && o.NgayDat < endDate
                     select new { od.SoLuong, od.Price };

        decimal tongTien = 0m;
        foreach (var item in result)
        {
            if (item.SoLuong.HasValue && item.Price.HasValue)
            {
                tongTien += (decimal)(item.SoLuong.Value) * (decimal)(item.Price.Value);
            }
        }
        return tongTien;
    }

    private decimal ThongKeDoanhThuThang(int thang, int nam)
    {
        var result = from od in _db.Order_Detail
                     join o in _db.Orders on od.ID_Order equals o.ID
                     where o.Status == "Hoàn thành"
                     && o.NgayDat.Value.Month == thang
                     && o.NgayDat.Value.Year == nam
                     select new { od.SoLuong, od.Price };

        decimal tongTien = 0m;
        foreach (var item in result)
        {
            if (item.SoLuong.HasValue && item.Price.HasValue)
            {
                tongTien += (decimal)(item.SoLuong.Value) * (decimal)(item.Price.Value);
            }
        }
        return tongTien;
    }

    private decimal ThongKeDoanhThuNam(int nam)
    {
        var result = from od in _db.Order_Detail
                     join o in _db.Orders on od.ID_Order equals o.ID
                     where o.Status == "Hoàn thành"
                     && o.NgayDat.Value.Year == nam
                     select new { od.SoLuong, od.Price };

        decimal tongTien = 0m;
        foreach (var item in result)
        {
            if (item.SoLuong.HasValue && item.Price.HasValue)
            {
                tongTien += (decimal)(item.SoLuong.Value) * (decimal)(item.Price.Value);
            }
        }
        return tongTien;
    }
}