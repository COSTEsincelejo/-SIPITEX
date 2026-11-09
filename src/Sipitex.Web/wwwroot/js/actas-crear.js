(() => {
  function fold(value) {
    return (value || '')
      .toLocaleLowerCase('es')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }

  function bindOrigen() {
    const origen = document.getElementById('Form_Origen');
    const orden = document.getElementById('Form_ProductionOrderId');
    if (!origen && !orden) return;

    const go = () => {
      const url = new URL(window.location.pathname, window.location.origin);
      if (origen && origen.value !== '') url.searchParams.set('origen', origen.value);
      if (orden && orden.value) url.searchParams.set('orderId', orden.value);
      window.location.href = url.pathname + url.search;
    };

    origen?.addEventListener('change', go);
    orden?.addEventListener('change', go);
  }

  function bindFiltros() {
    const table = document.getElementById('actaMovimientos');
    if (!table) return;

    const rows = Array.from(table.querySelectorAll('tbody tr'));
    const buscar = document.getElementById('actaBuscar');
    const desde = document.getElementById('actaDesde');
    const hasta = document.getElementById('actaHasta');
    const tipo = document.getElementById('actaTipo');
    const master = document.getElementById('actaSeleccionarVisibles');
    const resumen = document.getElementById('actaResumen');

    const visible = (row) => !row.classList.contains('acta-oculta');

    function paint() {
      const shown = rows.filter(visible).length;
      const selected = rows.filter((row) => row.querySelector('input[type="checkbox"]:checked')).length;
      if (resumen) {
        resumen.textContent = 'Mostrando ' + shown + ' de ' + rows.length + ' · ' + selected + ' seleccionados';
      }
      if (!master) return;
      const checks = rows
        .filter(visible)
        .map((row) => row.querySelector('input[type="checkbox"]'))
        .filter((box) => box && !box.disabled);
      const marked = checks.filter((box) => box.checked).length;
      master.checked = checks.length > 0 && marked === checks.length;
      master.indeterminate = marked > 0 && marked < checks.length;
      master.disabled = checks.length === 0;
    }

    function apply() {
      const q = fold(buscar ? buscar.value : '');
      const from = desde ? desde.value : '';
      const to = hasta ? hasta.value : '';
      const kind = tipo ? tipo.value : '';
      rows.forEach((row) => {
        const text = fold(row.getAttribute('data-texto') || '');
        const fecha = row.getAttribute('data-fecha') || '';
        const rowTipo = row.getAttribute('data-tipo') || '';
        const ok = (!q || text.includes(q))
          && (!from || fecha >= from)
          && (!to || fecha <= to)
          && (!kind || rowTipo === kind);
        row.classList.toggle('acta-oculta', !ok);
      });
      paint();
    }

    master?.addEventListener('change', () => {
      rows.forEach((row) => {
        if (!visible(row)) return;
        const box = row.querySelector('input[type="checkbox"]');
        if (!box || box.disabled) return;
        box.checked = master.checked;
      });
      paint();
    });

    rows.forEach((row) => {
      row.querySelector('input[type="checkbox"]')?.addEventListener('change', paint);
    });

    [buscar, desde, hasta, tipo].forEach((el) => {
      el?.addEventListener('input', apply);
      el?.addEventListener('change', apply);
    });
    apply();
  }

  document.addEventListener('DOMContentLoaded', () => {
    bindOrigen();
    bindFiltros();
  });
})();
