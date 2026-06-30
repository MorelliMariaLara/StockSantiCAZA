const api = {
  async request(path, options = {}) {
    const controller = new AbortController();
    const timeoutMs = options.timeoutMs ?? 15000;
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

    let response;
    try {
      response = await fetch(path, {
        credentials: 'same-origin',
        signal: controller.signal,
        headers: {
          'Content-Type': 'application/json',
          ...(options.headers || {})
        },
        ...options
      });
    } catch (err) {
      if (err.name === 'AbortError') {
        throw new Error('El servidor no respondió a tiempo. El backend .NET no está en ejecución o la base SQL no responde. Probá /api/health en el navegador.');
      }
      throw new Error('No se pudo conectar con el servidor. Verifique que la aplicación esté publicada y en ejecución.');
    } finally {
      clearTimeout(timeoutId);
    }

    const isAuthMe = path.includes('/api/auth/me');
    const isLoginPage = window.location.pathname.replace(/\/$/, '') === '/login';

    if (response.status === 401 && !path.includes('/api/auth/login') && !isAuthMe) {
      if (!isLoginPage) {
        window.location.href = '/login';
      }
      throw new Error('No autorizado');
    }

    const contentType = response.headers.get('content-type') || '';
    const isJson = contentType.includes('application/json');
    let body = null;

    if (isJson) {
      body = await response.json().catch(() => null);
    } else if (!response.ok) {
      const text = await response.text().catch(() => '');
      body = text ? { error: text.slice(0, 500) } : null;
    } else {
      body = await response.blob();
    }

    if (!response.ok) {
      const message = body?.error || body?.errors?.join?.('\n') || `Error ${response.status}`;
      const error = new Error(message);
      error.status = response.status;
      error.body = body;
      throw error;
    }

    return body;
  },

  get(path, options = {}) {
    return this.request(path, options);
  },

  post(path, data) {
    return this.request(path, {
      method: 'POST',
      body: data === undefined ? undefined : JSON.stringify(data)
    });
  },

  delete(path) {
    return this.request(path, { method: 'DELETE' });
  },

  async upload(path, formData) {
    const response = await fetch(path, {
      method: 'POST',
      credentials: 'same-origin',
      body: formData
    });

    if (response.status === 401) {
      window.location.href = '/login';
      throw new Error('No autorizado');
    }

    const body = await response.json().catch(() => null);
    if (!response.ok) {
      const message = body?.error || body?.errors?.join?.('\n') || `Error ${response.status}`;
      const error = new Error(message);
      error.status = response.status;
      error.body = body;
      throw error;
    }

    return body;
  },

  async download(path, fileName) {
    const response = await fetch(path, { credentials: 'same-origin' });
    if (response.status === 401) {
      window.location.href = '/login';
      return;
    }
    if (!response.ok) {
      const body = await response.json().catch(() => null);
      throw new Error(body?.error || `Error ${response.status}`);
    }
    const blob = await response.blob();
    const disposition = response.headers.get('content-disposition');
    const match = disposition?.match(/filename="?([^"]+)"?/i);
    window.stockSanti.downloadBlob(match?.[1] || fileName, blob, blob.type);
  }
};

window.api = api;
