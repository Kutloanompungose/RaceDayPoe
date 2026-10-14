using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

// ---------- Auth / Users ----------
public class RegisterRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be 'Organiser' or 'Participant'.")]
    public string Role { get; set; } = "";
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [StringLength(20)] public string? Phone { get; set; }
}

public class UserDto
{
    public int UserID { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }

    public static UserDto From(User u) => new()
    {
        UserID = u.UserID, FullName = u.FullName, Email = u.Email, Role = u.Role, Phone = u.Phone
    };
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

// ---------- Events ----------
public class EventRequest
{
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [StringLength(1000)] public string? Description { get; set; }
    [Required] public DateTime? EventDate { get; set; }
    [Required, StringLength(50)] public string Province { get; set; } = "";
    [Required, StringLength(150)] public string Venue { get; set; } = "";
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be 'Run', 'Walk' or 'Cycle'.")]
    public string EventType { get; set; } = "";
}

public class EventDto
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string OrganiserName { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Province { get; set; } = "";
    public string Venue { get; set; } = "";
    public string EventType { get; set; } = "";

    public static readonly Expression<Func<Event, EventDto>> Projection = e => new EventDto
    {
        EventID = e.EventID, OrganiserID = e.OrganiserID, OrganiserName = e.Organiser.FullName,
        Name = e.Name, Description = e.Description, EventDate = e.EventDate,
        Province = e.Province, Venue = e.Venue, EventType = e.EventType
    };
}

public class EventDetailDto : EventDto
{
    public List<CategoryDto> Categories { get; set; } = new();
}

// ---------- Categories ----------
public class CategoryRequest
{
    [Required, StringLength(50)] public string Name { get; set; } = "";
    [Range(0.01, 999.99)] public decimal DistanceKm { get; set; }
    [Range(1, 1000000)] public int MaxParticipants { get; set; }
    [Range(0, 100000)] public decimal EntryFee { get; set; }
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }

    public static readonly Expression<Func<Category, CategoryDto>> Projection = c => new CategoryDto
    {
        CategoryID = c.CategoryID, EventID = c.EventID, Name = c.Name,
        DistanceKm = c.DistanceKm, MaxParticipants = c.MaxParticipants, EntryFee = c.EntryFee
    };
}

// ---------- Route info ----------
public class RouteRequest
{
    [Url, StringLength(255)] public string? RouteMapUrl { get; set; }
    [Required, StringLength(150)] public string StartPoint { get; set; } = "";
    [Required, StringLength(150)] public string EndPoint { get; set; } = "";
    [Range(0, 10000)] public int? ElevationGainM { get; set; }
    [StringLength(1000)] public string? Description { get; set; }
}

public class RouteDto
{
    public int RouteID { get; set; }
    public int CategoryID { get; set; }
    public string? RouteMapUrl { get; set; }
    public string StartPoint { get; set; } = "";
    public string EndPoint { get; set; } = "";
    public int? ElevationGainM { get; set; }
    public string? Description { get; set; }

    public static RouteDto From(RouteInfo r) => new()
    {
        RouteID = r.RouteID, CategoryID = r.CategoryID, RouteMapUrl = r.RouteMapURL,
        StartPoint = r.StartPoint, EndPoint = r.EndPoint, ElevationGainM = r.ElevationGainM, Description = r.Description
    };
}

// ---------- Enrolments ----------
public class EnrolRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")] public int CategoryId { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string ParticipantName { get; set; } = "";
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string? RaceNumber { get; set; }
    public string Status { get; set; } = "";

    public static readonly Expression<Func<Enrolment, EnrolmentDto>> Projection = e => new EnrolmentDto
    {
        EnrolmentID = e.EnrolmentID, ParticipantID = e.ParticipantID, ParticipantName = e.Participant.FullName,
        CategoryID = e.CategoryID, CategoryName = e.Category.Name,
        EventID = e.Category.EventID, EventName = e.Category.Event.Name, EventDate = e.Category.Event.EventDate,
        EnrolmentDate = e.EnrolmentDate, RaceNumber = e.RaceNumber, Status = e.Status
    };
}

// ---------- Results ----------
public class ResultRequest
{
    public TimeSpan? FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int? Position { get; set; }
    [Required, RegularExpression("^(Finished|DNF|DNS)$", ErrorMessage = "Status must be 'Finished', 'DNF' or 'DNS'.")]
    public string Status { get; set; } = "Finished";
}

public class CreateResultRequest : ResultRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "EnrolmentId is required.")] public int EnrolmentId { get; set; }
}

public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string ParticipantName { get; set; } = "";
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "";

    public static readonly Expression<Func<Result, ResultDto>> Projection = r => new ResultDto
    {
        ResultID = r.ResultID, EnrolmentID = r.EnrolmentID,
        ParticipantName = r.Enrolment.Participant.FullName,
        EventID = r.Enrolment.Category.EventID, EventName = r.Enrolment.Category.Event.Name,
        CategoryName = r.Enrolment.Category.Name,
        FinishTime = r.FinishTime, Position = r.Position, Status = r.Status
    };
}
