document.addEventListener('DOMContentLoaded', () => {
    const dashboardData = window.adminDashboardData || {};

    if (!window.Chart) {
        return;
    }

    const money = new Intl.NumberFormat('en-NP', {
        style: 'currency',
        currency: 'NPR',
        maximumFractionDigits: 0
    });

    const centerTextPlugin = {
        id: 'centerText',
        afterDraw(chart, args, pluginOptions) {
            const text = pluginOptions?.text;
            if (!text) return;

            const { ctx, chartArea } = chart;
            const centerX = (chartArea.left + chartArea.right) / 2;
            const centerY = (chartArea.top + chartArea.bottom) / 2;

            ctx.save();
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillStyle = '#0f172a';
            ctx.font = '700 16px system-ui, sans-serif';
            ctx.fillText(text.primary, centerX, centerY - 4);
            ctx.fillStyle = '#64748b';
            ctx.font = '700 11px system-ui, sans-serif';
            ctx.fillText(text.secondary, centerX, centerY + 16);
            ctx.restore();
        }
    };

    Chart.register(centerTextPlugin);

    const makeGradient = (ctx, area, color) => {
        const gradient = ctx.createLinearGradient(0, area.top, 0, area.bottom);
        gradient.addColorStop(0, color);
        gradient.addColorStop(1, 'rgba(255,255,255,0)');
        return gradient;
    };

    const truncateLabel = (value, maxLength) => {
        const text = String(value || '').trim();
        return text.length > maxLength ? `${text.slice(0, maxLength - 1)}...` : text;
    };

    const compactShelfLabel = (fullPath) => {
        const parts = String(fullPath || '').split('/').map((part) => part.trim()).filter(Boolean);

        if (parts.length >= 5) {
            const [warehouse, , room, rack, shelf] = parts;
            return [
                `${truncateLabel(rack, 16)} / ${truncateLabel(shelf, 12)}`,
                `${truncateLabel(room, 16)} - ${truncateLabel(warehouse, 20)}`
            ];
        }

        if (parts.length >= 4) {
            const [warehouse, , rack, shelf] = parts;
            return [
                `${truncateLabel(rack, 16)} / ${truncateLabel(shelf, 12)}`,
                truncateLabel(warehouse, 20)
            ];
        }

        return truncateLabel(fullPath, 32);
    };

    const utilization = dashboardData.utilization || { labels: [], values: [] };
    const utilizationCanvas = document.getElementById('warehouseUtilizationChart');
    if (utilizationCanvas) {
        new Chart(utilizationCanvas, {
            type: 'bar',
            data: {
                labels: utilization.labels,
                datasets: [{
                    label: 'Shelf fill %',
                    data: utilization.values,
                    borderRadius: 8,
                    backgroundColor: (context) => {
                        const value = context.raw || 0;
                        return value > 80 ? '#ef4444' : value > 60 ? '#f97316' : '#22c55e';
                    }
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            title: (items) => items.length ? items[0].label : '',
                            label: (context) => `${context.raw}% of shelf max quantity`
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        max: 100,
                        ticks: { callback: (value) => `${value}%` },
                        grid: { color: '#e2e8f0' }
                    },
                    y: {
                        ticks: {
                            color: '#475569',
                            font: { size: 11, weight: 700 },
                            callback: function (value) {
                                return compactShelfLabel(this.getLabelForValue(value));
                            }
                        },
                        grid: { display: false }
                    }
                }
            }
        });
    }

    const stockValue = dashboardData.stockValue || { labels: [], values: [], products: [], total: 0 };
    const stockByCategory = dashboardData.stockValueByCategory || { labels: [], values: [], total: 0 };
    const stockCanvas = document.getElementById('stockValueChart');
    let stockChart = null;

    const renderStockChart = (labels, values, products, total, isCategory) => {
        if (stockChart) {
            stockChart.destroy();
        }
        if (!stockCanvas) return;

        stockChart = new Chart(stockCanvas, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: values,
                    backgroundColor: ['#2563eb', '#059669', '#d97706', '#dc2626', '#0284c7', '#7c3aed', '#0f172a', '#84cc16', '#a1a1aa', '#f43f5e'],
                    borderWidth: 3,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '68%',
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 10, usePointStyle: true } },
                    centerText: {
                        text: {
                            primary: money.format(total || 0),
                            secondary: 'stock value'
                        }
                    },
                    tooltip: {
                        callbacks: {
                            title: (items) => {
                                const index = items[0]?.dataIndex || 0;
                                return (!isCategory && products) ? (products[index] || labels[index]) : labels[index];
                            },
                            label: (context) => `${context.label}: ${money.format(context.raw || 0)}`
                        }
                    }
                }
            }
        });
    };

    if (stockCanvas) {
        // Initial render
        renderStockChart(stockValue.labels, stockValue.values, stockValue.products, stockValue.total, false);

        const btnBySKU = document.getElementById('btnBySKU');
        const btnByCategory = document.getElementById('btnByCategory');
        const stockSubtext = document.getElementById('stockValueSubtext');

        if (btnBySKU && btnByCategory) {
            btnBySKU.addEventListener('click', () => {
                btnBySKU.classList.add('active');
                btnByCategory.classList.remove('active');
                if (stockSubtext) stockSubtext.textContent = 'Inventory capital concentration by SKU.';
                renderStockChart(stockValue.labels, stockValue.values, stockValue.products, stockValue.total, false);
            });

            btnByCategory.addEventListener('click', () => {
                btnByCategory.classList.add('active');
                btnBySKU.classList.remove('active');
                if (stockSubtext) stockSubtext.textContent = 'Inventory capital concentration by Category.';
                renderStockChart(stockByCategory.labels, stockByCategory.values, null, stockByCategory.total, true);
            });
        }
    }

    const movement = dashboardData.movementTrend || { labels: [], inbound: [], outbound: [] };
    const movementCanvas = document.getElementById('movementTrendChart');
    if (movementCanvas) {
        new Chart(movementCanvas, {
            type: 'line',
            data: {
                labels: movement.labels,
                datasets: [
                    {
                        label: 'Inbound',
                        data: movement.inbound,
                        borderColor: '#059669',
                        backgroundColor(context) {
                            const area = context.chart.chartArea;
                            return area ? makeGradient(context.chart.ctx, area, 'rgba(5, 150, 105, 0.22)') : 'rgba(5, 150, 105, 0.12)';
                        },
                        fill: true,
                        tension: 0.4,
                        pointRadius: 3
                    },
                    {
                        label: 'Outbound',
                        data: movement.outbound,
                        borderColor: '#dc2626',
                        backgroundColor(context) {
                            const area = context.chart.chartArea;
                            return area ? makeGradient(context.chart.ctx, area, 'rgba(220, 38, 38, 0.18)') : 'rgba(220, 38, 38, 0.1)';
                        },
                        fill: true,
                        tension: 0.4,
                        pointRadius: 3
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { position: 'bottom', labels: { usePointStyle: true } }
                },
                scales: {
                    y: { beginAtZero: true, grid: { color: '#e2e8f0' } },
                    x: { grid: { display: false } }
                }
            }
        });
    }

    const fulfillment = dashboardData.fulfillmentGauge || { value: 0, target: 20, remaining: 20 };
    const gaugeCanvas = document.getElementById('fulfillmentGaugeChart');
    if (gaugeCanvas) {
        new Chart(gaugeCanvas, {
            type: 'doughnut',
            data: {
                labels: ['Fulfilled', 'Remaining'],
                datasets: [{
                    data: [fulfillment.value, fulfillment.remaining],
                    backgroundColor: ['#2563eb', '#e2e8f0'],
                    borderWidth: 0,
                    circumference: 180,
                    rotation: 270
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '72%',
                plugins: {
                    legend: { display: false },
                    centerText: {
                        text: {
                            primary: `${fulfillment.value}/${fulfillment.target}`,
                            secondary: 'shipments'
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: (context) => `${context.label}: ${context.raw}`
                        }
                    }
                }
            }
        });
    }

    // --- Action Required / Alerts Section (Triage Modal + Quick Filters) ---
    const rawAlerts = dashboardData.lowStockAlerts || [];
    
    // Triage / Sort: OOS first, then by quantity ASC
    const triageAlerts = (alerts) => {
        return [...alerts].sort((a, b) => {
            const aOOS = parseFloat(a.quantity) <= 0 || a.severity === 'Error';
            const bOOS = parseFloat(b.quantity) <= 0 || b.severity === 'Error';
            if (aOOS && !bOOS) return -1;
            if (!aOOS && bOOS) return 1;

            const aQty = parseFloat(a.quantity) || 0;
            const bQty = parseFloat(b.quantity) || 0;
            return aQty - bQty;
        });
    };

    const allAlertsSorted = triageAlerts(rawAlerts);
    let currentAlertFilter = 'all';

    const getFilteredAlerts = (filter) => {
        if (filter === 'outofstock') {
            return allAlertsSorted.filter(a => parseFloat(a.quantity) <= 0 || a.severity === 'Error');
        } else if (filter === 'needpo') {
            return allAlertsSorted.filter(a => parseFloat(a.quantity) > 0 && a.severity === 'Warning');
        }
        return allAlertsSorted;
    };

    const renderDashboardAlerts = () => {
        const container = document.getElementById('alertsListContainer');
        if (!container) return;

        const filtered = getFilteredAlerts(currentAlertFilter);
        const top5 = filtered.slice(0, 5);

        // Update counts
        const textEl = document.getElementById('alertCountText');
        const badgeEl = document.getElementById('alertCountBadge');
        if (textEl) {
            textEl.textContent = `${filtered.length} low-stock item(s) need review.`;
        }
        if (badgeEl) {
            badgeEl.textContent = filtered.length;
            badgeEl.style.display = filtered.length > 0 ? 'inline-block' : 'none';
        }

        if (top5.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <span class="material-symbols-outlined">task_alt</span>
                    <strong>No low-stock alerts</strong>
                    <p>No items match the selected filter.</p>
                </div>
            `;
            return;
        }

        let html = '';
        top5.forEach(alert => {
            const isDanger = alert.severity === 'Error';
            html += `
                <div class="alert-item ${isDanger ? 'danger' : 'warning'}">
                    <div class="alert-row">
                        <div>
                            <span class="alert-sku">${alert.sku}</span>
                            <strong>${alert.product}</strong>
                        </div>
                        <span class="alert-qty">${alert.quantity} ${alert.unit || 'Units'}</span>
                    </div>
                    <div class="alert-footer">
                        <span>Reorder point: ${alert.reorderPoint}</span>
                        <a href="/Purchase/Orders" class="btn btn-sm ${isDanger ? 'btn-primary' : 'btn-light'}">
                            ${isDanger ? 'Create PO' : 'Review'}
                        </a>
                    </div>
                </div>
            `;
        });
        container.innerHTML = html;
    };

    // Filter tab events
    document.querySelectorAll('.panel-filter-bar [data-filter]').forEach(tab => {
        tab.addEventListener('click', (e) => {
            document.querySelectorAll('.panel-filter-bar [data-filter]').forEach(t => t.classList.remove('active'));
            tab.classList.add('active');
            currentAlertFilter = tab.getAttribute('data-filter');
            renderDashboardAlerts();
        });
    });

    // Initialize Alerts list
    renderDashboardAlerts();

    // Modal triage table logic
    const triageModal = document.getElementById('triageModal');
    const btnViewAll = document.getElementById('btnViewAllAlerts');
    const btnCloseModal = document.getElementById('btnCloseTriageModal');

    let modalPage = 1;
    const modalPageSize = 10;
    let modalFilteredAlerts = [];

    const updateModalData = () => {
        const searchVal = (document.getElementById('triageSearch')?.value || '').toLowerCase().trim();
        const severityVal = document.getElementById('triageSeverityFilter')?.value || '';

        modalFilteredAlerts = allAlertsSorted.filter(alert => {
            const matchesSearch = alert.sku.toLowerCase().includes(searchVal) || alert.product.toLowerCase().includes(searchVal);
            const matchesSeverity = severityVal === '' || alert.severity === severityVal;
            return matchesSearch && matchesSeverity;
        });

        modalPage = 1;
        renderModalTable();
    };

    const renderModalTable = () => {
        const tbody = document.getElementById('triageTableBody');
        if (!tbody) return;

        const totalItems = modalFilteredAlerts.length;
        const totalPages = Math.ceil(totalItems / modalPageSize) || 1;

        if (modalPage > totalPages) modalPage = totalPages;
        if (modalPage < 1) modalPage = 1;

        const startIndex = (modalPage - 1) * modalPageSize;
        const pageItems = modalFilteredAlerts.slice(startIndex, startIndex + modalPageSize);

        const btnPrev = document.getElementById('btnTriagePrev');
        const btnNext = document.getElementById('btnTriageNext');
        const info = document.getElementById('triagePageInfo');

        if (btnPrev) btnPrev.disabled = modalPage === 1;
        if (btnNext) btnNext.disabled = modalPage === totalPages;
        if (info) info.textContent = `Page ${modalPage} of ${totalPages} (${totalItems} total)`;

        if (pageItems.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="9" class="text-center" style="padding: 2rem;">
                        <div class="empty-state">
                            <span class="material-symbols-outlined">search_off</span>
                            <strong>No items found</strong>
                            <p>Try adjusting your search query or severity filter.</p>
                        </div>
                    </td>
                </tr>
            `;
            return;
        }

        let html = '';
        pageItems.forEach(alert => {
            const isDanger = alert.severity === 'Error';
            html += `
                <tr>
                    <td class="mono font-bold">${alert.sku}</td>
                    <td>${alert.product}</td>
                    <td><span class="badge bg-light text-dark">${alert.categoryName || 'General'}</span></td>
                    <td>${alert.warehouseName || 'Main'}</td>
                    <td class="mono">${alert.shelfCode || '-'}</td>
                    <td class="text-end mono font-semibold ${isDanger ? 'text-danger' : 'text-warning'}">${alert.quantity}</td>
                    <td class="text-end mono">${alert.reorderPoint}</td>
                    <td>
                        <span class="badge ${isDanger ? 'bg-danger' : 'bg-warning'} text-white" style="display:inline-block; padding: 0.25em 0.6em; border-radius: 4px;">
                            ${isDanger ? 'Out of Stock' : 'Need PO'}
                        </span>
                    </td>
                    <td class="text-center">
                        <a href="/Purchase/Orders" class="btn btn-sm ${isDanger ? 'btn-primary' : 'btn-light'}">
                            ${isDanger ? 'Create PO' : 'Review'}
                        </a>
                    </td>
                </tr>
            `;
        });
        tbody.innerHTML = html;
    };

    if (btnViewAll && triageModal) {
        btnViewAll.addEventListener('click', () => {
            triageModal.style.display = 'flex';
            updateModalData();
        });
    }

    if (btnCloseModal && triageModal) {
        btnCloseModal.addEventListener('click', () => {
            triageModal.style.display = 'none';
        });
    }

    document.getElementById('triageSearch')?.addEventListener('input', updateModalData);
    document.getElementById('triageSeverityFilter')?.addEventListener('change', updateModalData);

    document.getElementById('btnTriagePrev')?.addEventListener('click', () => {
        if (modalPage > 1) {
            modalPage--;
            renderModalTable();
        }
    });

    document.getElementById('btnTriageNext')?.addEventListener('click', () => {
        const totalPages = Math.ceil(modalFilteredAlerts.length / modalPageSize) || 1;
        if (modalPage < totalPages) {
            modalPage++;
            renderModalTable();
        }
    });

    window.addEventListener('click', (e) => {
        if (e.target === triageModal) {
            triageModal.style.display = 'none';
        }
    });

    // --- Recent Stock Movements Filters ---
    const movementsTable = document.getElementById('movementsTable');
    if (movementsTable) {
        const rows = Array.from(movementsTable.querySelectorAll('.movement-row'));
        
        const filterMovements = (filterType) => {
            let visibleCount = 0;
            rows.forEach(row => {
                const rowType = row.getAttribute('data-type');
                const matches = filterType === 'all' || rowType === filterType;
                
                if (matches && visibleCount < 10) {
                    row.style.display = '';
                    visibleCount++;
                } else {
                    row.style.display = 'none';
                }
            });

            const tbody = movementsTable.querySelector('tbody');
            let emptyRow = movementsTable.querySelector('.empty-filter-row');
            if (visibleCount === 0) {
                if (!emptyRow) {
                    emptyRow = document.createElement('tr');
                    emptyRow.className = 'empty-filter-row';
                    emptyRow.innerHTML = `
                        <td colspan="5">
                            <div class="empty-state">
                                <span class="material-symbols-outlined">history</span>
                                <strong>No matching movements</strong>
                                <p>No stock activity matches the selected filter.</p>
                            </div>
                        </td>
                    `;
                    tbody.appendChild(emptyRow);
                }
            } else if (emptyRow) {
                emptyRow.remove();
            }
        };

        // Initialize default view to show at most 10 rows
        filterMovements('all');

        // Bind movement filter events
        document.querySelectorAll('[data-movement-filter]').forEach(tab => {
            tab.addEventListener('click', (e) => {
                document.querySelectorAll('[data-movement-filter]').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                const movementFilter = tab.getAttribute('data-movement-filter');
                filterMovements(movementFilter);
            });
        });
    }

    // Tab Switching for Executive Command Hub
    const tabButtons = document.querySelectorAll('.dash-tab-btn');
    const tabPanes = document.querySelectorAll('.dash-tab-pane');

    tabButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetTabId = btn.getAttribute('data-tab');
            if (!targetTabId) return;

            // Activate button
            tabButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            // Show pane
            tabPanes.forEach(pane => {
                if (pane.id === targetTabId) {
                    pane.style.display = 'block';
                    pane.classList.add('active');
                } else {
                    pane.style.display = 'none';
                    pane.classList.remove('active');
                }
            });

            // Trigger window resize for chart responsiveness if switching to Operations
            if (targetTabId === 'tab-operations') {
                window.dispatchEvent(new Event('resize'));
            }
        });
    });
});

