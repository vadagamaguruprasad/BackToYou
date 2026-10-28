using System.ComponentModel.DataAnnotations;
using BackToYou.Models.Entities;

namespace BackToYou.Models.ViewModels
{
    public class CreateComplaintViewModel
    {
        [Display(Name = "Your Name (Optional / Leave blank to remain Anonymous)")]
        [StringLength(100)]
        public string? StudentName { get; set; }

        [Display(Name = "Student Roll Number / College ID (Used as verification key to check status later)")]
        [StringLength(50)]
        public string? RollNumber { get; set; }

        [Required(ErrorMessage = "Please select the type of incident.")]
        [Display(Name = "Type of Ragging")]
        public RaggingType RaggingType { get; set; }

        [Required(ErrorMessage = "Please enter the date when the incident occurred.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Incident")]
        public DateTime IncidentDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please enter the campus location where the incident took place.")]
        [StringLength(150)]
        [Display(Name = "Location of Incident (e.g. Boys Hostel Block B 3rd Floor, Canteen Corridor)")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide details about what happened.")]
        [StringLength(3000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 3000 characters.")]
        [Display(Name = "Description of Incident")]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Accused Person(s) Details (Names, departments, or physical identifiers if known)")]
        public string? AccusedDetails { get; set; }

        [StringLength(500)]
        [Display(Name = "Witnesses (Students, staff, or faculty who were present)")]
        public string? Witnesses { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Confidential Contact Email (Optional for committee updates)")]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Confidential Contact Phone (Optional for follow-up)")]
        public string? ContactPhone { get; set; }

        [Display(Name = "Keep My Identity Strictly Confidential")]
        public bool IsConfidential { get; set; } = true;

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must acknowledge that this form is not an emergency response service.")]
        [Display(Name = "I understand this form is for official institutional investigation and is not a 24/7 immediate physical emergency response dispatch.")]
        public bool EmergencyAcknowledged { get; set; }
    }

    public class ComplaintConfirmationViewModel
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public RaggingType RaggingType { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime IncidentDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public ComplaintStatus Status { get; set; }
    }

    public class CheckComplaintStatusViewModel
    {
        [Required(ErrorMessage = "Please enter your Complaint Reference Number.")]
        [StringLength(30)]
        [Display(Name = "Complaint Reference Number (e.g., ARC-2026-12345)")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Display(Name = "Roll Number or Follow-up Email (Optional verification key)")]
        [StringLength(100)]
        public string? VerificationKey { get; set; }
    }

    public class ComplaintStatusResultViewModel
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public ComplaintStatus Status { get; set; }
        public RaggingType RaggingType { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime IncidentDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ActionTaken { get; set; }
        public bool IsVerified { get; set; }
    }

    public class AdminComplaintListViewModel
    {
        public List<AntiRaggingComplaint> Complaints { get; set; } = new();
        public int TotalCount { get; set; }
        public int SubmittedCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
        public ComplaintStatus? SelectedStatus { get; set; }
        public RaggingType? SelectedType { get; set; }
        public string? SearchQuery { get; set; }
    }
}
