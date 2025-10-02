using QuanLyCuaHangTapHoa.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace QuanLyCuaHangTapHoa.Controllers
{
    public class HomeController : Controller
    {
        // Thay thế Facade bằng truy cập trực tiếp vào _db
        private readonly quantaphoaEntities _db;

        public HomeController()
        {
            // Khởi tạo database context
            _db = new quantaphoaEntities();
        }

        // GET: Admin/Home
        public ActionResult Indexadmin()
        {
            if (Session["NV"] == null)
            {
                return RedirectToAction("Loginadmin", "Home");
            }

            // Lấy dữ liệu thống kê trực tiếp
            var dsKhachHang = _db.KhachHangs.ToList();
            var dsProduct = _db.Products.ToList();
            var dsOrder = _db.Orders.ToList();
            var lowStockCount = dsProduct.Count(p => p.SoLuong < 50);

            // Các đơn hàng gần đây cần xử lý
            var recentOrders = dsOrder
                    .Where(o => o.Status == "Chưa giao hàng" || o.Status == "Đang giao hàng")
                    .OrderByDescending(o => o.NgayDat) // Sắp xếp theo ngày đặt mới nhất
                    .Take(4) // Chỉ lấy 4 đơn
                    .ToList();

            // Truyền dữ liệu vào View
            ViewBag.TotalCustomers = dsKhachHang.Count;
            ViewBag.TotalProducts = dsProduct.Count;
            ViewBag.TotalOrders = dsOrder.Count;
            ViewBag.LowStockCount = lowStockCount;
            ViewBag.RecentOrders = recentOrders;

            return View();
        }

        [HttpGet]
        public ActionResult getDataOrder()
        {
            try
            {
                // Lấy danh sách đơn hàng trực tiếp từ database
                var result = _db.Orders.ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ex.Message);
            }
        }

        // Đăng nhập admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Loginadmin(NhanVien objUser)
        {
            try
            {
                // Chỉ validate các trường cần thiết cho đăng nhập
                ModelState.Clear();
                if (string.IsNullOrEmpty(objUser.Email))
                {
                    ModelState.AddModelError("Email", "Vui lòng nhập email");
                    return View(objUser);
                }

                if (string.IsNullOrEmpty(objUser.Password))
                {
                    ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu");
                    return View(objUser);
                }

                // Xác thực người dùng trực tiếp
                var user = _db.NhanViens.FirstOrDefault(x =>
                    x.Email.ToLower() == objUser.Email.ToLower());

                if (user == null)
                {
                    ModelState.AddModelError("", "Email không tồn tại");
                    return View(objUser);
                }

                if (user.Password != objUser.Password)
                {
                    ModelState.AddModelError("", "Mật khẩu không chính xác");
                    return View(objUser);
                }

                if (user.MaChucVu != 1 && user.MaChucVu != 2)
                {
                    ModelState.AddModelError("", "Tài khoản không có quyền truy cập");
                    return View(objUser);
                }

                // Đăng nhập thành công
                Session["NV"] = user;
                Session["NVID"] = user.Id;
                return RedirectToAction("Indexadmin", "Home");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine($"Lỗi đăng nhập: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình đăng nhập");
                return View(objUser);
            }
        }

        [HttpGet]
        public ActionResult Loginadmin()
        {
            if (Session["NV"] != null)
            {
                return RedirectToAction("Indexadmin", "Home");
            }
            return View();
        }

        public ActionResult Logout()
        {
            Session["NV"] = null;
            return RedirectToAction("Loginadmin", "Home");
        }

        // Action lấy thống kê doanh số theo khoảng thời gian
        [HttpGet]
        public ActionResult GetSalesStatistics(string period = "month")
        {
            try
            {
                var result = new Dictionary<string, decimal>();
                var orders = _db.Orders.Where(o => o.NgayDat.HasValue).ToList();

                // Kiểm tra nếu không có dữ liệu
                if (orders.Count == 0)
                {
                    // Trả về dữ liệu mẫu khi không có đơn hàng
                    switch (period.ToLower())
                    {
                        case "day":
                            for (int i = 1; i <= 30; i++)
                            {
                                result.Add(i.ToString(), i * 200000);
                            }
                            break;
                        case "week":
                            for (int i = 1; i <= 4; i++)
                            {
                                result.Add("Tuần " + i, i * 1000000);
                            }
                            break;
                        case "month":
                            for (int i = 1; i <= 12; i++)
                            {
                                result.Add("T" + i, i * 2000000);
                            }
                            break;
                        case "year":
                            for (int i = 2022; i <= 2025; i++)
                            {
                                result.Add(i.ToString(), (i - 2021) * 10000000);
                            }
                            break;
                    }
                    return Json(result, JsonRequestBehavior.AllowGet);
                }

                // Xử lý thống kê bình thường nếu có dữ liệu
                switch (period.ToLower())
                {
                    case "day":
                        // Thống kê theo ngày trong tháng hiện tại
                        result = orders
                            .Where(o => o.NgayDat.Value.Month == DateTime.Now.Month && o.NgayDat.Value.Year == DateTime.Now.Year)
                            .GroupBy(o => o.NgayDat.Value.Day)
                            .OrderBy(g => g.Key)
                            .ToDictionary(
                                g => g.Key.ToString(),
                                g => (decimal)g.Sum(o => o.ThanhTien ?? 0)
                            );
                        break;

                    case "week":
                        // Thống kê theo tuần trong năm hiện tại
                        result = orders
                            .Where(o => o.NgayDat.Value.Year == DateTime.Now.Year)
                            .GroupBy(o => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                o.NgayDat.Value,
                                System.Globalization.CalendarWeekRule.FirstDay,
                                DayOfWeek.Monday))
                            .OrderBy(g => g.Key)
                            .ToDictionary(
                                g => "Tuần " + g.Key.ToString(),
                                g => (decimal)g.Sum(o => o.ThanhTien ?? 0)
                            );
                        break;

                    case "month":
                        // Thống kê theo tháng trong năm hiện tại
                        result = orders
                            .Where(o => o.NgayDat.Value.Year == DateTime.Now.Year)
                            .GroupBy(o => o.NgayDat.Value.Month)
                            .OrderBy(g => g.Key)
                            .ToDictionary(
                                g => "T" + g.Key.ToString(),
                                g => (decimal)g.Sum(o => o.ThanhTien ?? 0)
                            );
                        break;

                    case "year":
                        // Thống kê theo năm
                        result = orders
                            .GroupBy(o => o.NgayDat.Value.Year)
                            .OrderBy(g => g.Key)
                            .ToDictionary(
                                g => g.Key.ToString(),
                                g => (decimal)g.Sum(o => o.ThanhTien ?? 0)
                            );
                        break;
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ex.Message);
            }
        }

        // Action lấy danh sách sản phẩm sắp hết hàng
        [HttpGet]
        public ActionResult GetLowStockProducts(int threshold = 50)
        {
            try
            {
                var products = _db.Products.Where(p => p.SoLuong < threshold).ToList();
                return Json(products, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(ex.Message);
            }
        }

        // Action cập nhật trạng thái đơn hàng
        [HttpPost]
        public ActionResult UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                var order = _db.Orders.FirstOrDefault(o => o.ID == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Kiểm tra tính hợp lệ của trạng thái
                if (string.IsNullOrEmpty(status) || !IsValidStatus(status))
                {
                    return Json(new { success = false, message = "Trạng thái không hợp lệ" });
                }

                // Kiểm tra xem có thể chuyển từ trạng thái hiện tại sang trạng thái mới không
                if (!CanTransitionTo(order.Status, status))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể chuyển từ trạng thái {order.Status} sang trạng thái {status}"
                    });
                }

                // Cập nhật trạng thái đơn hàng và xử lý logic tương ứng
                UpdateOrderStatusWithLogic(order, status);
                _db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #region Helper Methods

        // Kiểm tra tính hợp lệ của trạng thái
        private bool IsValidStatus(string status)
        {
            string[] validStatuses = { "Chưa giao hàng", "Đang giao hàng", "Hoàn thành", "Đã hủy" };
            return validStatuses.Contains(status);
        }

        // Kiểm tra có thể chuyển từ trạng thái hiện tại sang trạng thái mới không
        private bool CanTransitionTo(string currentStatus, string newStatus)
        {
            // Nếu trạng thái giống nhau, luôn cho phép
            if (currentStatus == newStatus)
            {
                return true;
            }

            // Kiểm tra các quy tắc chuyển trạng thái
            switch (newStatus)
            {
                case "Chưa giao hàng":
                    return string.IsNullOrEmpty(currentStatus) || currentStatus == "Đang giao hàng";

                case "Đang giao hàng":
                    return currentStatus == "Chưa giao hàng";

                case "Hoàn thành":
                    return currentStatus == "Đang giao hàng";

                case "Đã hủy":
                    return currentStatus == "Chưa giao hàng" || currentStatus == "Đang giao hàng";

                default:
                    return false;
            }
        }

        // Cập nhật trạng thái đơn hàng và xử lý logic tương ứng
        private void UpdateOrderStatusWithLogic(Order order, string newStatus)
        {
            // Lưu lại trạng thái cũ để xử lý logic
            var oldStatus = order.Status;

            // Thực hiện cập nhật trạng thái dựa trên trạng thái mới
            switch (newStatus)
            {
                case "Chưa giao hàng":
                    // Xác nhận đơn hàng có thể ở trạng thái chưa giao hàng
                    order.NgayGiao = null;
                    order.Payment = false;
                    break;

                case "Đang giao hàng":
                    // Khi chuyển sang đang giao hàng, dự kiến ngày giao sau 3 ngày
                    if (order.NgayDat.HasValue)
                    {
                        order.NgayGiao = order.NgayDat.Value.AddDays(3);
                    }
                    else
                    {
                        // Nếu không có ngày đặt, sử dụng ngày hiện tại + 3 ngày
                        order.NgayDat = DateTime.Now;
                        order.NgayGiao = DateTime.Now.AddDays(3);
                    }
                    order.Payment = false;
                    break;

                case "Hoàn thành":
                    // Xử lý ngày giao hàng nếu chưa có
                    if (!order.NgayGiao.HasValue && order.NgayDat.HasValue)
                    {
                        order.NgayGiao = order.NgayDat.Value.AddDays(3);
                    }
                    else if (!order.NgayGiao.HasValue)
                    {
                        order.NgayGiao = DateTime.Now;
                    }

                    // Đánh dấu thanh toán đã hoàn thành
                    order.Payment = true;

                    // Cập nhật số lượng sản phẩm nếu chưa cập nhật trước đó
                    if (oldStatus != "Hoàn thành")
                    {
                        UpdateProductInventory(order);
                    }
                    break;

                case "Đã hủy":
                    // Xóa ngày giao và đánh dấu thanh toán là false
                    order.NgayGiao = null;
                    order.Payment = false;
                    break;
            }

            // Cập nhật trạng thái và thời gian cập nhật
            order.Status = newStatus;
            order.NgayCapNhat = DateTime.Now;
        }

        // Cập nhật kho hàng khi đơn hàng hoàn thành
        private void UpdateProductInventory(Order order)
        {
            // Lấy các chi tiết đơn hàng
            var orderDetails = _db.Order_Detail
                .Where(od => od.ID_Order == order.ID)
                .ToList();

            // Cập nhật số lượng sản phẩm trong kho và số lượng đã bán
            foreach (var detail in orderDetails)
            {
                var product = _db.Products.Find(detail.ID_Product);
                if (product != null && detail.SoLuong.HasValue)
                {
                    // Kiểm tra xem còn đủ hàng trong kho không
                    if (product.SoLuong < detail.SoLuong.Value)
                    {
                        throw new Exception($"Sản phẩm '{product.ProductName}' không đủ số lượng trong kho!");
                    }

                    // Giảm số lượng trong kho và tăng số lượng đã bán
                    product.SoLuong -= detail.SoLuong.Value;
                    product.ProductSold += detail.SoLuong.Value;
                    _db.Entry(product).State = System.Data.Entity.EntityState.Modified;
                }
            }
        }

        #endregion

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