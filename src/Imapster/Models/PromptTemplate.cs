namespace Imapster.Models;

public class PromptTemplate
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}