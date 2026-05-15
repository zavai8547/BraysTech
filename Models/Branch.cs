using BraysTech.Models;
using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public class Branch
    {
        [Key]
        public int BranchID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Phone { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<AppUser> Staff { get; set; } = new List<AppUser>();
        public ICollection<PhoneSale> Sales { get; set; } = new List<PhoneSale>();
        public ICollection<IMEIStock> Stock { get; set; } = new List<IMEIStock>();
    }
}