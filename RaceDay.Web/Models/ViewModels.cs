using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// ===== Shapes returned by the API (match the API's DTOs) =====
public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = "";
    public string EventName { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string EventType { get; set; } = "";
    public string? BannerUrl { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Distance { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public TimeSpan? FinishTime { get; set; }
    public int Position { get; set; }
    public DateTime CapturedAt { get; set; }
}

// ===== Account forms =====
public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email."), EmailAddress]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class RegisterViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match."), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose a role.")]
    public string Role { get; set; } = "";

    [Phone, StringLength(20), Display(Name = "Phone (optional)")]
    public string? PhoneNumber { get; set; }
}

public class ProfileViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Phone, StringLength(20), Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }

    // Read-only display values
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}

// ===== Organiser forms =====
public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required, StringLength(150), Display(Name = "Event name")]
    public string EventName { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose a date."), DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date")]
    public DateTime? EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = "";

    [Required(ErrorMessage = "Please choose an event type."), Display(Name = "Event type")]
    public string EventType { get; set; } = "";

    public string? CurrentBannerUrl { get; set; }

    [Display(Name = "Banner image (JPEG, PNG or WebP, max 5 MB)")]
    public IFormFile? BannerFile { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }

    [Required, StringLength(50), Display(Name = "Category name")]
    public string CategoryName { get; set; } = "";

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km."), Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Range(1, 100000), Display(Name = "Max participants")]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 99999999), Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }
}

public class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int ResultId { get; set; }
    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Position must be 1 or higher.")]
    public int Position { get; set; }
}

// ===== Page models =====
public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
    public List<ResultDto> Results { get; set; } = new();
    public bool IsPast => Event.EventDate.Date < DateTime.Today;
}

public class EventSummary
{
    public EventDto Event { get; set; } = new();
    public int Enrolments { get; set; }
}

public class DashboardViewModel
{
    public List<EventSummary> Events { get; set; } = new();
    public int TotalEnrolments => Events.Sum(e => e.Enrolments);
    public int UpcomingCount => Events.Count(e => e.Event.EventDate.Date >= DateTime.Today);
    public EventDto? NextEvent => Events.Select(e => e.Event).Where(e => e.EventDate.Date >= DateTime.Today).OrderBy(e => e.EventDate).FirstOrDefault();
}

public class CategoriesViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class OrganiserResultsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public Dictionary<int, ResultDto> ResultsByEnrolment { get; set; } = new();
}

public class MyResultRow
{
    public ResultDto Result { get; set; } = new();
    public DateTime? EventDate { get; set; }
    public int Finishers { get; set; }
}
