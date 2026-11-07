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
            case 'approval':
                showApproval(data);
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

// A human approves or rejects a tool call before it runs.
function showApproval({ requestId, tool, arguments: args }) {
    const card = addMessage('approval', `
        <div class="fw-semibold">Approval needed: <code>${tool}</code></div>
        <pre class="mb-2">${JSON.stringify(args, null, 2)}</pre>
        <button class="btn btn-sm btn-success me-2" data-ok="true">Approve</button>
        <button class="btn btn-sm btn-outline-success me-2" data-ok="true" data-always="true">Approve always</button>
        <button class="btn btn-sm btn-outline-danger" data-ok="false">Reject</button>`);
    card.querySelectorAll('button').forEach(button => button.addEventListener('click', async () => {
        card.querySelectorAll('button').forEach(b => b.disabled = true);
        const approved = button.dataset.ok === 'true';
        const always = button.dataset.always === 'true';
        card.insertAdjacentHTML('beforeend', `<div class="mt-1">${approved ? (always ? '✅ Approved for this session' : '✅ Approved') : '❌ Rejected'}</div>`);
        await stream('/Chat/Approve', { conversationId, requestId, approved, always });
    }));
}

form.addEventListener('submit', async e => {
    e.preventDefault();
    const text = input.value.trim();
    if (!text) return;
    input.value = '';
    addMessage('user', renderMarkdown(text));
    await stream('/Chat/Stream', { conversationId, agent: agentSelect.value, message: text });
});
