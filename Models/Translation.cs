namespace EbayChatBot.API.Models;

public class Translation
{
    public int TranslationId { get; set; }
    public int InquiryId { get; set; }

    public string OriginalContent { get; set; }
    public string TranslatedContent { get; set; }
    public string SourceLanguage { get; set; }
    public string TargetLanguage { get; set; }
    public DateTime CreatedAt { get; set; }

    public Inquiry Inquiry { get; set; }
}
