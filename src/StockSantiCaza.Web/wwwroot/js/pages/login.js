document.addEventListener('DOMContentLoaded', () => {
  const form = document.getElementById('login-form');
  const alerts = document.getElementById('login-alerts');
  const btn = document.getElementById('login-btn');

  function showError(message) {
    if (window.app?.renderAlerts) {
      app.renderAlerts(alerts, { error: message });
      return;
    }
    alerts.innerHTML = `<div class="alert alert-danger"><strong>No se pudo completar la operación</strong><ul><li>${message}</li></ul></div>`;
  }

  async function loginRequest(login, password) {
    if (window.api) {
      return api.post('/api/auth/login', { login, password });
    }

    const response = await fetch('/api/auth/login', {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ login, password })
    });
    const body = await response.json().catch(() => null);
    if (!response.ok) {
      throw new Error(body?.error || 'Usuario o contraseña incorrectos.');
    }
    return body;
  }

  async function checkSession() {
    if (!window.api) return null;
    try {
      return await app.loadUser();
    } catch {
      return null;
    }
  }

  checkSession().then((user) => {
    if (user) {
      window.location.href = user.esAdministrador ? '/inicio' : '/ventas/nueva';
    }
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    alerts.innerHTML = '';
    btn.disabled = true;
    btn.textContent = 'Ingresando...';

    try {
      const user = await loginRequest(
        document.getElementById('login').value,
        document.getElementById('password').value
      );
      window.location.href = user.esAdministrador ? '/inicio' : '/ventas/nueva';
    } catch (err) {
      const message = err.message === 'Failed to fetch'
        ? 'No se pudo conectar con el servidor. Verifique que la aplicación .NET esté publicada y en ejecución.'
        : (err.message || 'Usuario o contraseña incorrectos.');
      showError(message);
    } finally {
      btn.disabled = false;
      btn.textContent = 'Ingresar';
    }
  });
});
