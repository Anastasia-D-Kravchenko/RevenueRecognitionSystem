namespace RevenueRecognitionSystem.Api.Entities;

public class SoftwareCategory
{
    public int SoftwareCategoryId { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<Software> Softwares { get; set; } = new List<Software>();
}
