using PagedList;
using QuanLyCuaHangTapHoa.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLyCuaHangTapHoa.Utils;
using System.Data.Entity;
using System.Net;
using System.Windows.Input;

namespace QuanLyCuaHangTapHoa.Controllers
{
    public class ProductController : Controller
    {
        quantaphoaEntities _db = new quantaphoaEntities();
        public static readonly string PRODUCT_IMG_PATH = ConfigParser.Parse("products_img_path");

        //----------------------------------------------------------------------User--------------------------------------------------------------------//
        public ActionResult TatCaSanPham(string searchString, int? category, string sortOrder, int? page)
        {
            var products = _db.Products.Include(p => p.Catalog).AsQueryable();

            // Lọc theo search
            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString));
                ViewBag.searchStr = searchString;
            }

            // Lọc theo danh mục
            if (category.HasValue)
            {
                products = products.Where(p => p.CatalogId == category.Value);
                ViewBag.SelectedCategory = category;
            }

            // Sắp xếp
            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "priceAsc":
                    products = products.OrderBy(p => p.UnitPrice);
                    break;
                case "priceDesc":
                    products = products.OrderByDescending(p => p.UnitPrice);
                    break;
                case "bestseller":
                    products = products.OrderByDescending(p => p.ProductSold);
                    break;
                case "nameAsc": // Sắp xếp A-Z
                    products = products.OrderBy(p => p.ProductName);
                    break;
                case "nameDesc": // Sắp xếp Z-A
                    products = products.OrderByDescending(p => p.ProductName);
                    break;
                default: // newest
                    products = products.OrderByDescending(p => p.Id);
                    break;
            }

            // Lấy danh mục kèm theo số lượng sản phẩm cho filter
            ViewBag.Categories = _db.Catalogs
            .ToList()
            .Select(c => new CatalogViewModel
            {
                ID = c.ID,
                CatalogName = c.CatalogName,
                ProductCount = c.Products.Count,
                Icon = GetCategoryIcon(c.CatalogName)
            })
            .ToList();

            // Tính toán tổng số sản phẩm và thông tin hiển thị
            var totalItems = products.Count();
            int pageSize = 9; // Hiển thị 9 sản phẩm mỗi trang
            int pageNumber = (page ?? 1);

            ViewBag.TotalProducts = totalItems;
            ViewBag.ShowingFrom = ((pageNumber - 1) * pageSize) + 1;
            ViewBag.ShowingTo = Math.Min(pageNumber * pageSize, totalItems);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Thêm thông tin cho breadcrumb
            ViewBag.CategoryName = category.HasValue
                ? _db.Catalogs.FirstOrDefault(c => c.ID == category)?.CatalogName
                : "Tất cả sản phẩm";

            // Chuyển đổi sang PagedList
            var pagedProducts = products.ToPagedList(pageNumber, pageSize);

            return View(pagedProducts);
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

            return "fa-box"; // Icon mặc định
        }

        public ActionResult ChitietSP(int? id)
        {
            try
            {
                if (!id.HasValue || id <= 0)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return RedirectToAction("TatCaSanPham");
                }

                var product = _db.Products
                    .Include(p => p.Catalog)
                    .Include(p => p.DanhGias)
                    .FirstOrDefault(p => p.Id == id);

                if (product == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return RedirectToAction("TatCaSanPham");
                }

                // Lấy danh sách đánh giá kèm thông tin người dùng
                var reviews = _db.DanhGias
                    .Include(d => d.KhachHang)
                    .Where(d => d.id_sp == id)
                    .OrderByDescending(d => d.Ngaycapnhap)
                    .ToList();

                // Tính điểm đánh giá trung bình
                double averageRating = 0;
                if (reviews.Any())
                {
                    averageRating = reviews.Average(r => r.Rating ?? 0);
                    product.AverageRating = Math.Round(averageRating, 1);
                }

                // Lấy sản phẩm tương tự
                ViewBag.RelatedProducts = GetRelatedProducts(product.Id, product.CatalogId.Value);

                ViewBag.Reviews = reviews;
                ViewBag.AverageRating = averageRating;
                ViewBag.ReviewCount = reviews.Count;

                // Kiểm tra người dùng hiện tại đã đánh giá chưa 
                if (Session["UserID"] != null)
                {
                    int userId = (int)Session["UserID"];
                    var existingReview = reviews.FirstOrDefault(r => r.id_kh == userId);
                    ViewBag.UserReview = existingReview; // Truyền review hiện có của user (nếu có)
                }

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi tải thông tin sản phẩm";
                return RedirectToAction("TatCaSanPham");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiDanhGia(int id_sp, string content, double? rating)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để đánh giá sản phẩm";
                    return RedirectToAction("Login", "User");
                }

                // Validate input
                if (string.IsNullOrEmpty(content))
                {
                    TempData["ReviewError"] = "Vui lòng nhập nội dung đánh giá";
                    return RedirectToAction("ChitietSP", new { id = id_sp });
                }

                if (content.Length < 10)
                {
                    TempData["ReviewError"] = "Nội dung đánh giá phải có ít nhất 10 ký tự";
                    return RedirectToAction("ChitietSP", new { id = id_sp });
                }

                if (!rating.HasValue || rating < 1 || rating > 5)
                {
                    TempData["ReviewError"] = "Vui lòng chọn số sao từ 1-5";
                    return RedirectToAction("ChitietSP", new { id = id_sp });
                }

                int userId = (int)Session["UserID"];

                // Kiểm tra sản phẩm có tồn tại
                var product = _db.Products.Find(id_sp);
                if (product == null)
                {
                    TempData["ReviewError"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("TatCaSanPham");
                }

                // Tạo đánh giá mới
                var review = new DanhGia
                {
                    id_sp = id_sp,
                    id_kh = userId,
                    Content = content,
                    Rating = rating,
                    Ngaycapnhap = DateTime.Now
                };

                _db.DanhGias.Add(review);
                _db.SaveChanges();

                // Cập nhật rating trung bình cho sản phẩm
                var reviews = _db.DanhGias.Where(d => d.id_sp == id_sp);
                if (reviews.Any())
                {
                    var avgRating = reviews.Average(d => d.Rating ?? 0);
                    product.AverageRating = Math.Round(avgRating, 1);
                    _db.SaveChanges();
                }

                TempData["ReviewSuccess"] = "Cảm ơn bạn đã đánh giá sản phẩm";
                return RedirectToAction("ChitietSP", new { id = id_sp });
            }
            catch (Exception ex)
            {
                // Log error
                TempData["ReviewError"] = "Có lỗi xảy ra khi gửi đánh giá. Vui lòng thử lại sau.";
                return RedirectToAction("ChitietSP", new { id = id_sp });
            }
        }

        // Thêm action để load thêm đánh giá (phân trang)
        public ActionResult LoadMoreReviews(int productId, int page = 1, int pageSize = 5)
        {
            var reviews = _db.DanhGias
                .Include(d => d.KhachHang)
                .Where(d => d.id_sp == productId)
                .OrderByDescending(d => d.Ngaycapnhap)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return PartialView("_ReviewsList", reviews);
        }

        private string GetStockStatus(int quantity)
        {
            if (quantity <= 0)
                return "Hết hàng";
            if (quantity <= 50)
                return "Sắp hết hàng";
            return "Còn hàng";
        }

        private double CalculateDiscount(double originalPrice, string discountPercentage)
        {
            if (double.TryParse(discountPercentage, out double discount))
            {
                return (originalPrice * discount) / 100;
            }
            return 0;
        }

        // Phương thức cập nhật lượt xem sản phẩm (tùy chọn)
        private void UpdateProductViews(int productId)
        {
            try
            {
                var product = _db.Products.Find(productId);
                if (product != null)
                {
                    // Thêm logic cập nhật lượt xem ở đây nếu bạn muốn theo dõi
                    _db.SaveChanges();
                }
            }
            catch (Exception)
            {
                // Log lỗi nếu cần
            }
        }

        // Phương thức kiểm tra và xử lý giỏ hàng (tùy chọn)
        [HttpPost]
        public JsonResult CheckProductAvailability(int productId, int quantity)
        {
            try
            {
                var product = _db.Products.Find(productId);
                if (product == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

                if (product.SoLuong < quantity)
                    return Json(new { success = false, message = "Số lượng sản phẩm trong kho không đủ" });

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        //----------------------------------------------------------------End - User-------------------------------------------------------------------//
        public ActionResult Indexadminsp(string sort, int? page, string searchString, int? category)
        {
            const int pageSize = 10;
            int pageNumber = page ?? 1;

            var products = _db.Products.Include("Catalog").ToList();

            if (category.HasValue && category != 0) // Assuming 0 means "all categories"
            {
                products = products.Where(p => p.Catalog.ID == category).ToList();
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                products = products.Where(p => p.ProductName.ToLower().Contains(searchString) ||
                                               p.Catalog.CatalogName.ToLower().Contains(searchString)).ToList();
            }

            // Filter products based on the sort parameter
            if (sort == "pre-sold")
            {
                products = products.Where(p => p.SoLuong > 0 && p.SoLuong <= 50).ToList();
            }
            else if (sort == "sold")
            {
                products = products.Where(p => p.SoLuong == 0).ToList();
            }
            else if (sort == "available")
            {
                products = products.Where(p => p.SoLuong > 50).ToList();
            }

            ViewBag.totalPage = Math.Ceiling((double)products.Count() / pageSize);
            ViewBag.products = products.ToPagedList(pageNumber, pageSize);
            ViewBag.searchStr = searchString;
            ViewBag.Sort = sort;
            ViewBag.category = category;

            return View(ViewBag.products);
        }

        public ActionResult Create()
        {
            ViewBag.CatalogId = new SelectList(_db.Catalogs, "ID", "CatalogName");
            return View();
        }
        [HttpPost, ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product sanPham)
        {
            if (ModelState.IsValid)
            {
                if (sanPham.ProductThumbnailStream != null)
                {
                    string extension = Path.GetExtension(sanPham.ProductThumbnailStream.FileName).ToLower();
                    string modifiedFileName = $"{DateTime.Now.ToString("hhmmss_ddMMyyyy")}{extension}";
                    sanPham.ProductThumbnailStream.SaveAs($"{Server.MapPath(PRODUCT_IMG_PATH)}/{modifiedFileName}");
                    sanPham.Picture = modifiedFileName;
                }
                sanPham.NgayNhapHang = DateTime.Now;
                Random prCode = new Random();
                sanPham.ProductCode = String.Concat("PR", prCode.Next(5000, 7000).ToString());
                sanPham.ProductSold = 0;
                sanPham.UnitPrice = sanPham.ProductSale != null
                    ? (sanPham.UnitPrice = sanPham.PriceOld - (sanPham.PriceOld * int.Parse(sanPham.ProductSale)) / 100)
                    : sanPham.UnitPrice = sanPham.PriceOld;

                // Thêm sản phẩm trực tiếp vào cơ sở dữ liệu
                if (sanPham.PriceOld >= 0)
                {
                    _db.Products.Add(sanPham);
                    _db.SaveChanges();
                    return RedirectToAction("Indexadminsp");
                }
                else
                {
                    // Xử lý trường hợp dữ liệu không hợp lệ
                    ModelState.AddModelError("", "Dữ liệu không hợp lệ");
                }
            }

            ViewBag.CatalogId = new SelectList(_db.Catalogs, "ID", "CatalogName", sanPham.CatalogId);
            return View();


        }
        //ProductController
        //Sửa Sản phẩm
        public ActionResult Edit(int id)
        {
            Product product = _db.Products.Find(id);
            ViewBag.CatalogId = new SelectList(_db.Catalogs, "ID", "CatalogName", product.CatalogId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult Edit(Product sanPham)
        {
            var pr = _db.Products.FirstOrDefault(p => p.Id == sanPham.Id);
            if (ModelState.IsValid)
            {
                if (sanPham.ProductThumbnailStream != null)
                {
                    // Lấy đường dẫn và lưu trữ ảnh
                    string extension = Path.GetExtension(sanPham.ProductThumbnailStream.FileName).ToLower();
                    string modifiedFileName = $"{sanPham.ProductName.ToLower()}_{DateTime.Now.ToString("hhmmss_ddMMyyyy")}{extension}";
                    string path = Path.Combine(Server.MapPath("~/Resources/Pictures/Products/"), modifiedFileName);
                    sanPham.ProductThumbnailStream.SaveAs(path);
                    pr.Picture = modifiedFileName; // Cập nhật đường dẫn ảnh trong database
                }

                // Cập nhật thông tin sản phẩm
                pr.CatalogId = sanPham.CatalogId;
                pr.ProductName = sanPham.ProductName;
                pr.PriceOld = sanPham.PriceOld;
                pr.UnitPrice = (sanPham.ProductSale != null) ? (sanPham.PriceOld - (sanPham.PriceOld * int.Parse(sanPham.ProductSale)) / 100) : sanPham.PriceOld;
                pr.ProductCode = sanPham.ProductCode;
                pr.ProductSale = sanPham.ProductSale;
                pr.SoLuong = sanPham.SoLuong;
                pr.NgayNhapHang = DateTime.Now;
                pr.MoTa = sanPham.MoTa;

                _db.Entry(pr).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Indexadminsp");
            }
            ViewBag.CatalogId = new SelectList(_db.Catalogs, "ID", "CatalogName", sanPham.CatalogId);
            return View(sanPham);
        }
        //Hàm xóa sản phẩm 
        [HttpGet]
        public ActionResult Delete(int id)
        {
            Product pro = _db.Products.FirstOrDefault(p => p.Id == id);
            if (pro != null)
                return View(pro);
            else
                return RedirectToAction("Indexadminsp");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            int i = int.Parse(id);
            var productDB = _db.Products.FirstOrDefault(p => p.Id == i);
            if (productDB != null)
            {
                _db.Products.Remove(productDB); _db.SaveChanges();
            }
            return RedirectToAction("Indexadminsp");
        }

        public ActionResult Detail(int? id)
        {
            try
            {
                // Kiểm tra id hợp lệ
                if (!id.HasValue || id <= 0)
                {
                    // Chuyển hướng về trang danh sách nếu id không hợp lệ
                    return RedirectToAction("TatCaSanPham");
                }

                Product product = _db.Products
                    .Include(p => p.Catalog)
                    .FirstOrDefault(p => p.Id == id);

                if (product == null)
                {
                    // Chuyển hướng về trang danh sách nếu không tìm thấy sản phẩm
                    return RedirectToAction("TatCaSanPham");
                }

                return View(product);
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                return RedirectToAction("TatCaSanPham");
            }
        }
        private List<Product> GetRelatedProducts(int productId, int catalogId, int count = 8)
        {
            // Lấy các sản phẩm cùng danh mục, ngoại trừ sản phẩm hiện tại
            var relatedProducts = _db.Products
                .Where(p => p.Id != productId && p.CatalogId == catalogId)
                .OrderByDescending(p => p.ProductSold) // Sắp xếp theo số lượng bán (nếu muốn hiển thị sản phẩm bán chạy)
                .Take(count)
                .ToList();

            // Nếu không đủ sản phẩm liên quan, bổ sung thêm các sản phẩm bán chạy khác
            if (relatedProducts.Count < count)
            {
                var additionalProducts = _db.Products
                    .Where(p => p.Id != productId && p.CatalogId != catalogId)
                    .OrderByDescending(p => p.ProductSold)
                    .Take(count - relatedProducts.Count)
                    .ToList();

                relatedProducts.AddRange(additionalProducts);
            }

            return relatedProducts;
        }
    }
}