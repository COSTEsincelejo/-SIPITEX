// Combobox reutilizable. Mejora <input data-combobox="..."> sin cambiar name ni el valor enviado.
// Origen de las opciones, en este orden:
//   data-items='[{"nombre":"Camisa","codigo":"PRD-1","materiales":3}]'
//   data-combobox-source (o el valor de data-combobox) apuntando a un
//   <script type="application/json"> o a un <select>/<div> con <option> ocultos
//     (<option value="Camisa" data-codigo="PRD-1" data-materiales="3">).
(function (root, doc) {
  const MENSAJE_VACIO = 'No hay productos con ese nombre';
  const MENSAJE_INVALIDO = 'Elija un producto de la lista.';

  function plegar(texto) {
    return String(texto || '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }

  function escapeHtml(texto) {
    return String(texto).replace(/[&<>"']/g, (c) => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
  }

  function mapa(source) {
    const inicios = [];
    let plegado = '';
    for (let i = 0; i < source.length; i++) {
      const ch = plegar(source[i]);
      for (let k = 0; k < ch.length; k++) inicios.push(i);
      plegado += ch;
    }
    return { plegado, inicios };
  }

  function resaltar(texto, consulta) {
    const source = String(texto || '');
    const q = plegar(consulta).trim();
    if (!q) return escapeHtml(source);
    const { plegado, inicios } = mapa(source);
    const marcas = [];
    let desde = 0;
    while (desde < plegado.length) {
      const en = plegado.indexOf(q, desde);
      if (en < 0) break;
      marcas.push([en, en + q.length]);
      desde = en + q.length;
    }
    if (!marcas.length) return escapeHtml(source);
    let html = '';
    let cursor = 0;
    marcas.forEach(([inicio, fin]) => {
      const origIni = inicios[inicio];
      const origFin = inicios[fin] ?? source.length;
      if (origIni > cursor) html += escapeHtml(source.slice(cursor, origIni));
      html += '<mark class="search-mark">' + escapeHtml(source.slice(origIni, origFin)) + '</mark>';
      cursor = origFin;
    });
    html += escapeHtml(source.slice(cursor));
    return html;
  }

  function parseJson(texto) {
    try {
      const data = JSON.parse(texto || '[]');
      return Array.isArray(data) ? data : [];
    } catch {
      return [];
    }
  }

  function normalizar(lista) {
    return lista.map((item) => {
      if (typeof item === 'string') return { nombre: item.trim(), codigo: '', materiales: 0 };
      const nombre = String(item.nombre || item.Nombre || item.value || '').trim();
      const codigo = String(item.codigo || item.Codigo || '').trim();
      const materiales = Number(item.materiales ?? item.Materiales ?? 0);
      return { nombre, codigo, materiales: Number.isFinite(materiales) ? materiales : 0 };
    }).filter((item) => item.nombre);
  }

  function leerOpciones(nodo) {
    return Array.from(nodo.querySelectorAll('option')).map((op) => ({
      nombre: (op.value || op.textContent || '').trim(),
      codigo: (op.getAttribute('data-codigo') || '').trim(),
      materiales: Number(op.getAttribute('data-materiales') || 0)
    })).filter((item) => item.nombre);
  }

  function leerItems(input) {
    const enAtributo = input.getAttribute('data-items');
    if (enAtributo) return normalizar(parseJson(enAtributo));
    const id = input.getAttribute('data-combobox-source') || input.getAttribute('data-combobox');
    if (!id || !doc.getElementById) return [];
    const nodo = doc.getElementById(id);
    if (!nodo || nodo === input) return [];
    const tipo = (nodo.getAttribute('type') || '').toLowerCase();
    if (nodo.tagName === 'SCRIPT' || tipo === 'application/json') return normalizar(parseJson(nodo.textContent));
    return leerOpciones(nodo);
  }

  function detalle(item) {
    const n = item.materiales;
    const materiales = n === 1 ? '1 material' : n + ' materiales';
    return item.codigo ? item.codigo + ' · ' + materiales : materiales;
  }

  function iniciar(input) {
    if (!input || input.getAttribute('data-combobox-listo') === '1') return;
    input.setAttribute('data-combobox-listo', '1');
    const items = leerItems(input);
    const caja = doc.createElement('div');
    caja.className = 'combobox';
    input.parentNode.insertBefore(caja, input);
    caja.appendChild(input);
    input.classList.add('combobox-input');
    if (!input.getAttribute('autocomplete')) input.autocomplete = 'off';

    const lupa = doc.createElement('i');
    lupa.className = 'fas fa-magnifying-glass combobox-icon';
    lupa.setAttribute('aria-hidden', 'true');
    caja.insertBefore(lupa, input);

    const flecha = doc.createElement('button');
    flecha.type = 'button';
    flecha.className = 'combobox-toggle';
    flecha.setAttribute('aria-label', 'Mostrar opciones');
    flecha.innerHTML = '<i class="fas fa-chevron-down" aria-hidden="true"></i>';

    const panel = doc.createElement('div');
    panel.className = 'search-dropdown combobox-panel';
    panel.id = (input.id || 'combobox') + '-lista';
    panel.setAttribute('role', 'listbox');
    panel.setAttribute('aria-label', input.getAttribute('aria-label') || 'Opciones');
    panel.hidden = true;
    caja.append(flecha, panel);

    input.setAttribute('role', 'combobox');
    input.setAttribute('aria-autocomplete', 'list');
    input.setAttribute('aria-haspopup', 'listbox');
    input.setAttribute('aria-expanded', 'false');
    input.setAttribute('aria-controls', panel.id);

    const crearUrl = input.getAttribute('data-combobox-create-url');
    const crearLabel = input.getAttribute('data-combobox-create-label') || 'Crear ficha técnica';
    let planos = [];
    let activo = -1;
    let listaCompleta = true;

    function coincide(nombre) {
      return items.some((item) => item.nombre === nombre);
    }

    function aplicarValidez() {
      const valor = input.value.trim();
      if (!valor || coincide(input.value)) input.setCustomValidity('');
      else input.setCustomValidity(MENSAJE_INVALIDO);
    }

    function abierto() {
      return !panel.hidden;
    }

    function setExpanded(abiertoPanel) {
      panel.hidden = !abiertoPanel;
      input.setAttribute('aria-expanded', abiertoPanel ? 'true' : 'false');
      if (!abiertoPanel) input.removeAttribute('aria-activedescendant');
    }

    function cerrar() {
      setExpanded(false);
      activo = -1;
    }

    function setActivo(indice) {
      const nodos = panel.querySelectorAll('[data-combobox-item]');
      nodos.forEach((el) => {
        el.classList.remove('is-active');
        el.setAttribute('aria-selected', 'false');
      });
      if (indice < 0 || indice >= nodos.length) {
        activo = -1;
        input.removeAttribute('aria-activedescendant');
        return;
      }
      activo = indice;
      nodos[indice].classList.add('is-active');
      nodos[indice].setAttribute('aria-selected', 'true');
      input.setAttribute('aria-activedescendant', nodos[indice].id);
      if (typeof nodos[indice].scrollIntoView === 'function') {
        nodos[indice].scrollIntoView({ block: 'nearest' });
      }
    }

    function elegir(item) {
      if (!item || item.crear) {
        if (item && item.url) root.location.href = item.url;
        return;
      }
      input.value = item.nombre;
      input.setCustomValidity('');
      cerrar();
      input.focus();
    }

    function pintar(consulta, completa) {
      listaCompleta = completa;
      const q = plegar(consulta).trim();
      const visibles = completa || !q
        ? items.slice()
        : items.filter((item) => plegar(item.nombre).includes(q));
      planos = visibles.map((item) => ({ item }));
      activo = -1;
      panel.innerHTML = '';
      if (!visibles.length) {
        const vacio = doc.createElement('div');
        vacio.className = 'search-empty';
        vacio.textContent = MENSAJE_VACIO;
        panel.appendChild(vacio);
        if (crearUrl) {
          const enlace = doc.createElement('a');
          enlace.className = 'search-item combobox-crear';
          enlace.href = crearUrl;
          enlace.id = panel.id + '-crear';
          enlace.setAttribute('role', 'option');
          enlace.setAttribute('data-combobox-item', '');
          enlace.setAttribute('aria-selected', 'false');
          enlace.textContent = crearLabel;
          enlace.addEventListener('mousedown', (e) => {
            e.preventDefault();
            root.location.href = crearUrl;
          });
          enlace.addEventListener('mouseenter', () => setActivo(0));
          panel.appendChild(enlace);
          planos = [{ crear: true, url: crearUrl }];
        }
        setExpanded(true);
        return;
      }
      visibles.forEach((item, idx) => {
        const el = doc.createElement('div');
        el.className = 'search-item';
        el.id = panel.id + '-opt-' + idx;
        el.setAttribute('role', 'option');
        el.setAttribute('data-combobox-item', '');
        el.setAttribute('aria-selected', 'false');
        const cuerpo = doc.createElement('span');
        cuerpo.className = 'search-item-body';
        const titulo = doc.createElement('span');
        titulo.className = 'search-item-text combobox-nombre';
        titulo.innerHTML = resaltar(item.nombre, completa ? '' : consulta);
        const meta = doc.createElement('span');
        meta.className = 'combobox-meta';
        meta.textContent = detalle(item);
        cuerpo.append(titulo, meta);
        el.appendChild(cuerpo);
        el.addEventListener('mousedown', (e) => {
          e.preventDefault();
          elegir(item);
        });
        el.addEventListener('mouseenter', () => setActivo(idx));
        panel.appendChild(el);
      });
      setExpanded(true);
    }

    function abrirCompleta() {
      pintar('', true);
    }

    flecha.addEventListener('mousedown', (e) => {
      e.preventDefault();
      if (abierto()) cerrar();
      else abrirCompleta();
      input.focus();
    });

    input.addEventListener('focus', () => abrirCompleta());
    input.addEventListener('click', () => {
      if (!abierto()) abrirCompleta();
    });
    input.addEventListener('input', () => {
      aplicarValidez();
      pintar(input.value, false);
    });
    input.addEventListener('blur', () => {
      aplicarValidez();
      root.setTimeout(() => {
        if (!caja.contains(doc.activeElement)) cerrar();
      }, 0);
    });
    input.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        if (!abierto()) abrirCompleta();
        if (!planos.length) return;
        setActivo(activo < 0 ? 0 : Math.min(activo + 1, planos.length - 1));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        if (!abierto() || !planos.length) return;
        setActivo(activo < 0 ? planos.length - 1 : Math.max(activo - 1, 0));
      } else if (e.key === 'Enter') {
        if (abierto() && activo >= 0) {
          e.preventDefault();
          const elegido = planos[activo];
          if (elegido.crear) root.location.href = elegido.url;
          else elegir(elegido.item);
        }
      } else if (e.key === 'Escape') {
        if (abierto()) {
          e.preventDefault();
          cerrar();
        }
      } else if (e.key === 'Tab') {
        cerrar();
      }
    });

    doc.addEventListener('mousedown', (e) => {
      if (!caja.contains(e.target)) cerrar();
    });

    aplicarValidez();
  }

  function arrancar() {
    if (!doc || !doc.querySelectorAll) return;
    doc.querySelectorAll('input[data-combobox]').forEach(iniciar);
  }

  if (doc && doc.addEventListener) {
    if (doc.readyState === 'loading') doc.addEventListener('DOMContentLoaded', arrancar);
    else arrancar();
  }
})(typeof window !== 'undefined' ? window : globalThis, typeof document !== 'undefined' ? document : null);
