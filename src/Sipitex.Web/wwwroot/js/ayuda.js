// Centro de ayuda: filtro de temas, panel "?" y recorrido del primer ingreso.
(function (doc, win) {
  const CLAVE_TOUR = 'sipitex-ayuda-tour';

  const MENSAJE_VACIO = 'No encontramos exactamente eso. Prueba con otras palabras o revisa las categorías de abajo.';

  function leerTemas() {
    const nodo = doc.getElementById('ayuda-temas');
    if (!nodo) return [];
    try {
      const datos = JSON.parse(nodo.textContent || '[]');
      return Array.isArray(datos) ? datos : [];
    } catch (e) {
      return [];
    }
  }

  function filtrarAyuda() {
    const input = doc.getElementById('ayudaBuscar');
    const panel = doc.getElementById('ayudaSugerencias');
    const estado = doc.getElementById('ayudaBuscarEstado');
    if (!input || !panel) return;

    let espera = 0;
    let control = null;
    let consultaActual = '';

    function cerrar() {
      panel.hidden = true;
      panel.replaceChildren();
      input.setAttribute('aria-expanded', 'false');
      if (estado) estado.textContent = '';
    }

    function enfocar(id) {
      const articulo = doc.getElementById(id);
      if (!articulo) return;
      const detalle = articulo.querySelector('details');
      if (detalle) detalle.open = true;
      articulo.classList.add('ayuda-tema-foco');
      articulo.scrollIntoView({ behavior: 'smooth', block: 'start' });
      win.setTimeout(() => articulo.classList.remove('ayuda-tema-foco'), 2400);
    }

    function pintar(data) {
      panel.replaceChildren();
      const resultados = data && Array.isArray(data.resultados) ? data.resultados : [];
      if (!consultaActual) {
        cerrar();
        return;
      }

      panel.hidden = false;
      input.setAttribute('aria-expanded', 'true');
      if (data && data.sugerencia && resultados.length) {
        const titulo = doc.createElement('p');
        titulo.className = 'ayuda-sugerencias-titulo';
        titulo.textContent = 'Quizá quisiste decir…';
        panel.appendChild(titulo);
      }

      if (!resultados.length) {
        const vacio = doc.createElement('p');
        vacio.className = 'ayuda-sugerencias-vacio';
        vacio.textContent = MENSAJE_VACIO;
        panel.appendChild(vacio);
        if (estado) estado.textContent = MENSAJE_VACIO;
        return;
      }

      resultados.forEach((item) => {
        const enlace = doc.createElement('a');
        enlace.className = 'ayuda-sugerencia';
        enlace.href = '#' + item.id;
        enlace.setAttribute('role', 'option');
        const tope = doc.createElement('span');
        tope.className = 'ayuda-sugerencia-top';
        const tipo = doc.createElement('span');
        tipo.className = 'ayuda-tipo ' + (item.tipo === 'Explicación' ? 'ayuda-tipo-explica' : 'ayuda-tipo-paso');
        tipo.textContent = item.tipo || 'Paso a paso';
        const categoria = doc.createElement('span');
        categoria.className = 'ayuda-sugerencia-cat';
        categoria.textContent = item.categoria || '';
        tope.append(tipo, categoria);
        const titulo = doc.createElement('strong');
        titulo.textContent = item.titulo || '';
        const desc = doc.createElement('span');
        desc.className = 'ayuda-sugerencia-desc';
        desc.textContent = item.descripcion || '';
        enlace.append(tope, titulo, desc);
        enlace.addEventListener('click', () => {
          enfocar(item.id);
          cerrar();
        });
        panel.appendChild(enlace);
      });

      if (estado) {
        estado.textContent = resultados.length === 1 ? '1 guía' : resultados.length + ' guías';
      }
    }

    function consultar(q) {
      if (control) control.abort();
      control = new AbortController();
      const signal = control.signal;
      fetch('/Ayuda/Buscar?q=' + encodeURIComponent(q), {
        headers: { Accept: 'application/json' },
        signal: signal
      }).then((resp) => {
        if (!resp.ok) throw new Error('buscar');
        return resp.json();
      }).then((data) => {
        if (input.value.trim() !== q) return;
        pintar(data);
      }).catch((error) => {
        if (error && error.name === 'AbortError') return;
        if (input.value.trim() !== q) return;
        pintar({ sugerencia: false, resultados: [] });
      });
    }

    function programar() {
      win.clearTimeout(espera);
      const q = input.value.trim();
      consultaActual = q;
      if (!q) {
        if (control) control.abort();
        cerrar();
        return;
      }
      espera = win.setTimeout(() => consultar(q), 300);
    }

    input.addEventListener('input', programar);
    input.addEventListener('search', programar);
    input.addEventListener('focus', programar);
    input.addEventListener('keydown', (ev) => {
      if (ev.key === 'Escape') {
        cerrar();
        return;
      }
      if (ev.key !== 'Enter') return;
      const primero = panel.querySelector('a');
      if (!primero) return;
      ev.preventDefault();
      primero.click();
    });
    doc.addEventListener('mousedown', (ev) => {
      if (panel.hidden) return;
      if (ev.target === input || panel.contains(ev.target)) return;
      cerrar();
    });
  }

  function textoRoles(roles) {
    return (roles || []).join(' · ');
  }

  function llenarPanel(tema) {
    const titulo = doc.getElementById('ayudaPanelTitulo');
    const cuerpo = doc.getElementById('ayudaPanelCuerpo');
    if (!titulo || !cuerpo || !tema) return;
    titulo.textContent = tema.pregunta || tema.titulo || 'Ayuda';
    cuerpo.replaceChildren();

    const roles = doc.createElement('p');
    roles.className = 'ayuda-roles';
    textoRoles(tema.rolesPermitidos).split(' · ').filter(Boolean).forEach((rol) => {
      const badge = doc.createElement('span');
      badge.className = 'badge badge-info';
      badge.textContent = rol;
      roles.appendChild(badge);
    });
    cuerpo.appendChild(roles);

    const que = doc.createElement('p');
    que.innerHTML = '<strong>Qué es.</strong> ';
    que.append(doc.createTextNode(tema.queEs || ''));
    const para = doc.createElement('p');
    para.innerHTML = '<strong>Para qué sirve.</strong> ';
    para.append(doc.createTextNode(tema.paraQueSirve || ''));
    cuerpo.append(que, para);

    const lista = doc.createElement('ol');
    (tema.pasos || []).forEach((paso) => {
      const li = doc.createElement('li');
      li.textContent = paso;
      lista.appendChild(li);
    });
    cuerpo.appendChild(lista);

    const pie = doc.createElement('p');
    pie.className = 'ayuda-ir';
    const guia = doc.createElement('a');
    guia.href = '/Ayuda#' + encodeURIComponent(tema.id || '');
    guia.textContent = 'Ver la guía completa';
    pie.appendChild(guia);
    if (tema.urlRelacionada) {
      pie.append(doc.createTextNode(' · '));
      const ir = doc.createElement('a');
      ir.href = tema.urlRelacionada;
      ir.textContent = 'Abrir la pantalla';
      pie.appendChild(ir);
    }
    cuerpo.appendChild(pie);
  }

  function iniciarPanel(temas) {
    const panel = doc.getElementById('ayudaPanel');
    const boton = doc.getElementById('ayudaContextual');
    if (!panel || !boton) return;
    const id = boton.getAttribute('data-ayuda-id');
    const tema = temas.find((t) => t.id === id);
    if (!tema) return;

    let origen = null;
    function abrir() {
      origen = doc.activeElement;
      llenarPanel(tema);
      panel.hidden = false;
      boton.setAttribute('aria-expanded', 'true');
      const cerrar = panel.querySelector('[data-ayuda-cerrar]');
      if (cerrar) cerrar.focus();
    }
    function cerrar() {
      panel.hidden = true;
      boton.setAttribute('aria-expanded', 'false');
      if (origen && origen.focus) origen.focus();
    }

    boton.addEventListener('click', abrir);
    panel.querySelectorAll('[data-ayuda-cerrar]').forEach((nodo) => {
      nodo.addEventListener('click', cerrar);
    });
    doc.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && !panel.hidden) {
        e.preventDefault();
        cerrar();
      }
    });
  }

  const PASOS_TOUR = [
    { sel: '#sidebar', titulo: 'Menú', texto: 'Aquí están las secciones de su rol: Operación para el día a día y Análisis para cifras y reportes.' },
    { sel: '#globalSearch', titulo: 'Buscador', texto: 'Escriba lo que quiere hacer, por ejemplo «tela jersey» o «crear orden». Ctrl+K abre este cuadro. La equis borra el texto.' },
    { sel: '#topbarAlertas', titulo: 'Alertas', texto: 'La campana abre los avisos de stock y las notificaciones de su rol.' },
    { sel: '#topbarPerfil', titulo: 'Perfil', texto: 'Su nombre abre Mi perfil: foto, funciones y contraseña. Al final del menú también está Ayuda.' }
  ];
  const MARGEN = 16;
  const HUECO = 12;

  function claveTour() {
    const id = (doc.body && doc.body.getAttribute('data-usuario-id')) || '0';
    return CLAVE_TOUR + ':' + id;
  }

  function yaVisto() {
    try { return win.localStorage.getItem(claveTour()) === '1'; }
    catch (e) { return true; }
  }

  // El nodo existe, se pinta y entra en el viewport. Un menú cerrado o un control de otro rol no cuenta.
  function elementoVisible(sel) {
    const el = doc.querySelector(sel);
    if (!el || el.hidden) return null;
    const estilo = win.getComputedStyle(el);
    if (estilo.display === 'none' || estilo.visibility === 'hidden' || Number(estilo.opacity) === 0) return null;
    const r = el.getBoundingClientRect();
    if (r.width < 2 || r.height < 2) return null;
    if (r.right <= 0 || r.bottom <= 0 || r.left >= win.innerWidth || r.top >= win.innerHeight) return null;
    return el;
  }

  function pasosVisibles() {
    return PASOS_TOUR.filter((paso) => elementoVisible(paso.sel));
  }

  function iniciarTour() {
    const reducido = win.matchMedia && win.matchMedia('(prefers-reduced-motion: reduce)').matches;
    let fondo = null;
    let marco = null;
    let tarjeta = null;
    let activo = false;
    let indice = 0;
    let selector = '';
    let origenFoco = null;
    let escuchas = false;
    let generacion = 0;

    function marcarVisto() {
      try { win.localStorage.setItem(claveTour(), '1'); } catch (e) { /* sin almacenamiento */ }
    }

    function quitarCapa() {
      if (fondo) fondo.remove();
      if (marco) marco.remove();
      if (tarjeta) tarjeta.remove();
      fondo = null;
      marco = null;
      tarjeta = null;
      activo = false;
    }

    function cerrar() {
      const volver = origenFoco;
      try {
        marcarVisto();
        quitarCapa();
        if (volver && doc.contains(volver) && typeof volver.focus === 'function') volver.focus();
      } catch (e) {
        quitarCapa();
      }
    }

    function focos() {
      if (!tarjeta) return [];
      return [...tarjeta.querySelectorAll('button:not([disabled])')];
    }

    function crear() {
      fondo = doc.createElement('div');
      fondo.className = 'ayuda-tour-fondo';
      marco = doc.createElement('div');
      marco.className = 'ayuda-tour-marco';
      marco.setAttribute('aria-hidden', 'true');
      tarjeta = doc.createElement('div');
      tarjeta.className = 'ayuda-tour-card';
      tarjeta.setAttribute('role', 'dialog');
      tarjeta.setAttribute('aria-modal', 'true');
      tarjeta.setAttribute('aria-labelledby', 'ayudaTourTitulo');
      tarjeta.setAttribute('aria-describedby', 'ayudaTourTexto');
      tarjeta.tabIndex = -1;
      tarjeta.innerHTML =
        '<p class="ayuda-tour-paso" id="ayudaTourPaso"></p>' +
        '<h3 id="ayudaTourTitulo"></h3>' +
        '<p id="ayudaTourTexto"></p>' +
        '<div class="ayuda-tour-acciones">' +
        '<button type="button" class="ghost small" data-tour="omitir">Omitir</button>' +
        '<button type="button" class="ghost small" data-tour="anterior">Anterior</button>' +
        '<button type="button" class="primary small" data-tour="siguiente">Siguiente</button>' +
        '</div>';
      doc.body.append(fondo, marco, tarjeta);
      fondo.addEventListener('click', cerrar);
      tarjeta.querySelector('[data-tour="omitir"]').addEventListener('click', cerrar);
      tarjeta.querySelector('[data-tour="anterior"]').addEventListener('click', () => mover(-1));
      tarjeta.querySelector('[data-tour="siguiente"]').addEventListener('click', () => {
        const pasos = pasosVisibles();
        if (indice >= pasos.length - 1) cerrar();
        else mover(1);
      });
    }

    function pintarMarco(el) {
      const r = el.getBoundingClientRect();
      const top = Math.max(0, r.top);
      const left = Math.max(0, r.left);
      const right = Math.min(win.innerWidth, r.right);
      const bottom = Math.min(win.innerHeight, r.bottom);
      marco.style.top = top + 'px';
      marco.style.left = left + 'px';
      marco.style.width = Math.max(0, right - left) + 'px';
      marco.style.height = Math.max(0, bottom - top) + 'px';
    }

    function cabe(top, left, ancho, alto) {
      return top >= MARGEN
        && left >= MARGEN
        && top + alto <= win.innerHeight - MARGEN
        && left + ancho <= win.innerWidth - MARGEN;
    }

    function colocarTarjeta(el) {
      const angosta = win.innerWidth < 768;
      tarjeta.classList.toggle('ayuda-tour-abajo', angosta);
      tarjeta.style.transform = 'none';
      if (angosta) {
        tarjeta.style.top = 'auto';
        tarjeta.style.left = MARGEN + 'px';
        tarjeta.style.right = MARGEN + 'px';
        tarjeta.style.bottom = MARGEN + 'px';
        return;
      }

      tarjeta.style.right = 'auto';
      tarjeta.style.bottom = 'auto';
      tarjeta.style.visibility = 'hidden';
      tarjeta.style.top = MARGEN + 'px';
      tarjeta.style.left = MARGEN + 'px';
      const ancho = tarjeta.offsetWidth;
      const alto = tarjeta.offsetHeight;
      tarjeta.style.visibility = '';

      const r = el.getBoundingClientRect();
      const candidatos = [
        { top: Math.max(MARGEN, Math.min(r.top, win.innerHeight - MARGEN - alto)), left: r.right + HUECO },
        { top: r.bottom + HUECO, left: Math.max(MARGEN, Math.min(r.left, win.innerWidth - MARGEN - ancho)) },
        { top: r.top - HUECO - alto, left: Math.max(MARGEN, Math.min(r.left, win.innerWidth - MARGEN - ancho)) },
        { top: Math.max(MARGEN, Math.min(r.top, win.innerHeight - MARGEN - alto)), left: r.left - HUECO - ancho }
      ];
      let puesto = candidatos.find((c) => cabe(c.top, c.left, ancho, alto));
      if (!puesto) {
        puesto = {
          top: Math.max(MARGEN, (win.innerHeight - alto) / 2),
          left: Math.max(MARGEN, (win.innerWidth - ancho) / 2)
        };
      }
      puesto.top = Math.max(MARGEN, Math.min(puesto.top, win.innerHeight - MARGEN - alto));
      puesto.left = Math.max(MARGEN, Math.min(puesto.left, win.innerWidth - MARGEN - ancho));
      tarjeta.style.top = puesto.top + 'px';
      tarjeta.style.left = puesto.left + 'px';
    }

    function ubicarIndice(pasos) {
      const exacto = pasos.findIndex((paso) => paso.sel === selector);
      if (exacto >= 0) return exacto;
      const orden = PASOS_TOUR.findIndex((paso) => paso.sel === selector);
      const siguiente = pasos.findIndex((paso) => PASOS_TOUR.findIndex((item) => item.sel === paso.sel) > orden);
      return siguiente >= 0 ? siguiente : 0;
    }

    function mostrar(enfocarTarjeta) {
      const pasos = pasosVisibles();
      if (!pasos.length) {
        cerrar();
        return;
      }
      indice = ubicarIndice(pasos);
      const paso = pasos[indice];
      selector = paso.sel;
      const el = elementoVisible(paso.sel);
      if (!el) {
        cerrar();
        return;
      }
      tarjeta.querySelector('#ayudaTourPaso').textContent = 'Paso ' + (indice + 1) + ' de ' + pasos.length;
      tarjeta.querySelector('#ayudaTourTitulo').textContent = paso.titulo;
      tarjeta.querySelector('#ayudaTourTexto').textContent = paso.texto;
      const anterior = tarjeta.querySelector('[data-tour="anterior"]');
      anterior.disabled = indice === 0;
      const siguiente = tarjeta.querySelector('[data-tour="siguiente"]');
      siguiente.textContent = indice === pasos.length - 1 ? 'Listo' : 'Siguiente';
      pintarMarco(el);
      colocarTarjeta(el);
      if (enfocarTarjeta) tarjeta.focus();
    }

    function mover(delta) {
      const pasos = pasosVisibles();
      const actual = pasos.findIndex((paso) => paso.sel === selector);
      const siguiente = (actual < 0 ? 0 : actual) + delta;
      if (siguiente < 0 || siguiente >= pasos.length) return;
      selector = pasos[siguiente].sel;
      indice = siguiente;
      try { mostrar(false); }
      catch (e) { cerrar(); }
    }

    function alMover() {
      if (!activo) return;
      try { mostrar(false); }
      catch (e) { cerrar(); }
    }

    function alTecla(e) {
      if (!activo || !tarjeta) return;
      if (e.key === 'Escape') {
        e.preventDefault();
        cerrar();
        return;
      }
      if (e.key !== 'Tab') return;
      const lista = focos();
      if (!lista.length) return;
      const primero = lista[0];
      const ultimo = lista[lista.length - 1];
      if (e.shiftKey && (doc.activeElement === primero || doc.activeElement === tarjeta)) {
        e.preventDefault();
        ultimo.focus();
      } else if (!e.shiftKey && doc.activeElement === ultimo) {
        e.preventDefault();
        primero.focus();
      }
    }

    function escuchar() {
      if (escuchas) return;
      escuchas = true;
      win.addEventListener('resize', alMover);
      win.addEventListener('scroll', alMover, true);
      doc.addEventListener('keydown', alTecla);
    }

    function empezar() {
      const ticket = ++generacion;
      try {
        if (doc.readyState !== 'complete') {
          win.addEventListener('load', empezar, { once: true });
          return;
        }
        const lanzar = () => {
          try {
            if (ticket !== generacion) return;
            const pasos = pasosVisibles();
            if (!pasos.length) return;
            const foco = doc.activeElement;
            if (!tarjeta || !tarjeta.contains(foco)) origenFoco = foco;
            if (tarjeta) quitarCapa();
            crear();
            activo = true;
            selector = pasos[0].sel;
            indice = 0;
            escuchar();
            mostrar(true);
          } catch (e) {
            cerrar();
          }
        };
        if (doc.fonts && doc.fonts.ready) doc.fonts.ready.then(lanzar).catch(lanzar);
        else lanzar();
      } catch (e) {
        cerrar();
      }
    }

    const repetir = doc.getElementById('repetirTour');
    if (repetir) repetir.addEventListener('click', empezar);

    // La pantalla que pide aceptar la política tapa el resto de la app. El recorrido espera a la primera pantalla real.
    const enAceptarPolitica = /\/Privacidad\/Aceptar$/i.test(win.location.pathname);
    if (!yaVisto() && !reducido && !enAceptarPolitica) {
      if (doc.readyState === 'complete') empezar();
      else win.addEventListener('load', empezar, { once: true });
    }
  }

  function arrancar() {
    const temas = leerTemas();
    filtrarAyuda();
    iniciarPanel(temas);
    if (doc.getElementById('sidebar')) iniciarTour();
  }

  if (doc.readyState === 'loading') doc.addEventListener('DOMContentLoaded', arrancar);
  else arrancar();
})(document, window);
