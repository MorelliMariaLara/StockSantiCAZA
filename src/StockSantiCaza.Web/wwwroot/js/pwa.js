(() => {
  const DISMISS_KEY = 'santicaza-install-dismissed';

  function isStandalone() {
    return window.matchMedia('(display-mode: standalone)').matches
      || window.navigator.standalone === true;
  }

  function isIos() {
    const ua = window.navigator.userAgent || '';
    return /iPad|iPhone|iPod/.test(ua) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
  }

  function wasDismissed() {
    try {
      const raw = localStorage.getItem(DISMISS_KEY);
      if (!raw) return false;
      const at = Number(raw);
      if (!Number.isFinite(at)) return false;
      // Re-mostrar después de 14 días
      return Date.now() - at < 14 * 24 * 60 * 60 * 1000;
    } catch {
      return false;
    }
  }

  function dismiss() {
    try {
      localStorage.setItem(DISMISS_KEY, String(Date.now()));
    } catch { /* ignore */ }
  }

  function ensureInstallUi() {
    let host = document.getElementById('pwa-install');
    if (host) return host;

    host = document.createElement('div');
    host.id = 'pwa-install';
    host.className = 'pwa-install no-print';
    host.hidden = true;
    host.innerHTML = `
      <div class="pwa-install-card">
        <div class="pwa-install-copy">
          <strong>Instalar StockSantiCAZA</strong>
          <p id="pwa-install-text">Descárguela en el teléfono y ábrala como una app.</p>
        </div>
        <div class="pwa-install-actions">
          <button type="button" class="button primary" id="pwa-install-btn">Instalar</button>
          <button type="button" class="button ghost" id="pwa-install-close" aria-label="Cerrar">Ahora no</button>
        </div>
      </div>
    `;
    document.body.appendChild(host);
    return host;
  }

  function showBanner({ mode, deferredPrompt }) {
    if (isStandalone() || wasDismissed()) return;

    const host = ensureInstallUi();
    const text = host.querySelector('#pwa-install-text');
    const btn = host.querySelector('#pwa-install-btn');
    const close = host.querySelector('#pwa-install-close');

    if (mode === 'ios') {
      text.textContent = 'En Safari: Compartir → “Agregar a pantalla de inicio”.';
      btn.textContent = 'Cómo instalar';
      btn.onclick = () => {
        text.textContent = 'Toque Compartir (□↑) y luego “Agregar a pantalla de inicio”.';
        btn.hidden = true;
      };
    } else if (mode === 'android' && deferredPrompt) {
      text.textContent = 'Instálela para usarla a pantalla completa, como una app.';
      btn.hidden = false;
      btn.textContent = 'Instalar';
      btn.onclick = async () => {
        deferredPrompt.prompt();
        try {
          await deferredPrompt.userChoice;
        } catch { /* ignore */ }
        window.__santicazaDeferredPrompt = null;
        host.hidden = true;
        dismiss();
      };
    } else {
      text.textContent = 'Desde el menú del navegador elija “Instalar app” o “Agregar a la pantalla de inicio”.';
      btn.hidden = true;
    }

    close.onclick = () => {
      host.hidden = true;
      dismiss();
    };

    host.hidden = false;
  }

  async function registerServiceWorker() {
    if (!('serviceWorker' in navigator)) return;
    try {
      await navigator.serviceWorker.register('/sw.js', { scope: '/' });
    } catch (err) {
      console.warn('No se pudo registrar el service worker:', err);
    }
  }

  function initInstallPrompt() {
    if (isStandalone()) return;

    window.addEventListener('beforeinstallprompt', (event) => {
      event.preventDefault();
      window.__santicazaDeferredPrompt = event;
      showBanner({ mode: 'android', deferredPrompt: event });
    });

    // iOS no dispara beforeinstallprompt
    if (isIos()) {
      showBanner({ mode: 'ios' });
      return;
    }

    // Fallback genérico en login (útil si el evento tarda)
    if (location.pathname.replace(/\/$/, '') === '/login' || location.pathname.endsWith('login.html')) {
      setTimeout(() => {
        if (!window.__santicazaDeferredPrompt && !wasDismissed() && !isStandalone()) {
          const host = document.getElementById('pwa-install');
          if (!host || host.hidden) {
            showBanner({ mode: 'manual' });
          }
        }
      }, 1800);
    }
  }

  registerServiceWorker();
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initInstallPrompt);
  } else {
    initInstallPrompt();
  }
})();
