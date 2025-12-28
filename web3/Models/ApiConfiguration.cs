// Models/ApiConfiguration.cs
namespace ecom.Models
{
    public class ApiConfiguration
    {
        public int Id { get; set; }
        public string ApiName { get; set; } // "Zendrop", "CJDropshipping", etc.
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}