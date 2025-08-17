using Microsoft.AspNetCore.SignalR;
using EbayChatBot.API.Models;
using EbayChatBot.API.Data;

namespace EbayChatBot.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly EbayChatDbContext _dbContext;

        private static Dictionary<int, string> ConnectedUsers = new();

        public ChatHub(EbayChatDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext().Request.Query["userId"];

            if (int.TryParse(userId, out var intUserId))
                ConnectedUsers[intUserId] = Context.ConnectionId;

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = ConnectedUsers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            ConnectedUsers.Remove(userId);
            return base.OnDisconnectedAsync(exception);
        }


        public async Task SendMessage(string senderId, string receiverId, SenderType senderType, string message)
        {
            var chat = new ChatMessage
            {
                SenderEntityId = int.Parse(senderId),
                ReceiverEntityId = int.Parse(receiverId),
                SenderType = senderType,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.ChatMessages.Add(chat);
            await _dbContext.SaveChangesAsync();

            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, senderType, message);

            if (ConnectedUsers.TryGetValue(int.Parse(receiverId), out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, message);
            }

        }
    }
}
