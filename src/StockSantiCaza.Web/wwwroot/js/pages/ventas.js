document.addEventListener('DOMContentLoaded', async () => {
  const user = await app.initShell({ activePath: '/ventas', title: 'Ventas', modulo: 'ventas' });
  if (!user) return;

  const container = document.getElementById('page-content');
  const alertsEl = document.getElementById('page-alerts');
  const esAdmin = app.usuario.esAdministrador;
  const puedeVerMontos = app.usuario.puedeVerMontosVentas;

  const ESTADOS = ['Borrador', 'Confirmada', 'Facturada', 'Anulada'];

  const state = {
    ventas: [],
    desde: defaultDesde(),
    hasta: defaultHasta(),
    busqueda: '',
    estado: '',
    cargando: false,
    expanded: new Set()
  };

  function defaultDesde() {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return app.toInputDate(d);
  }

  function defaultHasta() {
    return app.toInputDate(new Date());
  }

  function estadoBadgeClass(estado) {
    if (estado === 'Anulada') return 'badge danger';
    if (estado === 'Confirmada' || estado === 'Facturada') return 'badge ok';
    return 'badge muted';
  }

  function formatearVendedor(venta) {
    return app.display(venta.vendedor, 'Sin vendedor asignado');
  }

  function numeroVenta(venta) {
    return venta.numeroComprobante || `VTA-${String(venta.id).padStart(8, '0')}`;
  }

  function tieneTrazabilidad(detalle) {
    return detalle.trazabilidad && detalle.trazabilidad !== 'No aplica';
  }

  function coincideBusqueda(venta, term) {
    const q = term.toLowerCase();
    if ((venta.numeroComprobante || '').toLowerCase().includes(q)) return true;
    if ((venta.cliente?.nombreRazonSocial || '').toLowerCase().includes(q)) return true;
    if ((venta.cliente?.dniCuit || '').toLowerCase().includes(q)) return true;
    if (formatearVendedor(venta).toLowerCase().includes(q)) return true;
    return (venta.detalles || []).some(d =>
      (d.productoSku || '').toLowerCase().includes(q)
      || (d.productoNombre || '').toLowerCase().includes(q)
      || (d.trazabilidad || '').toLowerCase().includes(q)
    );
  }

  function ventasFiltradas() {
    let list = [...state.ventas];

    if (!esAdmin) {
      list = list.filter(v => v.vendedorId === app.usuario.id);
    }

    if (esAdmin && state.estado) {
      list = list.filter(v => v.estado === state.estado);
    }

    if (state.busqueda.trim()) {
      const term = state.busqueda.trim();
      list = list.filter(v => coincideBusqueda(v, term));
    }

    return list.sort((a, b) => new Date(b.fecha) - new Date(a.fecha));
  }

  function resumenPorVendedor(filtradas) {
    const map = new Map();
    filtradas
      .filter(v => v.estado !== 'Anulada')
      .forEach(v => {
        const nombre = formatearVendedor(v);
        const prev = map.get(nombre) || { vendedor: nombre, cantidad: 0, total: 0 };
        prev.cantidad += 1;
        prev.total += v.total || 0;
        map.set(nombre, prev);
      });

    return [...map.values()].sort((a, b) => b.total - a.total || a.vendedor.localeCompare(b.vendedor));
  }

  function puedeEliminar(venta) {
    return esAdmin || venta.vendedorId === app.usuario.id;
  }

  function renderDetalleRow(venta, colSpan, visible) {
    const montosCols = puedeVerMontos
      ? '<th>Precio USD</th><th>Total USD</th>'
      : '';
    const detalleRows = (venta.detalles || []).map(d => {
      const montosCells = puedeVerMontos
        ? `<td>${app.formatUsd(d.precioUnitario)}</td><td>${app.formatUsd(d.total)}</td>`
        : '';
      return `<tr>
        <td><strong>${app.display(d.productoNombre)}</strong><small>${app.display(d.productoSku)}</small></td>
        <td>${app.display(d.categoria)}</td>
        <td>${app.display(d.trazabilidad)}</td>
        <td>${d.cantidad}</td>
        ${montosCells}
      </tr>`;
    }).join('');

    const obs = venta.observaciones
      ? `<p><strong>Observaciones:</strong> ${venta.observaciones}</p>`
      : '';

    return `<tr class="sale-detail-row" data-detail-for="${venta.id}" ${visible ? '' : 'hidden'}>
      <td colspan="${colSpan}">
        <div class="sale-detail">
          ${obs}
          <table>
            <thead>
              <tr>
                <th>Producto</th>
                <th>Categoría</th>
                <th>Trazabilidad</th>
                <th>Cant.</th>
                ${montosCols}
              </tr>
            </thead>
            <tbody>${detalleRows}</tbody>
          </table>
        </div>
      </td>
    </tr>`;
  }

  function renderAdminCards(filtradas) {
    const totalVendido = filtradas
      .filter(v => v.estado !== 'Anulada')
      .reduce((sum, v) => sum + (v.total || 0), 0);
    const productosVendidos = filtradas
      .flatMap(v => v.detalles || [])
      .reduce((sum, d) => sum + (d.cantidad || 0), 0);
    const conTrazabilidad = filtradas
      .flatMap(v => v.detalles || [])
      .filter(tieneTrazabilidad).length;

    return `<section class="grid cards">
      <div class="card"><span class="metric-label">Ventas filtradas</span><strong>${filtradas.length}</strong></div>
      <div class="card"><span class="metric-label">Total vendido</span><strong>${app.formatUsd(totalVendido)}</strong></div>
      <div class="card"><span class="metric-label">Productos vendidos</span><strong>${productosVendidos}</strong></div>
      <div class="card warning-card"><span class="metric-label">Con trazabilidad</span><strong>${conTrazabilidad}</strong></div>
    </section>`;
  }

  function renderResumenVendedor(filtradas) {
    const resumen = resumenPorVendedor(filtradas);
    if (!resumen.length) {
      return `<section class="panel">
        <div class="section-header"><div><h2>Totales por vendedor</h2><p>Resumen del período y filtros aplicados (ventas no anuladas).</p></div></div>
        <p>No hay ventas para mostrar en el resumen por vendedor.</p>
      </section>`;
    }

    const rows = resumen.map(r => `<tr>
      <td><strong>${r.vendedor}</strong></td>
      <td>${r.cantidad}</td>
      <td>${app.formatUsd(r.total)}</td>
    </tr>`).join('');
    const totalCant = resumen.reduce((s, r) => s + r.cantidad, 0);
    const totalUsd = resumen.reduce((s, r) => s + r.total, 0);

    return `<section class="panel">
      <div class="section-header">
        <div>
          <h2>Totales por vendedor</h2>
          <p>Resumen del período y filtros aplicados (ventas no anuladas).</p>
        </div>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr><th>Vendedor</th><th>Cantidad de ventas</th><th>Total vendido USD</th></tr>
          </thead>
          <tbody>${rows}</tbody>
          <tfoot>
            <tr>
              <th>Total general</th>
              <th>${totalCant}</th>
              <th>${app.formatUsd(totalUsd)}</th>
            </tr>
          </tfoot>
        </table>
      </div>
    </section>`;
  }

  function renderTabla(filtradas) {
    const colSpan = puedeVerMontos ? 7 : 6;
    const totalHeader = puedeVerMontos ? '<th>Total</th>' : '';

    if (!filtradas.length) {
      return `<section class="panel">
        <div class="section-header">
          <div><h2>Registro de ventas</h2><p>Cliente, vendedor y detalle por serie o lote.</p></div>
        </div>
        <p>No hay ventas para mostrar.</p>
      </section>`;
    }

    const rows = filtradas.map(venta => {
      const expanded = state.expanded.has(venta.id);
      const totalCell = puedeVerMontos ? `<td>${app.formatUsd(venta.total)}</td>` : '';
      const deleteBtn = puedeEliminar(venta)
        ? `<button type="button" class="button btn-delete" data-delete="${venta.id}">Borrar</button>`
        : '';
      const toggleBtn = `<button type="button" class="button ghost" data-toggle="${venta.id}" aria-expanded="${expanded}">
        ${expanded ? 'Ocultar' : 'Detalle'}
      </button>`;

      return `<tr>
        <td>${app.formatDateTime(venta.fecha)}</td>
        <td><strong>${numeroVenta(venta)}</strong></td>
        <td>${app.display(venta.cliente?.nombreRazonSocial)}<small>${app.display(venta.cliente?.dniCuit)}</small></td>
        <td>${formatearVendedor(venta)}</td>
        <td><span class="${estadoBadgeClass(venta.estado)}">${venta.estado}</span></td>
        ${totalCell}
        <td class="row-actions">${toggleBtn}${deleteBtn}</td>
      </tr>
      ${renderDetalleRow(venta, colSpan, expanded)}`;
    }).join('');

    return `<section class="panel">
      <div class="section-header">
        <div><h2>Registro de ventas</h2><p>Cliente, vendedor y detalle por serie o lote.</p></div>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Nº venta</th>
              <th>Cliente</th>
              <th>Vendedor</th>
              <th>Estado</th>
              ${totalHeader}
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>${rows}</tbody>
        </table>
      </div>
    </section>`;
  }

  function renderFilters() {
    const adminFilters = esAdmin ? `
      <label>
        Desde
        <input type="date" class="input" id="filtro-desde" value="${state.desde}" />
      </label>
      <label>
        Hasta
        <input type="date" class="input" id="filtro-hasta" value="${state.hasta}" />
      </label>` : '';

    const estadoFilter = esAdmin ? `
      <label>
        Estado
        <select class="input" id="filtro-estado">
          <option value="">Todos</option>
          ${ESTADOS.map(e => `<option value="${e}"${state.estado === e ? ' selected' : ''}>${e}</option>`).join('')}
        </select>
      </label>` : '';

    const hint = !esAdmin
      ? '<p class="hint">Mostrando sus ventas recientes...</p>'
      : '';

    return `<div class="section-header">
      <div>
        <p>${esAdmin ? 'Consulte ventas, totales y trazabilidad.' : 'Consulte el registro de ventas y trazabilidad.'}</p>
      </div>
      <a class="button primary" href="/ventas/nueva">Nueva venta</a>
    </div>
    <section class="panel">
      <div class="grid sales-filters">
        ${adminFilters}
        <label>
          Buscar
          <input type="search" class="input" id="filtro-busqueda" value="${state.busqueda}" placeholder="Cliente, vendedor, número, producto, serie o lote..." />
        </label>
        ${estadoFilter}
        <button type="button" class="button" id="btn-actualizar" ${state.cargando ? 'disabled' : ''}>
          ${state.cargando ? 'Cargando...' : 'Actualizar'}
        </button>
      </div>
      ${hint}
    </section>`;
  }

  function renderResultados(filtradas = ventasFiltradas()) {
    if (state.cargando && !state.ventas.length) {
      return '<p>Cargando ventas...</p>';
    }

    let html = '';
    if (esAdmin) {
      html += renderAdminCards(filtradas);
      html += renderResumenVendedor(filtradas);
    }
    html += renderTabla(filtradas);
    return html;
  }

  function render() {
    container.innerHTML = `${renderFilters()}<div id="ventas-resultados">${renderResultados()}</div>`;
    bindFilterEvents();
    bindResultEvents();
  }

  function renderResultadosOnly() {
    const resultados = document.getElementById('ventas-resultados');
    if (!resultados) {
      render();
      return;
    }
    resultados.innerHTML = renderResultados();
    bindResultEvents();
  }

  function bindFilterEvents() {
    document.getElementById('btn-actualizar')?.addEventListener('click', cargarVentas);

    document.getElementById('filtro-busqueda')?.addEventListener('input', (e) => {
      state.busqueda = e.target.value;
      renderResultadosOnly();
    });

    document.getElementById('filtro-estado')?.addEventListener('change', (e) => {
      state.estado = e.target.value;
      renderResultadosOnly();
    });

    document.getElementById('filtro-desde')?.addEventListener('change', (e) => {
      state.desde = e.target.value;
    });

    document.getElementById('filtro-hasta')?.addEventListener('change', (e) => {
      state.hasta = e.target.value;
    });
  }

  function bindResultEvents() {
    const resultados = document.getElementById('ventas-resultados') || container;

    resultados.querySelectorAll('[data-toggle]').forEach(btn => {
      btn.addEventListener('click', () => {
        const id = Number(btn.dataset.toggle);
        if (state.expanded.has(id)) {
          state.expanded.delete(id);
        } else {
          state.expanded.add(id);
        }
        renderResultadosOnly();
      });
    });

    resultados.querySelectorAll('[data-delete]').forEach(btn => {
      btn.addEventListener('click', () => eliminarVenta(Number(btn.dataset.delete)));
    });
  }

  async function cargarVentas() {
    if (esAdmin && state.desde && state.hasta && state.desde > state.hasta) {
      await dialogs.alert('La fecha desde no puede ser posterior a la fecha hasta.');
      return;
    }

    state.cargando = true;
    app.renderAlerts(alertsEl, {});
    render();

    try {
      let url = '/api/ventas';
      if (esAdmin && state.desde && state.hasta) {
        url += `?desde=${encodeURIComponent(state.desde)}&hasta=${encodeURIComponent(state.hasta)}`;
      }
      state.ventas = await api.get(url);
    } catch (err) {
      state.ventas = [];
      app.renderAlerts(alertsEl, { error: err.message });
    } finally {
      state.cargando = false;
      render();
    }
  }

  async function eliminarVenta(id) {
    const venta = state.ventas.find(v => v.id === id);
    if (!venta || !puedeEliminar(venta)) return;

    const referencia = numeroVenta(venta);
    const cliente = app.display(venta.cliente?.nombreRazonSocial);
    const ok = await dialogs.ask(
      `¿Eliminar la venta ${referencia} de ${cliente}? Se revertirá el stock asociado.`,
      { title: 'Confirmar', confirmText: 'Eliminar' }
    );
    if (!ok) return;

    try {
      await api.delete(`/api/ventas/${id}`);
      state.expanded.delete(id);
      await dialogs.alert(`Venta ${referencia} eliminada correctamente.`, { kind: 'success', title: 'Listo' });
      await cargarVentas();
    } catch (err) {
      const msg = err.body?.errors?.join?.('\n') || err.message;
      await dialogs.alert(msg);
    }
  }

  await cargarVentas();
});
