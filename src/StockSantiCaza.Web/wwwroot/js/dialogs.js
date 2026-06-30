const dialogs = {
  confirmDialog: null,
  alertDialog: null,

  init() {
    if (this.confirmDialog) {
      return;
    }

    document.body.insertAdjacentHTML('beforeend', `
      <dialog id="confirm-dialog" class="ui-dialog confirm-dialog no-print">
        <div class="confirm-panel" role="alertdialog" aria-modal="true" aria-labelledby="confirm-title">
          <div class="confirm-icon" aria-hidden="true">!</div>
          <h3 id="confirm-title">Confirmar</h3>
          <p id="confirm-message"></p>
          <div class="confirm-actions">
            <button type="button" class="button btn-delete" id="confirm-ok">Confirmar</button>
            <button type="button" class="button ghost" id="confirm-cancel">Cancelar</button>
          </div>
        </div>
      </dialog>
      <dialog id="alert-dialog" class="ui-dialog alert-dialog no-print">
        <div class="alert-panel error" role="alertdialog" aria-modal="true" aria-labelledby="alert-title">
          <div class="alert-icon" aria-hidden="true">✕</div>
          <h3 id="alert-title">Aviso</h3>
          <p class="alert-message" id="alert-message"></p>
          <div class="alert-actions">
            <button type="button" class="button primary" id="alert-ok">Entendido</button>
          </div>
        </div>
      </dialog>
    `);

    this.confirmDialog = document.getElementById('confirm-dialog');
    this.alertDialog = document.getElementById('alert-dialog');

    this.confirmDialog.addEventListener('cancel', (e) => {
      e.preventDefault();
      this._resolveConfirm?.(false);
      stockSanti.closeDialog(this.confirmDialog);
    });
  },

  ask(message, { title = 'Confirmar', confirmText = 'Confirmar' } = {}) {
    this.init();
    return new Promise((resolve) => {
      this._resolveConfirm = resolve;
      document.getElementById('confirm-title').textContent = title;
      document.getElementById('confirm-message').textContent = message;
      const okBtn = document.getElementById('confirm-ok');
      okBtn.textContent = confirmText;

      const onOk = () => {
        cleanup();
        resolve(true);
        stockSanti.closeDialog(this.confirmDialog);
      };
      const onCancel = () => {
        cleanup();
        resolve(false);
        stockSanti.closeDialog(this.confirmDialog);
      };
      const cleanup = () => {
        okBtn.removeEventListener('click', onOk);
        document.getElementById('confirm-cancel').removeEventListener('click', onCancel);
      };

      okBtn.addEventListener('click', onOk);
      document.getElementById('confirm-cancel').addEventListener('click', onCancel);
      stockSanti.openDialog(this.confirmDialog);
    });
  },

  alert(message, { title = 'Aviso', kind = 'error' } = {}) {
    this.init();
    return new Promise((resolve) => {
      const panel = this.alertDialog.querySelector('.alert-panel');
      panel.className = `alert-panel ${kind}`;
      const icons = { error: '✕', warning: '!', success: '✓', info: 'i' };
      panel.querySelector('.alert-icon').textContent = icons[kind] || '!';
      document.getElementById('alert-title').textContent = title;
      document.getElementById('alert-message').textContent = message;

      const onOk = () => {
        okBtn.removeEventListener('click', onOk);
        resolve();
        stockSanti.closeDialog(this.alertDialog);
      };
      const okBtn = document.getElementById('alert-ok');
      okBtn.addEventListener('click', onOk);
      stockSanti.openDialog(this.alertDialog);
    });
  }
};

window.dialogs = dialogs;
