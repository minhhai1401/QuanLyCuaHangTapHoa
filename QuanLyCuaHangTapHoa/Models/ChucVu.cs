namespace QuanLyCuaHangTapHoa.Models
{
    using System;
    using System.Collections.Generic;

    public partial class ChucVu
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ChucVu()
        {
            this.NhanViens = new HashSet<NhanVien>();
        }

        public int MaChucVu { get; set; }
        public string ChucVu1 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<NhanVien> NhanViens { get; set; }
    }
}
