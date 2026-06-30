document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('login-form');
  const alerts = document.getElementById('login-alerts');
  const btn = document.getElementById('login-btn');

  async function diagnosticarServidor() {
    btn.disabled = true;
    app.renderAlerts(alerts, { info: 'Comprobando servidor...' });

    try {
      await api.get('/api/health', { timeoutMs: 10000 });
    } catch (err) {
      app.renderAlerts(alerts, {
        error: 'El backend .NET no responde. Subí el publish completo a Ferozo (incluye StockSantiCaza.Web.exe, web.config y todas las .dll). Probá en el navegador: /api/health'
      });
      btn.disabled = false;
      return false;
    }

    try {
      const db = await api.get('/api/health/db', { timeoutMs: 15000 });
      if (db.database !== 'connected') {
        throw new Error(db.error || 'Base de datos no disponible');
      }
    } catch (err) {
      app.renderAlerts(alerts, {
        error: `La aplicación corre pero la base SQL falla: ${err.message || err}. Revisá appsettings.Production.json (servidor sql2016, base w400048_santicazarmeria).`
      });
      btn.disabled = false;
      return false;
    }

    alerts.innerHTML = '';
    btn.disabled = false;
    return true;
  }

  diagnosticarServidor().then((ok) => {
    if (!ok) return;

    return app.loadUser().then((user) => {
      if (user) {
        window.location.href = user.esAdministrador ? '/inicio' : '/ventas/nueva';
      }
    });
  }).catch(() => {
    // Sin sesión: el formulario queda listo.
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
      });
      window.location.href = user.esAdministrador ? '/inicio' : '/ventas/nueva';
    } catch (err) {
      app.renderAlerts(alerts, { error: err.message || 'Usuario o contraseña incorrectos.' });
    } finally {
      btn.disabled = false;
      btn.textContent = 'Ingresar';
    }
  });
});
