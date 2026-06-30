document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('login-form');
  const alerts = document.getElementById('login-alerts');
  const btn = document.getElementById('login-btn');

  async function esperarBaseDeDatos() {
    for (let intento = 0; intento < 90; intento++) {
      try {
        const health = await api.get('/api/health');
        if (health.ready) {
          return;
        }
        if (health.database === 'failed') {
          throw new Error(health.error || 'No se pudo inicializar la base de datos.');
        }
      } catch (err) {
        if (err.status && err.status !== 503) {
          throw err;
        }
      }

      app.renderAlerts(alerts, {
        info: 'La base de datos se está inicializando. Esto puede tardar un minuto la primera vez...'
      });
      btn.disabled = true;
      await new Promise((resolve) => setTimeout(resolve, 2000));
    }

    throw new Error('La inicialización de la base de datos está tardando demasiado. Recargue la página en unos minutos.');
  }

  esperarBaseDeDatos()
    .then(() => {
      btn.disabled = false;
      alerts.innerHTML = '';
      return app.loadUser();
    })
    .then((user) => {
      if (user) {
        window.location.href = user.esAdministrador ? '/' : '/ventas/nueva';
      }
    })
    .catch((err) => {
      app.renderAlerts(alerts, { error: err.message || 'No se pudo conectar con el servidor.' });
      btn.disabled = false;
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
      window.location.href = user.esAdministrador ? '/' : '/ventas/nueva';
    } catch (err) {
      app.renderAlerts(alerts, { error: err.message || 'Usuario o contraseña incorrectos.' });
    } finally {
      btn.disabled = false;
      btn.textContent = 'Ingresar';
    }
  });
});
