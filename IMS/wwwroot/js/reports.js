// Reports & Analytics - Fully Interactive Script
document.addEventListener('DOMContentLoaded', function () {
    let currentReportKey = 'current-stock';
    let currentReportData = null;

    // DOM Elements
    const searchInput = document.getElementById('reportSearch');
    const categoryFilter = document.getElementById('reportCategoryFilter');
    const statusFilter = document.getElementById('reportStatusFilter');
    const reportRows = Array.from(document.querySelectorAll('.report-row'));
    const emptyRow = document.getElementById('reportEmptyRow');

    // Report Viewer Elements
    const viewerModalEl = document.getElementById('reportViewerModal');
    const viewerModal = viewerModalEl ? new bootstrap.Modal(viewerModalEl) : null;
    const viewerTitle = document.getElementById('reportViewerTitle');
    const viewerCategory = document.getElementById('reportViewerCategory');
    const viewerDescription = document.getElementById('reportViewerDescription');
    const viewerMetricsContainer = document.getElementById('viewerMetricsContainer');
    const viewerTableHead = document.getElementById('viewerTableHead');
    const viewerTableBody = document.getElementById('viewerTableBody');
    const viewerSearchInput = document.getElementById('viewerSearchInput');
    const viewerRefreshBtn = document.getElementById('viewerRefreshBtn');
    const viewerPrintBtn = document.getElementById('viewerPrintBtn');
    const viewerExportCsvBtn = document.getElementById('viewerExportCsvBtn');
    const viewerGeneratedTimestamp = document.getElementById('viewerGeneratedTimestamp');
    const viewerRecordCount = document.getElementById('viewerRecordCount');

    // History Modal Elements
    const historyModalEl = document.getElementById('reportHistoryModal');
    const historyModal = historyModalEl ? new bootstrap.Modal(historyModalEl) : null;
    const historyModalTitle = document.getElementById('historyModalTitle');

    // Scheduled Reports Elements
    const scheduledReportsBtn = document.getElementById('scheduledReportsBtn');
    const scheduledReportsList = document.getElementById('scheduledReportsList');

    // ==========================================
    // 1. Main Page Table Filtering
    // ==========================================
    const normalize = (val) => String(val || '').trim().toLowerCase();

    function applyMainFilters() {
        const query = normalize(searchInput?.value);
        const selectedCat = normalize(categoryFilter?.value);
        const selectedStatus = normalize(statusFilter?.value);
        let visibleCount = 0;

        reportRows.forEach(row => {
            const matchesQuery = !query || normalize(row.dataset.search).includes(query);
            const matchesCat = !selectedCat || selectedCat === 'all categories' || normalize(row.dataset.category) === selectedCat;
            const matchesStatus = !selectedStatus || selectedStatus === 'all statuses' || normalize(row.dataset.status) === selectedStatus;
            const isVisible = matchesQuery && matchesCat && matchesStatus;

            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount++;
        });

        if (emptyRow) emptyRow.style.display = visibleCount === 0 ? '' : 'none';
    }

    [searchInput, categoryFilter, statusFilter].forEach(el => {
        el?.addEventListener('input', applyMainFilters);
        el?.addEventListener('change', applyMainFilters);
    });

    function escapeHtml(str) {
        if (!str) return '';
        return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // ==========================================
    // 2. Load & Render Report in Viewer Modal
    // ==========================================
    async function loadReport(reportKey, autoPrint = false) {
        currentReportKey = reportKey;
        if (viewerModal) viewerModal.show();

        if (viewerTableHead) viewerTableHead.innerHTML = '';
        if (viewerTableBody) {
            viewerTableBody.innerHTML = `
                <tr>
                    <td colspan="10" class="text-center py-5 text-muted">
                        <div class="spinner-border spinner-border-sm text-primary me-2"></div>
                        Querying live database records for <strong>${escapeHtml(reportKey)}</strong>...
                    </td>
                </tr>
            `;
        }
        if (viewerMetricsContainer) viewerMetricsContainer.innerHTML = '';

        try {
            const res = await fetch(`?handler=ReportData&reportKey=${encodeURIComponent(reportKey)}`);
            if (!res.ok) throw new Error('Failed to fetch report data');
            const result = await res.json();

            if (!result.success || !result.data) {
                throw new Error(result.message || 'Error loading report');
            }

            const data = result.data;
            currentReportData = data;

            // Update Metadata
            if (viewerTitle) viewerTitle.textContent = data.title || 'Report Viewer';
            if (viewerCategory) viewerCategory.textContent = data.category || 'General';
            if (viewerDescription) viewerDescription.textContent = data.description || '';
            if (viewerGeneratedTimestamp) viewerGeneratedTimestamp.textContent = data.generatedAt || new Date().toLocaleString();
            if (viewerRecordCount) viewerRecordCount.textContent = data.totalCount || data.rows.length;

            // Render Metrics Chips
            if (viewerMetricsContainer && data.summaryMetrics) {
                let chipsHtml = '';
                for (const [key, val] of Object.entries(data.summaryMetrics)) {
                    chipsHtml += `
                        <div class="metric-chip">
                            <span class="chip-label">${escapeHtml(key)}:</span>
                            <span class="chip-value">${escapeHtml(val)}</span>
                        </div>
                    `;
                }
                viewerMetricsContainer.innerHTML = chipsHtml;
            }

            // Render Table Headers
            if (viewerTableHead && data.headers) {
                let headHtml = '<tr>';
                data.headers.forEach(h => {
                    const isNumeric = /qty|amount|value|price|cost|pct|%|stock/i.test(h);
                    headHtml += `<th class="${isNumeric ? 'text-right' : ''}">${escapeHtml(h)}</th>`;
                });
                headHtml += '</tr>';
                viewerTableHead.innerHTML = headHtml;
            }

            // Render Table Rows
            renderViewerRows(data.rows, data.headers);

            if (autoPrint) {
                setTimeout(() => {
                    window.print();
                }, 500);
            }

        } catch (err) {
            console.error('Error loading report:', err);
            if (viewerTableBody) {
                viewerTableBody.innerHTML = `
                    <tr>
                        <td colspan="10" class="text-center py-4 text-danger">
                            <span class="material-symbols-outlined d-block mb-1" style="font-size: 28px;">error</span>
                            Failed to load report data: ${escapeHtml(err.message)}
                        </td>
                    </tr>
                `;
            }
        }
    }

    function renderViewerRows(rows, headers = []) {
        if (!viewerTableBody) return;

        if (!rows || rows.length === 0) {
            viewerTableBody.innerHTML = `
                <tr>
                    <td colspan="${headers.length || 8}" class="text-center py-5 text-muted">
                        <span class="material-symbols-outlined d-block mb-1" style="font-size: 32px; opacity: 0.5;">inbox</span>
                        No records found for this report.
                    </td>
                </tr>
            `;
            return;
        }

        const filterQuery = normalize(viewerSearchInput?.value);
        let rowsHtml = '';
        let matchCount = 0;

        rows.forEach(row => {
            const rowText = row.join(' ').toLowerCase();
            if (filterQuery && !rowText.includes(filterQuery)) return;

            matchCount++;
            rowsHtml += '<tr>';
            row.forEach((cell, idx) => {
                const header = headers[idx] || '';
                const isNumeric = /qty|amount|value|price|cost|pct|%|stock/i.test(header);
                const isStatus = /status/i.test(header);
                
                let cellContent = escapeHtml(cell);
                if (isStatus) {
                    const statusClass = cell === 'Optimal' || cell === 'Fulfilled' || cell === 'Closed' ? 'status-ready' : (cell === 'Critical' || cell === 'Cancelled' ? 'bg-danger text-white' : 'bg-warning text-dark');
                    cellContent = `<span class="status-badge ${statusClass}" style="padding: 2px 6px; border-radius: 4px; font-size: 11px;">${cellContent}</span>`;
                }

                rowsHtml += `<td class="${isNumeric ? 'text-right font-mono' : ''}">${cellContent}</td>`;
            });
            rowsHtml += '</tr>';
        });

        if (matchCount === 0) {
            rowsHtml = `
                <tr>
                    <td colspan="${headers.length || 8}" class="text-center py-4 text-muted">
                        No rows match your filter "${escapeHtml(filterQuery)}".
                    </td>
                </tr>
            `;
        }

        viewerTableBody.innerHTML = rowsHtml;
        if (viewerRecordCount) viewerRecordCount.textContent = matchCount;
    }

    // In-Modal Live Filter
    if (viewerSearchInput) {
        viewerSearchInput.addEventListener('input', () => {
            if (currentReportData && currentReportData.rows) {
                renderViewerRows(currentReportData.rows, currentReportData.headers);
            }
        });
    }

    // Modal Toolbar Actions
    if (viewerRefreshBtn) {
        viewerRefreshBtn.addEventListener('click', () => {
            if (currentReportKey) loadReport(currentReportKey);
        });
    }

    if (viewerPrintBtn) {
        viewerPrintBtn.addEventListener('click', () => {
            window.print();
        });
    }

    if (viewerExportCsvBtn) {
        viewerExportCsvBtn.addEventListener('click', () => {
            if (currentReportKey) {
                window.location.href = `?handler=ExportCsv&reportKey=${encodeURIComponent(currentReportKey)}`;
            }
        });
    }

    // ==========================================
    // 3. Row Action Button Triggers
    // ==========================================
    // Run Button
    document.querySelectorAll('.js-run-report').forEach(btn => {
        btn.addEventListener('click', function () {
            const key = this.dataset.reportKey || 'current-stock';
            if (viewerSearchInput) viewerSearchInput.value = '';
            loadReport(key, false);
        });
    });

    // Export PDF Button
    document.querySelectorAll('.js-export-pdf').forEach(btn => {
        btn.addEventListener('click', function () {
            const key = this.dataset.reportKey || 'current-stock';
            loadReport(key, true);
        });
    });

    // Export Excel / CSV Button
    document.querySelectorAll('.js-export-excel').forEach(btn => {
        btn.addEventListener('click', function () {
            const key = this.dataset.reportKey || 'current-stock';
            window.location.href = `?handler=ExportCsv&reportKey=${encodeURIComponent(key)}`;
        });
    });

    // View History Button
    document.querySelectorAll('.js-view-history').forEach(btn => {
        btn.addEventListener('click', function () {
            const name = this.dataset.reportName || 'Report';
            if (historyModalTitle) historyModalTitle.textContent = `${name} - Generation History`;
            if (historyModal) historyModal.show();
        });
    });

    // ==========================================
    // 4. Scheduled Reports Modal Loader
    // ==========================================
    async function loadSchedules() {
        if (!scheduledReportsList) return;
        scheduledReportsList.innerHTML = `
            <div class="text-center py-4 text-muted">
                <div class="spinner-border spinner-border-sm me-2"></div> Loading automated schedules...
            </div>
        `;

        try {
            const res = await fetch('?handler=ReportSchedules');
            if (!res.ok) throw new Error('Failed to load schedules');
            const data = await res.json();

            if (data.success && data.schedules && data.schedules.length > 0) {
                let html = '';
                data.schedules.forEach(s => {
                    html += `
                        <div class="schedule-item-card">
                            <div class="schedule-info">
                                <h6>${escapeHtml(s.reportName)}</h6>
                                <div class="schedule-meta">
                                    <span><span class="material-symbols-outlined" style="font-size: 14px; vertical-align: middle;">schedule</span> ${escapeHtml(s.frequency)} (${escapeHtml(s.time)})</span>
                                    <span><span class="material-symbols-outlined" style="font-size: 14px; vertical-align: middle;">mail</span> ${escapeHtml(s.recipients)}</span>
                                </div>
                                <div class="mt-1" style="font-size: 11px; color: #64748b;">
                                    Format: <strong>${escapeHtml(s.format)}</strong> | Next Run: <span class="text-primary font-mono">${escapeHtml(s.nextRun)}</span>
                                </div>
                            </div>
                            <div>
                                <div class="form-check form-switch">
                                    <input class="form-check-input" type="checkbox" role="switch" ${s.isActive ? 'checked' : ''} id="schedSwitch_${s.scheduleID}">
                                    <label class="form-check-label text-muted" for="schedSwitch_${s.scheduleID}" style="font-size: 12px;">${s.isActive ? 'Active' : 'Paused'}</label>
                                </div>
                            </div>
                        </div>
                    `;
                });
                scheduledReportsList.innerHTML = html;
            } else {
                scheduledReportsList.innerHTML = `
                    <div class="text-center py-4 text-muted">
                        No automated schedules configured.
                    </div>
                `;
            }
        } catch (err) {
            scheduledReportsList.innerHTML = `
                <div class="text-center py-4 text-danger">
                    Failed to fetch schedules: ${escapeHtml(err.message)}
                </div>
            `;
        }
    }

    if (scheduledReportsBtn) {
        scheduledReportsBtn.addEventListener('click', () => {
            loadSchedules();
        });
    }
});
