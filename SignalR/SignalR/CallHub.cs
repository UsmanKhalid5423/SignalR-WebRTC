using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR
{
    public class CallHub : Hub
    {
        private readonly IUserCacheService _userCache;

        // Track the call status (whether it has been answered or not)
        private static Dictionary<string, bool> ongoingCalls = new Dictionary<string, bool>();

        public CallHub(IUserCacheService userCache)
        {
            _userCache = userCache;
        }

        public override async Task OnConnectedAsync()
        {
            var role = Context.GetHttpContext()?.Request.Query["role"].ToString();
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            if (string.IsNullOrWhiteSpace(role))
            {
                await Clients.Caller.SendAsync("Error", "Role is required.");
                Context.Abort();
                return;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = Context.ConnectionId;
            }

            if (role == "admin")
            {
                await _userCache.AddAdminConnectionAsync(Context.ConnectionId, userId);

                // Optionally add this connection to a SignalR group "admins"
                await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

                // Send active users list to this admin
                var activeUsers = await _userCache.GetActiveUsersAsync();
                await Clients.Caller.SendAsync("ActiveUsersUpdated", activeUsers);
            }
            else if (role == "client")
            {
                await _userCache.AddActiveUserAsync(Context.ConnectionId, userId);
                await UpdateActiveUsers();
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Invalid role.");
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
        }

        private async Task UpdateActiveUsers()
        {
            var activeUsers = await _userCache.GetActiveUsersAsync();
            await Clients.All.SendAsync("UpdateActiveUsers", activeUsers);
        }

        // Client calls this to initiate a call — notify all admins
        //public async Task CallUser(string targetConnectionId, object offer)
        //{
        //    var callerRole = await _userCache.GetUserRoleAsync(Context.ConnectionId);
        //    if (callerRole != "client")
        //    {
        //        await Clients.Caller.SendAsync("Error", "Only clients can initiate calls.");
        //        return;
        //    }

        //    var admins = await _userCache.GetActiveAdminsAsync();
        //    if (admins.Count == 0)
        //    {
        //        await Clients.Caller.SendAsync("Error", "No admin is currently online.");
        //        return;
        //    }

        //    // Send offer to all admins in the "admins" group
        //    await Clients.Group("admins").SendAsync("ReceiveOffer", Context.ConnectionId, offer);

        //    // Set the call status to "ringing" for this particular call
        //    ongoingCalls[Context.ConnectionId] = true;

        //    // Notify the client that their call is ringing
        //    await Clients.Caller.SendAsync("Ringing");
        //}


        public async Task CallUser(string targetConnectionId, object offer)
        {
            var callerRole = await _userCache.GetUserRoleAsync(Context.ConnectionId);
            if (callerRole != "client")
            {
                await Clients.Caller.SendAsync("Error", "Only clients can initiate calls.");
                return;
            }

            var admins = await _userCache.GetActiveAdminsAsync();
            if (admins.Count == 0)
            {
                await Clients.Caller.SendAsync("Error", "No admin is currently online.");
                return;
            }

            // Send offer to all admins in the "admins" group
            await Clients.Group("admins").SendAsync("ReceiveOffer", Context.ConnectionId, offer);

            // Inform client how many admins were contacted
            await Clients.Caller.SendAsync("Ringing", admins.Count);

            ongoingCalls[Context.ConnectionId] = true;
        }



        // Admin answers the call
        //public async Task AnswerCall(string callerConnectionId, object answer)
        //{
        //    var adminRole = await _userCache.GetUserRoleAsync(Context.ConnectionId);
        //    if (adminRole != "admin")
        //    {
        //        await Clients.Caller.SendAsync("Error", "Only admins can answer calls.");
        //        return;
        //    }

        //    // Mark the call as answered
        //    if (ongoingCalls.ContainsKey(callerConnectionId))
        //    {
        //        ongoingCalls[callerConnectionId] = false; // Stop ringing for this call
        //    }

        //    // Notify the caller (client) that the call is answered
        //    await Clients.Client(callerConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, answer);
        //    // Stop ringing for the other admins (if any)
        //    //await Clients.Group("admins").SendAsync("StopRinging", callerConnectionId);
        //    await Clients.Client("admin").SendAsync("StopRinging", callerConnectionId);

        //}


        public async Task AnswerCall(string callerConnectionId, object answer)
        {
            var adminRole = await _userCache.GetUserRoleAsync(Context.ConnectionId);
            if (adminRole != "admin")
            {
                await Clients.Caller.SendAsync("Error", "Only admins can answer calls.");
                return;
            }

            // Mark the call as answered
            if (ongoingCalls.ContainsKey(callerConnectionId))
            {
                ongoingCalls[callerConnectionId] = false; // Stop ringing for this call
            }

            // Notify the caller (client) that the call is answered
            await Clients.Client(callerConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, answer);

            // Stop ringing for all admins
            await Clients.Group("admins").SendAsync("StopRinging", callerConnectionId);

            // Optionally, inform all admins that the call is now answered
            await Clients.Group("admins").SendAsync("CallAnswered", callerConnectionId);
        }



        // Admin declines the call
        //public async Task DeclineCall(string callerConnectionId)
        //{
        //    var adminRole = await _userCache.GetUserRoleAsync(Context.ConnectionId);
        //    if (adminRole != "admin")
        //    {
        //        await Clients.Caller.SendAsync("Error", "Only admins can decline calls.");
        //        return;
        //    }

        //    // Stop ringing for all admins
        //    await Clients.Group("admins").SendAsync("StopRinging", callerConnectionId);

        //    // Notify the caller (client) that the call is declined
        //    await Clients.Client(callerConnectionId).SendAsync("CallDeclined");
        //    await Clients.Client(callerConnectionId).SendAsync("StatusChanged", "Call Declined", "error");
        //}

        public async Task DeclineCall(string callerConnectionId)
        {
            var adminRole = await _userCache.GetUserRoleAsync(Context.ConnectionId);
            if (adminRole != "admin")
            {
                await Clients.Caller.SendAsync("Error", "Only admins can decline calls.");
                return;
            }

            // Notify the caller (client) that the call is declined by this admin
            await Clients.Client(callerConnectionId).SendAsync("CallDeclined", Context.ConnectionId);

            // Notify only this admin to stop the ringing sound
            await Clients.Caller.SendAsync("StopRinging", callerConnectionId);

            // Continue ringing for other admins (don't send a "StopRinging" to the other admins)
        }



        // Handle ICE candidate exchange
        public async Task SendIceCandidate(string targetConnectionId, object candidate)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveCandidate", Context.ConnectionId, candidate);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var role = await _userCache.GetUserRoleAsync(Context.ConnectionId);

            if (role == "client")
            {
                await _userCache.RemoveActiveUserAsync(Context.ConnectionId);
                await UpdateActiveUsers();
            }
            else if (role == "admin")
            {
                await _userCache.RemoveAdminConnectionAsync(Context.ConnectionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");
            }

            // Remove ongoing call tracking if the caller or admin disconnects
            if (ongoingCalls.ContainsKey(Context.ConnectionId))
            {
                ongoingCalls.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // End the call
        public async Task EndCall(string otherUserId)
        {
            var callerConnectionId = Context.ConnectionId;

            await Clients.Client(callerConnectionId).SendAsync("CallEnded", otherUserId);
            await Clients.Client(otherUserId).SendAsync("CallEnded", callerConnectionId);
        }
    }
}
