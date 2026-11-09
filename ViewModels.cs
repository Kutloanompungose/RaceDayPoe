using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

// ---------- account ----------
public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That doesn't look like a valid email address."), StringLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please choose a password."), StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password."), DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please tell us how you will use RaceDay.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Organiser or Participant.")]
    [Display(Name = "I am a")]
    public string Role { get; set; } = "Participant";
}

// ---------- organiser ----------
public class DashboardViewModel
{
    public List<EventSummary> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int UpcomingCount => Events.Count(e => e.EventDate >= DateTime.UtcNow);
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int MaxEnrolments => Events.Count == 0 ? 0 : Events.Max(e => e.EnrolmentCount);
    public List<EventSummary> Upcoming => Events.Where(e => e.EventDate >= DateTime.UtcNow).OrderBy(e => e.EventDate).ToList();
    public EventSummary? NextEvent => Upcoming.FirstOrDefault();
}

public class EventFormViewModel
{
    public int? EventID { get; set; }

    [Required(ErrorMessage = "Please give the event a name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = "";

    [StringLength(1000, ErrorMessage = "The description can be at most 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date and start time.")]
    [Display(Name = "Date and start time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please choose a province.")]
    public string Province { get; set; } = "";

    [Required(ErrorMessage = "Please enter the venue / location."), StringLength(150)]
    [Display(Name = "Venue / location")]
    public string Venue { get; set; } = "";

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Headline distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }

    public static readonly string[] Provinces =
    {
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "Northern Cape", "North West", "Western Cape"
    };

    public static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };

    public EventBody ToBody() => new()
    {
        Name = Name.Trim(), Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        EventDate = EventDate, Province = Province, Venue = Venue.Trim(), EventType = EventType, DistanceKm = DistanceKm
    };

    public static EventFormViewModel From(EventInfo e) => new()
    {
        EventID = e.EventID, Name = e.Name, Description = e.Description, EventDate = e.EventDate,
        Province = e.Province, Venue = e.Venue, EventType = e.EventType, DistanceKm = e.DistanceKm,
        ExistingBannerUrl = e.BannerUrl
    };
}

public class CategoryFormViewModel
{
    public int? CategoryID { get; set; }
    public int EventID { get; set; }

    [Required(ErrorMessage = "Please name the category (e.g. 10km, Under 18, 5km Fun Walk)."), StringLength(50)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = "";

    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(1, 1000000, ErrorMessage = "Enter a limit of at least 1 participant.")]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 500;

    [Range(0, 100000, ErrorMessage = "The entry fee cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    public CategoryBody ToBody() => new()
    {
        Name = Name.Trim(), DistanceKm = DistanceKm, MaxParticipants = MaxParticipants, EntryFee = EntryFee
    };
}

public class CategoriesViewModel
{
    public EventDetail Event { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EnrolmentsViewModel
{
    public EventDetail Event { get; set; } = new();
    public List<EnrolmentInfo> Enrolments { get; set; } = new();
}

public class ResultRow
{
    public EnrolmentInfo Enrolment { get; set; } = new();
    public ResultInfo? Result { get; set; }
}

public class ResultsViewModel
{
    public EventDetail Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
    public bool EventHasHappened => Event.EventDate <= DateTime.UtcNow;
}

// ---------- participant ----------
public class EventFilter
{
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public string? Province { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public bool IsActive => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(EventType)
                            || !string.IsNullOrWhiteSpace(Province) || From.HasValue || To.HasValue;
}

public class BrowseEventsViewModel
{
    public EventFilter Filter { get; set; } = new();
    public List<EventInfo> Events { get; set; } = new();
    public int TotalUpcoming { get; set; }
    public HashSet<int> EnrolledEventIds { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDetail Event { get; set; } = new();
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
    public bool HasHappened => Event.EventDate < DateTime.UtcNow;
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    [StringLength(20, ErrorMessage = "Phone number can be at most 20 characters.")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Phone numbers can only contain digits, spaces, + - and brackets.")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    public string? ProfilePictureUrl { get; set; }
}
