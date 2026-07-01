document.addEventListener('DOMContentLoaded', async () => {
  const user = await app.initShell({ activePath: '/reportes', title: 'Reportes', modulo: 'reportes' });
  if (!user) return;

  const contentEl = document.getElementById('page-content');
  const alertsEl = document.getElementById('page-alerts');

  const state = {
    resumen: null,
    desde: '',
    hasta: '',
    cargando: false,
    descargando: false
  };

  function todayInputDate() {
    return new Date().toISOString().slice(0, 10);
  }

  function daysAgoInputDate(days) {
    const date = new Date();
    date.setDate(date.getDate() - days);
    return date.toISOString().slice(0, 10);
  }

  function escapeHtml(value) {
    return String(value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function formatPeriodoLabel() {
    return `${app.formatDate(state.desde)} a ${app.formatDate(state.hasta)}`;
  }

  function renderFilters() {
    return `<section class="panel no-print">
      <div class="section-header">
        <div>
          <h2>Reportes de ventas y ganancias</h2>
          <p>Seleccione el período y actualice el resumen operativo.</p>
        </div>
      </div>
      <div class="grid filters">
        <label>
          Desde
          <input type="date" class="input" id="fecha-desde" value="${escapeHtml(state.desde)}" />
        </label>
        <label>
          Hasta
          <input type="date" class="input" id="fecha-hasta" value="${escapeHtml(state.hasta)}" />
        </label>
        <button type="button" class="button" id="btn-actualizar"${state.cargando ? ' disabled' : ''}>
          ${state.cargando ? 'Cargando...' : 'Actualizar'}
        </button>
        <button type="button" class="button primary" id="btn-excel"${state.descargando ? ' disabled' : ''}>
          ${state.descargando ? 'Generando...' : 'Descargar Excel'}
        </button>
        <button type="button" class="button ghost" id="btn-imprimir">Imprimir</button>
      </div>
    </section>`;
  }

  function renderCards() {
    const r = state.resumen;
    return `<div class="grid cards">
      <div class="card">
        <span class="metric-label">Ventas</span>
        <strong>${r.cantidadVentas}</strong>
      </div>
      <div class="card">
        <span class="metric-label">Total vendido (USD)</span>
        <strong>${app.formatUsd(r.totalVentas)}</strong>
      </div>
      <div class="card">
        <span class="metric-label">Ganancia (USD)</span>
        <strong>${app.formatUsd(r.gananciaTotal)}</strong>
      </div>
      <div class="card">
        <span class="metric-label">Movimientos de stock</span>
        <strong>${r.movimientosStock}</strong>
      </div>
      <div class="card warning-card">
        <span class="metric-label">Alertas de stock</span>
        <strong>${r.productosConStockMinimo}</strong>
      </div>
    </div>`;
  }

  function renderAlertasTable() {
    const alertas = state.resumen?.alertasStock || [];
    if (!alertas.length) {
      return '<p>Sin alertas de stock mínimo.</p>';
    }

    const rows = alertas.map((a) => `<tr class="danger-row">
      <td>${escapeHtml(app.display(a.sku))}</td>
      <td>${escapeHtml(app.display(a.nombre))}</td>
      <td>${a.stockActual}</td>
      <td>${a.stockMinimo}</td>
    </tr>`).join('');

    return `<div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>SKU</th>
            <th>Producto</th>
            <th>Stock</th>
            <th>Mínimo</th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
    </div>`;
  }

  function renderReporte() {
    if (!state.resumen) {
      return '<p>Cargando reporte...</p>';
    }

    return `<section class="print-area">
      <header class="print-header">
        <h2>Resumen operativo</h2>
        <p>Período: ${escapeHtml(formatPeriodoLabel())}</p>
      </header>
      ${renderCards()}
      <section class="panel report-card">
        <h3>Productos en stock mínimo</h3>
        ${renderAlertasTable()}
      </section>
    </section>`;
  }

  function render() {
    contentEl.innerHTML = renderFilters() + renderReporte();
    bindEvents();
  }

  function readDatesFromDom() {
    state.desde = document.getElementById('fecha-desde')?.value || state.desde;
    state.hasta = document.getElementById('fecha-hasta')?.value || state.hasta;
  }

  function validateDates() {
    if (state.desde > state.hasta) {
      app.renderAlerts(alertsEl, { error: 'La fecha desde no puede ser posterior a la fecha hasta.' });
      return false;
    }
    app.renderAlerts(alertsEl, {});
    return true;
  }

  function buildQuery() {
    return `desde=${encodeURIComponent(state.desde)}&hasta=${encodeURIComponent(state.hasta)}`;
  }

  async function cargarReporte() {
    readDatesFromDom();
    if (!validateDates()) return;

    state.cargando = true;
    render();

    try {
      state.resumen = await api.get(`/api/reportes/periodo?${buildQuery()}`);
    } catch (err) {
      state.resumen = null;
      app.renderAlerts(alertsEl, { error: err.message });
    } finally {
      state.cargando = false;
      render();
    }
  }

  async function descargarExcel() {
    readDatesFromDom();
    if (!validateDates()) return;

    state.descargando = true;
    render();

    try {
      await api.download(`/api/reportes/excel?${buildQuery()}`, 'reporte.xlsx');
    } catch (err) {
      app.renderAlerts(alertsEl, { error: `No se pudo generar el Excel: ${err.message}` });
    } finally {
      state.descargando = false;
      render();
    }
  }

  function imprimir() {
    stockSanti.printPage();
  }

  function bindEvents() {
    document.getElementById('btn-actualizar')?.addEventListener('click', cargarReporte);
    document.getElementById('btn-excel')?.addEventListener('click', descargarExcel);
    document.getElementById('btn-imprimir')?.addEventListener('click', imprimir);
  }

  state.desde = daysAgoInputDate(30);
  state.hasta = todayInputDate();

  try {
    await cargarReporte();
  } catch (err) {
    contentEl.innerHTML = '';
    app.renderAlerts(alertsEl, { error: err.message });
  }
});
