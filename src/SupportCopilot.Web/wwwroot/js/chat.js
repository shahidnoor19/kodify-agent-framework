// Chat page: streams the agent's answer word by word.
const messages = document.getElementById('messages');
const form = document.getElementById('chat-form');
const input = document.getElementById('message');
const agentSelect = document.getElementById('agent');

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
            case 'delta':
                bubble ??= addMessage('assistant', '');
                text += data.text;
                bubble.innerHTML = renderMarkdown(text);
                messages.scrollTop = messages.scrollHeight;
                break;
            case 'error':
                addMessage('error', `⚠️ ${data.message}`);
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
    await stream('/Chat/Stream', { agent: agentSelect.value, message: text });
});
