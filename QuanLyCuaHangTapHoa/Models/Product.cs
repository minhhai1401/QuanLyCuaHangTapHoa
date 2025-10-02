using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;

namespace QuanLyCuaHangTapHoa.Models
{
    public partial class Product
    {
        public Product()
        {
            this.DanhGias = new HashSet<DanhGia>();
        }
        public int Id { get; set; }
        public Nullable<int> CatalogId { get; set; }
        public string Picture { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public Nullable<double> PriceOld { get; set; }
        public string ProductSale { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<int> SoLuong { get; set; }
        public Nullable<int> ProductSold { get; set; }
        public Nullable<System.DateTime> NgayNhapHang { get; set; }
        public string MoTa { get; set; }

        public virtual Catalog Catalog { get; set; }

        // Add this property for file upload
        [NotMapped]
        public HttpPostedFileBase ProductThumbnailStream { get; set; }
        public string FullPicturePath
        {
            get
            {
                if (!string.IsNullOrEmpty(Picture))
                {
                    // Construct the full path of the product image
                    // This assumes that the images are stored in a folder named "Resources/Pictures/Products"
                    return $"/Resources/Pictures/Products/{Picture}";
                }
                else
                {
                    return "/Resources/Pictures/Products/no-image.png";
                }
            }
        }
        public virtual ICollection<DanhGia> DanhGias { get; set; }

        [NotMapped]
        public Nullable<double> AverageRating { get; set; }
    }
}
