namespace zd8.DTOs;

public class AssignBedRequest
{
    public int BedTypeId { get; set; }
    public int WardId { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}