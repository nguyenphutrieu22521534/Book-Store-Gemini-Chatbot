// ===== BookMart Chatbot Widget =====

(function() {
    'use strict';

    // Configuration
    const GROQ_API_URL = '/Home/ChatbotWidget';
    const STORAGE_KEY = 'bookmart_chat_history';
    const MAX_HISTORY = 50;

    // DOM Elements
    let toggleBtn, chatWindow, messagesContainer, inputField, sendBtn, suggestionsContainer;

    // Chat history
    let chatHistory = [];

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', init);

    function init() {
        toggleBtn = document.getElementById('chatbot-toggle');
        chatWindow = document.getElementById('chatbot-window');
        messagesContainer = document.getElementById('chatbot-messages');
        inputField = document.getElementById('chatbot-input');
        sendBtn = document.getElementById('chatbot-send');
        suggestionsContainer = document.getElementById('chatbot-suggestions');

        if (!toggleBtn || !chatWindow) return;

        // Load history from localStorage
        loadHistory();

        // Event listeners
        toggleBtn.addEventListener('click', toggleChat);
        sendBtn.addEventListener('click', sendMessage);
        inputField.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') sendMessage();
        });

        // Suggestion buttons
        document.querySelectorAll('.suggestion-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                inputField.value = btn.textContent;
                sendMessage();
            });
        });
    }

    function toggleChat() {
        const isActive = chatWindow.classList.toggle('active');
        toggleBtn.classList.toggle('active', isActive);
        
        if (isActive) {
            inputField.focus();
            scrollToBottom();
        }
    }

    function loadHistory() {
        try {
            const saved = localStorage.getItem(STORAGE_KEY);
            if (saved) {
                chatHistory = JSON.parse(saved);
                chatHistory.forEach(msg => appendMessage(msg.role, msg.content, false));
            }
        } catch (e) {
            console.error('Error loading chat history:', e);
        }
    }

    function saveHistory() {
        try {
            // Keep only recent messages
            if (chatHistory.length > MAX_HISTORY) {
                chatHistory = chatHistory.slice(-MAX_HISTORY);
            }
            localStorage.setItem(STORAGE_KEY, JSON.stringify(chatHistory));
        } catch (e) {
            console.error('Error saving chat history:', e);
        }
    }

    function appendMessage(role, content, save = true) {
        const msgDiv = document.createElement('div');
        msgDiv.className = `chat-message ${role}`;
        msgDiv.innerHTML = formatMessage(content);
        messagesContainer.appendChild(msgDiv);
        scrollToBottom();

        if (save) {
            chatHistory.push({ role, content });
            saveHistory();
        }

        // Hide suggestions after first message
        if (suggestionsContainer && chatHistory.length > 0) {
            suggestionsContainer.style.display = 'none';
        }
    }

    function formatMessage(text) {
        // Convert markdown-like formatting
        return text
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\n/g, '<br>')
            .replace(/- /g, '• ');
    }

    function showTyping() {
        const typingDiv = document.createElement('div');
        typingDiv.className = 'typing-indicator';
        typingDiv.id = 'typing-indicator';
        typingDiv.innerHTML = '<span></span><span></span><span></span>';
        messagesContainer.appendChild(typingDiv);
        scrollToBottom();
    }

    function hideTyping() {
        const typing = document.getElementById('typing-indicator');
        if (typing) typing.remove();
    }

    function scrollToBottom() {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    async function sendMessage() {
        const message = inputField.value.trim();
        if (!message) return;

        // Clear input
        inputField.value = '';

        // Add user message
        appendMessage('user', message);

        // Show typing indicator
        showTyping();

        try {
            const response = await fetch(GROQ_API_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ message: message })
            });

            hideTyping();

            if (!response.ok) {
                throw new Error('Network response was not ok');
            }

            const data = await response.json();
            appendMessage('bot', data.response || 'Xin lỗi, tôi không thể xử lý yêu cầu này.');

        } catch (error) {
            hideTyping();
            console.error('Error:', error);
            appendMessage('bot', 'Xin lỗi, đã xảy ra lỗi. Vui lòng thử lại sau.');
        }
    }

    // Expose clear history function
    window.clearChatbotHistory = function() {
        chatHistory = [];
        localStorage.removeItem(STORAGE_KEY);
        messagesContainer.innerHTML = '';
        if (suggestionsContainer) {
            suggestionsContainer.style.display = 'flex';
        }
    };
})();
