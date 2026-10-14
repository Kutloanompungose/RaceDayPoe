using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public class User
{
    [Key] public int UserID { get; set; }
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [Required, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MaxLength(255)] public string PasswordHash { get; set; } = "";
    [Required, MaxLength(20)] public string Role { get; set; } = Roles.Participant;
    [MaxLength(20)] public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Event
{
    [Key] public int EventID { get; set; }
    public int OrganiserID { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    [Required, MaxLength(50)] public string Province { get; set; } = "";
    [Required, MaxLength(150)] public string Venue { get; set; } = "";
    [Required, MaxLength(20)] public string EventType { get; set; } = "Run"; // Run | Walk | Cycle
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Organiser { get; set; } = null!;
    public List<Category> Categories { get; set; } = new();
}

public class Category
{
    [Key] public int CategoryID { get; set; }
    public int EventID { get; set; }
    [Required, MaxLength(50)] public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; } = 500;
    public decimal EntryFee { get; set; }

    public Event Event { get; set; } = null!;
    public RouteInfo? Route { get; set; }
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class RouteInfo
{
    [Key] public int RouteID { get; set; }
    public int CategoryID { get; set; }
    [MaxLength(255)] public string? RouteMapURL { get; set; }
    [Required, MaxLength(150)] public string StartPoint { get; set; } = "";
    [Required, MaxLength(150)] public string EndPoint { get; set; } = "";
    public int? ElevationGainM { get; set; } = 0;
    [MaxLength(1000)] public string? Description { get; set; }

    public Category Category { get; set; } = null!;
}

public class Enrolment
{
    [Key] public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    [MaxLength(10)] public string? RaceNumber { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending"; // Pending | Confirmed | Cancelled

    public User Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}

public class Result
{
    [Key] public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Finished"; // Finished | DNF | DNS
    public int CapturedByOrganiserID { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public Enrolment Enrolment { get; set; } = null!;
    public User CapturedByOrganiser { get; set; } = null!;
}
