document.addEventListener("DOMContentLoaded", function () {
    const chatBubble = document.getElementById("chatBubble");
    const chatWindow = document.getElementById("chatWindow");
    const chatBody = document.getElementById("chatBody");
    const chatInput = document.getElementById("chatInput");
    const chatSendBtn = document.getElementById("chatSendBtn");

    // FIX 1: Để mặc định là 0. Số 0 báo hiệu cho Server biết là "Tạo session mới đi"
    let currentSessionId = 0;

    // Giữ nguyên logic lấy ProductId
    let url = "/chatHub";
    if (window.currentProductId) {
        url += "?productId=" + window.currentProductId;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(url)
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // 2. Xử lý khi nhận được tin nhắn từ Admin
    connection.on("ReceiveMessage", (sessionId, sender, message) => {
        if (sender === "Admin") {
            appendMessage(message, "msg-admin");
        }
    });

    // 3. Xử lý khi Hub trả về SessionId THẬT sau khi đã lưu xuống DB
    connection.on("ReceiveSessionId", (sessionId) => {
        currentSessionId = sessionId; // Cập nhật ID thật để các tin sau gửi đúng chỗ
        console.log("Đã cập nhật Session ID mới: " + currentSessionId);
    });

    // 4. Hàm để hiển thị tin nhắn
    function appendMessage(message, cssClass) {
        const msgDiv = document.createElement("div");
        msgDiv.classList.add("msg", cssClass);
        msgDiv.textContent = message;
        chatBody.appendChild(msgDiv);
        chatBody.scrollTop = chatBody.scrollHeight;
    }

    // 5. SỬA HÀM GỬI TIN NHẮN
    async function sendMessage() {
        const message = chatInput.value.trim();

        // FIX 2: Bỏ điều kiện "currentSessionId > 0". 
        // Luôn cho phép gửi tin, kể cả khi chưa có ID (lần đầu sẽ gửi 0)
        if (message.length > 0) {
            try {
                // Gửi 0 lên server lần đầu, Server sẽ tự tạo session và trả ID về ở bước 3
                await connection.invoke("SendMessageFromUser", currentSessionId, message);

                appendMessage(message, "msg-user");
                chatInput.value = "";
            } catch (e) {
                console.error(e.toString());
            }
        }
    }

    // 6. Gán sự kiện (Giữ nguyên logic hiển thị box sản phẩm)
    chatBubble.addEventListener("click", () => {
        const isOpening = chatWindow.style.display !== "flex";
        chatWindow.style.display = isOpening ? "flex" : "none";

        if (isOpening && window.currentProductId) {
            if (!document.getElementById("product-context-user")) {
                const productHtml = `
                    <div class="product-context-user" id="product-context-user">
                        <img src="${window.currentProductImage}" alt="" />
                        <div>
                            <small>Bạn đang hỏi về sản phẩm:</small>
                            <p>${window.currentProductName}</p>
                        </div>
                    </div>`;
                chatBody.insertAdjacentHTML('afterbegin', productHtml);
            }
        }
    });

    chatSendBtn.addEventListener("click", sendMessage);
    chatInput.addEventListener("keypress", function (e) {
        if (e.key === "Enter") {
            sendMessage();
        }
    });

    // 7. Bắt đầu kết nối (Giữ nguyên)
    async function start() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.log(err);
            setTimeout(start, 5000);
        }
    };
    connection.onclose(async () => {
        await start();
    });

    start();
});