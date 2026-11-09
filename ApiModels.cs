namespace RaceDay.Web.Models.Api;

// Plain data shapes returned by the RaceDay API. The MVC app only displays these; it holds no business rules.

public class UserInfo
{
    public int UserID { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class LoginResult
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = new();
}

public class EventInfo
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
    public decimal? DistanceKm { get; set; }
    public string? BannerUrl { get; set; }
}

public class EventSummary : EventInfo
{
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
}

public class EventDetail : EventInfo
{
    public List<CategoryInfo> Categories { get; set; } = new();
}

public class CategoryInfo
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class EnrolmentInfo
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
}

public class ResultInfo
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string ParticipantName { get; set; } = "";
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "";
    public int TotalFinishers { get; set; }
}

// ----- request bodies sent to the API -----

public class RegisterBody
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "";
}

public class LoginBody
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ProfileBody
{
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
}

public class EventBody
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? EventDate { get; set; }
    public string Province { get; set; } = "";
    public string Venue { get; set; } = "";
    public string EventType { get; set; } = "";
    public decimal? DistanceKm { get; set; }
}

public class CategoryBody
{
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class EnrolBody
{
    public int CategoryId { get; set; }
}

public class ResultBody
{
    /// <summary>Only used when capturing a new result.</summary>
    public int? EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}
