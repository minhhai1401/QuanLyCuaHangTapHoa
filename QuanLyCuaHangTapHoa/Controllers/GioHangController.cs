using Newtonsoft.Json.Linq;
using QuanLyCuaHangTapHoa.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace QuanLyCuaHangTapHoa.Controllers
{
    public class GiohangController : Controller
    {
        private readonly quantaphoaEntities _db = new quantaphoaEntities();

        // GET: Giohang
        public ActionResult Index()
        {
            List<Giohang> listGiohang = Laygiohang();
            if (listGiohang.Count == 0)
            {
                return RedirectToAction("TrangChu", "User");
            }
            ViewBag.Tongsoluong = TongSoLuong();
            ViewBag.Tongtien = TongTien();
            ViewBag.Tongsoluongsanpham = TongSoLuongSanPham();
            return View(listGiohang);
        }

        // Hàm lấy giỏ hàng
        public List<Giohang> Laygiohang()
        {
            List<Giohang> listGiohang = Session["Giohang"] as List<Giohang>;
            if (listGiohang == null)
            {
                listGiohang = new List<Giohang>();
                Session["Giohang"] = listGiohang;
            }
            return listGiohang;
        }

        // Thêm vào giỏ hàng
        public ActionResult ThemGioHang(int id, string strURL)
        {
            List<Giohang> listGiohang = Laygiohang();
            Giohang product = listGiohang.Find(n => n.IdProduct == id);
            Product product1 = _db.Products.Single(n => n.Id == id);

            var result = new
            {
                success = false,
                message = "",
                cartCount = 0
            };

            if (product == null)
            {
                product = new Giohang(id);
                if (product.SoLuong <= product1.SoLuong)
                {
                    listGiohang.Add(product);
                    result = new
                    {
                        success = true,
                        message = "Thêm sản phẩm vào giỏ hàng thành công",
                        cartCount = listGiohang.Sum(n => n.SoLuong)
                    };
                }
            }
            else if (product.SoLuong >= product1.SoLuong)
            {
                result = new
                {
                    success = false,
                    message = "Sản phẩm không được vượt quá số lượng tồn",
                    cartCount = listGiohang.Sum(n => n.SoLuong)
                };
            }
            else
            {
                product.SoLuong++;
                result = new
                {
                    success = true,
                    message = "Thêm sản phẩm vào giỏ hàng thành công",
                    cartCount = listGiohang.Sum(n => n.SoLuong)
                };
            }

            if (Request.IsAjaxRequest())
            {
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            return Redirect(strURL);
        }

        // Cập nhật giỏ hàng
        [HttpPost]
        public ActionResult CapnhatGiohang(int id, FormCollection collection)
        {
            List<Giohang> listGiohang = Laygiohang();
            Giohang sanpham = listGiohang.SingleOrDefault(n => n.IdProduct == id);
            Product product1 = _db.Products.Single(n => n.Id == id);

            if (sanpham != null)
            {
                if (collection["txtSoLuong"] != null)
                {
                    int soLuong = int.Parse(collection["txtSoLuong"]);
                    if (soLuong > product1.SoLuong)
                    {
                        TempData["msg"] = "Số lượng không được vượt quá số lượng tồn kho";
                    }
                    else if (soLuong <= 0)
                    {
                        listGiohang.Remove(sanpham);
                    }
                    else
                    {
                        sanpham.SoLuong = soLuong;
                    }
                }
            }

            return RedirectToAction("Index");
        }

        // Xóa giỏ hàng
        public ActionResult XoaGioHang(int id)
        {
            List<Giohang> listGiohang = Laygiohang();
            Giohang sanpham = listGiohang.SingleOrDefault(n => n.IdProduct == id);
            if (sanpham != null)
            {
                listGiohang.RemoveAll(n => n.IdProduct == id);
            }
            return RedirectToAction("Index");
        }

        // Xóa tất cả giỏ hàng
        public ActionResult XoaTatCaGioHang()
        {
            List<Giohang> listGiohang = Laygiohang();
            listGiohang.Clear();
            return RedirectToAction("Index");
        }

        // Tính tổng số lượng
        private int TongSoLuong()
        {
            int tsl = 0;
            List<Giohang> listGiohang = Session["Giohang"] as List<Giohang>;
            if (listGiohang != null)
            {
                tsl = listGiohang.Sum(n => n.SoLuong);
            }
            return tsl;
        }

        // Tính tổng số lượng sản phẩm
        private int TongSoLuongSanPham()
        {
            int tsl = 0;
            List<Giohang> listGiohang = Session["Giohang"] as List<Giohang>;
            if (listGiohang != null)
            {
                tsl = listGiohang.Count;
            }
            return tsl;
        }

        // Tính tổng tiền
        private double TongTien()
        {
            double tt = 0;
            List<Giohang> listGiohang = Session["Giohang"] as List<Giohang>;
            if (listGiohang != null)
            {
                tt = listGiohang.Sum(n => n.ThanhTien);
            }
            return tt;
        }

        // Đặt hàng GET
        [HttpGet]
        public ActionResult DatHang()
        {
            if (Session["TaiKhoan"] == null)
            {
                TempData["error"] = "Vui lòng đăng nhập để đặt hàng!";
                return RedirectToAction("Login", "User");
            }

            List<Giohang> listGiohang = Laygiohang();
            if (listGiohang == null || listGiohang.Count == 0)
            {
                TempData["error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index");
            }

            ViewBag.Tongsoluong = TongSoLuong();
            ViewBag.Tongtien = TongTien();
            ViewBag.Tongsoluongsanpham = TongSoLuongSanPham();

            return View(listGiohang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatHang(FormCollection collection)
        {
            try
            {
                if (Session["TaiKhoan"] == null)
                {
                    return RedirectToAction("Login", "User");
                }

                KhachHang kh = (KhachHang)Session["TaiKhoan"];
                List<Giohang> gh = Laygiohang();

                if (gh == null || gh.Count == 0)
                {
                    return RedirectToAction("Index");
                }

                string paymentMethod = collection["paymentMethod"];
                if (string.IsNullOrEmpty(paymentMethod))
                {
                    TempData["error"] = "Vui lòng chọn phương thức thanh toán!";
                    return RedirectToAction("DatHang");
                }

                // Lấy thông tin shipping từ Session nếu có
                var shippingInfo = Session["OrderShippingInfo"] as ShippingInfo;

                // Tạo đơn hàng mới
                Order dh = new Order
                {
                    ID_KH = kh.idUser,
                    NgayDat = DateTime.Now,
                    NgayCapNhat = DateTime.Now,
                    Status = "Chưa giao hàng",
                    Payment = paymentMethod.Equals("momo", StringComparison.OrdinalIgnoreCase) ||
                              paymentMethod.Equals("paypal", StringComparison.OrdinalIgnoreCase),
                    ThanhTien = TongTien(),
                    TongSoLuong = TongSoLuong(),
                    Id_NV = null,
                    PaymentMethod = paymentMethod // Lưu phương thức thanh toán
                };

                // Set thông tin shipping
                if (shippingInfo != null)
                {
                    dh.ShippingName = shippingInfo.ShippingName;
                    dh.ShippingPhone = shippingInfo.ShippingPhone;
                    dh.ShippingAddress = shippingInfo.ShippingAddress;
                    dh.Address = shippingInfo.ShippingAddress; // Để tương thích ngược
                }
                else
                {
                    // Sử dụng thông tin mặc định từ profile
                    dh.ShippingName = kh.HoTen;
                    dh.ShippingPhone = kh.Sdt;
                    dh.ShippingAddress = kh.Address;
                    dh.Address = kh.Address;
                }

                // Thêm đơn hàng vào database
                _db.Orders.Add(dh);
                _db.SaveChanges();

                // Thêm chi tiết đơn hàng
                foreach (var item in gh)
                {
                    var product = _db.Products.Find(item.IdProduct);
                    if (product != null)
                    {
                        if (product.SoLuong < item.SoLuong)
                        {
                            TempData["error"] = $"Sản phẩm {product.ProductName} không đủ số lượng!";
                            return RedirectToAction("Index");
                        }

                        Order_Detail ctdh = new Order_Detail
                        {
                            ID_Order = dh.ID,
                            ID_Product = item.IdProduct,
                            SoLuong = item.SoLuong,
                            Price = item.ThanhTien
                        };

                        _db.Order_Detail.Add(ctdh);

                        // Cập nhật số lượng sản phẩm
                        product.SoLuong -= item.SoLuong;
                        product.ProductSold += item.SoLuong;
                        _db.Entry(product).State = EntityState.Modified;
                    }
                }

                _db.SaveChanges();

                // Xóa session
                Session["Giohang"] = null;
                Session.Remove("OrderShippingInfo");

                TempData["success"] = "Đặt hàng thành công!";
                return RedirectToAction("XacnhanDonhang");
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("DatHang");
            }
        }

        [HttpPost]
        public JsonResult CapNhatThongTinGiaoHang(string fullName, string phone, string address)
        {
            try
            {
                if (Session["TaiKhoan"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại" });
                }

                // Validate dữ liệu
                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(address))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin" });
                }

                var shippingInfo = new ShippingInfo
                {
                    ShippingName = fullName.Trim(),
                    ShippingPhone = phone.Trim(),
                    ShippingAddress = address.Trim(),
                    CreatedAt = DateTime.Now
                };

                // Lưu thông tin shipping tạm thời vào Session
                Session["OrderShippingInfo"] = shippingInfo;

                return Json(new
                {
                    success = true,
                    message = "Cập nhật thông tin giao hàng thành công",
                    data = new
                    {
                        fullName = shippingInfo.ShippingName,
                        phone = shippingInfo.ShippingPhone,
                        address = shippingInfo.ShippingAddress
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public ActionResult XacnhanDonhang()
        {
            return View();
        }

        //-----------------------------------------------------------------------------MOMO------------------------------------------------------------//
        public ActionResult Payment()
        {
            try
            {
                if (Session["TaiKhoan"] == null)
                {
                    return RedirectToAction("Login", "User");
                }

                KhachHang kh = (KhachHang)Session["TaiKhoan"];
                List<Giohang> gh = Laygiohang();

                if (gh == null || !gh.Any())
                {
                    return RedirectToAction("Index", "GioHang");
                }

                // Cấu hình thanh toán MoMo
                string endpoint = "https://test-payment.momo.vn/gw_payment/transactionProcessor";
                string partnerCode = "MOMO";
                string accessKey = "F8BBA842ECF85";
                string serectkey = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
                string returnUrl = "https://localhost:44351/GioHang/ReturnUrl";
                string notifyurl = "http://ba1adf48beba.ngrok.io/GioHang/SavePayment";

                string storeName = "Tap Hoa 888";
                string amount = TongTien().ToString();
                string orderId = DateTime.Now.Ticks.ToString();
                string requestId = DateTime.Now.Ticks.ToString();
                string orderInfo = $"{storeName} - Don hang #{orderId}";
                string extraData = "";

                // Tạo chuỗi hash để ký
                string rawHash =
                    "partnerCode=" + partnerCode +
                    "&accessKey=" + accessKey +
                    "&requestId=" + requestId +
                    "&amount=" + amount +
                    "&orderId=" + orderId +
                    "&orderInfo=" + orderInfo +
                    "&returnUrl=" + returnUrl +
                    "&notifyUrl=" + notifyurl +
                    "&extraData=" + extraData;

                MoMoSecurity crypto = new MoMoSecurity();
                string signature = crypto.signSHA256(rawHash, serectkey);

                JObject message = new JObject
        {
            { "partnerCode", partnerCode },
            { "accessKey", accessKey },
            { "requestId", requestId },
            { "amount", amount },
            { "orderId", orderId },
            { "orderInfo", orderInfo },
            { "returnUrl", returnUrl },
            { "notifyUrl", notifyurl },
            { "extraData", extraData },
            { "requestType", "captureMoMoWallet" },
            { "signature", signature }
        };

                string responseFromMomo = PaymentRequest.sendPaymentRequest(endpoint, message.ToString());
                JObject jmessage = JObject.Parse(responseFromMomo);

                if (jmessage.GetValue("errorCode").ToString() == "0")
                {
                    // Lưu thông tin đơn hàng vào TempData để xử lý sau khi thanh toán thành công
                    TempData["OrderInfo"] = new
                    {
                        CustomerId = kh.idUser,
                        Amount = amount,
                        OrderId = orderId,
                        Cart = gh
                    };

                    return Redirect(jmessage.GetValue("payUrl").ToString());
                }
                else
                {
                    TempData["error"] = "Lỗi kết nối đến cổng thanh toán: " + jmessage.GetValue("message").ToString();
                    return RedirectToAction("DatHang");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("DatHang");
            }
        }

        public ActionResult ReturnUrl()
        {
            try
            {
                string param = Request.QueryString.ToString().Substring(0, Request.QueryString.ToString().IndexOf("signature") - 1);
                param = Server.UrlDecode(param);
                MoMoSecurity crypto = new MoMoSecurity();
                string serectkey = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
                string signature = crypto.signSHA256(param, serectkey);

                if (signature != Request["signature"].ToString())
                {
                    TempData["error"] = "Thông tin thanh toán không hợp lệ!";
                    return RedirectToAction("ThatBai");
                }

                string errorCode = Request.QueryString["errorCode"];

                if (errorCode == "0")
                {
                    // Lấy thông tin khách hàng
                    KhachHang kh = (KhachHang)Session["TaiKhoan"];
                    if (kh == null)
                    {
                        TempData["error"] = "Không tìm thấy thông tin tài khoản";
                        return RedirectToAction("ThatBai");
                    }

                    // Lấy thông tin giỏ hàng
                    List<Giohang> gh = Laygiohang();
                    if (gh == null || !gh.Any())
                    {
                        TempData["error"] = "Giỏ hàng trống";
                        return RedirectToAction("ThatBai");
                    }

                    // Lấy thông tin giao hàng
                    var shippingInfo = Session["OrderShippingInfo"] as ShippingInfo;

                    // Tạo đơn hàng mới
                    Order dh = new Order
                    {
                        ID_KH = kh.idUser,
                        NgayDat = DateTime.Now,
                        NgayCapNhat = DateTime.Now,
                        Status = "Chưa giao hàng", // Đổi thành "Chưa giao hàng" thay vì "Đã thanh toán MoMo"
                        Payment = true, // Đánh dấu là đã thanh toán
                        ThanhTien = TongTien(),
                        TongSoLuong = TongSoLuong(),
                        Id_NV = null,
                        PaymentMethod = "MoMo" // Thêm thông tin phương thức thanh toán
                    };

                    // Thêm thông tin giao hàng
                    if (shippingInfo != null)
                    {
                        dh.ShippingName = shippingInfo.ShippingName;
                        dh.ShippingPhone = shippingInfo.ShippingPhone;
                        dh.ShippingAddress = shippingInfo.ShippingAddress;
                        dh.Address = shippingInfo.ShippingAddress;
                    }
                    else
                    {
                        dh.ShippingName = kh.HoTen;
                        dh.ShippingPhone = kh.Sdt;
                        dh.ShippingAddress = kh.Address;
                        dh.Address = kh.Address;
                    }

                    _db.Orders.Add(dh);
                    _db.SaveChanges();

                    // Thêm chi tiết đơn hàng
                    foreach (var item in gh)
                    {
                        Order_Detail ctdh = new Order_Detail
                        {
                            ID_Order = dh.ID,
                            ID_Product = item.IdProduct,
                            SoLuong = item.SoLuong,
                            Price = item.ThanhTien
                        };

                        _db.Order_Detail.Add(ctdh);

                        // Cập nhật số lượng sản phẩm
                        var product = _db.Products.Find(item.IdProduct);
                        if (product != null)
                        {
                            product.SoLuong -= item.SoLuong;
                            product.ProductSold += item.SoLuong;
                            _db.Entry(product).State = EntityState.Modified;
                        }
                    }

                    _db.SaveChanges();

                    // Xóa session
                    Session["Giohang"] = null;
                    Session.Remove("OrderShippingInfo");

                    TempData["success"] = "Thanh toán MoMo thành công!";
                    return RedirectToAction("XacnhanDonhang");
                }

                TempData["error"] = "Thanh toán thất bại! Mã lỗi: " + errorCode;
                return RedirectToAction("ThatBai");
            }
            catch (Exception ex)
            {
                TempData["error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("ThatBai");
            }
        }

        // Lớp MoMoSecurity để tạo chữ ký
        public class MoMoSecurity
        {
            public string signSHA256(string message, string key)
            {
                byte[] keyByte = Encoding.UTF8.GetBytes(key);
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                using (var hmacsha256 = new HMACSHA256(keyByte))
                {
                    byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                    string hex = BitConverter.ToString(hashmessage);
                    hex = hex.Replace("-", "").ToLower();
                    return hex;
                }
            }
        }

        // Lớp PaymentRequest để gửi request đến MoMo
        public class PaymentRequest
        {
            public static string sendPaymentRequest(string endpoint, string postJsonString)
            {
                try
                {
                    HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(endpoint);

                    var postData = postJsonString;

                    var data = Encoding.UTF8.GetBytes(postData);

                    httpWReq.ProtocolVersion = HttpVersion.Version11;
                    httpWReq.Method = "POST";
                    httpWReq.ContentType = "application/json";
                    httpWReq.ContentLength = data.Length;
                    httpWReq.ReadWriteTimeout = 30000;
                    httpWReq.Timeout = 15000;
                    Stream stream = httpWReq.GetRequestStream();
                    stream.Write(data, 0, data.Length);
                    stream.Close();

                    HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();

                    string jsonresponse = "";

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string temp = null;
                        while ((temp = reader.ReadLine()) != null)
                        {
                            jsonresponse += temp;
                        }
                    }

                    return jsonresponse;
                }
                catch (WebException e)
                {
                    return e.Message;
                }
            }
        }

        public ActionResult ThatBai()
        {
            try
            {
                // Lấy OrderId từ Session nếu có
                int orderId = Session["OrderId"] as int? ?? 0;
                if (orderId > 0)
                {
                    var order = _db.Orders.Find(orderId);
                    if (order != null)
                    {
                        // Hoàn lại số lượng sản phẩm
                        var orderDetails = _db.Order_Detail.Where(x => x.ID_Order == orderId);
                        foreach (var detail in orderDetails)
                        {
                            var product = _db.Products.Find(detail.ID_Product);
                            if (product != null)
                            {
                                product.SoLuong += detail.SoLuong;
                                product.ProductSold -= detail.SoLuong;
                                _db.Entry(product).State = EntityState.Modified;
                            }
                        }

                        // Xóa chi tiết đơn hàng và đơn hàng
                        _db.Order_Detail.RemoveRange(orderDetails);
                        _db.Orders.Remove(order);
                        _db.SaveChanges();
                    }
                }

                // Xóa thông tin session liên quan
                Session["OrderId"] = null;

                // Nếu không có message lỗi trong TempData, thêm message mặc định
                if (TempData["error"] == null)
                {
                    TempData["error"] = "Đã xảy ra lỗi trong quá trình thanh toán. Vui lòng thử lại sau.";
                }

                return View();
            }
            catch (Exception ex)
            {
                // Log error nếu cần
                TempData["error"] = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        //----------------------------------------------------------------------PayPal------------------------------------------------------------------------------//

        public ActionResult PaymentWithPayPal()
        {
            try
            {
                if (Session["TaiKhoan"] == null)
                {
                    return RedirectToAction("Login", "User");
                }

                KhachHang kh = (KhachHang)Session["TaiKhoan"];
                List<Giohang> gh = Laygiohang();

                if (gh == null || !gh.Any())
                {
                    return RedirectToAction("Index");
                }

                var paypal = new PayPalModel(true);

                // Tạo mã đơn hàng
                string orderId = DateTime.Now.Ticks.ToString();
                paypal.item_name = $"Order_{orderId}";

                // Chuyển đổi tiền sang USD (làm tròn 2 chữ số thập phân)
                double tongTienVND = TongTien();
                double tyGiaUSD = 0.000043; // Cập nhật tỷ giá thực tế
                double tongTienUSD = Math.Round(tongTienVND * tyGiaUSD, 2);

                paypal.amount = tongTienUSD.ToString("0.00", CultureInfo.InvariantCulture);
                paypal.item_quantity = "1"; // PayPal thường yêu cầu quantity là 1 và amount là tổng

                // Lưu thông tin đơn hàng vào TempData
                var orderInfo = new
                {
                    OrderId = orderId,
                    CustomerId = kh.idUser,
                    Amount = tongTienVND,
                    Cart = gh,
                    CreatedAt = DateTime.Now
                };
                TempData["PayPalOrderInfo"] = orderInfo;

                return View(paypal);
            }
            catch (Exception ex)
            {
                TempData["error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("DatHang");
            }
        }

        [HttpPost]
        public ActionResult PayPalSuccess(FormCollection form)
        {
            try
            {
                // Verify payment
                string transactionId = form["txn_id"];
                string paymentStatus = form["payment_status"];
                string payerId = form["payer_id"];

                if (!string.IsNullOrEmpty(paymentStatus) && paymentStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    dynamic orderInfo = TempData["PayPalOrderInfo"];
                    if (orderInfo != null)
                    {
                        // Tạo đơn hàng mới
                        Order dh = new Order
                        {
                            ID_KH = orderInfo.CustomerId,
                            NgayDat = DateTime.Now,
                            NgayCapNhat = DateTime.Now,
                            Status = "Chưa giao hàng", // Thay đổi từ "Đã thanh toán PayPal" thành "Chưa giao hàng"
                            Payment = true, // Đánh dấu là đã thanh toán
                            PaymentMethod = "PayPal", // Lưu phương thức thanh toán
                            ThanhTien = orderInfo.Amount,
                            TongSoLuong = TongSoLuong(),
                            Id_NV = null
                        };

                        _db.Orders.Add(dh);
                        _db.SaveChanges();

                        foreach (var item in orderInfo.Cart)
                        {
                            // Thêm chi tiết đơn hàng
                            Order_Detail ctdh = new Order_Detail
                            {
                                ID_Order = dh.ID,
                                ID_Product = item.IdProduct,
                                SoLuong = item.SoLuong,
                                Price = item.ThanhTien
                            };

                            _db.Order_Detail.Add(ctdh);

                            // Cập nhật số lượng sản phẩm
                            var product = _db.Products.Find(item.IdProduct);
                            if (product != null)
                            {
                                product.SoLuong -= item.SoLuong;
                                product.ProductSold += item.SoLuong;
                                _db.Entry(product).State = EntityState.Modified;
                            }
                        }

                        _db.SaveChanges();
                        Session["Giohang"] = null;
                        TempData["success"] = "Thanh toán PayPal thành công!";
                        return RedirectToAction("XacnhanDonhang");
                    }
                }

                TempData["error"] = "Thanh toán thất bại!";
                return RedirectToAction("ThatBai");
            }
            catch (Exception ex)
            {
                TempData["error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("ThatBai");
            }
        }
    }
}