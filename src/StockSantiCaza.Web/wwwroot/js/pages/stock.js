document.addEventListener('DOMContentLoaded', async () => {
  const user = await app.initShell({ activePath: '/stock', title: 'Stock', modulo: 'stock' });
  if (!user) return;

  const container = document.getElementById('page-content');
  const alertsEl = document.getElementById('page-alerts');

  const state = {
    productos: [],
    categorias: [],
    puedeEditar: false,
    form: emptyForm(),
    filtros: { busqueda: '', categoria: '', estadoStock: '' },
    nuevaCategoria: { nombre: '', requiereSerie: false, requiereLote: false },
    archivoImportacion: null,
    resultadoImportacion: null,
    guardando: false,
    guardandoCategoria: false,
    importando: false,
    descargandoPlantilla: false
  };

  function emptyForm() {
    return {
      id: null,
      sku: '',
      nombre: '',
      descripcion: '',
      categoria: '',
      marca: '',
      modelo: '',
      calibre: '',
      precioUnitario: 0,
      stockActual: 0,
      stockMinimo: 1
    };
  }

  function escapeHtml(value) {
    return String(value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function formatearCaracteristicas(producto) {
    const partes = [producto.marca, producto.modelo, producto.calibre].filter(x => x && String(x).trim());
    return partes.length ? partes.join(' / ') : 'Sin características';
  }

  function obtenerClaseFila(producto) {
    if (producto.stockActual <= 0) return 'danger-row';
    if (producto.stockActual <= producto.stockMinimo) return 'warning-row';
    return '';
  }

  function productosFiltrados() {
    let query = [...state.productos];
    const { busqueda, categoria, estadoStock } = state.filtros;

    if (busqueda.trim()) {
      const term = busqueda.trim().toLowerCase();
      query = query.filter(p =>
        (p.sku || '').toLowerCase().includes(term) ||
        (p.nombre || '').toLowerCase().includes(term) ||
        (p.marca || '').toLowerCase().includes(term) ||
        (p.modelo || '').toLowerCase().includes(term) ||
        (p.calibre || '').toLowerCase().includes(term) ||
        (p.descripcion || '').toLowerCase().includes(term)
      );
    }

    if (categoria) {
      query = query.filter(p => (p.categoria || '').toLowerCase() === categoria.toLowerCase());
    }

    if (estadoStock === 'sin-stock') {
      query = query.filter(p => p.stockActual <= 0);
    } else if (estadoStock === 'minimo') {
      query = query.filter(p => p.stockActual > 0 && p.stockActual <= p.stockMinimo);
    } else if (estadoStock === 'disponible') {
      query = query.filter(p => p.stockActual > p.stockMinimo);
    }

    return query.sort((a, b) => {
      const na = (a.nombre || '').localeCompare(b.nombre || '', 'es');
      return na !== 0 ? na : (a.sku || '').localeCompare(b.sku || '', 'es');
    });
  }

  function metricas() {
    const productos = state.productos;
    return {
      total: productos.length,
      enMinimo: productos.filter(p => p.stockActual > 0 && p.stockActual <= p.stockMinimo).length,
      sinStock: productos.filter(p => p.stockActual <= 0).length,
      unidades: productos.reduce((sum, p) => sum + p.stockActual, 0)
    };
  }

  function categoriaOptions(selected = '') {
    return `<option value="">-</option>${state.categorias.map(c =>
      `<option value="${escapeHtml(c.nombre)}"${c.nombre === selected ? ' selected' : ''}>${escapeHtml(c.nombre)}</option>`
    ).join('')}`;
  }

  function renderMetricas() {
    const m = metricas();
    return `<section class="grid cards">
      <div class="card"><span class="metric-label">Productos en catálogo</span><strong>${m.total}</strong></div>
      <div class="card warning-card"><span class="metric-label">En stock mínimo</span><strong>${m.enMinimo}</strong></div>
      <div class="card danger-card"><span class="metric-label">Sin stock</span><strong>${m.sinStock}</strong></div>
      <div class="card"><span class="metric-label">Unidades totales</span><strong>${m.unidades}</strong></div>
    </section>`;
  }

  function renderReadOnlyAlert() {
    if (state.puedeEditar) return '';
    return `<div class="alert alert-success">
      <strong>Consulta de stock</strong> — Puede ver existencias y precios. La carga y edición de productos requiere permisos de stock.
    </div>`;
  }

  function renderProductForm() {
    if (!state.puedeEditar) return '';
    const f = state.form;
    const editing = f.id !== null;
    return `<section class="panel${editing ? ' panel-editing' : ''}" id="edit-form">
      <div class="section-header">
        <div>
          <h2>
            ${editing
              ? `<span class="step-badge edit">✎</span><span>Editar producto #${f.id}</span>`
              : `<span class="step-badge">+</span><span>Agregar producto</span>`}
          </h2>
          <p>Complete la clasificación, características y cantidad disponible.</p>
        </div>
        ${editing ? '<button type="button" class="button ghost" data-action="nuevo-producto">Nuevo producto</button>' : ''}
      </div>
      <form id="producto-form" class="stock-form-wrap">
        <div class="grid stock-form">
          <label>SKU<input class="input" name="sku" value="${escapeHtml(f.sku)}" /></label>
          <label>Nombre<input class="input" name="nombre" value="${escapeHtml(f.nombre)}" /></label>
          <label>Clasificación
            <select class="input" name="categoria">${categoriaOptions(f.categoria)}</select>
          </label>
          <label>Marca<input class="input" name="marca" value="${escapeHtml(f.marca)}" /></label>
          <label>Modelo<input class="input" name="modelo" value="${escapeHtml(f.modelo)}" /></label>
          <label>Calibre / medida<input class="input" name="calibre" value="${escapeHtml(f.calibre)}" /></label>
          <label>Precio (USD)<input class="input" type="number" step="0.01" min="0" name="precioUnitario" value="${f.precioUnitario}" /></label>
          <label>Stock actual<input class="input" type="number" step="1" min="0" name="stockActual" value="${f.stockActual}" /></label>
          <label>Stock mínimo<input class="input" type="number" step="1" min="0" name="stockMinimo" value="${f.stockMinimo}" /></label>
        </div>
        <label>Características / descripción<textarea class="input" name="descripcion" rows="3">${escapeHtml(f.descripcion)}</textarea></label>
        <div class="actions">
          <button type="submit" class="button primary" ${state.guardando ? 'disabled' : ''}>${state.guardando ? 'Guardando...' : 'Guardar producto'}</button>
          <button type="button" class="button ghost" data-action="nuevo-producto">Limpiar</button>
        </div>
      </form>
    </section>`;
  }

  function renderCategoriasSection() {
    if (!state.puedeEditar) return '';
    const nc = state.nuevaCategoria;
    const rows = state.categorias.length === 0
      ? '<p>No hay clasificaciones cargadas.</p>'
      : `<div class="table-wrap"><table>
          <thead><tr><th>Nombre</th><th>Serie</th><th>Lote</th><th></th></tr></thead>
          <tbody>${state.categorias.map(c => `<tr>
            <td>${escapeHtml(c.nombre)}</td>
            <td>${c.requiereSerie ? 'Sí' : '-'}</td>
            <td>${c.requiereLote ? 'Sí' : '-'}</td>
            <td><button type="button" class="button btn-delete" data-action="borrar-categoria" data-id="${c.id}">Borrar</button></td>
          </tr>`).join('')}
          </tbody></table></div>`;

    return `<section class="panel">
      <div class="section-header">
        <div>
          <h2>Clasificaciones de stock</h2>
          <p>Defina categorías como Arma, Miras, Munición, etc.</p>
        </div>
      </div>
      <div class="grid stock-form">
        <label>Nueva clasificación
          <input class="input" id="nueva-categoria-nombre" placeholder="Ej: Miras, Fundas, Repuestos..." value="${escapeHtml(nc.nombre)}" />
        </label>
        <label class="checkbox-label">
          <input type="checkbox" id="nueva-categoria-serie"${nc.requiereSerie ? ' checked' : ''} />
          Requiere número de serie
        </label>
        <label class="checkbox-label">
          <input type="checkbox" id="nueva-categoria-lote"${nc.requiereLote ? ' checked' : ''} />
          Requiere lote
        </label>
      </div>
      <div class="actions">
        <button type="button" class="button primary" id="btn-agregar-categoria" ${state.guardandoCategoria ? 'disabled' : ''}>
          ${state.guardandoCategoria ? 'Guardando...' : 'Agregar clasificación'}
        </button>
      </div>
      ${rows}
    </section>`;
  }

  function renderImportSection() {
    if (!state.puedeEditar) return '';
    const r = state.resultadoImportacion;
    const resultadoHtml = r ? `<div class="alert alert-success">
      <strong>Importación completada</strong>
      <p>Productos creados: ${r.creados}. Productos actualizados: ${r.actualizados}.</p>
      ${r.errores?.length ? `<ul>${r.errores.map(e => `<li>${escapeHtml(e)}</li>`).join('')}</ul>` : ''}
    </div>` : '';

    return `<section class="panel">
      <div class="section-header">
        <div>
          <h2>Importar desde Excel</h2>
          <p>Cargue o actualice productos masivamente desde un archivo .xlsx.</p>
        </div>
      </div>
      <div id="import-alerts"></div>
      ${resultadoHtml}
      <div class="grid stock-filters">
        <label>Archivo Excel (.xlsx)
          <input type="file" id="archivo-importacion" accept=".xlsx" />
          ${state.archivoImportacion ? `<small>${escapeHtml(state.archivoImportacion.name)}</small>` : ''}
        </label>
      </div>
      <div class="actions">
        <button type="button" class="button ghost" id="btn-descargar-plantilla" ${state.descargandoPlantilla ? 'disabled' : ''}>
          ${state.descargandoPlantilla ? 'Generando...' : 'Descargar plantilla'}
        </button>
        <button type="button" class="button primary" id="btn-importar" ${state.importando || !state.archivoImportacion ? 'disabled' : ''}>
          ${state.importando ? 'Importando...' : 'Importar'}
        </button>
      </div>
    </section>`;
  }

  function renderInventarioRows() {
    const filtrados = productosFiltrados();
    const rows = filtrados.map(p => {
      const acciones = state.puedeEditar
        ? `<div class="row-actions">
            <button type="button" class="button btn-edit" data-action="editar-producto" data-id="${p.id}">Editar</button>
            <button type="button" class="button btn-delete" data-action="borrar-producto" data-id="${p.id}">Borrar</button>
          </div>`
        : '';

      return `<tr class="${obtenerClaseFila(p)}">
        <td>${escapeHtml(app.display(p.sku))}</td>
        <td><strong>${escapeHtml(app.display(p.nombre))}</strong>${p.descripcion ? `<small>${escapeHtml(p.descripcion)}</small>` : ''}</td>
        <td>${escapeHtml(app.display(p.categoria))}</td>
        <td>${escapeHtml(formatearCaracteristicas(p))}</td>
        <td><strong>${p.stockActual}</strong><small>Mínimo ${p.stockMinimo}</small></td>
        <td>${app.formatUsd(p.precioUnitario)}</td>
        <td>${acciones}</td>
      </tr>`;
    }).join('');

    return rows || '<tr><td colspan="7">No hay productos que coincidan con los filtros.</td></tr>';
  }

  function renderInventario() {
    const filtroCategorias = `<option value="">Todas</option>${state.categorias.map(c =>
      `<option value="${escapeHtml(c.nombre)}"${state.filtros.categoria === c.nombre ? ' selected' : ''}>${escapeHtml(c.nombre)}</option>`
    ).join('')}`;

    return `<section class="panel" id="stock-inventario">
      <div class="section-header">
        <div>
          <h2>Existencias</h2>
          <p>Consulte cuánto hay disponible y qué productos faltan reponer.</p>
        </div>
      </div>
      <div class="grid stock-filters">
        <label>Buscar
          <input class="input" id="filtro-busqueda" placeholder="SKU, nombre, marca, modelo, calibre..." value="${escapeHtml(state.filtros.busqueda)}" />
        </label>
        <label>Clasificación
          <select class="input" id="filtro-categoria">${filtroCategorias}</select>
        </label>
        <label>Estado de stock
          <select class="input" id="filtro-estado">
            <option value=""${state.filtros.estadoStock === '' ? ' selected' : ''}>Todos</option>
            <option value="sin-stock"${state.filtros.estadoStock === 'sin-stock' ? ' selected' : ''}>Sin stock</option>
            <option value="minimo"${state.filtros.estadoStock === 'minimo' ? ' selected' : ''}>Stock mínimo</option>
            <option value="disponible"${state.filtros.estadoStock === 'disponible' ? ' selected' : ''}>Disponible</option>
          </select>
        </label>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>SKU</th><th>Producto</th><th>Clasificación</th><th>Características</th><th>Stock</th><th>Precio</th><th></th>
            </tr>
          </thead>
          <tbody id="stock-tbody">${renderInventarioRows()}</tbody>
        </table>
      </div>
    </section>`;
  }

  function render() {
    container.innerHTML = `
      ${renderReadOnlyAlert()}
      ${renderMetricas()}
      ${renderProductForm()}
      ${renderCategoriasSection()}
      ${renderImportSection()}
      ${renderInventario()}
    `;
    bindEvents();
  }

  function renderInventarioTableBody() {
    const tbody = document.getElementById('stock-tbody');
    if (!tbody) {
      render();
      return;
    }
    tbody.innerHTML = renderInventarioRows();
    bindTableEvents();
  }

  function renderMetricasOnly() {
    const cards = container.querySelector('.grid.cards');
    if (cards) {
      const temp = document.createElement('div');
      temp.innerHTML = renderMetricas();
      cards.replaceWith(temp.firstElementChild);
    }
  }

  function bindFilterEvents() {
    document.getElementById('filtro-busqueda')?.addEventListener('input', (e) => {
      state.filtros.busqueda = e.target.value;
      renderInventarioTableBody();
    });
    document.getElementById('filtro-categoria')?.addEventListener('change', (e) => {
      state.filtros.categoria = e.target.value;
      renderInventarioTableBody();
    });
    document.getElementById('filtro-estado')?.addEventListener('change', (e) => {
      state.filtros.estadoStock = e.target.value;
      renderInventarioTableBody();
    });
  }

  function bindTableEvents() {
    container.querySelectorAll('[data-action="editar-producto"]').forEach(btn => {
      btn.addEventListener('click', () => editarProducto(Number(btn.dataset.id)));
    });
    container.querySelectorAll('[data-action="borrar-producto"]').forEach(btn => {
      btn.addEventListener('click', () => borrarProducto(Number(btn.dataset.id)));
    });
  }

  function bindEvents() {
    document.getElementById('producto-form')?.addEventListener('submit', guardarProducto);

    container.querySelectorAll('[data-action="nuevo-producto"]').forEach(btn => {
      btn.addEventListener('click', () => {
        state.form = emptyForm();
        app.renderAlerts(alertsEl, {});
        render();
      });
    });

    document.getElementById('btn-agregar-categoria')?.addEventListener('click', agregarCategoria);
    container.querySelectorAll('[data-action="borrar-categoria"]').forEach(btn => {
      btn.addEventListener('click', () => borrarCategoria(Number(btn.dataset.id)));
    });

    document.getElementById('archivo-importacion')?.addEventListener('change', (e) => {
      state.archivoImportacion = e.target.files?.[0] || null;
      state.resultadoImportacion = null;
      render();
    });

    document.getElementById('btn-descargar-plantilla')?.addEventListener('click', descargarPlantilla);
    document.getElementById('btn-importar')?.addEventListener('click', importarExcel);

    bindFilterEvents();
    bindTableEvents();
  }

  async function cargarDatos() {
    const datos = await api.get('/api/stock');
    state.productos = datos.productos || [];
    state.categorias = datos.categorias || [];
    state.puedeEditar = !!datos.puedeEditar;
  }

  function leerFormularioProducto(formEl) {
    const data = new FormData(formEl);
    return {
      id: state.form.id,
      sku: data.get('sku') || '',
      nombre: data.get('nombre') || '',
      descripcion: data.get('descripcion') || '',
      categoria: data.get('categoria') || '',
      marca: data.get('marca') || '',
      modelo: data.get('modelo') || '',
      calibre: data.get('calibre') || '',
      precioUnitario: Number(data.get('precioUnitario')) || 0,
      stockActual: Math.max(0, parseInt(data.get('stockActual'), 10) || 0),
      stockMinimo: Math.max(0, parseInt(data.get('stockMinimo'), 10) || 0)
    };
  }

  async function guardarProducto(e) {
    e.preventDefault();
    const payload = leerFormularioProducto(e.target);
    state.guardando = true;
    render();

    try {
      const guardado = await api.post('/api/stock/productos', payload);
      await cargarDatos();

      if (payload.id === null) {
        state.form = emptyForm();
        app.renderAlerts(alertsEl, { success: 'Producto cargado correctamente.' });
      } else {
        state.form = {
          id: guardado.id,
          sku: guardado.sku || '',
          nombre: guardado.nombre || '',
          descripcion: guardado.descripcion || '',
          categoria: guardado.categoria || '',
          marca: guardado.marca || '',
          modelo: guardado.modelo || '',
          calibre: guardado.calibre || '',
          precioUnitario: guardado.precioUnitario,
          stockActual: guardado.stockActual,
          stockMinimo: guardado.stockMinimo
        };
        app.renderAlerts(alertsEl, { success: 'Producto actualizado correctamente.' });
      }
    } catch (err) {
      const errors = err.body?.errors || [err.message];
      app.renderAlerts(alertsEl, { errors });
    } finally {
      state.guardando = false;
      render();
    }
  }

  function editarProducto(id) {
    const producto = state.productos.find(p => p.id === id);
    if (!producto) return;

    state.form = {
      id: producto.id,
      sku: producto.sku || '',
      nombre: producto.nombre || '',
      descripcion: producto.descripcion || '',
      categoria: producto.categoria || '',
      marca: producto.marca || '',
      modelo: producto.modelo || '',
      calibre: producto.calibre || '',
      precioUnitario: producto.precioUnitario,
      stockActual: producto.stockActual,
      stockMinimo: producto.stockMinimo
    };
    app.renderAlerts(alertsEl, {});
    render();
    stockSanti.scrollToElement('edit-form');
  }

  async function borrarProducto(id) {
    const producto = state.productos.find(p => p.id === id);
    if (!producto) return;

    const confirmado = await dialogs.ask(
      `¿Eliminar el producto ${app.display(producto.nombre)} (${app.display(producto.sku)})? Esta acción no se puede deshacer.`,
      { confirmText: 'Eliminar' }
    );
    if (!confirmado) return;

    try {
      await api.delete(`/api/stock/productos/${id}`);
      if (state.form.id === id) state.form = emptyForm();
      await cargarDatos();
      app.renderAlerts(alertsEl, { success: 'Producto eliminado correctamente.' });
      render();
    } catch (err) {
      await dialogs.alert(err.message || 'No se pudo eliminar el producto.');
    }
  }

  async function agregarCategoria() {
    const nombre = document.getElementById('nueva-categoria-nombre')?.value?.trim() || '';
    const requiereSerie = document.getElementById('nueva-categoria-serie')?.checked || false;
    const requiereLote = document.getElementById('nueva-categoria-lote')?.checked || false;

    if (!nombre) return;

    state.nuevaCategoria = { nombre, requiereSerie, requiereLote };
    state.guardandoCategoria = true;
    render();

    try {
      await api.post('/api/stock/categorias', { nombre, requiereSerie, requiereLote });
      state.nuevaCategoria = { nombre: '', requiereSerie: false, requiereLote: false };
      await cargarDatos();
      app.renderAlerts(alertsEl, { success: 'Clasificación agregada correctamente.' });
    } catch (err) {
      await dialogs.alert(err.message || 'No se pudo agregar la clasificación.', { kind: 'warning' });
    } finally {
      state.guardandoCategoria = false;
      render();
    }
  }

  async function borrarCategoria(id) {
    const categoria = state.categorias.find(c => c.id === id);
    if (!categoria) return;

    const confirmado = await dialogs.ask(
      `¿Eliminar la clasificación ${categoria.nombre}?`,
      { confirmText: 'Eliminar' }
    );
    if (!confirmado) return;

    try {
      await api.delete(`/api/stock/categorias/${id}`);
      await cargarDatos();
      app.renderAlerts(alertsEl, { success: 'Clasificación eliminada correctamente.' });
      render();
    } catch (err) {
      await dialogs.alert(err.message || 'No se pudo eliminar la clasificación.');
    }
  }

  async function descargarPlantilla() {
    const importAlerts = document.getElementById('import-alerts');
    state.descargandoPlantilla = true;
    render();

    try {
      await api.download('/api/stock/plantilla', 'plantilla-stock.xlsx');
    } catch (err) {
      if (importAlerts) {
        app.renderAlerts(importAlerts, { error: `No se pudo generar la plantilla: ${err.message}` });
      }
    } finally {
      state.descargandoPlantilla = false;
      render();
    }
  }

  async function importarExcel() {
    if (!state.archivoImportacion) return;

    const importAlerts = document.getElementById('import-alerts');
    if (importAlerts) importAlerts.innerHTML = '';

    state.importando = true;
    render();

    try {
      const formData = new FormData();
      formData.append('archivo', state.archivoImportacion);
      state.resultadoImportacion = await api.upload('/api/stock/importar', formData);
      state.archivoImportacion = null;
      await cargarDatos();
      app.renderAlerts(alertsEl, { success: 'Importación completada.' });
    } catch (err) {
      if (importAlerts) {
        app.renderAlerts(importAlerts, { error: `No se pudo importar el archivo: ${err.message}` });
      }
    } finally {
      state.importando = false;
      render();
    }
  }

  try {
    await cargarDatos();
    render();
  } catch (err) {
    container.innerHTML = '';
    app.renderAlerts(alertsEl, { error: err.message });
  }
});
