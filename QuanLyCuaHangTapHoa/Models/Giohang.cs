using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace QuanLyCuaHangTapHoa.Models
{
    public class Giohang
    {
        quantaphoaEntities _db = new quantaphoaEntities();

        [Display(Name = "Mã sản phẩm")]
        public int IdProduct { get; set; }

        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; }

        [Display(Name = "Ảnh sản phẩm")]
        public string Picture { get; set; }

        [Display(Name = "Giá gốc")]
        public Double GiaGoc { get; set; }

        [Display(Name = "Số lượng")]
        public int SoLuong { get; set; }

        [Display(Name = "Giảm giá")]
        public int? GiamGia { get; set; }

        [Display(Name = "Đơn giá sau giảm")]
        public Double DonGia { get; set; }

        [Display(Name = "Danh mục")]
        public string Brand { get; set; }

        [Display(Name = "Thành tiền")]
        public Double ThanhTien
        {
            get { return SoLuong * DonGia; }
        }

        // Constructor
        public Giohang(int MaSP)
        {
            IdProduct = MaSP;
            Product product = _db.Products.Single(n => n.Id == IdProduct);
            ProductName = product.ProductName;
            Picture = product.Picture;
            GiaGoc = double.Parse(product.PriceOld.ToString());
            GiamGia = string.IsNullOrEmpty(product.ProductSale) ? 0 : int.Parse(product.ProductSale);
            DonGia = double.Parse(product.UnitPrice.ToString());
            SoLuong = 1;
            Brand = product.Catalog.CatalogName;
        }

        // Phương thức lấy đường dẫn ảnh đầy đủ
        public string DuongDanAnh
        {
            get
            {
                if (!string.IsNullOrEmpty(Picture))
                {
                    return $"/Resources/Pictures/Products/{Picture}";
                }
                return "/Resources/Pictures/Products/default-product.jpg";
            }
        }

        // Phương thức kiểm tra số lượng tồn
        public bool KiemTraSoLuongTon(int soLuongMua)
        {
            Product product = _db.Products.Find(IdProduct);
            return product != null && soLuongMua <= product.SoLuong;
        }

        // Phương thức tính giảm giá
        public string HienThiGiamGia
        {
            get
            {
                return GiamGia.HasValue && GiamGia.Value > 0 ? $"-{GiamGia}%" : "";
            }
        }

        // Phương thức định dạng giá
        public string DinhDangGia(double gia)
        {
            return string.Format("{0:N0}", gia);
        }
    }
}