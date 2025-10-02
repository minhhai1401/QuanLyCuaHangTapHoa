using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;

namespace QuanLyCuaHangTapHoa.Models
{
    public partial class KhachHang
    {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public KhachHang()
        {
            this.Orders = new HashSet<Order>();
            this.DanhGias = new HashSet<DanhGia>();
        }
        [Key]
        public int idUser { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public string Password { get; set; }
        public string Picture { get; set; }
        public string Address { get; set; }
        public DateTime? NgaySinh { get; set; }

        [StringLength(12, MinimumLength = 9, ErrorMessage = "CMT/CCCD không hợp lệ")]
        public string CMT { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Sdt { get; set; }

        public int? TichLuy { get; set; }

        [Display(Name = "Ngày tạo tài khoản")]
        public DateTime? NgayTao { get; set; }

        [NotMapped]
        [Display(Name = "Họ và tên")]
        public string HoTen
        {
            get { return $"{FirstName} {LastName}".Trim(); }
        }

        [NotMapped]
        [Display(Name = "Chọn ảnh đại diện")]
        public HttpPostedFileBase AnhDaiDien { get; set; }

        // Các trường mới cho chức năng đặt lại mật khẩu
        [Display(Name = "Mã đặt lại mật khẩu")]
        public string MaResetMatKhau { get; set; }

        [Display(Name = "Thời hạn mã đặt lại")]
        public DateTime? ThoiHanMaReset { get; set; }
        // Phương thức kiểm tra mã reset còn hiệu lực
        public bool KiemTraMaResetHopLe()
        {
            if (string.IsNullOrEmpty(MaResetMatKhau) || !ThoiHanMaReset.HasValue)
                return false;

            return ThoiHanMaReset.Value > DateTime.Now;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<DanhGia> DanhGias { get; set; }
    }
}
