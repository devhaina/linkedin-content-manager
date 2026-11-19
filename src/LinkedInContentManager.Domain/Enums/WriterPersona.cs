namespace LinkedInContentManager.Domain.Enums;

/// <summary>
/// The four writer personas the brief requires. Passed to the Writer and Optimizer
/// agents as the <c>{persona}</c> prompt input. Fixed by spec (unlike niches, which
/// are seeded configurable data), so modeled as an enum rather than a lookup table.
/// </summary>
public enum WriterPersona
{
    /// <summary>First-person, strategic. Executive / thought leadership.</summary>
    ExecutiveThoughtLeadership = 0,

    /// <summary>Hands-on, technical, example-driven.</summary>
    Practitioner = 1,

    /// <summary>Short sentences, warm. Conversational and approachable.</summary>
    ConversationalApproachable = 2,

    /// <summary>Polished, measured. Formal and authoritative.</summary>
    FormalAuthoritative = 3
}
