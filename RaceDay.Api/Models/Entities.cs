using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("Role")]
public class Role
{
    [Key] public int RoleId { get; set; }
    [Required, MaxLength(20)] public string RoleName { get; set; } = "";
    public ICollection<User> Users { get; set; } = new List<User>();
}

[Table("User")]
public class User
{
    [Key] public int UserId { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [Required, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MaxLength(255)] public string PasswordHash { get; set; } = "";
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("Event")]
public class Event
{
    [Key] public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public User Organiser { get; set; } = null!;
    [Required, MaxLength(150)] public string EventName { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    [Required, MaxLength(150)] public string Location { get; set; } = "";
    [Required, MaxLength(20)] public string EventType { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

[Table("Category")]
public class Category
{
    [Key] public int CategoryId { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    [Required, MaxLength(50)] public string CategoryName { get; set; } = "";
    [Column(TypeName = "decimal(5,2)")] public decimal Distance { get; set; }
    public int MaxParticipants { get; set; } = 100;
    [Column(TypeName = "decimal(10,2)")] public decimal EntryFee { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

[Table("Enrolment")]
public class Enrolment
{
    [Key] public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public User Participant { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(20)] public string Status { get; set; } = "Confirmed";
    public Result? Result { get; set; }
}

[Table("Result")]
public class Result
{
    [Key] public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
    public TimeSpan? FinishTime { get; set; }
    public int Position { get; set; }
    public int CapturedByOrganiserId { get; set; }
    public User CapturedByOrganiser { get; set; } = null!;
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}
