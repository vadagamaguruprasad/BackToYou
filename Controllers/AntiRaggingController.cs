using System.Security.Cryptography;
using BackToYou.Data;
using BackToYou.Models.Entities;
using BackToYou.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackToYou.Controllers
{
    public class AntiRaggingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AntiRaggingController> _logger;

        public AntiRaggingController(ApplicationDbContext context, ILogger<AntiRaggingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /AntiRagging
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /AntiRagging/FileComplaint
        [HttpGet]
        public IActionResult FileComplaint()
        {
            return View(new CreateComplaintViewModel());
        }

        // POST: /AntiRagging/FileComplaint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FileComplaint(CreateComplaintViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Generate unique reference number: ARC-YYYY-XXXXX
            string refNo;
            do
            {
                refNo = $"ARC-{DateTime.UtcNow.Year}-{GenerateRandomCode(5)}";
            }
            while (await _context.AntiRaggingComplaints.AnyAsync(c => c.ReferenceNumber == refNo));

            var complaint = new AntiRaggingComplaint
            {
                ReferenceNumber = refNo,
                StudentName = string.IsNullOrWhiteSpace(model.StudentName) ? "Anonymous" : model.StudentName.Trim(),
                RollNumber = string.IsNullOrWhiteSpace(model.RollNumber) ? null : model.RollNumber.Trim().ToUpperInvariant(),
                RaggingType = model.RaggingType,
                IncidentDate = model.IncidentDate,
                Location = model.Location.Trim(),
                Description = model.Description.Trim(),
                AccusedDetails = string.IsNullOrWhiteSpace(model.AccusedDetails) ? null : model.AccusedDetails.Trim(),
                Witnesses = string.IsNullOrWhiteSpace(model.Witnesses) ? null : model.Witnesses.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim().ToLowerInvariant(),
                ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone) ? null : model.ContactPhone.Trim(),
                IsConfidential = model.IsConfidential,
                Status = ComplaintStatus.Submitted,
                CreatedAt = DateTime.UtcNow
            };

            _context.AntiRaggingComplaints.Add(complaint);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New Anti-Ragging complaint submitted with reference {ReferenceNumber}", refNo);

            return RedirectToAction(nameof(Confirmation), new { refNo });
        }

        // GET: /AntiRagging/Confirmation
        [HttpGet]
        public async Task<IActionResult> Confirmation(string refNo)
        {
            if (string.IsNullOrWhiteSpace(refNo))
            {
                return RedirectToAction(nameof(Index));
            }

            var complaint = await _context.AntiRaggingComplaints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ReferenceNumber == refNo);

            if (complaint == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var vm = new ComplaintConfirmationViewModel
            {
                ReferenceNumber = complaint.ReferenceNumber,
                RollNumber = complaint.RollNumber,
                RaggingType = complaint.RaggingType,
                Location = complaint.Location,
                IncidentDate = complaint.IncidentDate,
                CreatedAt = complaint.CreatedAt,
                Status = complaint.Status
            };

            return View(vm);
        }

        // GET: /AntiRagging/Track
        [HttpGet]
        public IActionResult Track(string? refNo = null)
        {
            var model = new CheckComplaintStatusViewModel
            {
                ReferenceNumber = refNo ?? string.Empty
            };
            return View(model);
        }

        // POST: /AntiRagging/Track
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Track(CheckComplaintStatusViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cleanRef = model.ReferenceNumber.Trim().ToUpperInvariant();
            var complaint = await _context.AntiRaggingComplaints
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ReferenceNumber == cleanRef);

            if (complaint == null)
            {
                ModelState.AddModelError(string.Empty, "No complaint found with this Reference Number. Please check and try again.");
                return View(model);
            }

            // Verify with verification key if complainant entered one (Roll Number or Email)
            bool isVerified = true;
            if (!string.IsNullOrWhiteSpace(model.VerificationKey))
            {
                var key = model.VerificationKey.Trim().ToLowerInvariant();
                bool matchesRoll = !string.IsNullOrEmpty(complaint.RollNumber) && complaint.RollNumber.ToLowerInvariant() == key;
                bool matchesEmail = !string.IsNullOrEmpty(complaint.ContactEmail) && complaint.ContactEmail.ToLowerInvariant() == key;

                if (!matchesRoll && !matchesEmail && (!string.IsNullOrEmpty(complaint.RollNumber) || !string.IsNullOrEmpty(complaint.ContactEmail)))
                {
                    isVerified = false;
                }
            }

            var result = new ComplaintStatusResultViewModel
            {
                ReferenceNumber = complaint.ReferenceNumber,
                Status = complaint.Status,
                RaggingType = complaint.RaggingType,
                Location = complaint.Location,
                IncidentDate = complaint.IncidentDate,
                CreatedAt = complaint.CreatedAt,
                UpdatedAt = complaint.UpdatedAt,
                ActionTaken = complaint.ActionTaken,
                IsVerified = isVerified
            };

            ViewBag.StatusResult = result;
            return View(model);
        }

        private static string GenerateRandomCode(int length)
        {
            const string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // exclude 0, 1, I, O to prevent ambiguity
            var result = new char[length];
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }
            return new string(result);
        }
    }
}
