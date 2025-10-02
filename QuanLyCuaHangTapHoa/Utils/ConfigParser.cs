namespace QuanLyCuaHangTapHoa.Utils
{
    using System;
    using System.Diagnostics;
    using System.Configuration;

    public class ConfigParser
    {
        public static string Parse(string key)
        {
            string result = string.Empty;
            try
            {
                result = ConfigurationManager.AppSettings[key.Trim()].Trim();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result;
        }
    }
}