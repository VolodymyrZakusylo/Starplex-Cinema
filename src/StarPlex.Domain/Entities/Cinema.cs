using StarPlex.Domain.Common;

namespace StarPlex.Domain.Entities;

public class Cinema : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public ICollection<Hall> Halls { get; set; } = new List<Hall>();
    public Cinema()
    {
    }


    public Cinema(string name, string address, string city)
    {
        Name = name;
        Address = address;
        City = city;
    }
}
