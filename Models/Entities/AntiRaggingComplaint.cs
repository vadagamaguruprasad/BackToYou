using System.ComponentModel.DataAnnotations;

namespace BackToYou.Models.Entities
{
    public class AntiRaggingComplaint
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Complaint Reference")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Student Name")]
        public string? StudentName { get; set; }

        [StringLength(50)]
        [Display(Name = "Roll Number / Student ID")]
        public string? RollNumber { get; set; }

        [Required]
        [Display(Name = "Type of Ragging")]
        public RaggingType RaggingType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Incident")]
        public DateTime IncidentDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(150)]
        [Display(Name = "Location of Incident")]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(3000)]
        [Display(Name = "Incident Description")]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Accused Person(s) Details")]
        public string? AccusedDetails { get; set; }

        [StringLength(500)]
        [Display(Name = "Witness(es)")]
        public string? Witnesses { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Follow-up Email")]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        [Phone]
        [Display(Name = "Follow-up Phone")]
        public string? ContactPhone { get; set; }

        [Display(Name = "Confidential Submission")]
        public bool IsConfidential { get; set; } = true;

        [Display(Name = "Status")]
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;

        [StringLength(1500)]
        [Display(Name = "Action Taken / Committee Remarks")]
        public string? ActionTaken { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? ReviewedBy { get; set; }
    }
}
