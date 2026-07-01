document.addEventListener('DOMContentLoaded', async () => {
  const user = await app.initShell({ activePath: '/proveedores', title: 'Proveedores', modulo: 'proveedores' });
  if (!user) return;

  const alertsEl = document.getElementById('page-alerts');
  const contentEl = document.getElementById('page-content');

  const state = {
    proveedores: [],
    form: createEmptyForm(),
    formDeuda: createEmptyMovimientoForm(),
    formPago: createEmptyMovimientoForm(),
    proveedorSeleccionadoId: null,
    busqueda: '',
    guardando: false,
    guardandoMovimiento: false
  };

  function createEmptyForm() {
    return {
      id: null,
      nombreRazonSocial: '',
      telefono: '',
      email: '',
      domicilio: '',
      observaciones: '',
      deudaInicial: ''
    };
  }

  function createEmptyMovimientoForm() {
    return {
      fecha: todayInputDate(),
      monto: '',
      observaciones: ''
    };
  }

  function todayInputDate() {
    return new Date().toISOString().slice(0, 10);
  }

  function escapeAttr(value) {
    return String(value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/</g, '&lt;');
  }

  function proveedorSeleccionado() {
    if (state.proveedorSeleccionadoId === null) return null;
    return state.proveedores.find((p) => p.id === state.proveedorSeleccionadoId) || null;
  }

  function saldoProveedor(proveedor) {
    return Number(proveedor?.saldo ?? 0);
  }

  function saldoTotalAdeudado() {
    return state.proveedores.reduce((sum, p) => sum + saldoProveedor(p), 0);
  }

  function conSaldoPendiente() {
    return state.proveedores.filter((p) => saldoProveedor(p) > 0).length;
  }

  function filteredProveedores() {
    const q = state.busqueda.trim().toLowerCase();
    let list = [...state.proveedores];
    if (q) {
      list = list.filter((p) =>
        (p.nombreRazonSocial || '').toLowerCase().includes(q)
        || (p.telefono || '').toLowerCase().includes(q)
        || (p.email || '').toLowerCase().includes(q)
        || (p.domicilio || '').toLowerCase().includes(q)
      );
    }
    return list.sort((a, b) => {
      const saldoDiff = saldoProveedor(b) - saldoProveedor(a);
      if (saldoDiff !== 0) return saldoDiff;
      return (a.nombreRazonSocial || '').localeCompare(b.nombreRazonSocial || '', 'es');
    });
  }

  function movimientosOrdenados(proveedor) {
    return [...(proveedor?.movimientos || [])].sort((a, b) => {
      const fa = new Date(a.fecha).getTime();
      const fb = new Date(b.fecha).getTime();
      if (fa !== fb) return fa - fb;
      return (a.id || 0) - (b.id || 0);
    });
  }

  function renderMetrics() {
    return `<section class="grid cards">
      <div class="card danger-card">
        <span class="metric-label">Saldo total adeudado</span>
        <strong>${app.formatUsd(saldoTotalAdeudado())}</strong>
      </div>
      <div class="card">
        <span class="metric-label">Total proveedores</span>
        <strong>${state.proveedores.length}</strong>
      </div>
      <div class="card warning-card">
        <span class="metric-label">Con saldo pendiente</span>
        <strong>${conSaldoPendiente()}</strong>
      </div>
    </section>`;
  }

  function renderFormHeader() {
    if (state.form.id) {
      return `<span class="step-badge edit">✎</span><span>Editar proveedor #${state.form.id}</span>`;
    }
    return '<span class="step-badge">+</span><span>Agregar proveedor</span>';
  }

  function renderDeudaInicialField() {
    if (state.form.id) return '';
    return `<label>
      Deuda inicial USD
      <input type="number" class="input" id="deudaInicial" min="0" step="0.01" value="${escapeAttr(state.form.deudaInicial)}" />
    </label>`;
  }

  function renderForm() {
    const editing = state.form.id !== null;
    return `<section class="panel${editing ? ' panel-editing' : ''}" id="edit-form">
      <div class="section-header">
        <div>
          <h2>${renderFormHeader()}</h2>
          <p>Datos de contacto y deuda inicial opcional al crear.</p>
        </div>
        ${editing ? '<button type="button" class="button ghost" id="btn-nuevo-proveedor">Nuevo proveedor</button>' : ''}
      </div>
      <form id="proveedor-form">
        <div class="grid client-form">
          <label>
            Nombre / razón social *
            <input type="text" class="input" id="nombreRazonSocial" value="${escapeAttr(state.form.nombreRazonSocial)}" required />
          </label>
          <label>
            Teléfono
            <input type="text" class="input" id="telefono" value="${escapeAttr(state.form.telefono)}" />
          </label>
          <label>
            Email
            <input type="email" class="input" id="email" value="${escapeAttr(state.form.email)}" />
          </label>
          <label>
            Domicilio
            <input type="text" class="input" id="domicilio" value="${escapeAttr(state.form.domicilio)}" />
          </label>
          <label>
            Observaciones
            <input type="text" class="input" id="observaciones" value="${escapeAttr(state.form.observaciones)}" />
          </label>
          ${renderDeudaInicialField()}
        </div>
        <div class="actions">
          <button type="submit" class="button primary" id="btn-guardar"${state.guardando ? ' disabled' : ''}>
            ${state.guardando ? 'Guardando...' : 'Guardar proveedor'}
          </button>
          <button type="button" class="button ghost" id="btn-limpiar">Limpiar</button>
        </div>
      </form>
    </section>`;
  }

  function renderMovimientoFormFields(prefix, form) {
    return `<div class="grid client-form">
      <label>
        Fecha
        <input type="date" class="input" id="${prefix}-fecha" value="${escapeAttr(form.fecha)}" required />
      </label>
      <label>
        Monto USD *
        <input type="number" class="input" id="${prefix}-monto" min="0.01" step="0.01" value="${escapeAttr(form.monto)}" required />
      </label>
      <label>
        Observaciones
        <input type="text" class="input" id="${prefix}-observaciones" placeholder="Opcional" value="${escapeAttr(form.observaciones)}" />
      </label>
    </div>`;
  }

  function renderMovimientosRows(proveedor) {
    let saldoAcumulado = 0;
    const movimientos = movimientosOrdenados(proveedor);
    if (!movimientos.length) {
      return '<tr><td colspan="5">Sin movimientos registrados.</td></tr>';
    }
    return movimientos.map((m) => {
      const esPago = m.tipo === 'Pago';
      saldoAcumulado += esPago ? -Number(m.monto) : Number(m.monto);
      return `<tr>
        <td>${app.formatDate(m.fecha)}</td>
        <td><span class="badge ${esPago ? 'ok' : 'warning'}">${esPago ? 'Pago' : 'Deuda'}</span></td>
        <td>${app.formatUsd(m.monto)}</td>
        <td>${app.display(m.observaciones)}</td>
        <td>${app.formatUsd(saldoAcumulado)}</td>
      </tr>`;
    }).join('');
  }

  function renderCuentaPanel() {
    const proveedor = proveedorSeleccionado();
    if (!proveedor) return '';

    const saldo = saldoProveedor(proveedor);
    const saldoClass = saldo > 0 ? ' saldo-pendiente' : '';

    return `<section class="panel panel-editing" id="proveedor-cuenta">
      <div class="section-header">
        <div>
          <h2>Cuenta: ${app.display(proveedor.nombreRazonSocial)}</h2>
          <p>Saldo pendiente: <strong class="${saldoClass.trim()}">${app.formatUsd(saldo)}</strong></p>
        </div>
        <button type="button" class="button ghost" id="btn-cerrar-cuenta">Cerrar</button>
      </div>
      <div class="grid sale-header">
        <section class="subpanel">
          <h3>Registrar deuda</h3>
          <p class="hint">Aumenta el saldo adeudado al proveedor.</p>
          <form id="form-deuda">
            ${renderMovimientoFormFields('deuda', state.formDeuda)}
            <div class="actions">
              <button type="submit" class="button"${state.guardandoMovimiento ? ' disabled' : ''}>
                ${state.guardandoMovimiento ? 'Guardando...' : 'Registrar deuda'}
              </button>
            </div>
          </form>
        </section>
        <section class="subpanel">
          <h3>Registrar pago</h3>
          <p class="hint">Descuenta del saldo pendiente. Saldo actual: ${app.formatUsd(saldo)}</p>
          <form id="form-pago">
            ${renderMovimientoFormFields('pago', state.formPago)}
            <div class="actions">
              <button type="submit" class="button primary"${state.guardandoMovimiento ? ' disabled' : ''}>
                ${state.guardandoMovimiento ? 'Guardando...' : 'Registrar pago'}
              </button>
            </div>
          </form>
        </section>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Tipo</th>
              <th>Monto USD</th>
              <th>Observaciones</th>
              <th>Saldo acumulado</th>
            </tr>
          </thead>
          <tbody id="movimientos-tbody">${renderMovimientosRows(proveedor)}</tbody>
        </table>
      </div>
    </section>`;
  }

  function renderTableRows() {
    const proveedores = filteredProveedores();
    if (!proveedores.length) {
      return '<tr><td colspan="4">No hay proveedores para mostrar.</td></tr>';
    }
    return proveedores.map((p) => {
      const saldo = saldoProveedor(p);
      const selected = state.proveedorSeleccionadoId === p.id;
      const saldoClass = saldo > 0 ? 'saldo-pendiente' : '';
      const contacto = (p.telefono || p.email)
        ? `<div>${app.display(p.telefono)}</div><small>${app.display(p.email)}</small>`
        : '-';
      const obs = p.observaciones && String(p.observaciones).trim()
        ? `<small>${app.display(p.observaciones)}</small>`
        : '';
      return `<tr data-id="${p.id}" class="${selected ? 'panel-editing' : ''}">
        <td>
          <strong>${app.display(p.nombreRazonSocial)}</strong>
          <small>${app.display(p.telefono)}</small>
          ${obs}
        </td>
        <td>${contacto}</td>
        <td><strong class="${saldoClass}">${app.formatUsd(saldo)}</strong></td>
        <td>
          <div class="row-actions">
            <button type="button" class="button btn-edit" data-action="edit">Editar</button>
            <button type="button" class="button ghost" data-action="cuenta">Cuenta</button>
            <button type="button" class="button btn-delete" data-action="delete">Borrar</button>
          </div>
        </td>
      </tr>`;
    }).join('');
  }

  function renderTable() {
    return `<section class="panel">
      <div class="section-header">
        <div>
          <h2>Proveedores registrados</h2>
          <p>Saldo = deudas registradas − pagos realizados.</p>
        </div>
        <label>
          Buscar
          <input type="text" class="input" id="busqueda" placeholder="Nombre, teléfono o email..." value="${escapeAttr(state.busqueda)}" />
        </label>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Proveedor</th>
              <th>Contacto</th>
              <th>Saldo pendiente</th>
              <th></th>
            </tr>
          </thead>
          <tbody id="proveedores-tbody">${renderTableRows()}</tbody>
        </table>
      </div>
    </section>`;
  }

  function render() {
    contentEl.innerHTML = renderMetrics() + renderForm() + renderCuentaPanel() + renderTable();
    bindEvents();
  }

  function renderTableBody() {
    const tbody = contentEl.querySelector('#proveedores-tbody');
    if (tbody) tbody.innerHTML = renderTableRows();
  }

  function renderMetricsSection() {
    const metrics = contentEl.querySelector('.grid.cards');
    if (metrics) metrics.outerHTML = renderMetrics();
  }

  function renderCuentaSection() {
    const existing = contentEl.querySelector('#proveedor-cuenta');
    const html = renderCuentaPanel();
    if (existing) {
      if (html) existing.outerHTML = html;
      else existing.remove();
    } else if (html) {
      const form = contentEl.querySelector('#edit-form');
      if (form) form.insertAdjacentHTML('afterend', html);
      else contentEl.insertAdjacentHTML('beforeend', html);
    }
    bindCuentaEvents();
  }

  function renderFormSection() {
    const formHtml = renderForm();
    const existingForm = contentEl.querySelector('#edit-form');
    if (existingForm) {
      existingForm.outerHTML = formHtml;
      bindFormEvents();
    } else {
      render();
    }
  }

  function readFormFromDom() {
    return {
      id: state.form.id,
      nombreRazonSocial: document.getElementById('nombreRazonSocial')?.value || '',
      telefono: document.getElementById('telefono')?.value || '',
      email: document.getElementById('email')?.value || '',
      domicilio: document.getElementById('domicilio')?.value || '',
      observaciones: document.getElementById('observaciones')?.value || '',
      deudaInicial: document.getElementById('deudaInicial')?.value ?? state.form.deudaInicial
    };
  }

  function syncFormFromDom() {
    state.form = readFormFromDom();
  }

  function readMovimientoFromDom(prefix) {
    return {
      fecha: document.getElementById(`${prefix}-fecha`)?.value || todayInputDate(),
      monto: document.getElementById(`${prefix}-monto`)?.value || '',
      observaciones: document.getElementById(`${prefix}-observaciones`)?.value || ''
    };
  }

  function buildPayload(form) {
    const payload = {
      id: form.id,
      nombreRazonSocial: form.nombreRazonSocial,
      telefono: form.telefono,
      email: form.email,
      domicilio: form.domicilio,
      observaciones: form.observaciones
    };
    if (form.id === null) {
      payload.deudaInicial = form.deudaInicial === '' ? 0 : Number(form.deudaInicial);
    }
    return payload;
  }

  function buildMovimientoPayload(form, tipo) {
    return {
      tipo,
      fecha: form.fecha,
      monto: Number(form.monto),
      observaciones: form.observaciones
    };
  }

  function setFormFromProveedor(proveedor) {
    state.form = {
      id: proveedor.id,
      nombreRazonSocial: proveedor.nombreRazonSocial || '',
      telefono: proveedor.telefono || '',
      email: proveedor.email || '',
      domicilio: proveedor.domicilio || '',
      observaciones: proveedor.observaciones || '',
      deudaInicial: ''
    };
  }

  function nuevoProveedor() {
    state.form = createEmptyForm();
    app.renderAlerts(alertsEl, {});
    render();
  }

  async function editarProveedor(proveedor, scroll) {
    app.renderAlerts(alertsEl, {});
    setFormFromProveedor(proveedor);
    render();
    if (scroll) stockSanti.scrollToElement('edit-form');
  }

  async function abrirCuenta(proveedor) {
    app.renderAlerts(alertsEl, {});
    state.proveedorSeleccionadoId = proveedor.id;
    state.formDeuda = createEmptyMovimientoForm();
    state.formPago = createEmptyMovimientoForm();
    try {
      const movimientos = await api.get(`/api/proveedores/${proveedor.id}/movimientos`);
      const idx = state.proveedores.findIndex((p) => p.id === proveedor.id);
      if (idx >= 0) {
        state.proveedores[idx] = { ...state.proveedores[idx], movimientos };
      }
    } catch (err) {
      app.renderAlerts(alertsEl, { error: err.message });
      return;
    }
    renderCuentaSection();
    renderTableBody();
    stockSanti.scrollToElement('proveedor-cuenta');
  }

  function cerrarCuenta() {
    state.proveedorSeleccionadoId = null;
    state.formDeuda = createEmptyMovimientoForm();
    state.formPago = createEmptyMovimientoForm();
    renderCuentaSection();
    renderTableBody();
  }

  async function guardarProveedor(e) {
    e.preventDefault();
    if (state.guardando) return;

    syncFormFromDom();
    const esNuevo = state.form.id === null;

    if (esNuevo && state.form.deudaInicial !== '' && Number(state.form.deudaInicial) < 0) {
      app.renderAlerts(alertsEl, { errors: ['La deuda inicial no puede ser negativa.'] });
      return;
    }

    state.guardando = true;
    renderFormSection();

    try {
      const saved = await api.post('/api/proveedores', buildPayload(state.form));
      await loadProveedores(false);
      if (esNuevo) {
        state.form = createEmptyForm();
      } else {
        const proveedor = state.proveedores.find((p) => p.id === saved.id) || saved;
        setFormFromProveedor(proveedor);
      }
      state.guardando = false;
      app.renderAlerts(alertsEl, {
        success: esNuevo ? 'Proveedor creado correctamente.' : 'Proveedor actualizado correctamente.'
      });
      render();
    } catch (err) {
      state.guardando = false;
      const errors = err.body?.errors || null;
      app.renderAlerts(alertsEl, errors ? { errors } : { error: err.message });
      renderFormSection();
    }
  }

  async function registrarMovimiento(e, tipo) {
    e.preventDefault();
    if (state.guardandoMovimiento) return;

    const proveedor = proveedorSeleccionado();
    if (!proveedor) return;

    const prefix = tipo === 'Deuda' ? 'deuda' : 'pago';
    const form = readMovimientoFromDom(prefix);
    const monto = Number(form.monto);

    if (!monto || monto <= 0) {
      app.renderAlerts(alertsEl, { errors: ['El monto debe ser mayor a cero.'] });
      return;
    }

    if (tipo === 'Pago' && monto > saldoProveedor(proveedor)) {
      app.renderAlerts(alertsEl, {
        errors: [`El pago no puede superar el saldo pendiente (${app.formatUsd(saldoProveedor(proveedor))}).`]
      });
      return;
    }

    state.guardandoMovimiento = true;
    renderCuentaSection();

    try {
      const saved = await api.post(
        `/api/proveedores/${proveedor.id}/movimientos`,
        buildMovimientoPayload(form, tipo)
      );
      await loadProveedores(false);
      const idx = state.proveedores.findIndex((p) => p.id === saved.id);
      if (idx >= 0) {
        state.proveedores[idx] = saved;
      }
      state.proveedorSeleccionadoId = saved.id;
      state.formDeuda = createEmptyMovimientoForm();
      state.formPago = createEmptyMovimientoForm();
      state.guardandoMovimiento = false;
      app.renderAlerts(alertsEl, {
        success: tipo === 'Deuda' ? 'Deuda registrada correctamente.' : 'Pago registrado correctamente.'
      });
      renderMetricsSection();
      renderCuentaSection();
      renderTableBody();
    } catch (err) {
      state.guardandoMovimiento = false;
      const errors = err.body?.errors || null;
      app.renderAlerts(alertsEl, errors ? { errors } : { error: err.message });
      renderCuentaSection();
    }
  }

  async function borrarProveedor(proveedor) {
    const confirmado = await dialogs.ask(
      `¿Eliminar al proveedor ${app.display(proveedor.nombreRazonSocial)}? Se borrarán también sus deudas y pagos.`,
      { confirmText: 'Eliminar' }
    );
    if (!confirmado) return;

    try {
      await api.delete(`/api/proveedores/${proveedor.id}`);
      if (state.form.id === proveedor.id) {
        state.form = createEmptyForm();
      }
      if (state.proveedorSeleccionadoId === proveedor.id) {
        state.proveedorSeleccionadoId = null;
      }
      await loadProveedores(false);
      app.renderAlerts(alertsEl, { success: 'Proveedor eliminado correctamente.' });
      render();
    } catch (err) {
      await dialogs.alert(err.message);
    }
  }

  function bindFormEvents() {
    document.getElementById('proveedor-form')?.addEventListener('submit', guardarProveedor);
    document.getElementById('btn-limpiar')?.addEventListener('click', nuevoProveedor);
    document.getElementById('btn-nuevo-proveedor')?.addEventListener('click', nuevoProveedor);
  }

  function bindCuentaEvents() {
    document.getElementById('btn-cerrar-cuenta')?.addEventListener('click', cerrarCuenta);
    document.getElementById('form-deuda')?.addEventListener('submit', (e) => registrarMovimiento(e, 'Deuda'));
    document.getElementById('form-pago')?.addEventListener('submit', (e) => registrarMovimiento(e, 'Pago'));
  }

  function bindEvents() {
    bindFormEvents();
    bindCuentaEvents();

    document.getElementById('busqueda')?.addEventListener('input', (e) => {
      state.busqueda = e.target.value;
      renderTableBody();
    });

    contentEl.querySelector('#proveedores-tbody')?.addEventListener('click', async (e) => {
      const btn = e.target.closest('button[data-action]');
      if (!btn) return;
      const row = btn.closest('tr[data-id]');
      if (!row) return;
      const id = Number(row.dataset.id);
      const proveedor = state.proveedores.find((p) => p.id === id);
      if (!proveedor) return;

      if (btn.dataset.action === 'edit') {
        await editarProveedor(proveedor, true);
      } else if (btn.dataset.action === 'cuenta') {
        await abrirCuenta(proveedor);
      } else if (btn.dataset.action === 'delete') {
        await borrarProveedor(proveedor);
      }
    });
  }

  async function loadProveedores(showLoading = true) {
    if (showLoading) {
      contentEl.innerHTML = '<p>Cargando proveedores...</p>';
    }
    state.proveedores = await api.get('/api/proveedores');
    if (state.proveedorSeleccionadoId !== null) {
      const exists = state.proveedores.some((p) => p.id === state.proveedorSeleccionadoId);
      if (!exists) state.proveedorSeleccionadoId = null;
    }
  }

  try {
    await loadProveedores();
    render();
  } catch (err) {
    contentEl.innerHTML = '';
    app.renderAlerts(alertsEl, { error: err.message });
  }
});
