export function update(element, mode) {
    if (!element) return;
    if (!element.chatScroll) {
        element.chatScroll = {height: 0, atBottom: true};
        element.addEventListener('scroll', () => {
            element.chatScroll.atBottom = element.scrollHeight - element.scrollTop - element.clientHeight < 64;
        }, {passive: true});
    }
    const state = element.chatScroll;
    if (mode === 'older') element.scrollTop += element.scrollHeight - state.height;
    else if (state.atBottom || mode === 'latest') element.scrollTop = element.scrollHeight;
    state.height = element.scrollHeight;
}
