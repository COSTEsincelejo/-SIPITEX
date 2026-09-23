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

    // Confirm destructive / sensitive actions with the shared modal.
    // form.submit() does not re-fire this listener, so confirming cannot loop.
    const confirmModal = initConfirmModal();
    document.querySelectorAll('form[data-confirm]').forEach((form) => {
      form.addEventListener('submit', (e) => {
        e.preventDefault();
        if (confirmModal) {
          confirmModal.open(form);
          return;
        }
        const message = form.getAttribute('data-confirm') || '¿Confirmar acción?';
        if (window.confirm(message)) form.submit();
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

    // PlantaInventario: validar CantidadAprobada <= max (min solicitada, stock) antes de enviar
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

    // Buscador global del header (módulos estáticos + /api/busqueda)
    initGlobalSearch();
  });

  function initConfirmModal() {
    const modal = document.getElementById('confirmModal');
    const dialog = modal?.querySelector('.confirm-modal-dialog');
    const titleEl = document.getElementById('confirmModalTitle');
    const messageEl = document.getElementById('confirmModalMessage');
    const acceptBtn = document.getElementById('confirmModalAccept');
    if (!modal || !dialog || !titleEl || !messageEl || !acceptBtn) return null;

    let pendingForm = null;
    let lastFocus = null;

    function focusable() {
      return [...dialog.querySelectorAll('button:not([disabled])')];
    }

    function close() {
      if (modal.hidden) return;
      modal.hidden = true;
      document.body.classList.remove('confirm-modal-open');
      pendingForm = null;
      const restore = lastFocus;
      lastFocus = null;
      if (restore && typeof restore.focus === 'function') restore.focus();
    }

    function open(form) {
      const message = form.getAttribute('data-confirm') || '¿Confirmar acción?';
      const variant = form.getAttribute('data-confirm-variant') === 'primary' ? 'primary' : 'danger';
      const title = form.getAttribute('data-confirm-title') || 'Confirmar acción';
      const okLabel = form.getAttribute('data-confirm-ok') || 'Confirmar';
      titleEl.textContent = title;
      messageEl.textContent = message;
      acceptBtn.textContent = okLabel;
      acceptBtn.className = variant;
      pendingForm = form;
      lastFocus = document.activeElement;
      modal.hidden = false;
      document.body.classList.add('confirm-modal-open');
      const cancelBtn = dialog.querySelector('[data-confirm-dismiss]');
      (cancelBtn || acceptBtn).focus();
    }

    modal.addEventListener('click', (e) => {
      if (e.target.closest('[data-confirm-dismiss]')) close();
    });

    acceptBtn.addEventListener('click', () => {
      const form = pendingForm;
      close();
      if (form) form.submit();
    });

    document.addEventListener('keydown', (e) => {
      if (modal.hidden) return;
      if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        close();
        return;
      }
      if (e.key !== 'Tab') return;
      const items = focusable();
      if (!items.length) return;
      const first = items[0];
      const last = items[items.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    }, true);

    return { open };
  }

  function initGlobalSearch() {
    const root = document.getElementById('globalSearch');
    const input = document.getElementById('globalSearchInput');
    const dropdown = document.getElementById('globalSearchResults');
    if (!root || !input || !dropdown) return;

    const apiUrl = root.getAttribute('data-search-api') || '/api/busqueda';
    const modules = [
      { texto: 'Inventario', url: '/Inventario', keywords: 'inventario materiales stock plantaInventario', icon: 'fa-boxes-stacked' },
      { texto: 'Órdenes de producción', url: '/Ordenes', keywords: 'ordenes órdenes producción op', icon: 'fa-clipboard-list' },
      { texto: 'MRP / Materiales', url: '/Mrp', keywords: 'mrp bom materiales requerimientos ficha técnica', icon: 'fa-diagram-project' },
      { texto: 'Fichas & producción', url: '/Fichas', keywords: 'fichas producción instructor turno', icon: 'fa-people-group' },
      { texto: 'Mis solicitudes', url: '/SolicitudesMaterial', keywords: 'solicitudes material pedido', icon: 'fa-clipboard-list' },
      { texto: 'Solicitudes de materiales', url: '/PlantasInventarioSolicitudes', keywords: 'plantaInventario solicitudes materiales cola', icon: 'fa-truck-ramp-box' },
      { texto: 'Materiales de órdenes', url: '/PlantasInventarioOrdenes', keywords: 'materiales órdenes entrega planta inventario', icon: 'fa-clipboard-check' },
      { texto: 'Reingreso desde etapas', url: '/PlantasInventarioOrdenes/Reingreso', keywords: 'reingreso etapas trazo corte confección', icon: 'fa-rotate-left' },
      { texto: 'Movimientos de stock', url: '/Inventario/Movimientos', keywords: 'movimientos stock historial entrada salida', icon: 'fa-clock-rotate-left' },
      { texto: 'Actas de ingreso/egreso', url: '/Actas', keywords: 'actas ingreso egreso conformidad firma pdf', icon: 'fa-file-signature' },
      { texto: 'Trazabilidad', url: '/Trazabilidad', keywords: 'trazabilidad código único prenda qr sip', icon: 'fa-barcode' },
      { texto: 'Control de calidad', url: '/Calidad', keywords: 'calidad inspección reproceso bueno regular malo', icon: 'fa-clipboard-check' },
      { texto: 'Grupos de confección', url: '/GruposConfeccion', keywords: 'grupo confección instructor prendas', icon: 'fa-people-group' },
      { texto: 'Consumo de materiales', url: '/Consumos', keywords: 'consumo materiales ficha costo promedio', icon: 'fa-scissors' },
      { texto: 'Costeo de prendas', url: '/Costos', keywords: 'costeo costo tarifa mano de obra', icon: 'fa-coins' },
      { texto: 'Estadísticas', url: '/Estadisticas', keywords: 'estadísticas kpi dashboard gráficos', icon: 'fa-chart-line' },
      { texto: 'Reportes', url: '/Reportes', keywords: 'reportes pdf excel exportar', icon: 'fa-file-export' },
      { texto: 'Alertas', url: '/Alertas', keywords: 'alertas notificaciones correo', icon: 'fa-bell' },
      { texto: 'Usuarios', url: '/Account/Users', keywords: 'usuarios administración cuentas', icon: 'fa-users-gear' },
      { texto: 'Plantas de inventario', url: '/PlantasInventario', keywords: 'plantas inventario administración almacén encargado bodega bodega', icon: 'fa-warehouse' },
      { texto: 'Inventario por bodega', url: '/PlantasInventario/Consultar', keywords: 'inventario bodega planta stock insumos faltantes', icon: 'fa-clipboard-list' },
      { texto: 'Mi perfil', url: '/Account/Profile', keywords: 'perfil cuenta foto contraseña', icon: 'fa-user' }
    ];

    const categoryIcons = {
      'Módulos': 'fa-compass',
      'Materiales': 'fa-boxes-stacked',
      'Órdenes': 'fa-clipboard-list',
      'Fichas': 'fa-people-group',
      'Solicitudes': 'fa-truck-ramp-box',
      'Trazabilidad': 'fa-barcode'
    };

    let debounceTimer = null;
    let activeIndex = -1;
    let flatItems = [];
    let abortController = null;
    const DEBOUNCE_MS = 300;

    function normalize(text) {
      return (text || '')
        .toLowerCase()
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '');
    }

    function matchModules(query) {
      const nq = normalize(query);
      if (!nq) return [];
      return modules
        .filter((m) => normalize(m.texto).includes(nq) || normalize(m.keywords).includes(nq))
        .slice(0, 8)
        .map((m) => ({
          texto: m.texto,
          url: m.url,
          categoria: 'Módulos',
          icon: m.icon
        }));
    }

    function closeDropdown() {
      dropdown.hidden = true;
      dropdown.innerHTML = '';
      input.setAttribute('aria-expanded', 'false');
      activeIndex = -1;
      flatItems = [];
    }

    function openDropdown() {
      dropdown.hidden = false;
      input.setAttribute('aria-expanded', 'true');
    }

    function setActive(index) {
      const nodes = dropdown.querySelectorAll('[data-search-item]');
      nodes.forEach((el) => el.classList.remove('is-active'));
      if (index < 0 || index >= nodes.length) {
        activeIndex = -1;
        return;
      }
      activeIndex = index;
      nodes[index].classList.add('is-active');
      nodes[index].scrollIntoView({ block: 'nearest' });
    }

    function goTo(url) {
      if (!url) return;
      window.location.href = url;
    }

    function render(query, entityItems) {
      const moduleItems = matchModules(query);
      const all = [...moduleItems, ...(entityItems || [])];
      flatItems = all;

      if (!all.length) {
        const safe = query.replace(/[<>&"]/g, '');
        dropdown.innerHTML = `<div class="search-empty">Sin resultados para '<strong></strong>'</div>`;
        dropdown.querySelector('strong').textContent = safe;
        openDropdown();
        activeIndex = -1;
        return;
      }

      const groups = new Map();
      all.forEach((item, idx) => {
        const cat = item.categoria || 'Otros';
        if (!groups.has(cat)) groups.set(cat, []);
        groups.get(cat).push({ ...item, _idx: idx });
      });

      const parts = [];
      for (const [cat, items] of groups) {
        parts.push(`<div class="search-group-label">${cat}</div>`);
        items.forEach((item) => {
          const icon = item.icon || categoryIcons[cat] || 'fa-search';
          parts.push(
            `<a class="search-item" role="option" href="${item.url}" data-search-item data-index="${item._idx}">` +
            `<i class="fas ${icon}" aria-hidden="true"></i><span></span></a>`
          );
        });
      }
      dropdown.innerHTML = parts.join('');
      dropdown.querySelectorAll('[data-search-item]').forEach((el) => {
        const idx = Number(el.getAttribute('data-index'));
        const span = el.querySelector('span');
        if (span && flatItems[idx]) span.textContent = flatItems[idx].texto;
        el.addEventListener('mouseenter', () => setActive(idx));
      });
      openDropdown();
      setActive(all.length ? 0 : -1);
    }

    async function runSearch(query) {
      const q = (query || '').trim();
      if (!q) {
        closeDropdown();
        return;
      }

      const modulesOnly = matchModules(q);
      // Feedback inmediato con módulos; "sin resultados" solo tras la API
      if (modulesOnly.length) render(q, []);

      if (abortController) abortController.abort();
      abortController = new AbortController();

      try {
        const res = await fetch(`${apiUrl}?q=${encodeURIComponent(q)}`, {
          headers: { Accept: 'application/json' },
          signal: abortController.signal,
          credentials: 'same-origin'
        });
        if (!res.ok) {
          if (!modulesOnly.length) {
            dropdown.innerHTML = `<div class="search-empty">Sin resultados para '<strong></strong>'</div>`;
            dropdown.querySelector('strong').textContent = q;
            openDropdown();
          }
          return;
        }
        const data = await res.json();
        const entities = Array.isArray(data?.resultados) ? data.resultados : [];
        render(q, entities);
      } catch (err) {
        if (err?.name === 'AbortError') return;
        if (!modulesOnly.length) {
          dropdown.innerHTML = `<div class="search-empty">Sin resultados para '<strong></strong>'</div>`;
          dropdown.querySelector('strong').textContent = q;
          openDropdown();
        }
      }
    }

    input.addEventListener('input', () => {
      clearTimeout(debounceTimer);
      const value = input.value;
      debounceTimer = setTimeout(() => runSearch(value), DEBOUNCE_MS);
    });

    input.addEventListener('keydown', (e) => {
      if (dropdown.hidden && (e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
        if (input.value.trim()) runSearch(input.value);
        return;
      }
      if (dropdown.hidden) return;

      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setActive(Math.min(activeIndex + 1, flatItems.length - 1));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setActive(Math.max(activeIndex - 1, 0));
      } else if (e.key === 'Enter') {
        if (activeIndex >= 0 && flatItems[activeIndex]) {
          e.preventDefault();
          goTo(flatItems[activeIndex].url);
        }
      } else if (e.key === 'Escape') {
        e.preventDefault();
        closeDropdown();
        input.blur();
      }
    });

    document.addEventListener('click', (e) => {
      if (!root.contains(e.target)) closeDropdown();
    });

    input.addEventListener('focus', () => {
      if (input.value.trim() && flatItems.length) openDropdown();
    });
  }
})();
