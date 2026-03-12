namespace Imapster.ViewModels;

public record PromptTemplate(
    int? Id,
    string Content,
    string Language,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);