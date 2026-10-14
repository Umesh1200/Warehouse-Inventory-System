document.addEventListener('DOMContentLoaded', () => {
    const data = window.inventoryOverviewData || {};

    if (!window.Chart) {
        return;
    }

    const money = new Intl.NumberFormat('en-NP', {
        style: 'currency',
        currency: 'NPR',
        maximumFractionDigits: 0
    });

    const hasValues = (values) => Array.isArray(values) && values.some((value) => Number(value) > 0);

    const emptyPlugin = {
        id: 'emptyState',
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
            ctx.fillText('No data for selected filters', (chartArea.left + chartArea.right) / 2, (chartArea.top + chartArea.bottom) / 2);
            ctx.restore();
        }
    };

    Chart.register(emptyPlugin);

    const stockHealth = data.stockHealth || { labels: [], values: [] };
    const stockHealthCanvas = document.getElementById('stockHealthChart');
    if (stockHealthCanvas) {
        new Chart(stockHealthCanvas, {
            type: 'bar',
            data: {
                labels: stockHealth.labels,
                datasets: [{
                    data: stockHealth.values,
                    borderRadius: 8,
                    backgroundColor: ['#0f9f6e', '#d97706', '#dc2626', '#2563eb', '#7f1d1d']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: (context) => `${context.raw} SKUs`
                        }
                    }
                },
                scales: {
                    x: { grid: { display: false }, ticks: { color: '#475569', font: { size: 11, weight: 700 } } },
                    y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: '#e2e8f0' } }
                }
            }
        });
    }

    const quantityMix = data.quantityMix || { labels: [], values: [] };
    const quantityMixCanvas = document.getElementById('quantityMixChart');
    if (quantityMixCanvas) {
        new Chart(quantityMixCanvas, {
            type: 'doughnut',
            data: {
                labels: quantityMix.labels,
                datasets: [{
                    data: quantityMix.values,
                    backgroundColor: ['#0f9f6e', '#2563eb', '#d97706', '#dc2626'],
                    borderWidth: 0
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
                            label: (context) => `${context.label}: ${Number(context.raw || 0).toLocaleString()} units`
                        }
                    }
                }
            }
        });
    }

    const categoryValue = data.categoryValue || { labels: [], values: [] };
    const categoryValueCanvas = document.getElementById('categoryValueChart');
    if (categoryValueCanvas) {
        new Chart(categoryValueCanvas, {
            type: 'bar',
            data: {
                labels: categoryValue.labels,
                datasets: [{
                    data: categoryValue.values,
                    borderRadius: 8,
                    backgroundColor: '#0058be'
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
                            label: (context) => money.format(context.raw || 0)
                        }
                    }
                },
                scales: {
                    x: { beginAtZero: true, ticks: { callback: (value) => money.format(value) }, grid: { color: '#e2e8f0' } },
                    y: { grid: { display: false }, ticks: { color: '#475569', font: { size: 11, weight: 700 } } }
                }
            }
        });
    }

    const reorderRisk = data.reorderRisk || { labels: [], values: [], products: [], available: [], reorder: [] };
    const reorderRiskCanvas = document.getElementById('reorderRiskChart');
    if (reorderRiskCanvas) {
        new Chart(reorderRiskCanvas, {
            type: 'bar',
            data: {
                labels: reorderRisk.labels,
                datasets: [{
                    label: 'Shortage',
                    data: reorderRisk.values,
                    borderRadius: 8,
                    backgroundColor: '#dc2626'
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
                                return reorderRisk.products?.[index] || items[0]?.label || '';
                            },
                            label: (context) => {
                                const index = context.dataIndex;
                                return [
                                    `Short by ${Number(context.raw || 0).toLocaleString()} units`,
                                    `Available: ${Number(reorderRisk.available?.[index] || 0).toLocaleString()}`,
                                    `Reorder level: ${Number(reorderRisk.reorder?.[index] || 0).toLocaleString()}`
                                ];
                            }
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
});
