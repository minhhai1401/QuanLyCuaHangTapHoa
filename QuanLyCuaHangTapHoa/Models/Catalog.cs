namespace QuanLyCuaHangTapHoa.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Catalog
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Catalog()
        {
            this.Products = new HashSet<Product>();
        }

        public int ID { get; set; }
        public string CatalogCode { get; set; }
        public string CatalogName { get; set; }
        public string ProductOrigin { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Product> Products { get; set; }
    }
}
