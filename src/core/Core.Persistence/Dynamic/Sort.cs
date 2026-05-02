namespace Core.Persistence.Dynamic;

public class Sort
{
    public required string Field { get; set; }
    public required string Dir { get; set; }

    public Sort() { }

    public Sort(string field, string dir)
    {
        Field = field;
        Dir = dir;
    }
}
