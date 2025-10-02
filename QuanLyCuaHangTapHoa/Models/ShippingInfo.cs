using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyCuaHangTapHoa.Models
{
    public class ShippingInfo
    {
        public int? OrderId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        [StringLength(100)]
        public string ShippingName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string ShippingPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}