public enum ReservationResult
{
    Success,
    NotFound,
    AlreadyReserved,
    Sold,
    ReservationLimitExceeded,
    DatabaseError
}

public class ReservationException : Exception
{
    public ReservationException(string message, Exception innerException) 
        : base(message, innerException) { }
}