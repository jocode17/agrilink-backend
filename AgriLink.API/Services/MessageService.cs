using Microsoft.EntityFrameworkCore;
using AgriLink.API.Data;
using AgriLink.API.Models.Entities;

namespace AgriLink.API.Services;

// ── DTOs ─────────────────────────────────────────────────────

public class ConversationDto
{
    public Guid Id { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

// ── Interface ────────────────────────────────────────────────

public interface IMessageService
{
    Task<List<ConversationDto>> GetFarmerConversations(Guid userId);
    Task<List<MessageDto>> GetMessages(Guid conversationId);
    Task<MessageDto> SendMessage(Guid userId, Guid conversationId, string content);
    Task<List<object>> GetAvailableFarmers();
    Task<object> StartConversation(Guid buyerId, Guid farmerId);
}

// ── Implementation ───────────────────────────────────────────

public class MessageService : IMessageService
{
    private readonly AgriLinkDbContext _db;

    public MessageService(AgriLinkDbContext db)
    {
        _db = db;
    }

    public async Task<List<ConversationDto>> GetFarmerConversations(Guid userId)
    {
        var farmer = await _db.FarmerProfiles
            .FirstOrDefaultAsync(f => f.UserId == userId);

        if (farmer == null)
            throw new Exception("Farmer profile not found");

        var conversations = await _db.Conversations
            .Include(c => c.Buyer)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .Where(c => c.FarmerId == farmer.Id)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        return conversations.Select(c => new ConversationDto
        {
            Id = c.Id,
            BuyerName = c.Buyer?.FullName ?? "Unknown",
            LastMessage = c.Messages.FirstOrDefault()?.Content ?? "No messages yet",
            LastMessageTime = c.Messages.FirstOrDefault()?.CreatedAt ?? c.CreatedAt,
            UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != userId)
        }).ToList();
    }

    public async Task<List<MessageDto>> GetMessages(Guid conversationId)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Farmer)
            .Include(c => c.Buyer)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
            throw new Exception("Conversation not found");

        var messages = await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(m => new MessageDto
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.SenderId == conversation.Farmer.UserId
                ? conversation.Farmer.OwnerName
                : conversation.Buyer.FullName,
            Content = m.Content,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task<MessageDto> SendMessage(Guid userId, Guid conversationId, string content)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
            throw new Exception("Conversation not found");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = userId,
            Content = content,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = "You",
            Content = message.Content,
            IsRead = false,
            CreatedAt = message.CreatedAt,
        };
    }

    public async Task<List<object>> GetAvailableFarmers()
    {
        var farmers = await _db.FarmerProfiles
            .Select(f => new { f.Id, f.FarmName, f.OwnerName, f.Address })
            .ToListAsync();
        return farmers.Cast<object>().ToList();
    }

    public async Task<object> StartConversation(Guid buyerId, Guid farmerId)
    {
        var existing = await _db.Conversations
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId && c.FarmerId == farmerId);
        if (existing != null)
            return new { existing.Id, existing.FarmerId, farmerName = "" };

        var conv = new Conversation
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            FarmerId = farmerId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync();
        return new { conv.Id, conv.FarmerId, farmerName = "" };
    }
}
