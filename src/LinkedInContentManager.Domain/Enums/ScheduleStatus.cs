namespace LinkedInContentManager.Domain.Enums;

/// <summary>
/// <see cref="Recommended"/> is the Scheduling Agent's non-binding brief (day/time/timezone
/// it suggests). <see cref="Published"/> may only be reached through
/// <c>PublishingSchedule.MarkPublished</c>, which requires a real external LinkedIn post ID —
/// there is no path from <see cref="Scheduled"/> to <see cref="Published"/> that doesn't go
/// through an actual confirmed API call.
/// </summary>
public enum ScheduleStatus
{
    Recommended = 0,
    Scheduled = 1,
    Published = 2,
    Failed = 3,
    Cancelled = 4
}
