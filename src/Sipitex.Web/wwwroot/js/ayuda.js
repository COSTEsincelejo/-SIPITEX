// Centro de ayuda: filtro de temas, panel "?" y recorrido del primer ingreso.
(function (doc, win) {
  const CLAVE_TOUR = 'sipitex-ayuda-tour';

  function plegar(texto) {
    return String(texto || '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9\s]/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
  }

  function distancia(a, b) {
    const limite = a.length >= 8 || b.length >= 8 ? 2 : 1;
    if (Math.abs(a.length - b.length) > limite) return limite + 1;
    const filas = [new Array(b.length + 1), new Array(b.length + 1)];
    for (let j = 0; j <= b.length; j++) filas[0][j] = j;
    for (let i = 1; i <= a.length; i++) {
      const actual = filas[i % 2];
      const anterior = filas[(i - 1) % 2];
      actual[0] = i;
      let minimo = actual[0];
      for (let j = 1; j <= b.length; j++) {
        const costo = a[i - 1] === b[j - 1] ? 0 : 1;
        actual[j] = Math.min(actual[j - 1] + 1, anterior[j] + 1, anterior[j - 1] + costo);
        if (actual[j] < minimo) minimo = actual[j];
      }
      if (minimo > limite) return limite + 1;
    }
    return filas[a.length % 2][b.length];
  }

  function coincide(consulta, texto) {
    const q = plegar(consulta);
    if (!q) return true;
    const base = plegar(texto);
    const palabras = base.split(' ').filter(Boolean);
    return q.split(' ').filter(Boolean).every((token) => {
      if (base.includes(token)) return true;
      if (token.length < 5) return false;
      const tope = token.length >= 8 ? 2 : 1;
      return palabras.some((palabra) => distancia(token, palabra) <= tope);
    });
  }

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
    const estado = doc.getElementById('ayudaBuscarEstado');
    const vacio = doc.getElementById('ayudaVacio');
    if (!input) return;

    function aplicar() {
      const q = input.value || '';
      let visibles = 0;
      doc.querySelectorAll('[data-ayuda-id]').forEach((tema) => {
        const ok = coincide(q, tema.getAttribute('data-busca') || '');
        tema.hidden = !ok;
        if (ok) visibles += 1;
      });
      doc.querySelectorAll('[data-ayuda-grupo]').forEach((grupo) => {
        const tieneTema = grupo.querySelector('[data-ayuda-id]');
        if (!tieneTema) {
          const items = grupo.querySelectorAll('[data-glosario]');
          let alguno = false;
          items.forEach((item) => {
            const ok = coincide(q, item.getAttribute('data-busca') || '');
            item.hidden = !ok;
            if (ok) alguno = true;
          });
          grupo.hidden = q && !alguno;
          return;
        }
        const queda = grupo.querySelector('[data-ayuda-id]:not([hidden])');
        grupo.hidden = !queda;
      });
      if (vacio) vacio.hidden = !q || visibles > 0;
      if (estado) {
        estado.textContent = !q
          ? ''
          : (visibles === 1 ? '1 tema' : visibles + ' temas');
      }
    }

    input.addEventListener('input', aplicar);
  }

  function textoRoles(roles) {
    return (roles || []).join(' · ');
  }

  function llenarPanel(tema) {
    const titulo = doc.getElementById('ayudaPanelTitulo');
    const cuerpo = doc.getElementById('ayudaPanelCuerpo');
    if (!titulo || !cuerpo || !tema) return;
    titulo.textContent = tema.titulo || 'Ayuda';
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
    { sel: '#globalSearch', titulo: 'Buscador', texto: 'Escriba lo que quiere hacer, por ejemplo «crear orden» o «ayuda». Ctrl+K enfoca este cuadro.' },
    { sel: '#topbarAlertas', titulo: 'Alertas', texto: 'La campana abre los avisos de stock y las notificaciones de su rol.' },
    { sel: '#topbarPerfil', titulo: 'Perfil', texto: 'Su nombre abre Mi perfil: foto, funciones y contraseña. Al final del menú también está Ayuda.' }
  ];

  function iniciarTour() {
    const reducido = win.matchMedia && win.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const pasos = PASOS_TOUR.filter((p) => doc.querySelector(p.sel));
    if (!pasos.length) return;

    const marco = doc.createElement('div');
    marco.className = 'ayuda-tour-marco';
    marco.hidden = true;
    const tarjeta = doc.createElement('div');
    tarjeta.className = 'ayuda-tour-card';
    tarjeta.hidden = true;
    tarjeta.setAttribute('role', 'dialog');
    tarjeta.setAttribute('aria-modal', 'true');
    tarjeta.setAttribute('aria-labelledby', 'ayudaTourTitulo');
    tarjeta.innerHTML =
      '<p class="ayuda-tour-paso" id="ayudaTourPaso"></p>' +
      '<h3 id="ayudaTourTitulo"></h3>' +
      '<p id="ayudaTourTexto"></p>' +
      '<div class="ayuda-tour-acciones">' +
      '<button type="button" class="ghost small" data-tour="omitir">Omitir</button>' +
      '<button type="button" class="ghost small" data-tour="anterior">Anterior</button>' +
      '<button type="button" class="primary small" data-tour="siguiente">Siguiente</button>' +
      '</div>';
    doc.body.append(marco, tarjeta);

    let indice = 0;
    function marcarVisto() {
      try { win.localStorage.setItem(CLAVE_TOUR, '1'); } catch (e) { /* sin almacenamiento */ }
    }
    function cerrar() {
      marco.hidden = true;
      tarjeta.hidden = true;
      marcarVisto();
    }
    function ubicar() {
      const paso = pasos[indice];
      const el = doc.querySelector(paso.sel);
      if (!el) return;
      const r = el.getBoundingClientRect();
      const m = 6;
      marco.hidden = false;
      marco.style.top = Math.max(8, r.top - m) + 'px';
      marco.style.left = Math.max(8, r.left - m) + 'px';
      marco.style.width = r.width + m * 2 + 'px';
      marco.style.height = r.height + m * 2 + 'px';
      tarjeta.hidden = false;
      const abajo = r.bottom + 16 + 180 < win.innerHeight;
      tarjeta.style.top = (abajo ? r.bottom + 12 : Math.max(12, r.top - 12)) + 'px';
      tarjeta.style.left = Math.min(Math.max(12, r.left), win.innerWidth - 340) + 'px';
      if (!abajo) tarjeta.style.transform = 'translateY(-100%)';
      else tarjeta.style.transform = '';
      tarjeta.querySelector('#ayudaTourPaso').textContent = 'Paso ' + (indice + 1) + ' de ' + pasos.length;
      tarjeta.querySelector('#ayudaTourTitulo').textContent = paso.titulo;
      tarjeta.querySelector('#ayudaTourTexto').textContent = paso.texto;
      const anterior = tarjeta.querySelector('[data-tour="anterior"]');
      anterior.disabled = indice === 0;
      const siguiente = tarjeta.querySelector('[data-tour="siguiente"]');
      siguiente.textContent = indice === pasos.length - 1 ? 'Listo' : 'Siguiente';
    }
    function ir(delta) {
      indice = Math.min(pasos.length - 1, Math.max(0, indice + delta));
      ubicar();
      if (delta > 0) tarjeta.querySelector('[data-tour="siguiente"]').focus();
    }

    tarjeta.querySelector('[data-tour="siguiente"]').addEventListener('click', () => {
      if (indice >= pasos.length - 1) cerrar();
      else ir(1);
    });
    tarjeta.querySelector('[data-tour="anterior"]').addEventListener('click', () => ir(-1));
    tarjeta.querySelector('[data-tour="omitir"]').addEventListener('click', cerrar);
    doc.addEventListener('keydown', (e) => {
      if (tarjeta.hidden) return;
      if (e.key === 'Escape') {
        e.preventDefault();
        cerrar();
      }
    });
    win.addEventListener('resize', () => { if (!tarjeta.hidden) ubicar(); });

    function empezar() {
      indice = 0;
      ubicar();
      tarjeta.querySelector('[data-tour="siguiente"]').focus();
    }

    const repetir = doc.getElementById('repetirTour');
    if (repetir) {
      repetir.addEventListener('click', () => {
        try { win.localStorage.removeItem(CLAVE_TOUR); } catch (e) { /* sigue el recorrido */ }
        empezar();
      });
    }

    let visto = false;
    try { visto = win.localStorage.getItem(CLAVE_TOUR) === '1'; } catch (e) { visto = true; }
    if (!visto && !reducido) win.setTimeout(empezar, 700);
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
