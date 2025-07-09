📞 Client-Admin Video Call System with SignalR & Redis

This project is a real-time audio call system where clients can initiate a call, and available admins can accept or reject the call. It uses ASP.NET Core SignalR, Redis for state management, and a simple HTML+JavaScript client UI.

⚙️ Features
Real-time WebRTC audio call between client and admin.

Call routing to available admins (multiple admins notified).
Admins can accept or decline calls.
Redis-backed user tracking (admins and clients).
Web UI for clients (admin UI assumed separately).
Call timeout and busy tone features.

🚀 How to Run
1. 🧱 Start Redis with WSL
Redis is required for storing and retrieving active users/admins efficiently.

🔧 One-time Setup
If Redis is not already installed on your WSL (Ubuntu):
sudo apt update
sudo apt install redis-server
▶️ Start Redis
sudo service redis-server start
You can verify Redis is running:
redis-cli ping
Should return: PONG
To enable Redis on startup:
sudo systemctl enable redis-server
2. 🖥️ Run the SignalR Server
Prerequisites
.NET 8 SDK

Redis running (localhost:6379)

🔧 Build & Run
From the Server/ directory:
dotnet restore
dotnet build
dotnet run

By default, the SignalR Hub will be accessible at:
https://localhost:7035/callhub

3. 🌐 Open the Client UI
Open client.html in any modern browser (Chrome, Edge, Firefox).
Click on the Call button.
The call request will be sent to any connected admins.

🧠 Technologies Used
ASP.NET Core SignalR
Redis (via StackExchange.Redis)
WebRTC (Audio)
Microsoft.Extensions.Caching.Distributed
HTML/CSS/JavaScript
Font Awesome (for icons)

🧪 Test Scenarios
✅ Single client connects and calls.
✅ Multiple admins get notified.
✅ One admin answers, others stop ringing.
✅ Admin declines → continues to next available admin.
✅ No admin online → client sees error.
✅ Call ends → both sides are notified.



