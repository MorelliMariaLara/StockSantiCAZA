document.addEventListener('DOMContentLoaded', async () => {
  const user = await app.initShell({ activePath: '/usuarios', title: 'Usuarios', requireAdmin: true });
  if (!user) return;

  const contentEl = document.getElementById('page-content');
  const alertsEl = document.getElementById('page-alerts');

  const ROLES = [
    { value: 'Administrador', label: 'Administrador' },
    { value: 'Vendedor', label: 'Vendedor' }
  ];

  const state = {
    usuarios: [],
    form: createEmptyForm(),
    guardando: false
  };

  function createEmptyForm() {
    return {
      id: null,
      nombre: '',
      login: '',
      password: '',
      rol: 'Vendedor'
    };
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

  function renderFormHeader() {
    if (state.form.id) {
      return `<span class="step-badge edit">✎</span><span>Editar usuario #${state.form.id}</span>`;
    }
    return '<span class="step-badge">+</span><span>Nuevo usuario</span>';
  }

  function renderRolOptions() {
    return ROLES.map((r) =>
      `<option value="${escapeAttr(r.value)}"${state.form.rol === r.value ? ' selected' : ''}>${escapeHtml(r.label)}</option>`
    ).join('');
  }

  function renderForm() {
    const editing = state.form.id !== null;
    return `<section class="panel${editing ? ' panel-editing' : ''}" id="edit-form">
      <div class="section-header">
        <div>
          <h2>${renderFormHeader()}</h2>
          <p>Administre perfiles de acceso y roles del sistema.</p>
        </div>
        ${editing ? '<button type="button" class="button ghost" id="btn-nuevo-usuario">Nuevo usuario</button>' : ''}
      </div>
      <form id="usuario-form">
        <div class="grid client-form">
          <label>
            Nombre
            <input type="text" class="input" id="nombre" value="${escapeAttr(state.form.nombre)}" required />
          </label>
          <label>
            Usuario de acceso
            <input type="text" class="input" id="login" value="${escapeAttr(state.form.login)}" required />
          </label>
          <label>
            Contraseña${editing ? ' (opcional)' : ''}
            <input type="password" class="input" id="password" value="${escapeAttr(state.form.password)}"${editing ? '' : ' required'} />
          </label>
          <label>
            Rol
            <select class="input" id="rol">${renderRolOptions()}</select>
          </label>
        </div>
        <div class="actions">
          <button type="submit" class="button primary" id="btn-guardar"${state.guardando ? ' disabled' : ''}>
            ${state.guardando ? 'Guardando...' : 'Guardar usuario'}
          </button>
          <button type="button" class="button ghost" id="btn-limpiar">Limpiar</button>
        </div>
      </form>
    </section>`;
  }

  function renderTableRows() {
    if (!state.usuarios.length) {
      return '<tr><td colspan="4">No hay usuarios registrados.</td></tr>';
    }

    return state.usuarios.map((u) => `<tr data-id="${u.id}">
      <td>${escapeHtml(app.display(u.nombre))}</td>
      <td>${escapeHtml(app.display(u.login))}</td>
      <td>${escapeHtml(app.display(u.rol))}</td>
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
          <h2>Perfiles registrados</h2>
          <p>Consulte y administre los usuarios del sistema.</p>
        </div>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Nombre</th>
              <th>Login</th>
              <th>Rol</th>
              <th></th>
            </tr>
          </thead>
          <tbody id="usuarios-tbody">${renderTableRows()}</tbody>
        </table>
      </div>
    </section>`;
  }

  function render() {
    contentEl.innerHTML = renderForm() + renderTable();
    bindEvents();
  }

  function readFormFromDom() {
    return {
      id: state.form.id,
      nombre: document.getElementById('nombre')?.value || '',
      login: document.getElementById('login')?.value || '',
      password: document.getElementById('password')?.value || '',
      rol: document.getElementById('rol')?.value || 'Vendedor'
    };
  }

  function syncFormFromDom() {
    state.form = readFormFromDom();
  }

  function buildPayload(form) {
    const payload = {
      id: form.id,
      nombre: form.nombre,
      login: form.login,
      rol: form.rol
    };
    if (form.password && form.password.trim()) {
      payload.password = form.password;
    }
    return payload;
  }

  function setFormFromUsuario(usuario) {
    state.form = {
      id: usuario.id,
      nombre: usuario.nombre || '',
      login: usuario.login || '',
      password: '',
      rol: usuario.rol || 'Vendedor'
    };
  }

  function nuevoUsuario() {
    state.form = createEmptyForm();
    app.renderAlerts(alertsEl, {});
    render();
  }

  async function editarUsuario(usuario, scroll) {
    app.renderAlerts(alertsEl, {});
    setFormFromUsuario(usuario);
    render();
    if (scroll) stockSanti.scrollToElement('edit-form');
  }

  async function guardarUsuario(e) {
    e.preventDefault();
    if (state.guardando) return;

    syncFormFromDom();
    const esNuevo = state.form.id === null;
    const idGuardado = state.form.id;
    state.guardando = true;
    renderFormSection();

    try {
      await api.post('/api/usuarios', buildPayload(state.form));
      await loadUsuarios(false);

      if (esNuevo) {
        state.form = createEmptyForm();
        app.renderAlerts(alertsEl, { success: 'Usuario creado correctamente.' });
      } else {
        const editado = state.usuarios.find((u) => u.id === idGuardado);
        if (editado) setFormFromUsuario(editado);
        app.renderAlerts(alertsEl, { success: 'Usuario actualizado correctamente.' });
      }

      state.guardando = false;
      render();
    } catch (err) {
      state.guardando = false;
      const errors = err.body?.errors || null;
      app.renderAlerts(alertsEl, errors ? { errors } : { error: err.message });
      renderFormSection();
    }
  }

  async function borrarUsuario(usuario) {
    const confirmado = await dialogs.ask(
      `¿Eliminar al usuario ${app.display(usuario.nombre)} (${app.display(usuario.login)})? Esta acción no se puede deshacer.`,
      { confirmText: 'Eliminar' }
    );
    if (!confirmado) return;

    try {
      await api.delete(`/api/usuarios/${usuario.id}`);
      if (state.form.id === usuario.id) {
        state.form = createEmptyForm();
      }
      await loadUsuarios(false);
      app.renderAlerts(alertsEl, { success: 'Usuario eliminado correctamente.' });
      render();
    } catch (err) {
      await dialogs.alert(err.message);
    }
  }

  function renderFormSection() {
    const existingForm = contentEl.querySelector('#edit-form');
    const formHtml = renderForm();
    if (existingForm) {
      existingForm.outerHTML = formHtml;
      bindFormEvents();
    } else {
      render();
    }
  }

  function bindFormEvents() {
    document.getElementById('usuario-form')?.addEventListener('submit', guardarUsuario);
    document.getElementById('btn-limpiar')?.addEventListener('click', nuevoUsuario);
    document.getElementById('btn-nuevo-usuario')?.addEventListener('click', nuevoUsuario);
  }

  function bindEvents() {
    bindFormEvents();

    contentEl.querySelector('#usuarios-tbody')?.addEventListener('click', async (e) => {
      const btn = e.target.closest('button[data-action]');
      if (!btn) return;
      const row = btn.closest('tr[data-id]');
      if (!row) return;
      const id = Number(row.dataset.id);
      const usuario = state.usuarios.find((u) => u.id === id);
      if (!usuario) return;

      if (btn.dataset.action === 'edit') {
        await editarUsuario(usuario, true);
      } else if (btn.dataset.action === 'delete') {
        await borrarUsuario(usuario);
      }
    });
  }

  async function loadUsuarios(showLoading = true) {
    if (showLoading) {
      contentEl.innerHTML = '<p>Cargando usuarios...</p>';
    }
    state.usuarios = await api.get('/api/usuarios');
  }

  try {
    await loadUsuarios();
    render();
  } catch (err) {
    contentEl.innerHTML = '';
    app.renderAlerts(alertsEl, { error: err.message });
  }
});
