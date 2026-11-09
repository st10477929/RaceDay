using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

// ---------- Auth / Users ----------
public class RegisterRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required] public string Role { get; set; } = "";
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
}

public class LoginRequest
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public static UserDto From(User u) => new()
    {
        UserId = u.UserId, FullName = u.FullName, Email = u.Email,
        PhoneNumber = u.PhoneNumber, Role = u.Role.RoleName, CreatedAt = u.CreatedAt
    };
}

// ---------- Events ----------
public class EventRequest
{
    [Required, StringLength(150)] public string EventName { get; set; } = "";
    public string? Description { get; set; }
    [Required] public DateTime EventDate { get; set; }
    [Required, StringLength(150)] public string Location { get; set; } = "";
    /// <summary>Run, Walk or Cycle.</summary>
    [Required] public string EventType { get; set; } = "";
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

    public static EventDto From(Event e) => new()
    {
        EventId = e.EventId, OrganiserId = e.OrganiserId, OrganiserName = e.Organiser.FullName,
        EventName = e.EventName, Description = e.Description, EventDate = e.EventDate,
        Location = e.Location, EventType = e.EventType
    };
}

// ---------- Categories ----------
public class CategoryRequest
{
    [Required, StringLength(50)] public string CategoryName { get; set; } = "";
    [Range(0.01, 999.99)] public decimal Distance { get; set; }
    [Range(1, 100000)] public int MaxParticipants { get; set; } = 100;
    [Range(0, 99999999)] public decimal EntryFee { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Distance { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }

    public static CategoryDto From(Category c) => new()
    {
        CategoryId = c.CategoryId, EventId = c.EventId, CategoryName = c.CategoryName,
        Distance = c.Distance, MaxParticipants = c.MaxParticipants, EntryFee = c.EntryFee
    };
}

// ---------- Enrolments ----------
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

    public static EnrolmentDto From(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId, ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant.FullName, CategoryId = e.CategoryId,
        CategoryName = e.Category.CategoryName, EventId = e.Category.EventId,
        EventName = e.Category.Event.EventName, EnrolmentDate = e.EnrolmentDate, Status = e.Status
    };
}

// ---------- Results ----------
public class ResultRequest
{
    /// <summary>Finish time as hh:mm:ss, e.g. 00:48:35. Optional (e.g. DNF).</summary>
    public TimeSpan? FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int Position { get; set; }
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

    public static ResultDto From(Result r) => new()
    {
        ResultId = r.ResultId, EnrolmentId = r.EnrolmentId,
        ParticipantName = r.Enrolment.Participant.FullName,
        EventId = r.Enrolment.Category.EventId, EventName = r.Enrolment.Category.Event.EventName,
        CategoryName = r.Enrolment.Category.CategoryName,
        FinishTime = r.FinishTime, Position = r.Position, CapturedAt = r.CapturedAt
    };
}
