using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using PagedList;
using QuanLyCuaHangTapHoa.Models;

namespace QuanLyCuaHangTapHoa.Controllers
{
    public class UserController : Controller
    {
        private quantaphoaEntities db = new quantaphoaEntities();
        public ActionResult TrangChu(string searchString, int? category, int? page)
        {
            var products = db.Products.Include(p => p.Catalog).AsQueryable();

            ViewBag.Categories = db.Catalogs
                .ToList()
                .Select(c => new CatalogViewModel
                {
                    ID = c.ID,
                    CatalogName = c.CatalogName,
                    ProductCount = c.Products.Count,
                    Icon = GetCategoryIcon(c.CatalogName)
                })
                .ToList();

            ViewBag.SelectedCategory = category;

            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString));
            }

            if (category.HasValue)
            {
                products = products.Where(p => p.CatalogId == category.Value);
            }

            ViewBag.FeaturedProducts = products
                .OrderByDescending(p => p.Id)
                .Take(4)
                .ToList();

            ViewBag.BestSellers = products
                .OrderByDescending(p => p.SoLuong)
                .Take(4)
                .ToList();

            products = products.OrderByDescending(p => p.Id);

            int pageSize = 12;
            int pageNumber = (page ?? 1);

            var totalItems = products.Count();
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = pageNumber;

            var pagedProducts = products.ToPagedList(pageNumber, pageSize);

            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = pageNumber < ViewBag.TotalPages;

            ViewBag.TotalProducts = totalItems;
            ViewBag.ShowingFrom = ((pageNumber - 1) * pageSize) + 1;
            ViewBag.ShowingTo = Math.Min(pageNumber * pageSize, totalItems);

            ViewBag.CurrentSort = Request.QueryString["sort"];
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentSearch = searchString;

            return View(pagedProducts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            var user = db.KhachHangs.FirstOrDefault(u => u.Email == email);
            if (user == null || user.Password != password)
            {
                TempData["ErrorMessage"] = "Email hoặc mật khẩu không chính xác";
                return View();
            }

            // Refresh user data from database to ensure we have the latest
            db.Entry(user).Reload();

            // Lưu thông tin session
            Session["UserID"] = user.idUser;
            Session["UserName"] = user.HoTen;
            Session["UserEmail"] = user.Email;
            Session["TaiKhoan"] = user;  // Lưu toàn bộ object user

            TempData["SuccessMessage"] = "Đăng nhập thành công!";
            return RedirectToAction("TrangChu", "User");
        }

        public ActionResult Login()
        {
            if (Session["TaiKhoan"] != null)
            {
                return RedirectToAction("TrangChu", "User");
            }
            return View();
        }

        private string GetCategoryIcon(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return "fa-box";

            categoryName = categoryName.ToLower();

            if (categoryName.Contains("đồ uống"))
                return "fa-wine-bottle";
            if (categoryName.Contains("thực phẩm"))
                return "fa-utensils";
            if (categoryName.Contains("gia dụng"))
                return "fa-home";
            if (categoryName.Contains("văn phòng phẩm"))
                return "fa-pen";

            return "fa-box";
        }

        public ActionResult Register()
        {
            if (Session["UserID"] != null)
                return RedirectToAction("Login", "User");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register([Bind(Include = "FirstName,LastName,Email,Password")] KhachHang khachHang)
        {
            if (ModelState.IsValid)
            {
                var emailExists = db.KhachHangs.Any(u => u.Email == khachHang.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    return View(khachHang);
                }

                // Thiết lập các giá trị mặc định
                khachHang.NgayTao = DateTime.Now;
                khachHang.TichLuy = 0;

                db.KhachHangs.Add(khachHang);
                db.SaveChanges();

                Session["UserID"] = khachHang.idUser;
                Session["UserName"] = khachHang.HoTen;
                Session["UserEmail"] = khachHang.Email;

                return RedirectToAction("Login", "User");
            }

            return View(khachHang);
        }

        [HttpGet]
        public ActionResult Profile()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int userId = (int)Session["UserID"];
            var user = db.KhachHangs.Find(userId);

            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile([Bind(Include = "idUser,FirstName,LastName,Email,Address,NgaySinh,CMT,Sdt")] KhachHang khachHang, HttpPostedFileBase AnhDaiDien)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = db.KhachHangs.Find(khachHang.idUser);
                    if (existingUser == null)
                        return HttpNotFound();

                    if (existingUser.Email != khachHang.Email)
                    {
                        var emailExists = db.KhachHangs.Any(u => u.Email == khachHang.Email && u.idUser != khachHang.idUser);
                        if (emailExists)
                        {
                            ModelState.AddModelError("Email", "Email này đã được sử dụng");
                            return View(khachHang);
                        }
                    }

                    if (AnhDaiDien != null && AnhDaiDien.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(AnhDaiDien.FileName);
                        string path = Path.Combine(Server.MapPath("~/Resources/Pictures/Users"), fileName);

                        if (!string.IsNullOrEmpty(existingUser.Picture))
                        {
                            var oldPath = Path.Combine(Server.MapPath("~/Resources/Pictures/Users"), existingUser.Picture);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        AnhDaiDien.SaveAs(path);
                        existingUser.Picture = fileName;
                    }

                    existingUser.FirstName = khachHang.FirstName;
                    existingUser.LastName = khachHang.LastName;
                    existingUser.Email = khachHang.Email;
                    existingUser.Address = khachHang.Address;
                    existingUser.NgaySinh = khachHang.NgaySinh;
                    existingUser.CMT = khachHang.CMT;
                    existingUser.Sdt = khachHang.Sdt;

                    db.Entry(existingUser).State = EntityState.Modified;
                    db.SaveChanges();

                    Session["UserName"] = existingUser.HoTen;

                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thông tin. Vui lòng thử lại sau.");
                }
            }

            return View(khachHang);
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới và xác nhận mật khẩu không khớp";
                return View();
            }

            int userId = (int)Session["UserID"];
            var user = db.KhachHangs.Find(userId);

            if (user.Password != currentPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác";
                return View();
            }

            try
            {
                user.Password = newPassword;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại sau.";
                return View();
            }
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập email";
                return View();
            }

            var user = db.KhachHangs.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Email không tồn tại trong hệ thống";
                return View();
            }

            try
            {
                string resetToken = Guid.NewGuid().ToString();
                user.MaResetMatKhau = resetToken;
                user.ThoiHanMaReset = DateTime.Now.AddHours(24);

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                SendPasswordResetEmail(user.Email, resetToken);

                TempData["SuccessMessage"] = "Link đặt lại mật khẩu đã được gửi đến email của bạn!";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau.";
                return View();
            }
        }

        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var user = db.KhachHangs.FirstOrDefault(u => u.MaResetMatKhau == token && u.ThoiHanMaReset > DateTime.Now);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn";
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới và xác nhận mật khẩu không khớp";
                ViewBag.Token = token;
                return View();
            }

            var user = db.KhachHangs.FirstOrDefault(u => u.MaResetMatKhau == token && u.ThoiHanMaReset > DateTime.Now);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn";
                return RedirectToAction("Login");
            }

            try
            {
                user.Password = newPassword;
                user.MaResetMatKhau = null;
                user.ThoiHanMaReset = null;

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt lại mật khẩu. Vui lòng thử lại sau.";
                ViewBag.Token = token;
                return View();
            }
        }

        private void SendPasswordResetEmail(string email, string resetToken)
        {
            var resetLink = Url.Action("ResetPassword", "User",
                new { token = resetToken }, protocol: Request.Url.Scheme);

            var fromAddress = new MailAddress(ConfigurationManager.AppSettings["EmailFrom"], "Quản lý cửa hàng tạp hóa");
            var toAddress = new MailAddress(email);

            var smtp = new SmtpClient
            {
                Host = ConfigurationManager.AppSettings["SmtpHost"],
                Port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(
                    ConfigurationManager.AppSettings["SmtpUser"],
                    ConfigurationManager.AppSettings["SmtpPassword"])
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = "Đặt lại mật khẩu",
                Body = $@"
                <h2>Yêu cầu đặt lại mật khẩu</h2>
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                <p>Để đặt lại mật khẩu, vui lòng click vào link bên dưới:</p>
                <p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>
                <p>Link này sẽ hết hạn sau 24 giờ.</p>
                <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>",
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}