document.addEventListener('DOMContentLoaded', async () => {
  const user = await app.initShell({ activePath: '/ventas/nueva', title: 'Nueva venta' });
  if (!user) return;

  const contentEl = document.getElementById('page-content');
  const alertsEl = document.getElementById('page-alerts');
  const esAdmin = app.usuario.esAdministrador;

  const state = {
    clientes: [],
    productos: [],
    vendedores: [],
    items: [],
    busquedaProducto: '',
    busquedaCliente: '',
    productoSeleccionadoId: 0,
    cantidad: 1,
    precioUnitario: 0,
    clienteIdSeleccionado: 0,
    vendedorIdSeleccionado: app.usuario.id,
    observaciones: '',
    mostrarFormularioCliente: false,
    clienteNuevo: emptyClienteNuevo(),
    procesando: false,
    procesandoCliente: false,
    mensajeExito: null
  };

  function emptyClienteNuevo() {
    return { nombre: '', dniCuit: '', telefono: '', domicilio: '' };
  }

  function escapeHtml(value) {
    return String(value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function escapeAttr(value) {
    return escapeHtml(value);
  }

  function formatearCliente(cliente) {
    const datos = [];
    if (!app.esDniInterno(cliente.dniCuit)) {
      datos.push(cliente.dniCuit);
    }
    if (cliente.telefono && String(cliente.telefono).trim()) {
      datos.push(`Cel. ${cliente.telefono}`);
    }
    const nombre = cliente.nombreRazonSocial || '';
    return datos.length === 0 ? nombre : `${nombre} - ${datos.join(' - ')}`;
  }

  function formatearProductoOption(producto) {
    return `${producto.sku} - ${producto.nombre} (${producto.stockActual}) - ${app.formatUsd(producto.precioUnitario)}`;
  }

  function productoById(id) {
    return state.productos.find((p) => p.id === id) || null;
  }

  function clienteById(id) {
    return state.clientes.find((c) => c.id === id) || null;
  }

  function productosFiltrados() {
    const q = state.busquedaProducto.trim().toLowerCase();
    let list = state.productos;

    if (q) {
      list = list.filter((p) =>
        (p.sku || '').toLowerCase().includes(q)
        || (p.nombre || '').toLowerCase().includes(q)
        || (p.marca || '').toLowerCase().includes(q)
        || (p.calibre || '').toLowerCase().includes(q)
      );
    }

    let filtrados = list.slice(0, 30);

    if (state.productoSeleccionadoId > 0 && !filtrados.some((p) => p.id === state.productoSeleccionadoId)) {
      const seleccionado = productoById(state.productoSeleccionadoId);
      if (seleccionado) filtrados = [seleccionado, ...filtrados];
    }

    return filtrados;
  }

  function clientesFiltrados() {
    const q = state.busquedaCliente.trim().toLowerCase();
    let list = [...state.clientes];

    if (q) {
      list = list.filter((c) =>
        (c.nombreRazonSocial || '').toLowerCase().includes(q)
        || (c.dniCuit || '').toLowerCase().includes(q)
        || (c.telefono || '').toLowerCase().includes(q)
        || (c.email || '').toLowerCase().includes(q)
      );
    }

    list.sort((a, b) => (a.nombreRazonSocial || '').localeCompare(b.nombreRazonSocial || '', 'es'));
    let filtrados = list.slice(0, 50);

    if (state.clienteIdSeleccionado > 0 && !filtrados.some((c) => c.id === state.clienteIdSeleccionado)) {
      const seleccionado = clienteById(state.clienteIdSeleccionado);
      if (seleccionado) filtrados = [seleccionado, ...filtrados];
    }

    return filtrados;
  }

  function itemSubtotal(item) {
    return Math.max(0, item.precioUnitario * item.cantidad - item.descuento);
  }

  function calcularTotal() {
    return state.items.reduce((sum, item) => sum + itemSubtotal(item), 0);
  }

  function tieneProductos() {
    return state.items.length > 0 || state.productoSeleccionadoId > 0;
  }

  function clienteListo() {
    return state.clienteIdSeleccionado > 0
      || (state.mostrarFormularioCliente && state.clienteNuevo.nombre.trim().length > 0);
  }

  function puedeConfirmarVenta() {
    return !state.procesando
      && tieneProductos()
      && clienteListo()
      && state.vendedorIdSeleccionado > 0;
  }

  function productoSeleccionado() {
    return state.productoSeleccionadoId > 0 ? productoById(state.productoSeleccionadoId) : null;
  }

  function clienteSeleccionado() {
    return state.clienteIdSeleccionado > 0 ? clienteById(state.clienteIdSeleccionado) : null;
  }

  function renderAlerts() {
    if (state.mensajeExito) {
      alertsEl.innerHTML = `<div class="alert alert-success venta-flash"><strong>${escapeHtml(state.mensajeExito)}</strong></div>`;
    } else {
      alertsEl.innerHTML = '';
    }
  }

  function renderProductoSelectOptions() {
    return productosFiltrados().map((p) =>
      `<option value="${p.id}"${p.id === state.productoSeleccionadoId ? ' selected' : ''}>${escapeHtml(formatearProductoOption(p))}</option>`
    ).join('');
  }

  function renderClienteSelectOptions() {
    return clientesFiltrados().map((c) =>
      `<option value="${c.id}"${c.id === state.clienteIdSeleccionado ? ' selected' : ''}>${escapeHtml(formatearCliente(c))}</option>`
    ).join('');
  }

  function renderStockAlert() {
    const producto = productoSeleccionado();
    if (!producto || producto.stockActual > producto.stockMinimo) return '';
    return `<div class="product-alerts">
      <span class="badge danger">Stock mínimo: ${producto.stockActual}/${producto.stockMinimo}</span>
    </div>`;
  }

  function renderDetalleTable() {
    if (!state.items.length) return '';
    const rows = state.items.map((item, index) => `<tr>
      <td>${escapeHtml(item.producto.nombre)}</td>
      <td>${item.cantidad}</td>
      <td>${app.formatUsd(itemSubtotal(item))}</td>
      <td><button type="button" class="link-button" data-action="quitar-item" data-index="${index}">Quitar</button></td>
    </tr>`).join('');

    return `<div class="table-wrap venta-detalle-mini">
      <table>
        <thead>
          <tr>
            <th>Producto</th>
            <th>Cant.</th>
            <th>Total USD</th>
            <th></th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
    </div>
    <div class="totals venta-total-inline">
      <strong>Total: ${app.formatUsd(calcularTotal())}</strong>
    </div>`;
  }

  function renderVendedorField() {
    if (esAdmin) {
      const options = state.vendedores.map((v) =>
        `<option value="${v.id}"${v.id === state.vendedorIdSeleccionado ? ' selected' : ''}>${escapeHtml(v.nombre)} (${escapeHtml(v.rol)})</option>`
      ).join('');
      return `<label>
        Vendedor
        <select class="input" id="vendedor-select">
          <option value="0">Seleccione...</option>
          ${options}
        </select>
      </label>`;
    }
    return `<label>
      Vendedor
      <input class="input" value="${escapeAttr(app.usuario.nombre)}" disabled />
    </label>`;
  }

  function renderClienteSeleccionadoAlert() {
    const cliente = clienteSeleccionado();
    if (!cliente) return '';
    let extra = '';
    if (!app.esDniInterno(cliente.dniCuit)) {
      extra = ` <span>— ${escapeHtml(cliente.dniCuit)}</span>`;
    }
    return `<div class="alert alert-success client-selected">
      <strong>${escapeHtml(cliente.nombreRazonSocial)}</strong>${extra}
    </div>`;
  }

  function renderFormularioCliente() {
    if (!state.mostrarFormularioCliente) return '';
    const cn = state.clienteNuevo;
    return `<div class="subpanel cliente-venta-nuevo">
      <div class="section-header">
        <h3>Alta rápida de cliente</h3>
        <button type="button" class="link-button" id="btn-cerrar-cliente">Cancelar</button>
      </div>
      <p class="hint">El DNI/CUIT es opcional. Sin documento se genera un identificador interno.</p>
      <div class="grid client-form">
        <label>
          Nombre / razón social *
          <input type="text" class="input" id="cliente-nuevo-nombre" value="${escapeAttr(cn.nombre)}" />
        </label>
        <label>
          DNI/CUIT
          <input type="text" class="input" id="cliente-nuevo-dni" value="${escapeAttr(cn.dniCuit)}" placeholder="Opcional" />
        </label>
        <label>
          Teléfono
          <input type="text" class="input" id="cliente-nuevo-telefono" value="${escapeAttr(cn.telefono)}" placeholder="Opcional" />
        </label>
        <label>
          Domicilio
          <input type="text" class="input" id="cliente-nuevo-domicilio" value="${escapeAttr(cn.domicilio)}" placeholder="Opcional" />
        </label>
      </div>
      <div class="actions">
        <button type="button" class="button" id="btn-guardar-cliente"${state.procesandoCliente ? ' disabled' : ''}>
          ${state.procesandoCliente ? 'Guardando...' : 'Guardar y seleccionar'}
        </button>
      </div>
    </div>`;
  }

  function render() {
    renderAlerts();

    contentEl.innerHTML = `
      <section class="panel venta-rapida step-panel" id="venta-rapida">
        <h2><span class="step-badge">1</span> Producto</h2>
        <div class="grid product-entry">
          <label>
            Buscar producto
            <input type="text" class="input" id="busqueda-producto" placeholder="SKU, nombre, marca, calibre..." value="${escapeAttr(state.busquedaProducto)}" />
          </label>
          <label>
            Producto
            <select class="input" id="producto-select">
              <option value="0">Seleccione...</option>
              ${renderProductoSelectOptions()}
            </select>
          </label>
          <label>
            Cantidad
            <input type="number" class="input" id="cantidad" min="1" value="${state.cantidad}" />
          </label>
          <label>
            Precio USD
            <input type="number" class="input" id="precio-unitario" min="0" step="0.01" value="${state.precioUnitario || ''}" />
          </label>
        </div>
        ${renderStockAlert()}
        <div class="actions">
          <button type="button" class="button" id="btn-agregar-item"${state.productoSeleccionadoId <= 0 ? ' disabled' : ''}>Agregar al detalle</button>
          <small class="hint">Si olvida agregar, el producto seleccionado se incluye al guardar.</small>
        </div>
        ${renderDetalleTable()}
      </section>

      <section class="panel step-panel">
        <div class="section-header">
          <div>
            <h2><span class="step-badge">2</span> Cliente</h2>
            <p>Busque o cree el cliente de la venta.</p>
          </div>
          <button type="button" class="button ghost" id="btn-nuevo-cliente">+ Nuevo cliente</button>
        </div>
        <div class="grid sale-header">
          <label>
            Buscar cliente
            <input type="text" class="input" id="busqueda-cliente" placeholder="Nombre, DNI/CUIT, teléfono..." value="${escapeAttr(state.busquedaCliente)}" />
          </label>
          <label>
            Cliente
            <select class="input" id="cliente-select">
              <option value="0">Seleccione...</option>
              ${renderClienteSelectOptions()}
            </select>
          </label>
          ${renderVendedorField()}
        </div>
        ${renderClienteSeleccionadoAlert()}
        ${renderFormularioCliente()}
      </section>

      <section class="panel venta-guardar step-panel" id="venta-guardar">
        <h2><span class="step-badge">3</span> Confirmar venta</h2>
        <div class="grid sale-header">
          <label>
            Observaciones (opcional)
            <input type="text" class="input" id="observaciones" placeholder="Opcional" value="${escapeAttr(state.observaciones)}" />
          </label>
        </div>
        <ul class="sale-checklist">
          <li class="${tieneProductos() ? 'ok' : 'pending'}">
            ${tieneProductos() ? '✓' : '○'} Producto en la venta
          </li>
          <li class="${clienteListo() ? 'ok' : 'pending'}">
            ${clienteListo() ? '✓' : '○'} Cliente seleccionado
          </li>
        </ul>
        <div class="venta-acciones-finales">
          <button type="button" class="button primary button-xl" id="btn-guardar-venta"${!puedeConfirmarVenta() ? ' disabled' : ''}>
            ${state.procesando ? 'Guardando...' : 'Guardar venta'}
          </button>
        </div>
        <p class="sale-hint">Tras guardar, el formulario se limpia automáticamente para la próxima venta.</p>
      </section>`;

    bindEvents();
  }

  function syncClienteNuevoFromDom() {
    if (!state.mostrarFormularioCliente) return;
    state.clienteNuevo = {
      nombre: document.getElementById('cliente-nuevo-nombre')?.value || '',
      dniCuit: document.getElementById('cliente-nuevo-dni')?.value || '',
      telefono: document.getElementById('cliente-nuevo-telefono')?.value || '',
      domicilio: document.getElementById('cliente-nuevo-domicilio')?.value || ''
    };
  }

  function updateProductoSelect() {
    const select = document.getElementById('producto-select');
    if (!select) return;
    const current = state.productoSeleccionadoId;
    select.innerHTML = `<option value="0">Seleccione...</option>${renderProductoSelectOptions()}`;
    select.value = String(current);
  }

  function updateClienteSelect() {
    const select = document.getElementById('cliente-select');
    if (!select) return;
    const current = state.clienteIdSeleccionado;
    select.innerHTML = `<option value="0">Seleccione...</option>${renderClienteSelectOptions()}`;
    select.value = String(current);
  }

  function updateStockAlert() {
    const panel = document.getElementById('venta-rapida');
    if (!panel) return;
    const grid = panel.querySelector('.grid.product-entry');
    const existing = panel.querySelector('.product-alerts');
    const html = renderStockAlert();
    if (existing) {
      if (html) existing.outerHTML = html;
      else existing.remove();
    } else if (html && grid) {
      grid.insertAdjacentHTML('afterend', html);
    }
  }

  function updateDetalleSection() {
    const panel = document.getElementById('venta-rapida');
    if (!panel) return;
    const actions = panel.querySelector('.actions');
    if (!actions) return;

    const existingTable = panel.querySelector('.venta-detalle-mini');
    const existingTotal = panel.querySelector('.venta-total-inline');
    existingTable?.remove();
    existingTotal?.remove();

    if (state.items.length) {
      actions.insertAdjacentHTML('afterend', renderDetalleTable());
    }
  }

  function updateChecklist() {
    const checklist = contentEl.querySelector('.sale-checklist');
    if (!checklist) return;
    checklist.innerHTML = `
      <li class="${tieneProductos() ? 'ok' : 'pending'}">
        ${tieneProductos() ? '✓' : '○'} Producto en la venta
      </li>
      <li class="${clienteListo() ? 'ok' : 'pending'}">
        ${clienteListo() ? '✓' : '○'} Cliente seleccionado
      </li>`;

    const btn = document.getElementById('btn-guardar-venta');
    if (btn) {
      btn.disabled = !puedeConfirmarVenta();
      btn.textContent = state.procesando ? 'Guardando...' : 'Guardar venta';
    }
  }

  function onProductoChanged(id) {
    state.productoSeleccionadoId = id;
    const producto = productoSeleccionado();
    if (producto) {
      state.precioUnitario = producto.precioUnitario;
      state.cantidad = Math.max(1, state.cantidad);
    } else {
      state.precioUnitario = 0;
    }
    updateStockAlert();
    const btn = document.getElementById('btn-agregar-item');
    if (btn) btn.disabled = state.productoSeleccionadoId <= 0;
    const precioInput = document.getElementById('precio-unitario');
    if (precioInput) precioInput.value = state.precioUnitario || '';
  }

  function onClienteChanged(id) {
    state.clienteIdSeleccionado = id;
    if (id > 0) {
      state.mostrarFormularioCliente = false;
      state.clienteNuevo = emptyClienteNuevo();
    }
    render();
  }

  function agregarItem() {
    state.mensajeExito = null;
    app.renderAlerts(alertsEl, {});

    if (state.productoSeleccionadoId <= 0) {
      app.renderAlerts(alertsEl, { errors: ['Seleccione un producto para agregar.'] });
      return;
    }

    const producto = productoById(state.productoSeleccionadoId);
    if (!producto) {
      app.renderAlerts(alertsEl, { errors: ['El producto seleccionado no está disponible.'] });
      return;
    }

    const cantidad = Number(document.getElementById('cantidad')?.value) || state.cantidad;
    const precioUnitario = Number(document.getElementById('precio-unitario')?.value) || state.precioUnitario;

    if (cantidad < 1) {
      app.renderAlerts(alertsEl, { errors: ['La cantidad debe ser al menos 1.'] });
      return;
    }
    if (precioUnitario <= 0) {
      app.renderAlerts(alertsEl, { errors: ['El precio debe ser mayor a cero.'] });
      return;
    }

    state.items.push({
      productoId: producto.id,
      producto,
      cantidad,
      precioUnitario,
      descuento: 0
    });

    state.productoSeleccionadoId = 0;
    state.cantidad = 1;
    state.precioUnitario = 0;
    state.busquedaProducto = '';
    render();
  }

  function agregarProductoPendienteSiCorresponde() {
    if (state.productoSeleccionadoId <= 0) {
      return state.items.length > 0;
    }

    const producto = productoById(state.productoSeleccionadoId);
    if (!producto) {
      app.renderAlerts(alertsEl, { errors: ['El producto seleccionado no está disponible.'] });
      return false;
    }

    const cantidad = Number(document.getElementById('cantidad')?.value) || state.cantidad;
    const precioUnitario = Number(document.getElementById('precio-unitario')?.value) || state.precioUnitario;

    if (cantidad < 1) {
      app.renderAlerts(alertsEl, { errors: ['La cantidad debe ser al menos 1.'] });
      return false;
    }
    if (precioUnitario <= 0) {
      app.renderAlerts(alertsEl, { errors: ['El precio debe ser mayor a cero.'] });
      return false;
    }

    const yaEsta = state.items.some((item) =>
      item.productoId === producto.id
      && item.cantidad === cantidad
      && item.precioUnitario === precioUnitario
      && item.descuento === 0
    );

    if (!yaEsta) {
      state.items.push({
        productoId: producto.id,
        producto,
        cantidad,
        precioUnitario,
        descuento: 0
      });
    }

    state.productoSeleccionadoId = 0;
    state.cantidad = 1;
    state.precioUnitario = 0;
    state.busquedaProducto = '';
    return true;
  }

  function buildVentaPayload() {
    const observaciones = (document.getElementById('observaciones')?.value || state.observaciones).trim();
    return {
      clienteId: state.clienteIdSeleccionado > 0 ? state.clienteIdSeleccionado : null,
      vendedorId: state.vendedorIdSeleccionado > 0 ? state.vendedorIdSeleccionado : null,
      descuentoGeneral: 0,
      observaciones: observaciones || null,
      items: state.items.map((item) => ({
        productoId: item.productoId,
        cantidad: item.cantidad,
        precioUnitario: item.precioUnitario,
        descuento: item.descuento
      }))
    };
  }

  async function recargarProductos() {
    const datos = await api.get('/api/ventas/datos-nueva');
    state.productos = datos.productos || [];
  }

  async function recargarClientes(clienteId) {
    const datos = await api.get('/api/ventas/datos-nueva');
    state.clientes = datos.clientes || [];
    if (clienteId) {
      state.clienteIdSeleccionado = clienteId;
    }
  }

  function reiniciarParaNuevaVenta() {
    state.items = [];
    state.observaciones = '';
    state.clienteIdSeleccionado = 0;
    state.busquedaCliente = '';
    state.busquedaProducto = '';
    state.productoSeleccionadoId = 0;
    state.cantidad = 1;
    state.precioUnitario = 0;
    state.mostrarFormularioCliente = false;
    state.clienteNuevo = emptyClienteNuevo();
    state.vendedorIdSeleccionado = app.usuario.id;
    state.procesando = false;
    render();
    stockSanti.scrollToElement('venta-rapida');
  }

  async function guardarNuevoCliente() {
    syncClienteNuevoFromDom();
    state.mensajeExito = null;
    app.renderAlerts(alertsEl, {});

    const nombre = state.clienteNuevo.nombre.trim();
    if (!nombre) {
      app.renderAlerts(alertsEl, { errors: ['Debe indicar el nombre del cliente.'] });
      return false;
    }

    state.procesandoCliente = true;
    render();

    try {
      const payload = {
        nombre,
        dniCuit: state.clienteNuevo.dniCuit.trim(),
        telefono: state.clienteNuevo.telefono.trim() || null,
        email: null,
        domicilio: state.clienteNuevo.domicilio.trim() || null
      };

      const guardado = await api.post('/api/clientes/rapido', payload);
      await recargarClientes(guardado.id);
      state.mostrarFormularioCliente = false;
      state.clienteNuevo = emptyClienteNuevo();
      state.procesandoCliente = false;
      render();
      return true;
    } catch (err) {
      state.procesandoCliente = false;
      const errors = err.body?.errors || null;
      app.renderAlerts(alertsEl, errors ? { errors } : { error: err.message });
      render();
      return false;
    }
  }

  async function confirmarVenta() {
    if (state.procesando || !puedeConfirmarVenta()) return;

    state.mensajeExito = null;
    app.renderAlerts(alertsEl, {});

    state.observaciones = document.getElementById('observaciones')?.value || '';

    if (state.clienteIdSeleccionado <= 0 && state.mostrarFormularioCliente) {
      syncClienteNuevoFromDom();
      if (!await guardarNuevoCliente()) return;
    }

    if (!agregarProductoPendienteSiCorresponde()) return;

    const payload = buildVentaPayload();

    if (!payload.clienteId) {
      app.renderAlerts(alertsEl, { errors: ['Debe seleccionar un cliente o crear uno nuevo.'] });
      return;
    }
    if (!payload.vendedorId) {
      app.renderAlerts(alertsEl, { errors: ['Debe seleccionar el vendedor.'] });
      return;
    }
    if (!payload.items.length) {
      app.renderAlerts(alertsEl, { errors: ['Debe agregar al menos un producto.'] });
      return;
    }

    state.procesando = true;
    updateChecklist();

    try {
      const resultado = await api.post('/api/ventas', payload);
      state.mensajeExito = `Venta ${resultado.numeroComprobante} registrada — ${app.formatUsd(resultado.total)}.`;
      await recargarProductos();
      reiniciarParaNuevaVenta();
      renderAlerts();
    } catch (err) {
      state.procesando = false;
      const errors = err.body?.errors || null;
      app.renderAlerts(alertsEl, errors ? { errors } : { error: err.message });
      updateChecklist();
    }
  }

  function bindEvents() {
    document.getElementById('busqueda-producto')?.addEventListener('input', (e) => {
      state.busquedaProducto = e.target.value;
      updateProductoSelect();
    });

    document.getElementById('producto-select')?.addEventListener('change', (e) => {
      onProductoChanged(Number(e.target.value));
    });

    document.getElementById('cantidad')?.addEventListener('input', (e) => {
      state.cantidad = Math.max(1, Number(e.target.value) || 1);
    });

    document.getElementById('precio-unitario')?.addEventListener('input', (e) => {
      state.precioUnitario = Number(e.target.value) || 0;
    });

    document.getElementById('btn-agregar-item')?.addEventListener('click', agregarItem);

    contentEl.querySelector('#venta-rapida')?.addEventListener('click', (e) => {
      const btn = e.target.closest('[data-action="quitar-item"]');
      if (!btn) return;
      const index = Number(btn.dataset.index);
      if (Number.isNaN(index)) return;
      state.items.splice(index, 1);
      updateDetalleSection();
      updateChecklist();
    });

    document.getElementById('busqueda-cliente')?.addEventListener('input', (e) => {
      state.busquedaCliente = e.target.value;
      updateClienteSelect();
    });

    document.getElementById('cliente-select')?.addEventListener('change', (e) => {
      onClienteChanged(Number(e.target.value));
    });

    document.getElementById('vendedor-select')?.addEventListener('change', (e) => {
      state.vendedorIdSeleccionado = Number(e.target.value);
      updateChecklist();
    });

    document.getElementById('btn-nuevo-cliente')?.addEventListener('click', () => {
      state.mostrarFormularioCliente = true;
      state.clienteIdSeleccionado = 0;
      state.mensajeExito = null;
      app.renderAlerts(alertsEl, {});
      render();
    });

    document.getElementById('btn-cerrar-cliente')?.addEventListener('click', () => {
      state.mostrarFormularioCliente = false;
      state.clienteNuevo = emptyClienteNuevo();
      render();
    });

    document.getElementById('btn-guardar-cliente')?.addEventListener('click', () => {
      guardarNuevoCliente();
    });

    ['cliente-nuevo-nombre', 'cliente-nuevo-dni', 'cliente-nuevo-telefono', 'cliente-nuevo-domicilio'].forEach((id) => {
      document.getElementById(id)?.addEventListener('input', () => {
        syncClienteNuevoFromDom();
        updateChecklist();
      });
    });

    document.getElementById('observaciones')?.addEventListener('input', (e) => {
      state.observaciones = e.target.value;
    });

    document.getElementById('btn-guardar-venta')?.addEventListener('click', confirmarVenta);
  }

  try {
    const datos = await api.get('/api/ventas/datos-nueva');
    state.clientes = datos.clientes || [];
    state.productos = datos.productos || [];
    state.vendedores = datos.vendedores || [];
    state.vendedorIdSeleccionado = app.usuario.id;
    render();
  } catch (err) {
    contentEl.innerHTML = '';
    app.renderAlerts(alertsEl, { error: err.message });
  }
});
