using DinkToPdf;
using QuanLyCuaHangTapHoa.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace QuanLyCuaHangTapHoa.Controllers
{
    public class OrderController : Controller
    {
        private quantaphoaEntities _db = new quantaphoaEntities();
        private readonly bool _enableAutoComplete = false; // Set to false to disable auto-completion

        // GET: Order
        public ActionResult Index(string searchStr, string sort, int? page)
        {
            const int pageSize = 10;
            int pageNumber = page ?? 1;

            // Sử dụng Include để load thêm thông tin KhachHang
            var orders = _db.Orders.Include(o => o.KhachHang).ToList();

            // Chỉ tự động cập nhật nếu tính năng được bật
            if (_enableAutoComplete)
            {
                AutoUpdateOrderStatus(orders);
            }

            // Calculate statistics
            CalculateOrderStatistics(orders);

            if (!String.IsNullOrEmpty(searchStr))
            {
                searchStr = searchStr.ToLower();
                ViewBag.searchStr = searchStr;
                orders = orders.Where(p => p.ID.ToString().ToLower().Contains(searchStr)).ToList();
            }
            else
            {
                orders = Sort(orders, sort).ToList();
            }

            int totalOrders = orders.Count();
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.orderList = orders.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.Sort = sort; // Đảm bảo ViewBag.Sort luôn được set

            return View();
        }

        // Tách logic tự động cập nhật thành một phương thức riêng
        private void AutoUpdateOrderStatus(List<Order> orders)
        {
            foreach (var order in orders)
            {
                if (order.Status == "Đang giao hàng" && order.NgayDat.HasValue)
                {
                    var timeElapsed = DateTime.Now - order.NgayDat.Value;
                    if (timeElapsed.TotalDays >= 3)
                    {
                        Order editOrder = _db.Orders.Find(order.ID);

                        // Chỉ cập nhật nếu trạng thái vẫn là "Đang giao hàng"
                        if (editOrder.Status == "Đang giao hàng")
                        {
                            try
                            {
                                UpdateToCompletedState(editOrder, new Order
                                {
                                    NgayGiao = order.NgayDat.Value.AddDays(3)
                                });
                                _db.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                // Log lỗi nếu cần
                                Console.WriteLine($"Error auto-updating order {order.ID}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void CalculateOrderStatistics(List<Order> orders)
        {
            // Tổng số đơn hàng
            ViewBag.TotalOrders = orders.Count;

            // Tổng doanh thu (từ các đơn hàng đã hoàn thành)
            ViewBag.TotalRevenue = orders
                .Where(o => o.Status == "Hoàn thành")
                .Sum(o => o.ThanhTien) ?? 0;

            // Số đơn theo trạng thái
            ViewBag.WaitingOrders = orders.Count(o => o.Status == "Chưa giao hàng");
            ViewBag.DeliveringOrders = orders.Count(o => o.Status == "Đang giao hàng");
            ViewBag.CompletedOrders = orders.Count(o => o.Status == "Hoàn thành");
            ViewBag.CancelledOrders = orders.Count(o => o.Status == "Đã hủy");

            // Tổng số sản phẩm đã bán
            ViewBag.TotalProductsSold = orders
                .Where(o => o.Status == "Hoàn thành")
                .Sum(o => o.TongSoLuong) ?? 0;

            // Doanh thu trung bình mỗi đơn
            var completedOrders = orders.Count(o => o.Status == "Hoàn thành");
            ViewBag.AverageRevenue = completedOrders > 0
                ? ViewBag.TotalRevenue / completedOrders
                : 0;
        }

        private IEnumerable<Order> Sort(IEnumerable<Order> orders, string sort)
        {
            ViewBag.Sort = sort;
            var orderList = orders.ToList();

            // Xử lý sắp xếp
            if (String.IsNullOrEmpty(sort))
            {
                return orderList.OrderByDescending(s => s.NgayDat); // Sắp xếp mới nhất lên đầu
            }

            switch (sort)
            {
                case "Wait":
                    return orderList.Where(s => s.Status == "Chưa giao hàng")
                                  .OrderByDescending(s => s.NgayDat);
                case "Deli":
                    return orderList.Where(s => s.Status == "Đang giao hàng")
                                  .OrderByDescending(s => s.NgayDat);
                case "Done":
                    return orderList.Where(s => s.Status == "Hoàn thành")
                                  .OrderByDescending(s => s.NgayDat);
                case "Cancel":
                    return orderList.Where(s => s.Status == "Đã hủy")
                                  .OrderByDescending(s => s.NgayDat);
                default:
                    return orderList.OrderByDescending(s => s.NgayDat);
            }
        }

        public ActionResult Detail(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Order order = _db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // Load thông tin khách hàng (nếu cần)
            if (order.KhachHang == null && order.ID_KH.HasValue)
            {
                order.KhachHang = _db.KhachHangs.Find(order.ID_KH);
            }

            var orderDetails = _db.Order_Detail
                .Where(item => item.ID_Order == order.ID)
                .ToList();

            var orderDetailsWithProducts = orderDetails.Select(item => new
            {
                Item = item,
                Product = _db.Products.Find(item.ID_Product)
            }).ToList();

            ViewBag.OrderDetails = orderDetailsWithProducts;

            // Lấy các trạng thái hợp lệ tiếp theo dựa trên trạng thái hiện tại
            ViewBag.ValidNextStates = GetValidNextStates(order.Status);

            // Kiểm tra có thể cập nhật trạng thái không
            ViewBag.CanEditStatus = (order.Status != "Hoàn thành" && order.Status != "Đã hủy");

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Detail([Bind(Include = "ID, Status, NgayGiao")] Order model)
        {
            var order = _db.Orders.Find(model.ID);
            if (order == null)
            {
                return HttpNotFound();
            }

            try
            {
                // Cập nhật trạng thái đơn hàng
                UpdateOrderStatus(order, model.Status, model);
                _db.SaveChanges();

                TempData["Message"] = "Đơn hàng đã được cập nhật thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("Detail", new { id = order.ID });
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Order order = _db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra xem đơn hàng có thể chỉnh sửa không
            bool canEdit = order.Status != "Hoàn thành" && order.Status != "Đã hủy";
            ViewBag.IsEditable = canEdit;

            // Lấy các trạng thái hợp lệ cho đơn hàng hiện tại
            var validNextStates = GetValidNextStates(order.Status);
            ViewBag.Status = new SelectList(validNextStates, order.Status);

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID, Status, NgayGiao")] Order model)
        {
            var order = _db.Orders.Find(model.ID);
            if (order == null)
            {
                return HttpNotFound();
            }

            try
            {
                // Cập nhật trạng thái đơn hàng
                UpdateOrderStatus(order, model.Status, model);
                _db.SaveChanges();

                TempData["Message"] = "Đơn hàng đã được cập nhật thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Edit", new { id = order.ID });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult HuyDon(int id)
        {
            Order order = _db.Orders.Find(id);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            try
            {
                // Lưu lại trạng thái thanh toán và phương thức thanh toán
                bool? paymentStatus = order.Payment;
                string paymentMethod = order.PaymentMethod;

                // Chuyển đơn hàng sang trạng thái "Đã hủy"
                UpdateToCancelledState(order, new Order());

                // Khôi phục lại trạng thái thanh toán và phương thức thanh toán
                order.Payment = paymentStatus;
                order.PaymentMethod = paymentMethod;

                _db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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

        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                // Tìm order cần xóa
                var order = _db.Orders.Find(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Chỉ cho phép xóa đơn hàng đã hoàn thành hoặc đã hủy
                if (order.Status != "Hoàn thành" && order.Status != "Đã hủy")
                {
                    return Json(new { success = false, message = "Chỉ có thể xóa đơn hàng đã hoàn thành hoặc đã hủy" });
                }

                // Xóa các Order_Detail liên quan
                var orderDetails = _db.Order_Detail.Where(od => od.ID_Order == id);
                _db.Order_Detail.RemoveRange(orderDetails);

                // Xóa các DoanhThuView liên quan (nếu có)
                var doanhThuViews = _db.DoanhThuViews.Where(dtv => dtv.OrderId == id);
                if (doanhThuViews.Any())
                {
                    _db.DoanhThuViews.RemoveRange(doanhThuViews);
                }

                // Xóa Order
                _db.Orders.Remove(order);
                _db.SaveChanges();

                return Json(new { success = true, message = "Xóa đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa đơn hàng: " + ex.Message });
            }
        }

        public ActionResult Print(int id)
        {
            if (id <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Load order với thông tin khách hàng
            var order = _db.Orders
                .Include(o => o.KhachHang)
                .FirstOrDefault(o => o.ID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Load Order_Detail với thông tin sản phẩm
            var orderDetails = _db.Order_Detail
                .Include(od => od.Product)
                .Where(od => od.ID_Order == id)
                .ToList();

            ViewBag.OrderDetails = orderDetails;
            return View(order);
        }

        #region Order Status Management Methods

        // Phương thức cập nhật trạng thái đơn hàng
        private void UpdateOrderStatus(Order order, string newStatus, Order model)
        {
            // Kiểm tra tính hợp lệ của trạng thái mới
            if (string.IsNullOrEmpty(newStatus) || !IsValidStatus(newStatus))
            {
                throw new ArgumentException("Trạng thái không hợp lệ!");
            }

            // Lấy trạng thái hiện tại của đơn hàng
            var currentStatus = order.Status;

            // Không cần thay đổi nếu trạng thái giống nhau và không phải là cập nhật ngày giao
            if (currentStatus == newStatus &&
                !(newStatus == "Hoàn thành" && model.NgayGiao.HasValue))
            {
                // Không cần thực hiện thay đổi
                return;
            }

            // Kiểm tra xem có thể chuyển đổi sang trạng thái mới không
            if (!CanTransitionTo(currentStatus, newStatus))
            {
                string errorMessage = string.Format(
                    "Không thể chuyển từ trạng thái {0} sang trạng thái {1}!",
                    currentStatus ?? "Chưa xác định",
                    newStatus);

                throw new InvalidOperationException(errorMessage);
            }

            // Lưu lại trạng thái thanh toán và phương thức thanh toán
            bool? paymentStatus = order.Payment;
            string paymentMethod = order.PaymentMethod;

            // Thực hiện cập nhật trạng thái dựa trên trạng thái mới
            switch (newStatus)
            {
                case "Chưa giao hàng":
                    UpdateToPendingState(order, model);
                    break;
                case "Đang giao hàng":
                    UpdateToDeliveringState(order, model);
                    break;
                case "Hoàn thành":
                    UpdateToCompletedState(order, model);
                    break;
                case "Đã hủy":
                    UpdateToCancelledState(order, model);
                    break;
            }

            // Cập nhật trạng thái
            order.Status = newStatus;

            // Khôi phục lại trạng thái thanh toán và phương thức thanh toán
            order.Payment = paymentStatus;
            order.PaymentMethod = paymentMethod;
        }

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
                    return currentStatus == "Chưa giao hàng" || currentStatus == "Đang giao hàng";

                case "Hoàn thành":
                    return currentStatus == "Đang giao hàng";

                case "Đã hủy":
                    return currentStatus == "Chưa giao hàng" || currentStatus == "Đang giao hàng";

                default:
                    return false;
            }
        }

        // Lấy danh sách các trạng thái hợp lệ tiếp theo
        private List<string> GetValidNextStates(string currentStatus)
        {
            var validStates = new List<string>();

            // Nếu đã hoàn thành hoặc đã hủy, không thể thay đổi trạng thái
            if (currentStatus == "Hoàn thành" || currentStatus == "Đã hủy")
            {
                validStates.Add(currentStatus); // Chỉ bao gồm trạng thái hiện tại
                return validStates;
            }

            // Thêm các trạng thái có thể chuyển sang
            string[] allStatuses = { "Chưa giao hàng", "Đang giao hàng", "Hoàn thành", "Đã hủy" };
            foreach (var status in allStatuses)
            {
                if (CanTransitionTo(currentStatus, status))
                {
                    validStates.Add(status);
                }
            }

            return validStates;
        }

        // Cập nhật sang trạng thái "Chưa giao hàng"
        private void UpdateToPendingState(Order order, Order model)
        {
            // Xác nhận đơn hàng có thể ở trạng thái chưa giao hàng
            order.NgayGiao = null;

            // Không thay đổi trạng thái Payment và PaymentMethod
            // Giữ nguyên giá trị order.Payment và order.PaymentMethod

            order.NgayCapNhat = DateTime.Now;
        }

        // Cập nhật sang trạng thái "Đang giao hàng"
        private void UpdateToDeliveringState(Order order, Order model)
        {
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

            // Không thay đổi trạng thái thanh toán
            // Giữ nguyên giá trị order.Payment và order.PaymentMethod

            // Cập nhật thời gian cập nhật
            order.NgayCapNhat = DateTime.Now;
        }

        // Cập nhật sang trạng thái "Hoàn thành"
        private void UpdateToCompletedState(Order order, Order model)
        {
            // Xử lý ngày giao hàng
            if (model.NgayGiao.HasValue)
            {
                // Kiểm tra tính hợp lệ của ngày giao
                if (order.NgayDat.HasValue && model.NgayGiao.Value < order.NgayDat.Value)
                {
                    throw new Exception("Ngày giao không thể trước ngày đặt hàng!");
                }
                order.NgayGiao = model.NgayGiao;
            }
            else if (order.NgayDat.HasValue)
            {
                // Mặc định: Ngày giao = Ngày đặt + 3 ngày
                order.NgayGiao = order.NgayDat.Value.AddDays(3);
            }
            else
            {
                order.NgayGiao = DateTime.Now;
            }

            // Nếu đơn hàng chưa thanh toán (và không phải thanh toán online)
            // thì mới đánh dấu thanh toán đã hoàn thành
            // Giữ nguyên trạng thái thanh toán nếu đã thanh toán online
            if (order.Payment != true && string.IsNullOrEmpty(order.PaymentMethod))
            {
                order.Payment = true;
            }

            // Kiểm tra nếu trạng thái trước đó không phải "Hoàn thành"
            // => Cần cập nhật số lượng sản phẩm
            if (order.Status != "Hoàn thành")
            {
                UpdateProductInventory(order);
            }

            order.NgayCapNhat = DateTime.Now;
        }

        // Cập nhật kho hàng khi đơn hàng hoàn thành
        private void UpdateProductInventory(Order order)
        {
            try
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
                        _db.Entry(product).State = EntityState.Modified;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi cập nhật kho hàng: " + ex.Message);
            }
        }

        // Cập nhật sang trạng thái "Đã hủy"
        private void UpdateToCancelledState(Order order, Order model)
        {
            // Xóa ngày giao
            order.NgayGiao = null;

            // KHÔNG thay đổi trạng thái thanh toán
            // Giữ nguyên giá trị order.Payment và order.PaymentMethod

            order.NgayCapNhat = DateTime.Now;

            // Nếu đơn hàng đã ở trạng thái "Đang giao hàng", cần trả lại số lượng sản phẩm vào kho
            if (order.Status == "Đang giao hàng")
            {
                RestoreProductInventory(order);
            }
        }

        // Phương thức để trả lại số lượng khi hủy đơn
        private void RestoreProductInventory(Order order)
        {
            try
            {
                // Lấy các chi tiết đơn hàng
                var orderDetails = _db.Order_Detail
                    .Where(od => od.ID_Order == order.ID)
                    .ToList();

                // Trả lại số lượng sản phẩm vào kho
                foreach (var detail in orderDetails)
                {
                    var product = _db.Products.Find(detail.ID_Product);
                    if (product != null && detail.SoLuong.HasValue)
                    {
                        // Tăng số lượng trong kho và giảm số lượng đã bán
                        product.SoLuong += detail.SoLuong.Value;
                        product.ProductSold -= detail.SoLuong.Value;
                        _db.Entry(product).State = EntityState.Modified;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi cập nhật kho hàng: " + ex.Message);
            }
        }

        #endregion
    }
}