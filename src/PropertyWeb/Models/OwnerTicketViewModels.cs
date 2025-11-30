using System.ComponentModel.DataAnnotations;

namespace PropertyWeb.Models;

public class OwnerCreateTicketViewModel
{
    [Required]
    public Guid PropertyId { get; set; }

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength]
    public string? Description { get; set; }

    public Guid DraftTicketId { get; set; } = Guid.NewGuid();

    public string? UploadedImageKey { get; set; }

    public string? UploadedImageBucket { get; set; }

    public IEnumerable<Property_record> PropertyOptions { get; set; } = Enumerable.Empty<Property_record>();
}


