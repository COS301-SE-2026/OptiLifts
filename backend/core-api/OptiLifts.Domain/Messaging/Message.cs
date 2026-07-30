using OptiLifts.Domain.Common;

namespace OptiLifts.Domain.Messaging;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public DateTime SendAt { get; set; } = DateTime.UtcNow;

    [Encrypted]
    public string Content { get; set; } = string.Empty;
}