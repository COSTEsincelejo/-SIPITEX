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
    const sidebar = document.getElementById('sidebar');
    const menuToggle = document.getElementById('menuToggle');
    const backdrop = document.getElementById('sidebarBackdrop');
    const mq = window.matchMedia('(max-width: 980px)');

    function isMobile() {
      return mq.matches;
    }

    function setExpanded(expanded) {
      if (!sidebar || !menuToggle) return;

      if (isMobile()) {
        document.body.classList.remove('sidebar-collapsed');
        sidebar.classList.toggle('open', expanded);
        backdrop?.classList.toggle('show', expanded);
        if (backdrop) backdrop.hidden = !expanded;
      } else {
        sidebar.classList.remove('open');
        backdrop?.classList.remove('show');
        if (backdrop) backdrop.hidden = true;
        document.body.classList.toggle('sidebar-collapsed', !expanded);
      }

      menuToggle.setAttribute('aria-expanded', expanded ? 'true' : 'false');
    }

    function isExpanded() {
      if (!sidebar) return false;
      return isMobile()
        ? sidebar.classList.contains('open')
        : !document.body.classList.contains('sidebar-collapsed');
    }

    function syncSidebarToViewport() {
      // Móvil: cerrado por defecto | Escritorio: abierto por defecto
      setExpanded(!isMobile());
    }

    menuToggle?.addEventListener('click', () => {
      setExpanded(!isExpanded());
    });

    backdrop?.addEventListener('click', () => setExpanded(false));

    sidebar?.querySelectorAll('a.nav-item').forEach((link) => {
      link.addEventListener('click', () => {
        if (isMobile()) setExpanded(false);
      });
    });

    mq.addEventListener('change', syncSidebarToViewport);
    syncSidebarToViewport();

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

    // Chip instructor: toggle lectura / edición de Proceso
    document.querySelectorAll('[data-instructor-chip]').forEach((chip) => {
      const view = chip.querySelector('[data-chip-view]');
      const form = chip.querySelector('[data-chip-edit-form]');
      const editBtn = chip.querySelector('[data-chip-edit]');
      const cancelBtn = chip.querySelector('[data-chip-cancel]');
      const input = form?.querySelector('.chip-proceso-input');
      if (!view || !form || !editBtn || !cancelBtn || !input) return;

      const original = () => input.getAttribute('data-original-proceso') ?? '';

      editBtn.addEventListener('click', () => {
        view.hidden = true;
        form.hidden = false;
        input.value = original();
        input.focus();
      });

      cancelBtn.addEventListener('click', () => {
        input.value = original();
        form.hidden = true;
        view.hidden = false;
      });
    });

    // SolicitudMaterial: mostrar/ocultar formulario expandible por ficha
    document.querySelectorAll('[data-solicitud-toggle]').forEach((btn) => {
      btn.addEventListener('click', () => {
        const sel = btn.getAttribute('data-solicitud-toggle');
        const panel = sel ? document.querySelector(sel) : null;
        if (!panel) return;
        const open = panel.hidden;
        panel.hidden = !open;
        btn.setAttribute('aria-expanded', open ? 'true' : 'false');
      });
    });

    // SolicitudMaterial: filas dinámicas (agregar / quitar / reindexar)
    document.querySelectorAll('[data-solicitud-form]').forEach((form) => {
      const rowsHost = form.querySelector('[data-solicitud-rows]');
      const template = form.querySelector('template[data-solicitud-row-template]');
      const addBtn = form.querySelector('[data-solicitud-add]');
      const cancelBtn = form.querySelector('[data-solicitud-cancel]');
      if (!rowsHost || !template || !addBtn) return;

      const reindex = () => {
        const rows = [...rowsHost.querySelectorAll('[data-solicitud-row]')];
        rows.forEach((row, i) => {
          row.querySelectorAll('[name], [data-name-template]').forEach((el) => {
            const tpl = el.getAttribute('data-name-template');
            if (tpl) {
              el.setAttribute('name', tpl.replace('{i}', String(i)));
            } else if (el.name) {
              el.name = el.name.replace(/Detalles\[\d+]/, `Detalles[${i}]`);
            }
          });
          const removeBtn = row.querySelector('[data-solicitud-remove]');
          if (removeBtn) removeBtn.hidden = rows.length <= 1;
        });
      };

      addBtn.addEventListener('click', () => {
        const node = template.content.cloneNode(true);
        rowsHost.appendChild(node);
        reindex();
      });

      rowsHost.addEventListener('click', (e) => {
        const removeBtn = e.target.closest('[data-solicitud-remove]');
        if (!removeBtn || !rowsHost.contains(removeBtn)) return;
        const row = removeBtn.closest('[data-solicitud-row]');
        const rows = rowsHost.querySelectorAll('[data-solicitud-row]');
        if (!row || rows.length <= 1) return;
        row.remove();
        reindex();
      });

      cancelBtn?.addEventListener('click', () => {
        const panel = form.closest('.solicitud-form-row');
        if (panel) panel.hidden = true;
        const id = panel?.id ? `#${panel.id}` : null;
        if (id) {
          document.querySelectorAll(`[data-solicitud-toggle="${id}"]`).forEach((b) => {
            b.setAttribute('aria-expanded', 'false');
          });
        }
      });

      form.addEventListener('submit', (e) => {
        const rows = [...rowsHost.querySelectorAll('[data-solicitud-row]')];
        const valid = rows.some((row) => {
          const mat = Number(row.querySelector('select')?.value || 0);
          const qty = Number(row.querySelector('input[type="number"]')?.value || 0);
          return mat > 0 && qty > 0;
        });
        if (!valid) {
          e.preventDefault();
          window.SipitexToast('Agregue al menos un material con cantidad mayor a cero.', 'warning');
        }
      });

      reindex();
    });

    // Bodega: validar CantidadAprobada <= max (min solicitada, stock) antes de enviar
    document.querySelectorAll('[data-resolucion-form]').forEach((form) => {
      form.addEventListener('submit', (e) => {
        const inputs = [...form.querySelectorAll('input[data-max-aprobada]')];
        for (const input of inputs) {
          const max = Number(input.getAttribute('data-max-aprobada') || 0);
          const value = Number(input.value || 0);
          if (value < 0 || value > max) {
            e.preventDefault();
            window.SipitexToast(
              `La cantidad aprobada no puede superar ${max} (mínimo entre solicitada y stock).`,
              'warning');
            input.focus();
            return;
          }
        }
      });
    });
  });
})();
