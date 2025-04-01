using chat_backend.Models;
using Microsoft.AspNetCore.SignalR;

namespace chat_backend.Hubs;

public class ChatHub : Hub
{
    private readonly IDictionary<string, UserConnection> _connections;

    public ChatHub(IDictionary<string, UserConnection> connections)
    {
        _connections = connections;
    }

    public async Task SendMessage(string message)
    {
        if (_connections.TryGetValue(Context.ConnectionId, out var userConnection))
        {
            await Clients.Group(userConnection.RoomName)
                .SendAsync("ReceiveMessage", userConnection.Username, message);
        }
    }
    
    public async Task JoinRoom(UserConnection userConnection)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.RoomName);
        
        _connections[Context.ConnectionId] = userConnection;
        
        await Clients.Group(userConnection.RoomName).SendAsync("ReceiveMessage", "Чат-бот", 
            $"{userConnection.Username} присоединился к чату");
        
        await SendUsers(userConnection.RoomName);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connections.Remove(Context.ConnectionId, out var userConnection))
        {
            Clients.Group(userConnection.RoomName)
                .SendAsync("ReceiveMessage", "Чат-бот", $"{userConnection.Username} покинул чат");

            SendUsers(userConnection.RoomName);
        }
        
        return base.OnDisconnectedAsync(exception);
    }

    private Task SendUsers(string roomName)
    {
        var users = _connections.Values
            .Where(c => c.RoomName == roomName)
            .Select(c => c.Username);
        
        return Clients.Group(roomName).SendAsync("ReceiveUsers", users);
    }
}