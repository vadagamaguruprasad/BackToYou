namespace BackToYou.Models.Entities
{
    public enum ItemType
    {
        Lost = 1,
        Found = 2
    }

    public enum ItemStatus
    {
        Reported = 1,
        UnderClaim = 2,
        Claimed = 3,
        Closed = 4
    }

    public enum ClaimStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    public enum RaggingType
    {
        Physical = 1,
        Verbal = 2,
        Emotional = 3,         // Mental / Emotional Harassment
        Cyberbullying = 4,
        Threats = 5,
        Discrimination = 6,
        Other = 7
    }

    public enum ComplaintStatus
    {
        Submitted = 1,
        UnderReview = 2,
        ActionInProgress = 3,
        Resolved = 4
    }
}
