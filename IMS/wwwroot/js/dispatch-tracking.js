document.addEventListener('DOMContentLoaded', () => {
    const bindListFilters = (searchId, filterId, itemSelector, filterKey) => {
        const searchInput = document.getElementById(searchId);
        const filterInput = document.getElementById(filterId);
        const items = Array.from(document.querySelectorAll(itemSelector));

        const applyFilters = () => {
            const query = (searchInput?.value || '').trim().toLowerCase();
            const filterValue = filterInput?.value || '';

            items.forEach((item) => {
                const matchesQuery = !query || (item.dataset.search || '').toLowerCase().includes(query);
                const matchesFilter = !filterValue || item.dataset[filterKey] === filterValue;
                item.hidden = !(matchesQuery && matchesFilter);
            });
        };

        searchInput?.addEventListener('input', applyFilters);
        filterInput?.addEventListener('change', applyFilters);
    };

    bindListFilters('dispatchSearch', 'priorityFilter', '.queue-item', 'priority');
    bindListFilters('activitySearch', 'activityStatusFilter', '.activity-item', 'status');

    document.querySelectorAll('form[data-confirm-message]').forEach((form) => {
        form.addEventListener('submit', (event) => {
            const message = form.dataset.confirmMessage || 'Confirm this action?';
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });
});
