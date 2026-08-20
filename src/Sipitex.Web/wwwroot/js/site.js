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

      const bodegaSelect = form.querySelector('select[name="CreateSolicitud.BodegaId"]');
      const applyBodegaFilter = () => {
        if (!bodegaSelect) return;
        const selectedBodega = bodegaSelect.value;
        rowsHost.querySelectorAll('select').forEach((sel) => {
          const options = [...sel.querySelectorAll('option[data-bodega-id]')];
          if (options.length === 0) return;
          options.forEach((opt) => {
            const selectable = selectedBodega !== '' && opt.getAttribute('data-bodega-id') === selectedBodega;
            opt.hidden = !selectable;
            opt.disabled = !selectable;
          });
          const current = sel.selectedOptions[0];
          if (current && current.disabled) {
            sel.value = '';
          }
        });
      };

      addBtn.addEventListener('click', () => {
        const node = template.content.cloneNode(true);
        rowsHost.appendChild(node);
        reindex();
        applyBodegaFilter();
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
      bodegaSelect?.addEventListener('change', applyBodegaFilter);
      applyBodegaFilter();
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

    // Buscador global del header (módulos estáticos + /api/busqueda)
    initGlobalSearch();
  });

  function initGlobalSearch() {
    const root = document.getElementById('globalSearch');
    const input = document.getElementById('globalSearchInput');
    const dropdown = document.getElementById('globalSearchResults');
    if (!root || !input || !dropdown) return;

    const apiUrl = root.getAttribute('data-search-api') || '/api/busqueda';
    const modules = [
      { texto: 'Inventario', url: '/Inventario', keywords: 'inventario materiales stock bodega', icon: 'fa-boxes-stacked' },
      { texto: 'Órdenes de producción', url: '/Ordenes', keywords: 'ordenes órdenes producción op', icon: 'fa-clipboard-list' },
      { texto: 'MRP / Materiales', url: '/Mrp', keywords: 'mrp bom materiales requerimientos ficha técnica', icon: 'fa-diagram-project' },
      { texto: 'Fichas & producción', url: '/Fichas', keywords: 'fichas producción instructor turno', icon: 'fa-people-group' },
      { texto: 'Mis solicitudes', url: '/SolicitudesMaterial', keywords: 'solicitudes material pedido', icon: 'fa-clipboard-list' },
      { texto: 'Solicitudes de materiales', url: '/BodegaSolicitudes', keywords: 'bodega solicitudes materiales cola', icon: 'fa-truck-ramp-box' },
      { texto: 'Control de calidad', url: '/Calidad', keywords: 'calidad inspección reproceso', icon: 'fa-clipboard-check' },
      { texto: 'Estadísticas', url: '/Estadisticas', keywords: 'estadísticas kpi dashboard gráficos', icon: 'fa-chart-line' },
      { texto: 'Reportes', url: '/Reportes', keywords: 'reportes pdf excel exportar', icon: 'fa-file-export' },
      { texto: 'Alertas', url: '/Alertas', keywords: 'alertas notificaciones correo', icon: 'fa-bell' },
      { texto: 'Usuarios', url: '/Account/Users', keywords: 'usuarios administración cuentas', icon: 'fa-users-gear' },
      { texto: 'Mi perfil', url: '/Account/Profile', keywords: 'perfil cuenta foto contraseña', icon: 'fa-user' }
    ];

    const categoryIcons = {
      'Módulos': 'fa-compass',
      'Materiales': 'fa-boxes-stacked',
      'Órdenes': 'fa-clipboard-list',
      'Fichas': 'fa-people-group',
      'Solicitudes': 'fa-truck-ramp-box'
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
