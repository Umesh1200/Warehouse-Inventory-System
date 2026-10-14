document.addEventListener('DOMContentLoaded', () => {
    const pageData = window.stockMovementData || {};
    const details = Array.isArray(pageData.details) ? pageData.details : [];

    const numberFormat = new Intl.NumberFormat('en-NP', { maximumFractionDigits: 0 });
    const searchInput = document.getElementById('movementSearch');
    const typeFilter = document.getElementById('movementTypeFilter');
    const warehouseFilter = document.getElementById('movementWarehouseFilter');
    const dateFilter = document.getElementById('movementDateFilter');
    const resetFilters = document.getElementById('resetMovementFilters');
    const resultCount = document.getElementById('movementResultCount');
    const emptyRow = document.getElementById('movementEmptyRow');
    const rows = Array.from(document.querySelectorAll('.movement-row'));

    const normalize = (value) => String(value || '').trim().toLowerCase();
    const parseRowDate = (row) => {
        const date = new Date(`${row.dataset.date}T00:00:00`);
        return Number.isNaN(date.getTime()) ? null : date;
    };

    const matchesDateRange = (row, range) => {
        if (!range || range === 'all') return true;

        const rowDate = parseRowDate(row);
        if (!rowDate) return false;

        const today = new Date();
        today.setHours(0, 0, 0, 0);

        if (range === 'today') {
            return rowDate.getTime() === today.getTime();
        }

        const days = Number(range);
        if (!Number.isFinite(days)) return true;

        const start = new Date(today);
        start.setDate(start.getDate() - (days - 1));
        return rowDate >= start && rowDate <= today;
    };

    const applyFilters = () => {
        const search = normalize(searchInput?.value);
        const selectedType = normalize(typeFilter?.value);
        const selectedWarehouse = normalize(warehouseFilter?.value);
        const selectedDate = dateFilter?.value || 'all';
        let visibleCount = 0;

        rows.forEach((row) => {
            const matchesSearch = !search || normalize(row.dataset.search).includes(search);
            const matchesType = !selectedType || selectedType === 'all types' || normalize(row.dataset.type) === selectedType;
            const matchesWarehouse = !selectedWarehouse || selectedWarehouse === 'all warehouses' || normalize(row.dataset.warehouse) === selectedWarehouse;
            const isVisible = matchesSearch && matchesType && matchesWarehouse && matchesDateRange(row, selectedDate);

            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount += 1;
        });

        if (emptyRow) emptyRow.style.display = visibleCount === 0 ? '' : 'none';
        if (resultCount) resultCount.textContent = `${visibleCount} movement${visibleCount === 1 ? '' : 's'} shown`;
    };

    [searchInput, typeFilter, warehouseFilter, dateFilter].forEach((control) => {
        control?.addEventListener('input', applyFilters);
        control?.addEventListener('change', applyFilters);
    });

    resetFilters?.addEventListener('click', () => {
        if (searchInput) searchInput.value = '';
        if (typeFilter) typeFilter.value = 'All Types';
        if (warehouseFilter) warehouseFilter.value = 'All Warehouses';
        if (dateFilter) dateFilter.value = 'all';
        applyFilters();
    });

    applyFilters();

    const hasValues = (values) => Array.isArray(values) && values.some((value) => Number(value) > 0);

    const emptyStatePlugin = {
        id: 'movementEmptyState',
        afterDraw(chart) {
            const values = chart.data.datasets.flatMap((dataset) => dataset.data || []);
            if (hasValues(values)) return;

            const { ctx, chartArea } = chart;
            if (!chartArea) return;

            ctx.save();
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillStyle = '#64748b';
            ctx.font = '700 13px system-ui, sans-serif';
            ctx.fillText('No movement data available', (chartArea.left + chartArea.right) / 2, (chartArea.top + chartArea.bottom) / 2);
            ctx.restore();
        }
    };

    if (window.Chart) {
        Chart.register(emptyStatePlugin);

        const dailyTrend = pageData.dailyTrend || { labels: [], inbound: [], outbound: [] };
        const trendCanvas = document.getElementById('movementTrendChart');
        if (trendCanvas) {
            new Chart(trendCanvas, {
                type: 'line',
                data: {
                    labels: dailyTrend.labels,
                    datasets: [
                        {
                            label: 'Inbound',
                            data: dailyTrend.inbound,
                            borderColor: '#0f9f6e',
                            backgroundColor: 'rgba(15, 159, 110, 0.12)',
                            fill: true,
                            tension: 0.35,
                            pointRadius: 3
                        },
                        {
                            label: 'Outbound',
                            data: dailyTrend.outbound,
                            borderColor: '#dc2626',
                            backgroundColor: 'rgba(220, 38, 38, 0.1)',
                            fill: true,
                            tension: 0.35,
                            pointRadius: 3
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    interaction: { mode: 'index', intersect: false },
                    plugins: {
                        legend: { position: 'bottom', labels: { usePointStyle: true } },
                        tooltip: {
                            callbacks: {
                                label: (context) => `${context.dataset.label}: ${numberFormat.format(context.raw || 0)} units`
                            }
                        }
                    },
                    scales: {
                        x: { grid: { display: false }, ticks: { color: '#475569', font: { size: 11, weight: 700 } } },
                        y: { beginAtZero: true, grid: { color: '#e2e8f0' } }
                    }
                }
            });
        }

        const movementTypes = pageData.movementTypes || { labels: [], values: [] };
        const typeCanvas = document.getElementById('movementTypeChart');
        if (typeCanvas) {
            new Chart(typeCanvas, {
                type: 'doughnut',
                data: {
                    labels: movementTypes.labels,
                    datasets: [{
                        data: movementTypes.values,
                        backgroundColor: ['#2563eb', '#0f9f6e', '#dc2626', '#d97706', '#7c3aed', '#0284c7'],
                        borderColor: '#ffffff',
                        borderWidth: 3
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: '64%',
                    plugins: {
                        legend: { position: 'bottom', labels: { boxWidth: 10, usePointStyle: true } },
                        tooltip: {
                            callbacks: {
                                label: (context) => `${context.label}: ${numberFormat.format(context.raw || 0)} movements`
                            }
                        }
                    }
                }
            });
        }

        const warehouseFlow = pageData.warehouseFlow || { labels: [], inbound: [], outbound: [], net: [] };
        const warehouseCanvas = document.getElementById('warehouseFlowChart');
        if (warehouseCanvas) {
            new Chart(warehouseCanvas, {
                type: 'bar',
                data: {
                    labels: warehouseFlow.labels,
                    datasets: [
                        { label: 'Inbound', data: warehouseFlow.inbound, backgroundColor: '#0f9f6e', borderRadius: 8 },
                        { label: 'Outbound', data: warehouseFlow.outbound, backgroundColor: '#dc2626', borderRadius: 8 }
                    ]
                },
                options: {
                    indexAxis: 'y',
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { position: 'bottom', labels: { usePointStyle: true } },
                        tooltip: {
                            callbacks: {
                                afterBody: (items) => {
                                    const index = items[0]?.dataIndex ?? 0;
                                    const net = Number(warehouseFlow.net?.[index] || 0);
                                    return `Net: ${net >= 0 ? '+' : ''}${numberFormat.format(net)} units`;
                                },
                                label: (context) => `${context.dataset.label}: ${numberFormat.format(context.raw || 0)} units`
                            }
                        }
                    },
                    scales: {
                        x: { beginAtZero: true, grid: { color: '#e2e8f0' } },
                        y: { grid: { display: false }, ticks: { color: '#475569', font: { size: 11, weight: 700 } } }
                    }
                }
            });
        }

        const topProducts = pageData.topProducts || { labels: [], values: [], products: [] };
        const topProductsCanvas = document.getElementById('topProductsChart');
        if (topProductsCanvas) {
            new Chart(topProductsCanvas, {
                type: 'bar',
                data: {
                    labels: topProducts.labels,
                    datasets: [{
                        data: topProducts.values,
                        backgroundColor: '#0058be',
                        borderRadius: 8
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                title: (items) => {
                                    const index = items[0]?.dataIndex ?? 0;
                                    return topProducts.products?.[index] || items[0]?.label || '';
                                },
                                label: (context) => `${context.label}: ${numberFormat.format(context.raw || 0)} units moved`
                            }
                        }
                    },
                    scales: {
                        x: { grid: { display: false }, ticks: { color: '#475569', font: { size: 11, weight: 700 } } },
                        y: { beginAtZero: true, grid: { color: '#e2e8f0' } }
                    }
                }
            });
        }
    }

    const setText = (id, value) => {
        const element = document.getElementById(id);
        if (element) element.textContent = value || '-';
    };

    document.querySelectorAll('.js-view-movement').forEach((button) => {
        button.addEventListener('click', () => {
            const movementId = button.dataset.movementId;
            const movement = details.find((item) => item.id === movementId);
            if (!movement) return;

            setText('movementDetailSubtitle', `${movement.product} - ${movement.sku}`);
            setText('detailId', movement.id);
            setText('detailType', movement.type);
            setText('detailQty', movement.qty);
            setText('detailBalance', `${numberFormat.format(movement.beforeQty)} -> ${numberFormat.format(movement.afterQty)}`);
            setText('detailProduct', movement.product);
            setText('detailSku', movement.sku);
            setText('detailWarehouse', movement.warehouse);
            setText('detailShelf', movement.shelf);
            setText('detailRefType', movement.refType);
            setText('detailRefNo', movement.refNo);
            setText('detailUser', movement.performedBy);
            setText('detailDate', movement.fullDateTime || movement.dateTime);
            setText('detailRemarks', movement.remarks || 'No remarks recorded.');
        });
    });
});
