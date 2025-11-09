// Services/OrderPolicy.cs
using System;

namespace ShopMVC.Services
{
    public static class OrderPolicy
    {
        // Cho phép mở lại đơn trong 10 phút sau khi đánh dấu Hoàn tất
        public static readonly TimeSpan ReopenWindow = TimeSpan.FromMinutes(10);
    }
}
