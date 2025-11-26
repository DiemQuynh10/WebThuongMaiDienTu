using Microsoft.AspNetCore.SignalR;
using ShopMVC.Data;
using ShopMVC.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ShopMVC.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _db;
        private static readonly string _adminGroup = "Admins";

        public ChatHub(AppDbContext db)
        {
            _db = db;
        }

        // 1. KHI KẾT NỐI: CHỈ GOM NHÓM, KHÔNG LƯU DB ĐỂ TRÁNH SPAM
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var isAdmin = httpContext.Request.Query["isAdmin"].ToString() == "true";

            if (isAdmin)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, _adminGroup);
            }
            else
            {
                // Nếu là User, gom vào group theo UserId để Admin có thể reply lại
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                }
                // LƯU Ý: Đã xóa đoạn code tạo Session rỗng ở đây
            }
            await base.OnConnectedAsync();
        }

        // 2. KHI USER GỬI TIN NHẮN: LÚC NÀY MỚI TẠO SESSION NẾU CẦN
        public async Task SendMessageFromUser(int sessionId, string message)
        {
            var httpContext = Context.GetHttpContext();
            var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            ChatSession session = null;

            // TRƯỜNG HỢP 1: Chưa có Session (sessionId = 0 hoặc null từ client)
            // Hoặc Session cũ đã đóng, cần tạo cái mới
            if (sessionId <= 0)
            {
                // Kiểm tra xem user này có session nào đang mở (chưa đóng) không để dùng lại
                // (Tránh trường hợp F5 trang web tạo ra session mới liên tục)
                session = await _db.ChatSessions
                    .FirstOrDefaultAsync(s => s.UserId == userId && !s.DaDong);

                if (session == null)
                {
                    // Nếu không có cái nào đang mở -> Tạo mới hoàn toàn
                    var productIdStr = httpContext.Request.Query["productId"].ToString();
                    int? sanPhamId = null;
                    if (int.TryParse(productIdStr, out int pId)) sanPhamId = pId;

                    session = new ChatSession
                    {
                        UserConnectionId = Context.ConnectionId,
                        UserId = userId,
                        SanPhamId = sanPhamId,
                        ThoiGianTao = DateTime.Now,
                        DaDong = false // Mặc định là đang mở
                    };

                    _db.ChatSessions.Add(session);
                    await _db.SaveChangesAsync();

                    // BÁO CHO ADMIN BIẾT CÓ KHÁCH MỚI (Chỉ báo 1 lần khi tạo)
                    await Clients.Group(_adminGroup).SendAsync("NewUserConnected", session.Id, Context.ConnectionId);

                    // GỬI LẠI ID CHO CLIENT (để lần sau client gửi kèm ID này)
                    await Clients.Caller.SendAsync("ReceiveSessionId", session.Id);
                }
                else
                {
                    // Nếu tìm thấy session cũ đang mở, cập nhật lại ConnectionId mới nhất
                    session.UserConnectionId = Context.ConnectionId;
                    // Gửi lại ID cũ cho client đồng bộ
                    await Clients.Caller.SendAsync("ReceiveSessionId", session.Id);
                }
            }
            else
            {
                // TRƯỜNG HỢP 2: Đã có ID session gửi lên
                session = await _db.ChatSessions.FindAsync(sessionId);
            }

            // NẾU SESSION VẪN NULL (Lỗi hy hữu) -> RETURN
            if (session == null) return;

            // 3. LƯU TIN NHẮN
            var msg = new ChatMessage
            {
                ChatSessionId = session.Id,
                NoiDung = message,
                Sender = SenderType.User,
                ThoiGian = DateTime.Now
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            // 4. GỬI TIN NHẮN CHO ADMIN
            await Clients.Group(_adminGroup).SendAsync("ReceiveMessage", session.Id, "User", message);
        }

        // 3. ADMIN GỬI TIN (GIỮ NGUYÊN NHƯ CŨ)
        public async Task SendMessageFromAdmin(int sessionId, string userConnectionId, string message)
        {
            var msg = new ChatMessage
            {
                ChatSessionId = sessionId,
                NoiDung = message,
                Sender = SenderType.Admin,
                ThoiGian = DateTime.Now
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            var session = await _db.ChatSessions.FindAsync(sessionId);

            if (session != null && !string.IsNullOrEmpty(session.UserId))
            {
                await Clients.Group(session.UserId).SendAsync("ReceiveMessage", sessionId, "Admin", message);
            }
            else
            {
                await Clients.Client(userConnectionId).SendAsync("ReceiveMessage", sessionId, "Admin", message);
            }

            await Clients.Caller.SendAsync("ReceiveMessage", sessionId, "Admin", message);
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            var isAdmin = httpContext.Request.Query["isAdmin"].ToString() == "true";

            if (isAdmin)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, _adminGroup);
            }
            else
            {
                var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}