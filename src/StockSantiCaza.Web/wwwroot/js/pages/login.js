document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('login-form');
  const alerts = document.getElementById('login-alerts');
  const btn = document.getElementById('login-btn');

  app.loadUser().then((user) => {
    if (user) {
      window.location.href = user.esAdministrador ? '/' : '/ventas/nueva';
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
