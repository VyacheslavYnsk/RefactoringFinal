public class HallDto : Entity
{
    public required string Name { get; set; }
    public int Number { get; set; }

    public required int Rows { get; set; }

    public DateTime CreatedAt { get; set; }    

    public DateTime UpdatedAt { get; set; }    

}