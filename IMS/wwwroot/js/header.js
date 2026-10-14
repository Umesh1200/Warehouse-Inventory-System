document.addEventListener('DOMContentLoaded', function () {
    const searchToggle = document.getElementById('searchToggle');
    const searchPanel = document.getElementById('searchPanel');
    const searchInput = document.getElementById('searchInput');
    const searchResults = document.getElementById('searchResults');
    
    const notificationToggle = document.getElementById('notificationToggle');
    const notificationPanel = document.getElementById('notificationPanel');
    
    const userDropdown = document.getElementById('userDropdown');

    // Scrape sidebar links for the search functionality
    const sidebarLinks = Array.from(document.querySelectorAll('.ims-nav a')).map(a => ({
        text: a.querySelector('span:last-child')?.textContent.trim() || '',
        url: a.getAttribute('href')
    }));

    function hideSearch() {
        if (searchPanel) {
            searchPanel.style.display = 'none';
            if (searchToggle) searchToggle.setAttribute('aria-expanded', 'false');
        }
    }

    function hideNotif() {
        if (notificationPanel) {
            notificationPanel.style.display = 'none';
            if (notificationToggle) notificationToggle.setAttribute('aria-expanded', 'false');
        }
    }

    // --- Search Toggle ---
    if (searchToggle) {
        searchToggle.addEventListener('click', function (e) {
            e.stopPropagation(); // Prevent document click from firing immediately
            const isOpening = searchPanel.style.display === 'none';
            
            hideNotif(); // Close notification panel
            
            // Close Bootstrap user dropdown safely if open
            if (userDropdown && window.bootstrap) {
                const bsDropdown = bootstrap.Dropdown.getInstance(userDropdown);
                if (bsDropdown) bsDropdown.hide();
            }

            if (isOpening) {
                searchPanel.style.display = 'block';
                searchToggle.setAttribute('aria-expanded', 'true');
                setTimeout(() => { if(searchInput) searchInput.focus(); }, 10);
            } else {
                hideSearch();
            }
        });
    }

    if (searchPanel) {
        // Prevent clicks inside the panel from closing it
        searchPanel.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    }

    // --- Notifications Toggle ---
    if (notificationToggle) {
        notificationToggle.addEventListener('click', function (e) {
            e.stopPropagation();
            const isOpening = notificationPanel.style.display === 'none';
            
            hideSearch(); // Close search panel
            
            // Close Bootstrap user dropdown safely if open
            if (userDropdown && window.bootstrap) {
                const bsDropdown = bootstrap.Dropdown.getInstance(userDropdown);
                if (bsDropdown) bsDropdown.hide();
            }

            if (isOpening) {
                notificationPanel.style.display = 'block';
                notificationToggle.setAttribute('aria-expanded', 'true');
            } else {
                hideNotif();
            }
        });
    }

    if (notificationPanel) {
        notificationPanel.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    }

    // --- User Dropdown Toggle ---
    if (userDropdown) {
        userDropdown.addEventListener('click', function() {
            // Let Bootstrap handle opening the dropdown, but we must hide our custom panels
            hideSearch();
            hideNotif();
        });
    }

    // --- Document Click (Click Outside) ---
    document.addEventListener('click', function (e) {
        // Since the toggles and panels use stopPropagation, this only fires when clicking OUTSIDE of them.
        hideSearch();
        hideNotif();
        // We DO NOT manually close the Bootstrap dropdown here; Bootstrap's own document click listener handles that.
    });

    // --- Search Input Logic ---
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const term = this.value.toLowerCase();
            searchResults.innerHTML = '';
            if (!term) {
                searchResults.innerHTML = '<div class="p-2 text-muted">Type to search...</div>';
                return;
            }
            
            const filtered = sidebarLinks.filter(l => l.text.toLowerCase().includes(term));
            if (filtered.length === 0) {
                searchResults.innerHTML = '<div class="p-2 text-muted">No matching pages found</div>';
            } else {
                filtered.forEach(l => {
                    const a = document.createElement('a');
                    a.href = l.url;
                    a.className = 'd-block p-2 text-decoration-none text-dark hover-bg-light';
                    a.textContent = l.text;
                    searchResults.appendChild(a);
                });
            }
        });
    }

    // --- Escape Key ---
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            hideSearch();
            hideNotif();
            // Safely close Bootstrap dropdown
            if (userDropdown && window.bootstrap) {
                const bsDropdown = bootstrap.Dropdown.getInstance(userDropdown);
                if (bsDropdown) bsDropdown.hide();
            }
        }
    });
});
