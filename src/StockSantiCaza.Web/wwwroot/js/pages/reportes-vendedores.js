document.addEventListener('DOMContentLoaded', async () => {
  const user = await app.initShell({
    activePath: '/reportes/vendedores',
    title: 'Ventas por vendedor',
    modulo: 'reportes',
    requireAdmin: true
  });
  if (!user) return;

  const contentEl = document.getElementById('page-content');
  const alertsEl = document.getElementById('page-alerts');

  const state = {
    resumen: null,
    desde: '',
    hasta: '',
    cargando: false
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
          <h2>Ventas por vendedor y categoría</h2>
          <p>Solo administradores. Cantidades y montos de ventas no anuladas, desglosados por categoría de producto.</p>
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
        <button type="button" class="button ghost" id="btn-imprimir">Imprimir</button>
      </div>
    </section>`;
  }

  function renderCards() {
    const r = state.resumen;
    return `<div class="grid cards">
      <div class="card">
        <span class="metric-label">Vendedores con ventas</span>
        <strong>${(r.vendedores || []).length}</strong>
      </div>
      <div class="card">
        <span class="metric-label">Unidades vendidas</span>
        <strong>${r.cantidadUnidades}</strong>
      </div>
      <div class="card">
        <span class="metric-label">Total vendido (USD)</span>
        <strong>${app.formatUsd(r.montoTotal)}</strong>
      </div>
      <div class="card">
        <span class="metric-label">Categorías</span>
        <strong>${(r.totalesPorCategoria || []).length}</strong>
      </div>
    </div>`;
  }

  function renderCategoriaRows(categorias) {
    return (categorias || []).map((c) => `<tr>
      <td>${escapeHtml(c.categoria)}</td>
      <td>${c.cantidad}</td>
      <td>${app.formatUsd(c.monto)}</td>
    </tr>`).join('');
  }

  function renderVendedores() {
    const vendedores = state.resumen?.vendedores || [];
    if (!vendedores.length) {
      return `<section class="panel report-card">
        <h3>Detalle por vendedor</h3>
        <p>No hay ventas en el período seleccionado.</p>
      </section>`;
    }

    const blocks = vendedores.map((v) => {
      const rows = renderCategoriaRows(v.categorias);
      return `<section class="panel report-card">
        <div class="section-header">
          <div>
            <h3>${escapeHtml(v.vendedor)}</h3>
            <p>${v.cantidadUnidades} unidad(es) · ${app.formatUsd(v.montoTotal)}</p>
          </div>
        </div>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Categoría</th>
                <th>Cantidad</th>
                <th>Monto USD</th>
              </tr>
            </thead>
            <tbody>${rows}</tbody>
            <tfoot>
              <tr>
                <th>Total vendedor</th>
                <th>${v.cantidadUnidades}</th>
                <th>${app.formatUsd(v.montoTotal)}</th>
              </tr>
            </tfoot>
          </table>
        </div>
      </section>`;
    }).join('');

    return `<div class="print-area">
      <header class="print-header">
        <h2>Ventas por vendedor y categoría</h2>
        <p>Período: ${escapeHtml(formatPeriodoLabel())}</p>
      </header>
      ${blocks}
    </div>`;
  }

  function renderTotalesCategoria() {
    const totales = state.resumen?.totalesPorCategoria || [];
    if (!totales.length) {
      return '';
    }

    const rows = renderCategoriaRows(totales);
    return `<section class="panel report-card">
      <div class="section-header">
        <div>
          <h3>Totales generales por categoría</h3>
          <p>Suma de todos los vendedores en el período.</p>
        </div>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Categoría</th>
              <th>Cantidad</th>
              <th>Monto USD</th>
            </tr>
          </thead>
          <tbody>${rows}</tbody>
          <tfoot>
            <tr>
              <th>Total general</th>
              <th>${state.resumen.cantidadUnidades}</th>
              <th>${app.formatUsd(state.resumen.montoTotal)}</th>
            </tr>
          </tfoot>
        </table>
      </div>
    </section>`;
  }

  function renderReporte() {
    if (!state.resumen) {
      return '<p>Cargando reporte...</p>';
    }

    return `${renderCards()}${renderVendedores()}${renderTotalesCategoria()}`;
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
      state.resumen = await api.get(`/api/reportes/ventas-por-vendedor-categoria?${buildQuery()}`);
    } catch (err) {
      state.resumen = null;
      app.renderAlerts(alertsEl, { error: err.message });
    } finally {
      state.cargando = false;
      render();
    }
  }

  function bindEvents() {
    document.getElementById('btn-actualizar')?.addEventListener('click', cargarReporte);
    document.getElementById('btn-imprimir')?.addEventListener('click', () => stockSanti.printPage());
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
