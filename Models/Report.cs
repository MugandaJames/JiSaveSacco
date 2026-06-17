using System;
using System.ComponentModel.DataAnnotations;

namespace JiSaveSacco.API.Models
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }

        public int? GeneratedBy { get; set; }

        public string ReportType { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}