document.addEventListener('DOMContentLoaded', async () => {
  const user = await app.initShell({ activePath: '/clientes', title: 'Clientes' });
  if (!user) return;

  const alertsEl = document.getElementById('page-alerts');
  const contentEl = document.getElementById('page-content');

  const state = {
    clientes: [],
    form: createEmptyForm(),
    busqueda: '',
    guardando: false
  };

  function createEmptyForm() {
    return {
      id: null,
      nombreRazonSocial: '',
      dniCuit: '',
      email: '',
      telefono: '',
      domicilio: '',
      tieneClu: false,
      numeroLegajoClu: '',
      fechaEmisionClu: '',
      fechaVencimientoClu: ''
    };
  }

  function formatContacto(cliente) {
    const datos = [cliente.telefono, cliente.email, cliente.domicilio]
      .filter((x) => x && String(x).trim());
    return datos.length ? datos.join(' / ') : '-';
  }

  function formatClu(clu) {
    if (!clu || !clu.numeroLegajo || !String(clu.numeroLegajo).trim()) return '-';
    if (!clu.fechaVencimiento) return clu.numeroLegajo;
    const estado = clu.estaVigente ? 'vigente' : 'vencida';
    return `${clu.numeroLegajo} - ${estado} hasta ${app.formatDate(clu.fechaVencimiento)}`;
  }

  function filteredClientes() {
    const q = state.busqueda.trim().toLowerCase();
    let list = [...state.clientes];
    if (q) {
      list = list.filter((c) =>
        (c.nombreRazonSocial || '').toLowerCase().includes(q)
        || (c.dniCuit || '').toLowerCase().includes(q)
        || (c.email || '').toLowerCase().includes(q)
        || (c.telefono || '').toLowerCase().includes(q)
        || (c.domicilio || '').toLowerCase().includes(q)
      );
    }
    return list.sort((a, b) =>
      (a.nombreRazonSocial || '').localeCompare(b.nombreRazonSocial || '', 'es')
    );
  }

  function renderMetrics() {
    const total = state.clientes.length;
    const conClu = state.clientes.filter((c) => c.credencialClu).length;
    const vencidas = state.clientes.filter((c) => c.credencialClu && !c.credencialClu.estaVigente).length;
    return `<section class="grid cards">
      <div class="card">
        <span class="metric-label">Total clientes</span>
        <strong>${total}</strong>
      </div>
      <div class="card">
        <span class="metric-label">Con CLU cargada</span>
        <strong>${conClu}</strong>
      </div>
      <div class="card warning-card">
        <span class="metric-label">CLU vencidas</span>
        <strong>${vencidas}</strong>
      </div>
    </section>`;
  }

  function renderFormHeader() {
    if (state.form.id) {
      return `<span class="step-badge edit">✎</span><span>Editar cliente #${state.form.id}</span>`;
    }
    return '<span class="step-badge">+</span><span>Agregar cliente</span>';
  }

  function renderCluFields() {
    if (!state.form.tieneClu) return '';
    return `<div class="grid clu-form">
      <label>
        Número de legajo
        <input type="text" class="input" id="numeroLegajoClu" value="${escapeAttr(state.form.numeroLegajoClu)}" />
      </label>
      <label>
        Fecha de emisión
        <input type="date" class="input" id="fechaEmisionClu" value="${escapeAttr(state.form.fechaEmisionClu)}" />
      </label>
      <label>
        Fecha de vencimiento
        <input type="date" class="input" id="fechaVencimientoClu" value="${escapeAttr(state.form.fechaVencimientoClu)}" />
      </label>
    </div>`;
  }

  function renderForm() {
    const editing = state.form.id !== null;
    return `<section class="panel${editing ? ' panel-editing' : ''}" id="edit-form">
      <div class="section-header">
        <div>
          <h2>${renderFormHeader()}</h2>
          <p>Administre datos de contacto, estado y credencial CLU para luego vincularlos a ventas.</p>
        </div>
        ${editing ? '<button type="button" class="button ghost" id="btn-nuevo-cliente">Nuevo cliente</button>' : ''}
      </div>
      <form id="cliente-form">
        <div class="grid client-form">
          <label>
            Nombre / razón social
            <input type="text" class="input" id="nombreRazonSocial" value="${escapeAttr(state.form.nombreRazonSocial)}" required />
          </label>
          <label>
            DNI/CUIT
            <input type="text" class="input" id="dniCuit" value="${escapeAttr(state.form.dniCuit)}" />
          </label>
          <label>
            Email
            <input type="email" class="input" id="email" value="${escapeAttr(state.form.email)}" />
          </label>
          <label>
            Teléfono / celular
            <input type="text" class="input" id="telefono" value="${escapeAttr(state.form.telefono)}" />
          </label>
          <label>
            Domicilio
            <input type="text" class="input" id="domicilio" value="${escapeAttr(state.form.domicilio)}" />
          </label>
        </div>
        <section class="subpanel">
          <label class="checkbox-label">
            <input type="checkbox" id="tieneClu"${state.form.tieneClu ? ' checked' : ''} />
            Cargar credencial CLU
          </label>
          ${renderCluFields()}
        </section>
        <div class="actions">
          <button type="submit" class="button primary" id="btn-guardar"${state.guardando ? ' disabled' : ''}>
            ${state.guardando ? 'Guardando...' : 'Guardar cliente'}
          </button>
          <button type="button" class="button ghost" id="btn-limpiar">Limpiar</button>
        </div>
      </form>
    </section>`;
  }

  function renderTableRows() {
    const clientes = filteredClientes();
    if (!clientes.length) {
      return '<tr><td colspan="5">No hay clientes para mostrar.</td></tr>';
    }
    return clientes.map((c) => `<tr data-id="${c.id}">
      <td>
        <strong>${app.display(c.nombreRazonSocial)}</strong>
        <small>${app.display(c.dniCuit)}</small>
      </td>
      <td>${formatContacto(c)}</td>
      <td>${formatClu(c.credencialClu)}</td>
      <td>
        <span>${c.cantidadVentas} venta(s)</span>
        <small>${c.cantidadArmas} arma(s) registradas</small>
      </td>
      <td>
        <div class="row-actions">
          <button type="button" class="button btn-edit" data-action="edit">Editar</button>
          <button type="button" class="button btn-delete" data-action="delete">Borrar</button>
        </div>
      </td>
    </tr>`).join('');
  }

  function renderTable() {
    return `<section class="panel">
      <div class="section-header">
        <div>
          <h2>Listado de clientes</h2>
          <p>Busque clientes y consulte ventas vinculadas.</p>
        </div>
      </div>
      <div class="grid client-filters">
        <label>
          Buscar
          <input type="text" class="input" id="busqueda" placeholder="Nombre, DNI/CUIT, email, teléfono..." value="${escapeAttr(state.busqueda)}" />
        </label>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Cliente</th>
              <th>Contacto</th>
              <th>CLU</th>
              <th>Vinculaciones</th>
              <th></th>
            </tr>
          </thead>
          <tbody id="clientes-tbody">${renderTableRows()}</tbody>
        </table>
      </div>
    </section>`;
  }

  function render() {
    contentEl.innerHTML = renderMetrics() + renderForm() + renderTable();
    bindEvents();
  }

  function renderTableBody() {
    const tbody = contentEl.querySelector('#clientes-tbody');
    if (tbody) tbody.innerHTML = renderTableRows();
  }

  function escapeAttr(value) {
    return String(value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/</g, '&lt;');
  }

  function readFormFromDom() {
    const fechaEmision = document.getElementById('fechaEmisionClu')?.value || '';
    const fechaVencimiento = document.getElementById('fechaVencimientoClu')?.value || '';
    return {
      id: state.form.id,
      nombreRazonSocial: document.getElementById('nombreRazonSocial')?.value || '',
      dniCuit: document.getElementById('dniCuit')?.value || '',
      email: document.getElementById('email')?.value || '',
      telefono: document.getElementById('telefono')?.value || '',
      domicilio: document.getElementById('domicilio')?.value || '',
      tieneClu: document.getElementById('tieneClu')?.checked || false,
      numeroLegajoClu: document.getElementById('numeroLegajoClu')?.value || '',
      fechaEmisionClu: fechaEmision,
      fechaVencimientoClu: fechaVencimiento
    };
  }

  function syncFormFromDom() {
    state.form = readFormFromDom();
  }

  function toApiDate(value) {
    const trimmed = (value || '').trim();
    return trimmed || null;
  }

  function buildPayload(form) {
    return {
      id: form.id,
      nombreRazonSocial: form.nombreRazonSocial,
      dniCuit: form.dniCuit,
      email: form.email,
      telefono: form.telefono,
      domicilio: form.domicilio,
      tieneClu: form.tieneClu,
      numeroLegajoClu: form.numeroLegajoClu,
      fechaEmisionClu: form.tieneClu ? toApiDate(form.fechaEmisionClu) : null,
      fechaVencimientoClu: form.tieneClu ? toApiDate(form.fechaVencimientoClu) : null
    };
  }

  function setFormFromCliente(cliente) {
    state.form = {
      id: cliente.id,
      nombreRazonSocial: cliente.nombreRazonSocial || '',
      dniCuit: cliente.dniCuit || '',
      email: cliente.email || '',
      telefono: cliente.telefono || '',
      domicilio: cliente.domicilio || '',
      tieneClu: !!cliente.credencialClu,
      numeroLegajoClu: cliente.credencialClu?.numeroLegajo || '',
      fechaEmisionClu: app.toInputDate(cliente.credencialClu?.fechaEmision),
      fechaVencimientoClu: app.toInputDate(cliente.credencialClu?.fechaVencimiento)
    };
  }

  function nuevoCliente() {
    state.form = createEmptyForm();
    app.renderAlerts(alertsEl, {});
    render();
  }

  async function editarCliente(cliente, scroll) {
    app.renderAlerts(alertsEl, {});
    setFormFromCliente(cliente);
    render();
    if (scroll) stockSanti.scrollToElement('edit-form');
  }

  async function guardarCliente(e) {
    e.preventDefault();
    if (state.guardando) return;

    syncFormFromDom();
    const esNuevo = state.form.id === null;
    state.guardando = true;
    renderFormSection();

    try {
      const saved = await api.post('/api/clientes', buildPayload(state.form));
      await loadClientes(false);
      const cliente = state.clientes.find((c) => c.id === saved.id) || saved;
      setFormFromCliente(cliente);
      state.guardando = false;
      app.renderAlerts(alertsEl, {
        success: esNuevo ? 'Cliente agregado correctamente.' : 'Cliente actualizado correctamente.'
      });
      render();
    } catch (err) {
      state.guardando = false;
      const errors = err.body?.errors || null;
      app.renderAlerts(alertsEl, errors ? { errors } : { error: err.message });
      renderFormSection();
    }
  }

  async function borrarCliente(cliente) {
    const confirmado = await dialogs.ask(
      `¿Eliminar al cliente ${app.display(cliente.nombreRazonSocial)}? Esta acción no se puede deshacer.`,
      { confirmText: 'Eliminar' }
    );
    if (!confirmado) return;

    try {
      await api.delete(`/api/clientes/${cliente.id}`);
      if (state.form.id === cliente.id) {
        state.form = createEmptyForm();
      }
      await loadClientes(false);
      app.renderAlerts(alertsEl, { success: 'Cliente eliminado correctamente.' });
      render();
    } catch (err) {
      await dialogs.alert(err.message);
    }
  }

  function renderFormSection() {
    const metrics = contentEl.querySelector('.grid.cards');
    const formHtml = renderForm();
    const existingForm = contentEl.querySelector('#edit-form');
    if (existingForm) {
      existingForm.outerHTML = formHtml;
      bindFormEvents();
    } else if (metrics) {
      metrics.insertAdjacentHTML('afterend', formHtml);
      bindFormEvents();
    } else {
      render();
    }
  }

  function bindFormEvents() {
    document.getElementById('cliente-form')?.addEventListener('submit', guardarCliente);
    document.getElementById('btn-limpiar')?.addEventListener('click', nuevoCliente);
    document.getElementById('btn-nuevo-cliente')?.addEventListener('click', nuevoCliente);
    document.getElementById('tieneClu')?.addEventListener('change', () => {
      syncFormFromDom();
      renderFormSection();
    });
  }

  function bindEvents() {
    bindFormEvents();

    document.getElementById('busqueda')?.addEventListener('input', (e) => {
      state.busqueda = e.target.value;
      renderTableBody();
    });

    contentEl.querySelector('#clientes-tbody')?.addEventListener('click', async (e) => {
      const btn = e.target.closest('button[data-action]');
      if (!btn) return;
      const row = btn.closest('tr[data-id]');
      if (!row) return;
      const id = Number(row.dataset.id);
      const cliente = state.clientes.find((c) => c.id === id);
      if (!cliente) return;

      if (btn.dataset.action === 'edit') {
        await editarCliente(cliente, true);
      } else if (btn.dataset.action === 'delete') {
        await borrarCliente(cliente);
      }
    });
  }

  async function loadClientes(showLoading = true) {
    if (showLoading) {
      contentEl.innerHTML = '<p>Cargando clientes...</p>';
    }
    state.clientes = await api.get('/api/clientes');
  }

  try {
    await loadClientes();
    render();
  } catch (err) {
    contentEl.innerHTML = '';
    app.renderAlerts(alertsEl, { error: err.message });
  }
});
