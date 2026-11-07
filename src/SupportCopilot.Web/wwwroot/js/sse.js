// Read Server-Sent Events from a POST request (EventSource only supports GET).
async function postSse(url, body, onEvent) {
    const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
    });
    if (!response.ok) { onEvent('error', { message: `HTTP ${response.status}` }); return; }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    while (true) {
        const { value, done } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });

        let end;
        while ((end = buffer.indexOf('\n\n')) >= 0) {
            const block = buffer.slice(0, end);
            buffer = buffer.slice(end + 2);
            const name = /^event: (.*)$/m.exec(block)?.[1];
            const data = /^data: (.*)$/m.exec(block)?.[1];
            if (name && data) onEvent(name, JSON.parse(data));
        }
    }
}

// Model output can be steered by untrusted content (case files, MCP, web search), so the HTML is sanitized before innerHTML.
function renderMarkdown(text) {
    if (!window.marked || !window.DOMPurify) return text.replace(/&/g, '&amp;').replace(/</g, '&lt;');
    return DOMPurify.sanitize(marked.parse(text));
}
