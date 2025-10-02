using System.Configuration;

namespace QuanLyCuaHangTapHoa.Models
{
    public class PayPalModel
    {
        public string cmd { get; set; }
        public string business { get; set; }
        public string no_shipping { get; set; }
        public string @return { get; set; } 
        public string cancel_return { get; set; }
        public string notify_url { get; set; }
        public string currency_code { get; set; }
        public string item_name { get; set; }
        public string item_quantity { get; set; }
        public string amount { get; set; }
        public string actionURL { get; set; }

        public PayPalModel(bool useSandbox)
        {
            cmd = "_xclick";
            // Read from Web.config instead of hardcoding
            business = ConfigurationManager.AppSettings["business"];
            no_shipping = "1";
            @return = ConfigurationManager.AppSettings["return"];
            cancel_return = ConfigurationManager.AppSettings["cancel_return"];
            notify_url = ConfigurationManager.AppSettings["notify_url"];
            currency_code = ConfigurationManager.AppSettings["currency_code"];
            
            // Use the correct URL based on sandbox/production mode
            actionURL = useSandbox 
                ? ConfigurationManager.AppSettings["test_url"]
                : ConfigurationManager.AppSettings["Prod_url"];
        }
    }
}