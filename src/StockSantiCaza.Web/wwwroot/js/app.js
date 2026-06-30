const app = {
  usuario: null,

  formatUsd(amount) {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount || 0);
  },

  display(value, fallback = '-') {
    return value && String(value).trim() ? value : fallback;
  },

  formatDate(value) {
    if (!value) return '-';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('es-AR');
  },

  formatDateTime(value) {
    if (!value) return '-';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString('es-AR', {
      day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
    });
  },

  toInputDate(value) {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    return date.toISOString().slice(0, 10);
  },

  esDniInterno(dni) {
    return dni && /^S\d+/.test(dni);
  },

  renderAlerts(container, { errors = [], success = null, error = null, info = null } = {}) {
    if (!container) return;
    const items = [...errors];
    if (error) items.push(error);
    let html = '';
    if (info) {
      html += `<div class="alert alert-info">${info}</div>`;
    }
    if (items.length) {
      html += `<div class="alert alert-danger"><strong>No se pudo completar la operación</strong><ul>${items.map(e => `<li>${e}</li>`).join('')}</ul></div>`;
    }
    if (success) {
      html += `<div class="alert alert-success">${success}</div>`;
    }
    container.innerHTML = html;
  },

  async loadUser() {
    if (!window.api) {
      this.usuario = null;
      return null;
    }
    try {
      this.usuario = await api.get('/api/auth/me');
      return this.usuario;
    } catch (err) {
      this.usuario = null;
      if (err.status && err.status !== 401) {
        console.error('Error al verificar sesión:', err.message);
      }
      return null;
    }
  },

  async requireAuth({ admin = false, modulo = null, redirectLogin = true } = {}) {
    const user = await this.loadUser();
    if (!user) {
      if (redirectLogin) window.location.href = '/login';
      return null;
    }
    if (admin && !user.esAdministrador) {
      await dialogs.alert('No tiene permisos para acceder a esta sección. Solo administradores.');
      window.location.href = user.esAdministrador ? '/inicio' : '/ventas/nueva';
      return null;
    }
    return user;
  },

  renderNav(activePath) {
    const user = this.usuario;
    if (!user) return '';

    const items = [];
    if (user.esAdministrador) {
      items.push({ href: '/inicio', label: 'Dashboard', path: '/inicio' });
    }
    items.push(
      { href: '/ventas/nueva', label: 'Nueva venta', path: '/ventas/nueva' },
      { href: '/ventas', label: 'Historial ventas', path: '/ventas' },
      { href: '/clientes', label: 'Clientes', path: '/clientes' },
      { href: '/stock', label: 'Stock', path: '/stock' }
    );
    if (user.esAdministrador || user.rol === 'Vendedor') {
      items.push({ href: '/proveedores', label: 'Proveedores', path: '/proveedores' });
    }
    if (user.esAdministrador) {
      items.push(
        { href: '/reportes', label: 'Reportes', path: '/reportes' },
        { href: '/usuarios', label: 'Usuarios', path: '/usuarios' }
      );
    }

    const navLinks = items.map(item => {
      const active = activePath === item.path || (item.path !== '/' && activePath.startsWith(item.path));
      return `<a href="${item.href}"${active ? ' class="active"' : ''}><span class="nav-icon">◆</span> ${item.label}</a>`;
    }).join('');

    return `
      <div class="brand">
        <strong>StockSantiCAZA</strong>
        <small>Armería · Control trazable</small>
      </div>
      <div class="user-chip">
        <strong>${user.nombre}</strong>
        <small>${user.esAdministrador ? 'Administrador' : 'Vendedor'}</small>
      </div>
      <nav class="nav-menu">${navLinks}</nav>
      <button type="button" class="button ghost logout-button" id="logout-btn">Cerrar sesión</button>
    `;
  },

  initShell({ activePath, title, requireAdmin = false } = {}) {
    const shell = document.getElementById('app-shell');
    if (!shell) return Promise.resolve(null);

    return this.requireAuth({ admin: requireAdmin }).then(user => {
      if (!user) return null;

      const sidebar = document.getElementById('app-sidebar');
      if (sidebar) sidebar.innerHTML = this.renderNav(activePath);

      document.getElementById('logout-btn')?.addEventListener('click', async () => {
        await api.post('/api/auth/logout');
        window.location.href = '/login';
      });

      const toggle = document.getElementById('menu-toggle');
      const backdrop = document.getElementById('nav-backdrop');
      const closeMenu = () => {
        shell.classList.remove('nav-open');
        toggle?.setAttribute('aria-expanded', 'false');
        toggle.textContent = '☰';
      };
      toggle?.addEventListener('click', () => {
        const open = shell.classList.toggle('nav-open');
        toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        toggle.textContent = open ? '✕' : '☰';
      });
      backdrop?.addEventListener('click', closeMenu);
      shell.querySelectorAll('.nav-menu a').forEach(link => {
        link.addEventListener('click', closeMenu);
      });

      if (title) document.title = `${title} - StockSantiCAZA`;
      return user;
    });
  }
};

window.app = app;
