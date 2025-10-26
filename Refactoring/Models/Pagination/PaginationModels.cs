public class PaginationResponse<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public int Pages { get; set; }
}

public class PaginationRequest
{
    public int Page { get; set; } = 0;
    public int Size { get; set; } = 10;
}