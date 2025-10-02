using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyCuaHangTapHoa.Models
{
    public partial class DanhGia
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [MinLength(10, ErrorMessage = "Nội dung đánh giá phải có ít nhất 10 ký tự")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1-5 sao")]
        public double? Rating { get; set; }

        public DateTime? Ngaycapnhap { get; set; }
        public int? trangthai { get; set; }
        [Required]
        [ForeignKey("Product")]
        public int? id_sp { get; set; }

        [Required]
        [ForeignKey("KhachHang")]
        public int? id_kh { get; set; }

        public virtual KhachHang KhachHang { get; set; }
        public virtual Product Product { get; set; }
    }
}