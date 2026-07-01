document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('login-form');
  const alerts = document.getElementById('login-alerts');
  const btn = document.getElementById('login-btn');

  async function verificarServidor() {
    try {
      const health = await fetch('/api/health', { credentials: 'same-origin' });
      if (!health.ok) return;
      const info = await health.json();
      if (!info.tieneProductionJson && info.environment === 'Production') {
        app.renderAlerts(alerts, {
          info: 'Falta appsettings.Production.json en el servidor. El login no podrá conectar a la base de datos de Ferozo.'
        });
      }
      if (info.sqlServer && /LARA-NB|localhost|127\.0\.0\.1/i.test(info.sqlServer)) {
        app.renderAlerts(alerts, {
          error: `El servidor está usando la base LOCAL (${info.sqlServer}), no la de DonWeb. Suba appsettings.Production.json con Server=sql2016.`
        });
        return;
      }
      const dbCheck = await fetch('/api/health/db', {
        credentials: 'same-origin',
        signal: AbortSignal.timeout(30000)
      });
      if (!dbCheck.ok) {
        const body = await dbCheck.json().catch(() => ({}));
        app.renderAlerts(alerts, {
          info: body.mensaje || 'La base de datos no responde todavía. Podés intentar ingresar igual; si falla, revise appsettings.Production.json en el servidor.'
        });
        return;
      }
    } catch (err) {
      if (err.name === 'TimeoutError' || err.name === 'AbortError') {
        app.renderAlerts(alerts, {
          info: 'La base de datos tarda en responder (Ferozo). Intentá ingresar; si no funciona, revise appsettings.Production.json con Server=sql2016.'
        });
      }
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
      const message = err.name === 'AbortError' || err.message.includes('tiempo')
        ? 'La base de datos no respondió. En Ferozo debe existir appsettings.Production.json con Server=sql2016 y la contraseña correcta.'
        : (err.message || 'Usuario o contraseña incorrectos.');
      app.renderAlerts(alerts, { error: message });
    } finally {
      btn.disabled = false;
      btn.textContent = 'Ingresar';
    }
  });
});
