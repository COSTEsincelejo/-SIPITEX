// Cierra la sesión si no hay actividad. Los minutos salen de la meta sesion-minutos (appsettings).
(() => {
  const meta = document.querySelector('meta[name="sesion-minutos"]');
  if (!meta) return;

  const minutos = Number(meta.getAttribute('content'));
  if (!Number.isFinite(minutos) || minutos < 2) return;

  const totalMs = minutos * 60 * 1000;
  const avisoMs = 60 * 1000;
  const claveActividad = 'sipitex.ultimaActividad';
  const claveCerrar = 'sipitex.cerrar';
  const modal = document.getElementById('sesionAviso');
  const cuenta = document.getElementById('sesionCuenta');
  const formCerrar = document.getElementById('sesionCerrar');

  let ultima = Date.now();
  let avisoAbierto = false;
  let ultimoPing = 0;
  let ultimoAvisoLocal = 0;
  let cerrando = false;

  function guardarActividad(ahora) {
    ultima = ahora;
    if (ahora - ultimoAvisoLocal < 1000) return;
    ultimoAvisoLocal = ahora;
    try { localStorage.setItem(claveActividad, String(ahora)); } catch { /* modo privado */ }
  }

  function leerOtrasPestanas() {
    try {
      const valor = Number(localStorage.getItem(claveActividad));
      if (Number.isFinite(valor) && valor > ultima) ultima = valor;
    } catch { /* modo privado */ }
  }

  function mostrar(segundos) {
    avisoAbierto = true;
    if (!modal) return;
    modal.hidden = false;
    if (cuenta) cuenta.textContent = String(segundos);
  }

  function ocultar() {
    avisoAbierto = false;
    if (modal) modal.hidden = true;
  }

  function irAlLogin() {
    window.location.assign('/Account/Login?inactividad=1');
  }

  function cerrar() {
    if (cerrando) return;
    cerrando = true;
    try { localStorage.setItem(claveCerrar, String(Date.now())); } catch { /* modo privado */ }
    if (formCerrar) formCerrar.submit();
    else irAlLogin();
  }

  async function ping() {
    const headers = window.SipitexAntiforgery ? window.SipitexAntiforgery.headers() : { Accept: 'application/json' };
    const respuesta = await fetch('/Account/KeepAlive', {
      method: 'POST',
      headers,
      credentials: 'same-origin'
    });
    return respuesta.ok;
  }

  function actividad() {
    // Con el aviso abierto no cuenta el mouse: hay que pulsar Seguir conectado.
    if (avisoAbierto || cerrando) return;
    const ahora = Date.now();
    guardarActividad(ahora);
    if (ahora - ultimoPing < 60000) return;
    ultimoPing = ahora;
    ping().catch(() => {});
  }

  function revisar() {
    if (cerrando) return;
    leerOtrasPestanas();
    const restante = totalMs - (Date.now() - ultima);
    if (restante <= 0) {
      cerrar();
      return;
    }
    if (restante <= avisoMs) mostrar(Math.max(1, Math.ceil(restante / 1000)));
    else if (avisoAbierto) ocultar();
  }

  ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart'].forEach((evento) => {
    window.addEventListener(evento, actividad, { passive: true });
  });

  window.addEventListener('storage', (evento) => {
    if (evento.key === claveCerrar) irAlLogin();
    if (evento.key === claveActividad) revisar();
  });

  document.getElementById('sesionSeguir')?.addEventListener('click', async () => {
    const ok = await ping().catch(() => false);
    if (!ok) {
      cerrar();
      return;
    }
    ultimoPing = Date.now();
    guardarActividad(Date.now());
    ocultar();
  });

  document.getElementById('sesionSalir')?.addEventListener('click', () => cerrar());

  const fetchOriginal = window.fetch.bind(window);
  window.fetch = async (...args) => {
    const respuesta = await fetchOriginal(...args);
    if (respuesta.status === 401 && !window.location.pathname.toLowerCase().startsWith('/account/login'))
      window.location.assign('/Account/Login');
    return respuesta;
  };

  const enviarOriginal = XMLHttpRequest.prototype.send;
  XMLHttpRequest.prototype.send = function (...args) {
    this.addEventListener('load', () => {
      if (this.status === 401 && !window.location.pathname.toLowerCase().startsWith('/account/login'))
        window.location.assign('/Account/Login');
    });
    return enviarOriginal.apply(this, args);
  };

  guardarActividad(Date.now());
  window.setInterval(revisar, 1000);
})();
