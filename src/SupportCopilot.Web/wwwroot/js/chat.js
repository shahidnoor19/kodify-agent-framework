// Chat page: streams the agent's answer word by word.
const messages = document.getElementById('messages');
const form = document.getElementById('chat-form');
const input = document.getElementById('message');
const agentSelect = document.getElementById('agent');
let conversationId = window.conversationId;

document.querySelectorAll('[data-markdown]').forEach(el => el.innerHTML = renderMarkdown(el.dataset.markdown));
messages.scrollTop = messages.scrollHeight;

function addMessage(role, html) {
    const div = document.createElement('div');
    div.className = `msg ${role}`;
    div.innerHTML = html;
    messages.appendChild(div);
    messages.scrollTop = messages.scrollHeight;
    return div;
}

// One streaming answer. Text deltas go into a single assistant bubble.
async function stream(url, body) {
    let text = '';
    let bubble = null;
    await postSse(url, body, (event, data) => {
        switch (event) {
            case 'conversation':
                conversationId = data.id;
                break;
            case 'tool':
                addMessage('tool', `🔧 calling <code>${data.name}</code>`);
                break;
            case 'delta':
                bubble ??= addMessage('assistant', '');
                text += data.text;
                bubble.innerHTML = renderMarkdown(text);
                messages.scrollTop = messages.scrollHeight;
                break;
            case 'error':
                addMessage('error', `⚠️ ${data.message}`);
                break;
            case 'done':
                agentSelect.disabled = true;
                history.replaceState(null, '', `/Chat?id=${data.id}`);
                break;
        }
    });
}

form.addEventListener('submit', async e => {
    e.preventDefault();
    const text = input.value.trim();
    if (!text) return;
    input.value = '';
    addMessage('user', renderMarkdown(text));
    await stream('/Chat/Stream', { conversationId, agent: agentSelect.value, message: text });
});
