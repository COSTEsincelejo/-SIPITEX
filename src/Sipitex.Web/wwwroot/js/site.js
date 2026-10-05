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
    const loading = root ? root.querySelector('.search-loading') : null;
    if (!root || !input || !dropdown) return;

    const apiUrl = root.getAttribute('data-search-api') || '/Buscar/Sugerencias';
    const ejemplosBase = ['OP-001', 'stock crítico planta 1', 'camisa'];
    let debounceTimer = null;
    let activeIndex = -1;
    let flatItems = [];
    let abortController = null;
    let requestSeq = 0;
    const DEBOUNCE_MS = 250;

    function fold(text) {
      return (text || '')
        .toLowerCase()
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '');
    }

    function escapeHtml(text) {
      return String(text).replace(/[&<>"']/g, (c) => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
      }[c]));
    }

    function highlight(text, query) {
      const source = String(text || '');
      const tokens = fold(query).split(/\s+/).filter((t) => t.length >= 2);
      if (!tokens.length) return escapeHtml(source);

      const starts = [];
      let folded = '';
      for (let i = 0; i < source.length; i++) {
        const ch = fold(source[i]);
        for (let k = 0; k < ch.length; k++) starts.push(i);
        folded += ch;
      }

      const marks = [];
      tokens.forEach((token) => {
        let from = 0;
        while (from < folded.length) {
          const at = folded.indexOf(token, from);
          if (at < 0) break;
          marks.push([at, at + token.length]);
          from = at + token.length;
        }
      });
      if (!marks.length) return escapeHtml(source);

      marks.sort((a, b) => a[0] - b[0] || b[1] - a[1]);
      const merged = [];
      marks.forEach((range) => {
        const last = merged[merged.length - 1];
        if (!last || range[0] > last[1]) merged.push(range.slice());
        else last[1] = Math.max(last[1], range[1]);
      });

      let html = '';
      let cursor = 0;
      merged.forEach(([start, end]) => {
        const origStart = starts[start];
        const origEnd = (starts[end] ?? source.length);
        if (origStart > cursor) html += escapeHtml(source.slice(cursor, origStart));
        html += '<mark class="search-mark">' + escapeHtml(source.slice(origStart, origEnd)) + '</mark>';
        cursor = origEnd;
      });
      html += escapeHtml(source.slice(cursor));
      return html;
    }

    function setExpanded(open) {
      dropdown.hidden = !open;
      input.setAttribute('aria-expanded', open ? 'true' : 'false');
      if (!open) input.removeAttribute('aria-activedescendant');
    }

    function setLoading(on) {
      input.setAttribute('aria-busy', on ? 'true' : 'false');
      if (loading) loading.hidden = !on;
    }

    function closeDropdown() {
      setLoading(false);
      dropdown.innerHTML = '';
      setExpanded(false);
      activeIndex = -1;
      flatItems = [];
    }

    function setActive(index) {
      const nodes = dropdown.querySelectorAll('[data-search-item]');
      nodes.forEach((el) => {
        el.classList.remove('is-active');
        el.removeAttribute('aria-selected');
      });
      if (index < 0 || index >= nodes.length) {
        activeIndex = -1;
        input.removeAttribute('aria-activedescendant');
        return;
      }
      activeIndex = index;
      nodes[index].classList.add('is-active');
      nodes[index].setAttribute('aria-selected', 'true');
      input.setAttribute('aria-activedescendant', nodes[index].id);
      nodes[index].scrollIntoView({ block: 'nearest' });
    }

    function goTo(url) {
      if (!url) return;
      window.location.href = url;
    }

    function renderExamples(message, ejemplos) {
      const lista = (ejemplos && ejemplos.length) ? ejemplos : ejemplosBase;
      flatItems = [];
      activeIndex = -1;
      input.removeAttribute('aria-activedescendant');
      dropdown.innerHTML =
        '<div class="search-empty"></div><div class="search-examples"></div>';
      dropdown.querySelector('.search-empty').textContent = message;
      const host = dropdown.querySelector('.search-examples');
      lista.forEach((ejemplo) => {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'search-example';
        btn.textContent = ejemplo;
        btn.addEventListener('click', () => {
          input.value = ejemplo;
          input.focus();
          runSearch(ejemplo, false);
        });
        host.appendChild(btn);
      });
      setExpanded(true);
    }

    function render(query, data) {
      const grupos = Array.isArray(data?.grupos) ? data.grupos : [];
      const ejemplos = Array.isArray(data?.ejemplos) ? data.ejemplos : [];
      const entendi = typeof data?.entendi === 'string' ? data.entendi : '';
      flatItems = [];
      grupos.forEach((grupo) => {
        (grupo.items || []).forEach((item) => {
          flatItems.push({
            texto: item.texto || '',
            url: item.url || '',
            destino: item.destino || '',
            icono: item.icono || 'fa-search',
            tipo: grupo.tipo || 'Resultados'
          });
        });
      });

      if (!flatItems.length) {
        const mensaje = entendi
          ? entendi + '. Prueba con un ejemplo:'
          : 'No reconocí la búsqueda. Prueba con:';
        renderExamples(mensaje, ejemplos);
        return;
      }

      dropdown.innerHTML = '';
      if (entendi) {
        const frase = document.createElement('div');
        frase.className = 'search-entendi';
        frase.textContent = entendi;
        dropdown.appendChild(frase);
      }

      let idx = 0;
      grupos.forEach((grupo) => {
        const items = grupo.items || [];
        if (!items.length) return;
        const label = document.createElement('div');
        label.className = 'search-group-label';
        label.textContent = grupo.tipo || 'Resultados';
        dropdown.appendChild(label);
        items.forEach(() => {
          const item = flatItems[idx];
          const el = document.createElement('a');
          el.className = 'search-item';
          el.setAttribute('role', 'option');
          el.setAttribute('data-search-item', '');
          el.id = 'search-opt-' + idx;
          el.setAttribute('data-index', String(idx));
          el.href = item.url;
          const icon = document.createElement('i');
          icon.className = 'fas ' + (item.icono || 'fa-search');
          icon.setAttribute('aria-hidden', 'true');
          const body = document.createElement('span');
          body.className = 'search-item-body';
          const text = document.createElement('span');
          text.className = 'search-item-text';
          text.innerHTML = highlight(item.texto, query);
          const dest = document.createElement('span');
          dest.className = 'search-item-dest';
          dest.textContent = item.destino || '';
          body.append(text, dest);
          el.append(icon, body);
          const current = idx;
          el.addEventListener('mouseenter', () => setActive(current));
          dropdown.appendChild(el);
          idx += 1;
        });
      });
      setExpanded(true);
      setActive(-1);
    }

    async function runSearch(query, openBest) {
      const q = (query || '').trim();
      const seq = ++requestSeq;
      if (q.length < 2) {
        if (abortController) abortController.abort();
        setLoading(false);
        renderExamples('Escribe al menos 2 caracteres. Por ejemplo:', ejemplosBase);
        return;
      }

      if (abortController) abortController.abort();
      abortController = new AbortController();
      setLoading(true);
      if (dropdown.hidden) {
        dropdown.innerHTML = '<div class="search-empty">Buscando…</div>';
        setExpanded(true);
      }

      try {
        const res = await fetch(apiUrl + '?q=' + encodeURIComponent(q), {
          headers: { Accept: 'application/json' },
          signal: abortController.signal,
          credentials: 'same-origin'
        });
        if (seq !== requestSeq) return;
        setLoading(false);
        if (!res.ok) {
          renderExamples('No pude buscar ahora. Prueba con:', ejemplosBase);
          return;
        }
        const data = await res.json();
        if (seq !== requestSeq) return;
        render(q, data);
        if (openBest && flatItems[0]) goTo(flatItems[0].url);
      } catch (err) {
        if (err && err.name === 'AbortError') return;
        if (seq !== requestSeq) return;
        setLoading(false);
        renderExamples('No pude buscar ahora. Prueba con:', ejemplosBase);
      }
    }

    input.addEventListener('input', () => {
      clearTimeout(debounceTimer);
      const value = input.value;
      debounceTimer = setTimeout(() => runSearch(value, false), DEBOUNCE_MS);
    });

    input.addEventListener('focus', () => {
      if (!input.value.trim()) renderExamples('Prueba con:', ejemplosBase);
      else if (flatItems.length || dropdown.querySelector('.search-example, .search-entendi, .search-item')) setExpanded(true);
    });

    input.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        if (dropdown.hidden) {
          runSearch(input.value, false);
          return;
        }
        if (!flatItems.length) return;
        setActive(activeIndex < 0 ? 0 : Math.min(activeIndex + 1, flatItems.length - 1));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        if (!flatItems.length) return;
        setActive(activeIndex < 0 ? flatItems.length - 1 : Math.max(activeIndex - 1, 0));
      } else if (e.key === 'Enter') {
        if (activeIndex >= 0 && flatItems[activeIndex]) {
          e.preventDefault();
          goTo(flatItems[activeIndex].url);
          return;
        }
        if (flatItems[0]) {
          e.preventDefault();
          goTo(flatItems[0].url);
          return;
        }
        const q = input.value.trim();
        if (q.length >= 2) {
          e.preventDefault();
          clearTimeout(debounceTimer);
          runSearch(q, true);
        }
      } else if (e.key === 'Escape') {
        e.preventDefault();
        closeDropdown();
      }
    });

    document.addEventListener('keydown', (e) => {
      if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
        e.preventDefault();
        input.focus();
        input.select();
      }
    });

    document.addEventListener('click', (e) => {
      if (!root.contains(e.target)) closeDropdown();
    });
  }
})();
