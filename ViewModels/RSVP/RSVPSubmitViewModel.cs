using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventEase.ViewModels.RSVP
{
    public class RSVPSubmitViewModel : IValidatableObject
    {
        [Required]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Your full name is required.")]
        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Your email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(50)]
        [Display(Name = "Phone Number (Optional)")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Please select your RSVP status.")]
        [Display(Name = "RSVP Status")]
        public string Status { get; set; } = "Going"; // Going, Maybe, Not Going

        public List<CustomFieldAnswerViewModel> Answers { get; set; } = new List<CustomFieldAnswerViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Answers != null)
            {
                for (int i = 0; i < Answers.Count; i++)
                {
                    var ans = Answers[i];
                    if (ans.Required && string.IsNullOrWhiteSpace(ans.Response))
                    {
                        yield return new ValidationResult(
                            $"'{ans.Label}' is a required question.",
                            new[] { $"Answers[{i}].Response" });
                    }
                }
            }
        }
    }
}
