document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('login-form');
  const alerts = document.getElementById('login-alerts');
  const btn = document.getElementById('login-btn');

  async function verificarServidor() {
    try {
      const health = await fetch('/api/health', { credentials: 'same-origin', signal: AbortSignal.timeout(8000) });
      if (!health.ok) return;
      const info = await health.json();
      if (!info.tieneProductionJson && info.environment === 'Production') {
        app.renderAlerts(alerts, {
          info: 'Falta appsettings.Production.json en el servidor.'
        });
      }
    } catch {
      // En Ferozo la app puede tardar en arrancar; no bloquear el login.
    }
  }

  verificarServidor();

  app.loadUser().then((user) => {
    if (user) {
      window.location.href = app.homePath(user);
    }
  }).catch(() => {
    // Sin sesión: el formulario ya está listo para usar.
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    alerts.innerHTML = '';
    btn.disabled = true;
    btn.textContent = 'Ingresando...';

    try {
      const user = await api.post('/api/auth/login', {
        login: document.getElementById('login').value,
        password: document.getElementById('password').value
      }, { timeoutMs: 30000 });
      window.location.href = app.homePath(user);
    } catch (err) {
      let message = err.message || 'Usuario o contraseña incorrectos.';
      if (err.name === 'AbortError' || message.includes('tiempo')) {
        message = 'La base de datos no respondió. Abrí /api/health/sql-probe para diagnosticar.';
      }
      if (err.body?.ayuda) {
        message += ' ' + err.body.ayuda;
      }
      app.renderAlerts(alerts, { error: message });
    } finally {
      btn.disabled = false;
      btn.textContent = 'Ingresar';
    }
  });
});
