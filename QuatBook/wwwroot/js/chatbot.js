
function toggleChat() {
    const chat = document.getElementById("chat-container");
    chat.style.display = (chat.style.display === "none" || chat.style.display === "") ? "flex" : "none";
}

async function sendMessage() {
    const messageInput = document.getElementById("message");
    const message = messageInput.value.trim();
    if (!message) {
        alert("Vui lòng nhập tin nhắn.");
        return;
    }

    appendMessage("Bạn", message, "chat-user");

    const response = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: message })
    });

    const result = await response.json();
    let botMessage = result.response
        .replace(/\*\*(.*?)\*\*/g, "<strong>$1</strong>")
        .replace(/\n{2,}/g, "<br><br>")
        .replace(/\n/g, "<br>");

    appendMessage("Bot", botMessage, "chat-bot");
    messageInput.value = "";
}

function appendMessage(sender, text, className) {
    const chatBox = document.getElementById("chat-box");
    const bubble = document.createElement("div");
    bubble.className = `chat-bubble ${className}`;
    bubble.innerHTML = `<strong>${sender}:</strong> <br>${text}`;
    chatBox.appendChild(bubble);
    chatBox.scrollTop = chatBox.scrollHeight;
}
//enter

function handleKey(event) {
    if (event.key === "Enter") {
        event.preventDefault(); // tránh xuống dòng
        sendMessage();
    }
}

