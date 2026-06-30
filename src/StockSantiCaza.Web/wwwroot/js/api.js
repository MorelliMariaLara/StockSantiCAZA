const api = {
  async request(path, options = {}) {
    const response = await fetch(path, {
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/json',
        ...(options.headers || {})
      },
      ...options
    });

    if (response.status === 401 && !path.includes('/api/auth/login')) {
      window.location.href = '/login';
      throw new Error('No autorizado');
    }

    const contentType = response.headers.get('content-type') || '';
    const isJson = contentType.includes('application/json');
    const body = isJson ? await response.json().catch(() => null) : await response.blob();

    if (!response.ok) {
      const message = body?.error || body?.errors?.join?.('\n') || `Error ${response.status}`;
      const error = new Error(message);
      error.status = response.status;
      error.body = body;
      throw error;
    }

    return body;
  },

  get(path) {
    return this.request(path);
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
