using QuanLyCuaHangTapHoa.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace QuanLyCuaHangTapHoa.Controllers
{
    public class XemDonHangController : Controller
    {
        private readonly quantaphoaEntities _db = new quantaphoaEntities();

        public ActionResult Index()
        {
            try
            {
                if (Session["TaiKhoan"] == null)
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem đơn hàng!";
                    return RedirectToAction("Login", "User");
                }

                var kh = (KhachHang)Session["TaiKhoan"];

                // Lấy danh sách đơn hàng
                var dsDonHang = _db.Orders
                    .Where(o => o.ID_KH == kh.idUser)
                    .OrderByDescending(o => o.NgayDat)
                    .ToList();

                ViewBag.TotalOrders = dsDonHang.Count;
                ViewBag.WaitingOrders = dsDonHang.Count(o =>
                    o.Status == "Chờ xử lý" || o.Status == "Chưa giao hàng");
                ViewBag.DeliveringOrders = dsDonHang.Count(o =>
                    o.Status == "Đang giao" || o.Status == "Đang giao hàng");
                ViewBag.CompletedOrders = dsDonHang.Count(o => o.Status == "Hoàn thành");
                ViewBag.CancelledOrders = dsDonHang.Count(o => o.Status == "Đã hủy");
                ViewBag.TotalSpending = dsDonHang
                    .Where(o => o.Status == "Hoàn thành")
                    .Sum(o => o.ThanhTien ?? 0);

                return View(dsDonHang);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("TrangChu", "User");
            }
        }

        public ActionResult ChiTietDonHang(int id)
        {
            try
            {
                if (Session["TaiKhoan"] == null)
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem chi tiết đơn hàng!";
                    return RedirectToAction("Login", "User");
                }

                var kh = (KhachHang)Session["TaiKhoan"];

                // Lấy thông tin đơn hàng
                var donHang = _db.Orders
                    .Where(o => o.ID == id && o.ID_KH == kh.idUser)
                    .FirstOrDefault();

                if (donHang == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                    return RedirectToAction("Index");
                }

                // Lấy chi tiết đơn hàng và thông tin sản phẩm
                var orderDetails = _db.Order_Detail
                    .Where(od => od.ID_Order == id)
                    .Join(_db.Products,
                        od => od.ID_Product,
                        p => p.Id,
                        (od, p) => new
                        {
                            OrderDetail = od,
                            Product = p
                        })
                    .ToList();

                // Gán product vào từng order detail
                foreach (var item in orderDetails)
                {
                    item.OrderDetail.Product = item.Product;
                }

                // Gán danh sách order detail vào đơn hàng
                donHang.Order_Details = orderDetails.Select(x => x.OrderDetail).ToList();

                return View(donHang);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyDon(int id)
        {
            try
            {
                if (Session["TaiKhoan"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                var kh = (KhachHang)Session["TaiKhoan"];
                var donHang = _db.Orders.FirstOrDefault(o => o.ID == id && o.ID_KH == kh.idUser);

                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                if (donHang.Status != "Chờ xử lý" && donHang.Status != "Chưa giao hàng")
                {
                    return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng ở trạng thái chờ xử lý!" });
                }

                // Hoàn lại số lượng sản phẩm
                var orderDetails = _db.Order_Detail.Where(od => od.ID_Order == id).ToList();
                foreach (var detail in orderDetails)
                {
                    var product = _db.Products.Find(detail.ID_Product);
                    if (product != null)
                    {
                        product.SoLuong += detail.SoLuong ?? 0;
                        product.ProductSold -= detail.SoLuong ?? 0;
                        _db.Entry(product).State = EntityState.Modified;
                    }
                }

                donHang.Status = "Đã hủy";
                donHang.NgayCapNhat = DateTime.Now;
                _db.SaveChanges();

                return Json(new { success = true, message = "Hủy đơn hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi hủy đơn hàng: {ex.Message}" });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}