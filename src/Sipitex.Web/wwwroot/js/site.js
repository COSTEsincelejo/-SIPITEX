(() => {
  const toastHost = () => {
    let host = document.getElementById('toastHost');
    if (!host) {
      host = document.createElement('div');
      host.id = 'toastHost';
      host.className = 'toast-host';
      document.body.appendChild(host);
    }
    return host;
  };

  window.SipitexToast = function (message, type = 'info') {
    if (!message) return;
    const el = document.createElement('div');
    el.className = `toast ${type}`;
    const icon =
      type === 'success' ? 'fa-circle-check' :
      type === 'danger' ? 'fa-triangle-exclamation' :
      type === 'warning' ? 'fa-circle-exclamation' : 'fa-circle-info';
    el.innerHTML = `<i class="fas ${icon}"></i><div class="toast-body"></div><button type="button" class="toast-close" aria-label="Cerrar">&times;</button>`;
    el.querySelector('.toast-body').textContent = message;
    el.querySelector('.toast-close').addEventListener('click', () => el.remove());
    toastHost().appendChild(el);
    setTimeout(() => el.remove(), 4500);
  };

  function ensureLoader() {
    let loader = document.getElementById('appLoader');
    if (!loader) {
      loader = document.createElement('div');
      loader.id = 'appLoader';
      loader.className = 'app-loader';
      loader.innerHTML = '<div class="loader-card"><div class="spinner"></div><div>Preparando descarga…</div></div>';
      document.body.appendChild(loader);
    }
    return loader;
  }

  document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('menuToggle')?.addEventListener('click', () => {
      document.getElementById('sidebar')?.classList.toggle('open');
    });

    // Orden asignada: dropdown existente vs texto manual (mutuamente excluyentes)
    const orderSelect = document.getElementById('createFichaOrderSelect');
    const orderIdInput = document.getElementById('createFichaOrderId');
    const orderText = document.getElementById('createFichaOrderText');
    const orderManualWrap = document.getElementById('createFichaOrderManualWrap');
    if (orderSelect && orderIdInput && orderText && orderManualWrap) {
      const syncOrderMode = () => {
        const option = orderSelect.selectedOptions[0];
        const mode = option?.dataset?.mode || 'none';
        if (mode === 'manual') {
          orderIdInput.value = '';
          orderManualWrap.style.display = 'block';
          orderText.disabled = false;
          orderText.focus();
        } else if (mode === 'existing') {
          orderIdInput.value = orderSelect.value;
          orderText.value = '';
          orderText.disabled = true;
          orderManualWrap.style.display = 'none';
        } else {
          orderIdInput.value = '';
          orderText.value = '';
          orderText.disabled = true;
          orderManualWrap.style.display = 'none';
        }
      };
      orderSelect.addEventListener('change', syncOrderMode);
      syncOrderMode();
    }

    // Convert server flash alerts into toasts (keep inline for accessibility if needed)
    document.querySelectorAll('[data-toast]').forEach((node) => {
      const type = node.getAttribute('data-toast-type') || 'info';
      const msg = node.textContent?.trim();
      if (msg) window.SipitexToast(msg, type);
      node.remove();
    });

    // Confirm destructive / sensitive actions
    document.querySelectorAll('form[data-confirm]').forEach((form) => {
      form.addEventListener('submit', (e) => {
        const message = form.getAttribute('data-confirm') || '¿Confirmar acción?';
        if (!window.confirm(message)) e.preventDefault();
      });
    });

    // Report download loading indicator
    document.querySelectorAll('a.js-download').forEach((link) => {
      link.addEventListener('click', () => {
        const loader = ensureLoader();
        loader.classList.add('show');
        setTimeout(() => loader.classList.remove('show'), 1800);
      });
    });
  });
})();
