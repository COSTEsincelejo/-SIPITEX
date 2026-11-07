// Buscador del header. Puntúa el catálogo en el navegador (mismo criterio que
// BuscadorCatalogo.cs) y solo pide al servidor datos reales cuando el texto
// parece un código o un número (OP-001, SOL-0007, planta 2).
(function (root, doc) {
  const MAXIMO = 6;
  const UMBRAL = 36;
  const PUNTOS_FRASE = 80;
  const PUNTOS_FRASE_BORROSA = 42;
  const PUNTOS_INTENCION = 32;
  const PENALIZACION = 22;
  const PUNTOS_TOKEN = 14;
  const PUNTOS_TOKEN_BORROSO = 8;
  const PUNTOS_COBERTURA = 12;
  const PUNTOS_TOKEN_FRASE = 24;
  const DEBOUNCE_MS = 200;

  const STOPWORDS = new Set([
    'quiero', 'necesito', 'favor', 'porfa', 'el', 'la', 'los', 'las', 'de', 'del', 'al',
    'a', 'en', 'con', 'para', 'por', 'y', 'o', 'u', 'un', 'una', 'unos', 'unas',
    'mi', 'mis', 'su', 'sus', 'que', 'es', 'son', 'lo', 'me', 'se', 'como', 'donde',
    'ir', 'abrir', 'entrar', 'sipitex', 'hacia', 'sobre', 'desde', 'hasta'
  ]);

  const INTENCIONES = {
    crear: 'crear', creo: 'crear', registrar: 'crear', agregar: 'crear', alta: 'crear',
    nuevo: 'crear', nueva: 'crear', nuevos: 'crear', nuevas: 'crear',
    consultar: 'consultar', ver: 'consultar', listar: 'consultar', mostrar: 'consultar',
    buscar: 'consultar', revisar: 'consultar',
    editar: 'editar', modificar: 'editar', renombrar: 'editar', cambiar: 'editar', actualizar: 'editar',
    eliminar: 'eliminar', desactivar: 'eliminar', borrar: 'eliminar', quitar: 'eliminar', inactivar: 'eliminar'
  };

  function plegar(texto) {
    const base = String(texto || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    let out = '';
    for (const c of base) {
      if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c === ' ') out += c;
      else out += ' ';
    }
    return out;
  }

  function normalizar(texto) {
    return plegar(texto).replace(/\s+/g, ' ').trim();
  }

  function raiz(token) {
    if (token.length > 4 && token.endsWith('es')) return token.slice(0, -2);
    if (token.length > 3 && token.endsWith('s')) return token.slice(0, -1);
    return token;
  }

  function tope(token) {
    if (token.length >= 8) return 2;
    if (token.length >= 5) return 1;
    return 0;
  }

  function distancia(a, b) {
    const limite = Math.max(tope(a), tope(b));
    if (limite === 0 || Math.abs(a.length - b.length) > limite) return limite + 1;
    let anterior = [];
    let actual = [];
    for (let j = 0; j <= b.length; j++) anterior[j] = j;
    for (let i = 1; i <= a.length; i++) {
      actual = [i];
      let minimoFila = actual[0];
      for (let j = 1; j <= b.length; j++) {
        const costo = a[i - 1] === b[j - 1] ? 0 : 1;
        actual[j] = Math.min(actual[j - 1] + 1, anterior[j] + 1, anterior[j - 1] + costo);
        if (actual[j] < minimoFila) minimoFila = actual[j];
      }
      if (minimoFila > limite) return limite + 1;
      anterior = actual;
    }
    return anterior[b.length];
  }

  function claveDe(texto, quitarVerbos) {
    const partes = normalizar(texto).split(' ').filter(Boolean).filter((t) => !STOPWORDS.has(t));
    const utiles = quitarVerbos ? partes.filter((t) => !INTENCIONES[t]) : partes;
    return utiles.map(raiz).join(' ');
  }

  function preparar(accion) {
    const frases = new Set();
    const tokens = new Set();
    (accion.palabras || []).forEach((frase) => {
      [claveDe(frase, true), claveDe(frase, false)].forEach((clave) => {
        if (!clave) return;
        frases.add(clave);
        clave.split(' ').forEach((t) => tokens.add(t));
      });
    });
    return { accion, frases, tokens };
  }

  function leer(consulta) {
    const normalizada = normalizar(consulta);
    const letras = normalizada.replace(/[^a-z0-9]/g, '').length;
    if (letras < 2) return null;
    let intencion = null;
    const tokens = [];
    normalizada.split(' ').filter(Boolean).forEach((token) => {
      if (!intencion && INTENCIONES[token]) {
        intencion = INTENCIONES[token];
        return;
      }
      if (STOPWORDS.has(token)) return;
      tokens.push(raiz(token));
    });
    const clave = tokens.join(' ');
    if (!clave && !intencion) return null;
    return { intencion, clave, tokens };
  }

  function puntuar(lectura, preparada) {
    let puntaje = 0;
    let fraseExacta = false;
    if (lectura.clave) {
      for (const frase of preparada.frases) {
        if (frase === lectura.clave) {
          puntaje += PUNTOS_FRASE;
          fraseExacta = true;
          break;
        }
      }
      if (!fraseExacta && !lectura.clave.includes(' ')) {
        let mejor = 99;
        for (const frase of preparada.frases) {
          if (frase.includes(' ')) continue;
          mejor = Math.min(mejor, distancia(lectura.clave, frase));
        }
        if (mejor <= tope(lectura.clave)) puntaje += PUNTOS_FRASE_BORROSA;
      }
    }

    let cubiertos = 0;
    lectura.tokens.forEach((token) => {
      let mejor = 99;
      for (const candidato of preparada.tokens) {
        if (candidato === token) {
          mejor = 0;
          break;
        }
        mejor = Math.min(mejor, distancia(token, candidato));
      }
      if (mejor === 0) {
        puntaje += PUNTOS_TOKEN;
        cubiertos += 1;
      } else if (mejor <= tope(token)) {
        puntaje += PUNTOS_TOKEN_BORROSO;
        cubiertos += 1;
      }
    });

    if (lectura.tokens.length && cubiertos === lectura.tokens.length)
      puntaje += PUNTOS_COBERTURA;

    lectura.tokens.forEach((token) => {
      if (preparada.frases.has(token)) puntaje += PUNTOS_TOKEN_FRASE;
    });

    const intencionAccion = preparada.accion.intencion || '';
    if (lectura.intencion && intencionAccion === lectura.intencion)
      puntaje += PUNTOS_INTENCION;
    else if (lectura.intencion && intencionAccion)
      puntaje -= PENALIZACION;

    if (!lectura.tokens.length && lectura.intencion && intencionAccion === lectura.intencion)
      puntaje += PUNTOS_FRASE;

    return puntaje;
  }

  function prioridadDe(accion) {
    return accion.prioridad > 0 ? accion.prioridad : 1000;
  }

  function esTemaAyuda(item) {
    return !!(item && (item.esAyuda || item.categoria === 'Ayuda'));
  }

  function buscarEnCatalogo(catalogo, consulta, maximo) {
    const lectura = leer(consulta);
    if (!lectura) return [];
    const limite = maximo || MAXIMO;
    const ranked = (catalogo || [])
      .map(preparar)
      .map((item) => ({ item: item.accion, puntaje: puntuar(lectura, item) }))
      .filter((fila) => fila.puntaje >= UMBRAL)
      .sort((a, b) => b.puntaje - a.puntaje
        || prioridadDe(a.item) - prioridadDe(b.item)
        || String(a.item.titulo).localeCompare(String(b.item.titulo)));

    const pantallas = [];
    const ayudas = [];
    ranked.forEach((fila) => {
      if (esTemaAyuda(fila.item)) ayudas.push(fila.item);
      else pantallas.push(fila.item);
    });

    const salida = pantallas.slice(0, limite);
    const mejor = ayudas[0];
    if (mejor && !salida.some((a) => a.url === mejor.url)) {
      salida.splice(salida.length === 0 ? 0 : 1, 0, mejor);
      if (salida.length > limite) salida.pop();
    }
    return salida;
  }

  function frecuentesDe(catalogo, maximo) {
    return (catalogo || [])
      .filter((a) => a.prioridad > 0)
      .sort((a, b) => a.prioridad - b.prioridad || String(a.titulo).localeCompare(String(b.titulo)))
      .slice(0, maximo || MAXIMO);
  }

  function pareceDato(consulta) {
    const n = normalizar(consulta);
    if (/\d/.test(n)) return true;
    return /^(op|sol|prd|mat|fic)\b/.test(n);
  }

  root.SipitexBuscador = { buscarEnCatalogo, frecuentesDe, pareceDato, normalizar };

  function iniciar() {
    if (!doc || !doc.getElementById) return;
    enfocarAncla();

    const caja = doc.getElementById('globalSearch');
    const input = doc.getElementById('globalSearchInput');
    const dropdown = doc.getElementById('globalSearchResults');
    const clearBtn = doc.getElementById('globalSearchClear');
    if (!caja || !input || !dropdown) return;

    const PLACEHOLDER_LARGO = 'Buscar órdenes, materiales, fichas… (ej. tela jersey)';
    const PLACEHOLDER_CORTO = 'Buscar en SIPITEX…';
    const MAX_RECIENTES = 5;

    const apiUrl = caja.getAttribute('data-search-api') || '/Buscar/Sugerencias';
    const catalogo = leerCatalogo();
    let debounceTimer = null;
    let activeIndex = -1;
    let flatItems = [];
    let sonSugerencias = false;
    let abortController = null;
    let requestSeq = 0;

    function leerCatalogo() {
      const nodo = doc.getElementById('buscador-catalogo');
      if (!nodo) return [];
      try {
        const lista = JSON.parse(nodo.textContent || '[]');
        return Array.isArray(lista) ? lista : [];
      } catch {
        return [];
      }
    }

    function escapeHtml(texto) {
      return String(texto).replace(/[&<>"']/g, (c) => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
      }[c]));
    }

    function resaltar(texto, consulta) {
      const source = String(texto || '');
      const tokens = normalizar(consulta).split(' ').filter((t) => t.length >= 2 && !STOPWORDS.has(t) && !INTENCIONES[t]);
      if (!tokens.length) return escapeHtml(source);

      // Cada carácter plegado apunta al índice original, para no correr el resaltado con tildes.
      const inicios = [];
      let plegado = '';
      for (let i = 0; i < source.length; i++) {
        const ch = plegar(source[i]);
        for (let k = 0; k < ch.length; k++) inicios.push(i);
        plegado += ch;
      }

      const marcas = [];
      tokens.forEach((token) => {
        let desde = 0;
        while (desde < plegado.length) {
          const en = plegado.indexOf(token, desde);
          if (en < 0) break;
          marcas.push([en, en + token.length]);
          desde = en + token.length;
        }
      });
      if (!marcas.length) return escapeHtml(source);

      marcas.sort((a, b) => a[0] - b[0] || b[1] - a[1]);
      const unidas = [];
      marcas.forEach((rango) => {
        const ultima = unidas[unidas.length - 1];
        if (!ultima || rango[0] > ultima[1]) unidas.push(rango.slice());
        else ultima[1] = Math.max(ultima[1], rango[1]);
      });

      let html = '';
      let cursor = 0;
      unidas.forEach(([inicio, fin]) => {
        const origIni = inicios[inicio];
        const origFin = inicios[fin] ?? source.length;
        if (origIni > cursor) html += escapeHtml(source.slice(cursor, origIni));
        html += '<mark class="search-mark">' + escapeHtml(source.slice(origIni, origFin)) + '</mark>';
        cursor = origFin;
      });
      html += escapeHtml(source.slice(cursor));
      return html;
    }

    function setExpanded(abierto) {
      dropdown.hidden = !abierto;
      input.setAttribute('aria-expanded', abierto ? 'true' : 'false');
      if (!abierto) input.removeAttribute('aria-activedescendant');
    }

    function claveRecientes() {
      const id = (doc.body && doc.body.getAttribute('data-usuario-id')) || '0';
      return 'sipitex-busquedas-recientes:' + id;
    }

    function leerRecientes() {
      try {
        const lista = JSON.parse(root.localStorage.getItem(claveRecientes()) || '[]');
        if (!Array.isArray(lista)) return [];
        return lista.map((t) => String(t || '').trim()).filter(Boolean).slice(0, MAX_RECIENTES);
      } catch {
        return [];
      }
    }

    function guardarRecientes(lista) {
      try {
        root.localStorage.setItem(claveRecientes(), JSON.stringify(lista.slice(0, MAX_RECIENTES)));
      } catch {
        /* el navegador puede bloquear localStorage */
      }
    }

    function recordar(texto) {
      const q = String(texto || '').trim();
      if (q.length < 2) return;
      const previos = leerRecientes().filter((t) => t.toLowerCase() !== q.toLowerCase());
      guardarRecientes([q, ...previos]);
    }

    function leerAccesos() {
      const nodo = doc.getElementById('buscador-accesos');
      if (!nodo) return [];
      try {
        const lista = JSON.parse(nodo.textContent || '[]');
        return Array.isArray(lista) ? lista.filter((a) => a && a.url && a.titulo) : [];
      } catch {
        return [];
      }
    }

    function ajustarPlaceholder() {
      const estilo = root.getComputedStyle(input);
      const probe = doc.createElement('span');
      probe.style.cssText = 'position:absolute;left:-9999px;top:0;visibility:hidden;white-space:nowrap;';
      probe.style.font = estilo.font;
      probe.textContent = PLACEHOLDER_LARGO;
      doc.body.appendChild(probe);
      const disponible = input.clientWidth
        - parseFloat(estilo.paddingLeft || '0')
        - parseFloat(estilo.paddingRight || '0');
      const cabe = probe.getBoundingClientRect().width <= disponible + 1;
      probe.remove();
      input.placeholder = cabe ? PLACEHOLDER_LARGO : PLACEHOLDER_CORTO;
    }

    function syncClear() {
      if (!clearBtn) return;
      clearBtn.hidden = input.value.trim().length === 0;
    }

    function setLoading(activo) {
      input.setAttribute('aria-busy', activo ? 'true' : 'false');
      const previa = dropdown.querySelector('[data-search-status]');
      if (previa) previa.remove();
      if (!activo) return;
      const fila = doc.createElement('div');
      fila.className = 'search-status';
      fila.setAttribute('data-search-status', '');
      fila.textContent = 'Buscando…';
      dropdown.insertBefore(fila, dropdown.firstChild);
      if (dropdown.hidden) setExpanded(true);
    }

    function cerrar() {
      setLoading(false);
      dropdown.innerHTML = '';
      setExpanded(false);
      activeIndex = -1;
      flatItems = [];
    }

    function setActive(indice) {
      const nodos = dropdown.querySelectorAll('[data-search-item]');
      nodos.forEach((el) => {
        el.classList.remove('is-active');
        el.setAttribute('aria-selected', 'false');
      });
      if (indice < 0 || indice >= nodos.length) {
        activeIndex = -1;
        input.removeAttribute('aria-activedescendant');
        return;
      }
      activeIndex = indice;
      nodos[indice].classList.add('is-active');
      nodos[indice].setAttribute('aria-selected', 'true');
      input.setAttribute('aria-activedescendant', nodos[indice].id);
      nodos[indice].scrollIntoView({ block: 'nearest' });
    }

    function ir(url) {
      if (url) root.location.href = url;
    }

    function pintarItems(consulta, items) {
      sonSugerencias = false;
      flatItems = items;
      activeIndex = -1;
      dropdown.innerHTML = '';
      let categoria = '';
      items.forEach((item, idx) => {
        const grupo = item.categoria || 'Resultados';
        if (grupo !== categoria) {
          categoria = grupo;
          const etiqueta = doc.createElement('div');
          etiqueta.className = 'search-group-label';
          etiqueta.textContent = grupo;
          dropdown.appendChild(etiqueta);
        }
        const el = doc.createElement('a');
        el.className = 'search-item';
        el.href = item.url;
        el.id = 'search-opt-' + idx;
        el.setAttribute('role', 'option');
        el.setAttribute('data-search-item', '');
        el.setAttribute('aria-selected', 'false');
        const icono = doc.createElement('i');
        const nombre = item.icono || 'fa-magnifying-glass';
        icono.className = 'fas ' + nombre.replace(/^fas\s+/, '');
        icono.setAttribute('aria-hidden', 'true');
        const cuerpo = doc.createElement('span');
        cuerpo.className = 'search-item-body';
        const titulo = doc.createElement('span');
        titulo.className = 'search-item-text';
        titulo.innerHTML = resaltar(item.titulo || item.texto || '', consulta);
        const dest = doc.createElement('span');
        dest.className = 'search-item-dest';
        dest.textContent = item.descripcion || item.destino || '';
        cuerpo.append(titulo, dest);
        el.append(icono, cuerpo);
        el.addEventListener('mouseenter', () => setActive(idx));
        el.addEventListener('click', () => recordar(consulta));
        dropdown.appendChild(el);
      });
      setExpanded(true);
      setActive(-1);
    }

    function etiquetaGrupo(texto) {
      const etiqueta = doc.createElement('div');
      etiqueta.className = 'search-group-label';
      etiqueta.textContent = texto;
      dropdown.appendChild(etiqueta);
    }

    function opcionPanel(item, idx) {
      const el = item.url ? doc.createElement('a') : doc.createElement('button');
      if (item.url) el.href = item.url;
      else el.type = 'button';
      el.className = 'search-item';
      el.id = 'search-opt-' + idx;
      el.setAttribute('role', 'option');
      el.setAttribute('data-search-item', '');
      el.setAttribute('aria-selected', 'false');
      const icono = doc.createElement('i');
      icono.className = 'fas ' + (item.icono || 'fa-magnifying-glass').replace(/^fas\s+/, '');
      icono.setAttribute('aria-hidden', 'true');
      const cuerpo = doc.createElement('span');
      cuerpo.className = 'search-item-body';
      const titulo = doc.createElement('span');
      titulo.className = 'search-item-text';
      titulo.textContent = item.titulo;
      const dest = doc.createElement('span');
      dest.className = 'search-item-dest';
      dest.textContent = item.descripcion || '';
      cuerpo.append(titulo, dest);
      el.append(icono, cuerpo);
      el.addEventListener('mouseenter', () => setActive(idx));
      el.addEventListener('click', (e) => {
        if (item.accion === 'reciente') {
          e.preventDefault();
          input.value = item.titulo;
          syncClear();
          buscar(item.titulo);
        } else if (item.accion === 'borrar') {
          e.preventDefault();
          guardarRecientes([]);
          pintarInicio();
        }
      });
      return el;
    }

    function pintarInicio() {
      sonSugerencias = true;
      const recientes = leerRecientes();
      const accesos = leerAccesos();
      const items = [];
      dropdown.innerHTML = '';
      etiquetaGrupo('Búsquedas recientes');
      if (!recientes.length) {
        const vacio = doc.createElement('div');
        vacio.className = 'search-empty';
        vacio.textContent = 'Todavía no hay búsquedas.';
        dropdown.appendChild(vacio);
      } else {
        recientes.forEach((texto) => {
          const item = {
            accion: 'reciente',
            titulo: texto,
            descripcion: 'Repetir búsqueda',
            icono: 'fa-clock-rotate-left'
          };
          dropdown.appendChild(opcionPanel(item, items.length));
          items.push(item);
        });
        const borrar = {
          accion: 'borrar',
          titulo: 'Borrar historial',
          descripcion: 'Quita las búsquedas guardadas en este navegador',
          icono: 'fa-trash-can'
        };
        dropdown.appendChild(opcionPanel(borrar, items.length));
        items.push(borrar);
      }
      if (accesos.length) {
        etiquetaGrupo('Accesos rápidos');
        accesos.forEach((acceso) => {
          const item = {
            accion: 'enlace',
            titulo: acceso.titulo,
            descripcion: '',
            url: acceso.url,
            icono: acceso.icono || 'fa-arrow-right'
          };
          dropdown.appendChild(opcionPanel(item, items.length));
          items.push(item);
        });
      }
      flatItems = items;
      activeIndex = -1;
      setExpanded(true);
      setActive(-1);
    }

    function pintarVacio(mensaje, sugerencias) {
      sonSugerencias = true;
      flatItems = (sugerencias || []).map((item) => ({
        titulo: item.titulo,
        descripcion: item.descripcion,
        url: item.url,
        icono: item.icono,
        categoria: item.categoria
      }));
      activeIndex = -1;
      dropdown.innerHTML = '';
      const vacio = doc.createElement('div');
      vacio.className = 'search-empty';
      vacio.textContent = mensaje;
      dropdown.appendChild(vacio);
      if (flatItems.length) {
        const etiqueta = doc.createElement('div');
        etiqueta.className = 'search-group-label';
        etiqueta.textContent = 'Secciones frecuentes';
        dropdown.appendChild(etiqueta);
        flatItems.forEach((item, idx) => {
          const el = doc.createElement('a');
          el.className = 'search-item';
          el.href = item.url;
          el.id = 'search-opt-' + idx;
          el.setAttribute('role', 'option');
          el.setAttribute('data-search-item', '');
          el.setAttribute('aria-selected', 'false');
          const icono = doc.createElement('i');
          icono.className = 'fas ' + (item.icono || 'fa-magnifying-glass');
          icono.setAttribute('aria-hidden', 'true');
          const cuerpo = doc.createElement('span');
          cuerpo.className = 'search-item-body';
          const titulo = doc.createElement('span');
          titulo.className = 'search-item-text';
          titulo.textContent = item.titulo;
          const dest = doc.createElement('span');
          dest.className = 'search-item-dest';
          dest.textContent = item.descripcion || '';
          cuerpo.append(titulo, dest);
          el.append(icono, cuerpo);
          el.addEventListener('mouseenter', () => setActive(idx));
          dropdown.appendChild(el);
        });
      }
      setExpanded(true);
    }

    function aplanarDatos(data) {
      const grupos = Array.isArray(data && data.grupos) ? data.grupos : [];
      const items = [];
      grupos.forEach((grupo) => {
        (grupo.items || []).forEach((item) => {
          if (!item || !item.url) return;
          items.push({
            titulo: item.texto || '',
            descripcion: item.destino || '',
            url: item.url,
            icono: item.icono || 'fa-magnifying-glass',
            categoria: grupo.tipo || 'Datos'
          });
        });
      });
      return items;
    }

    function mezclar(acciones, datos) {
      const cupoDatos = Math.min(4, datos.length);
      const vistos = new Set();
      const mezcla = [];
      datos.slice(0, cupoDatos).forEach((item) => {
        if (vistos.has(item.url)) return;
        vistos.add(item.url);
        mezcla.push(item);
      });
      acciones.forEach((item) => {
        if (mezcla.length >= MAXIMO || vistos.has(item.url)) return;
        vistos.add(item.url);
        mezcla.push(item);
      });
      return mezcla.slice(0, MAXIMO);
    }

    function renderLocal(consulta) {
      const q = (consulta || '').trim();
      if (q.length < 2) {
        pintarVacio('Escribe qué quieres hacer. Por ejemplo: crear planta, ver stock, reingreso.', frecuentesDe(catalogo));
        return [];
      }
      const acciones = buscarEnCatalogo(catalogo, q, MAXIMO).map((item) => ({
        titulo: item.titulo,
        descripcion: item.descripcion,
        url: item.url,
        icono: item.icono,
        categoria: item.categoria
      }));
      if (!acciones.length) {
        pintarVacio("No encontré nada para '" + q + "'", frecuentesDe(catalogo));
        return [];
      }
      pintarItems(q, acciones);
      return acciones;
    }

    async function buscar(consulta) {
      const q = (consulta || '').trim();
      const seq = ++requestSeq;
      if (abortController) abortController.abort();
      if (!pareceDato(q) || q.length < 2) {
        renderLocal(q);
        setLoading(false);
        return;
      }

      const acciones = buscarEnCatalogo(catalogo, q, MAXIMO).map((item) => ({
        titulo: item.titulo,
        descripcion: item.descripcion,
        url: item.url,
        icono: item.icono,
        categoria: item.categoria
      }));
      if (acciones.length) {
        pintarItems(q, acciones);
      } else {
        flatItems = [];
        activeIndex = -1;
        sonSugerencias = false;
        dropdown.innerHTML = '';
        setExpanded(true);
      }

      abortController = new AbortController();
      setLoading(true);
      try {
        const res = await fetch(apiUrl + '?q=' + encodeURIComponent(q), {
          headers: { Accept: 'application/json' },
          signal: abortController.signal,
          credentials: 'same-origin'
        });
        if (seq !== requestSeq) return;
        setLoading(false);
        if (!res.ok) return;
        const data = await res.json();
        if (seq !== requestSeq) return;
        const datos = aplanarDatos(data);
        const accionesApi = Array.isArray(data && data.acciones) ? data.acciones : [];
        const base = acciones.length ? acciones : accionesApi.map((item) => ({
          titulo: item.titulo,
          descripcion: item.descripcion,
          url: item.url,
          icono: item.icono,
          categoria: item.categoria
        }));
        const mezcla = mezclar(base, datos);
        if (!mezcla.length)
          pintarVacio("No encontré nada para '" + q + "'", frecuentesDe(catalogo));
        else
          pintarItems(q, mezcla);
      } catch (err) {
        if (err && err.name === 'AbortError') return;
        if (seq !== requestSeq) return;
        setLoading(false);
      }
    }

    input.addEventListener('input', () => {
      syncClear();
      clearTimeout(debounceTimer);
      if (!input.value.trim()) {
        if (abortController) abortController.abort();
        setLoading(false);
        pintarInicio();
        return;
      }
      if (sonSugerencias) setExpanded(false);
      debounceTimer = setTimeout(() => buscar(input.value), DEBOUNCE_MS);
    });

    input.addEventListener('focus', () => {
      if (!input.value.trim()) pintarInicio();
      else if (flatItems.length) setExpanded(true);
    });

    if (clearBtn) {
      clearBtn.addEventListener('click', () => {
        input.value = '';
        syncClear();
        if (abortController) abortController.abort();
        clearTimeout(debounceTimer);
        setLoading(false);
        input.focus();
        pintarInicio();
      });
    }

    input.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        if (dropdown.hidden) {
          if (!input.value.trim()) {
            pintarInicio();
            if (flatItems.length) setActive(0);
          } else {
            buscar(input.value);
          }
          return;
        }
        if (!flatItems.length) return;
        setActive(activeIndex < 0 ? 0 : Math.min(activeIndex + 1, flatItems.length - 1));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        if (!flatItems.length) return;
        setActive(activeIndex < 0 ? flatItems.length - 1 : Math.max(activeIndex - 1, 0));
      } else if (e.key === 'Enter') {
        const elegido = activeIndex >= 0 ? flatItems[activeIndex] : (sonSugerencias ? null : flatItems[0]);
        if (elegido && elegido.accion === 'borrar') {
          e.preventDefault();
          guardarRecientes([]);
          pintarInicio();
          return;
        }
        if (elegido && elegido.accion === 'reciente') {
          e.preventDefault();
          input.value = elegido.titulo;
          syncClear();
          buscar(elegido.titulo);
          return;
        }
        if (elegido && elegido.url) {
          e.preventDefault();
          if (!elegido.accion) recordar(input.value);
          ir(elegido.url);
        } else if (input.value.trim().length >= 2) {
          recordar(input.value);
        }
      } else if (e.key === 'Escape') {
        e.preventDefault();
        cerrar();
      }
    });

    doc.addEventListener('keydown', (e) => {
      const tag = (e.target && e.target.tagName) || '';
      const escribe = /^(INPUT|TEXTAREA|SELECT)$/.test(tag) || (e.target && e.target.isContentEditable);
      if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
        e.preventDefault();
        input.focus();
        input.select();
        return;
      }
      if (e.key === '/' && !e.ctrlKey && !e.metaKey && !e.altKey && !escribe) {
        e.preventDefault();
        input.focus();
        input.select();
      }
    });

    doc.addEventListener('click', (e) => {
      if (!caja.contains(e.target)) cerrar();
    });

    ajustarPlaceholder();
    syncClear();
    root.addEventListener('resize', ajustarPlaceholder);
  }

  function enfocarAncla() {
    if (!root.location || !root.location.hash) return;
    const id = decodeURIComponent(root.location.hash.slice(1));
    if (!id || !doc.getElementById) return;
    const el = doc.getElementById(id);
    if (!el) return;
    if (typeof el.scrollIntoView === 'function') el.scrollIntoView({ block: 'center' });
    if (typeof el.focus === 'function') {
      try { el.focus({ preventScroll: true }); } catch { el.focus(); }
    }
  }

  if (doc && doc.addEventListener) {
    if (doc.readyState === 'loading') doc.addEventListener('DOMContentLoaded', iniciar);
    else iniciar();
  }
})(typeof window !== 'undefined' ? window : globalThis, typeof document !== 'undefined' ? document : null);
