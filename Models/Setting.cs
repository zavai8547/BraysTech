using System.ComponentModel.DataAnnotations;

namespace SplashCityCarwash.Models
{
    public class Setting
    {
        [Key]
        public int SettingID { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string? SettingValue { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}