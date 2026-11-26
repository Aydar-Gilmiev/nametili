export function init(root) {
    root.ondragstart = event => {
        const item = event.target.closest('[data-schedule-id]');
        if (!item || item.draggable !== true) return;
        event.dataTransfer.setData('text/plain', item.dataset.scheduleId);
        event.dataTransfer.effectAllowed = 'move';
    };
    root.ondragover = event => {
        if (!event.target.closest('.day-target:not(:disabled)')) return;
        event.preventDefault();
        event.dataTransfer.dropEffect = 'move';
    };
}
export function dispose(root) { if (root) { root.ondragstart = null; root.ondragover = null; } }
