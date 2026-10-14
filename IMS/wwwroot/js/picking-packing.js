document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('orderSearch');
    const statusFilter = document.getElementById('statusFilter');
    const queueItems = Array.from(document.querySelectorAll('.queue-item'));
    const allocateForm = document.querySelector('#allocateModal form');

    const applyFilters = () => {
        const query = (searchInput?.value || '').trim().toLowerCase();
        const status = statusFilter?.value || '';

        queueItems.forEach((item) => {
            const matchesQuery = !query || (item.dataset.search || '').toLowerCase().includes(query);
            const matchesStatus = !status || item.dataset.status === status;
            item.hidden = !(matchesQuery && matchesStatus);
        });
    };

    searchInput?.addEventListener('input', applyFilters);
    statusFilter?.addEventListener('change', applyFilters);

    allocateForm?.addEventListener('submit', (event) => {
        const selects = Array.from(allocateForm.querySelectorAll('select[name$=".ShelfID"]'));
        const missingShelf = selects.some((select) => !select.disabled && !select.value);

        if (missingShelf) {
            event.preventDefault();
            selects.forEach((select) => {
                select.classList.toggle('is-invalid', !select.disabled && !select.value);
            });
        }
    });
});
