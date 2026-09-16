(() => {
  function bindPad(canvas, hidden, clearBtn) {
    if (!canvas || !hidden) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const dpr = window.devicePixelRatio || 1;
    const cssWidth = canvas.clientWidth || 320;
    const cssHeight = canvas.clientHeight || 120;
    canvas.width = Math.floor(cssWidth * dpr);
    canvas.height = Math.floor(cssHeight * dpr);
    ctx.scale(dpr, dpr);
    ctx.lineWidth = 2.2;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.strokeStyle = '#1F3864';

    let drawing = false;
    let dirty = false;

    function pos(ev) {
      const rect = canvas.getBoundingClientRect();
      const src = ev.touches ? ev.touches[0] : ev;
      return { x: src.clientX - rect.left, y: src.clientY - rect.top };
    }

    function start(ev) {
      ev.preventDefault();
      drawing = true;
      const p = pos(ev);
      ctx.beginPath();
      ctx.moveTo(p.x, p.y);
    }

    function move(ev) {
      if (!drawing) return;
      ev.preventDefault();
      const p = pos(ev);
      ctx.lineTo(p.x, p.y);
      ctx.stroke();
      dirty = true;
    }

    function end() {
      drawing = false;
      if (dirty) hidden.value = canvas.toDataURL('image/png');
    }

    canvas.addEventListener('mousedown', start);
    canvas.addEventListener('mousemove', move);
    window.addEventListener('mouseup', end);
    canvas.addEventListener('touchstart', start, { passive: false });
    canvas.addEventListener('touchmove', move, { passive: false });
    canvas.addEventListener('touchend', end);

    clearBtn?.addEventListener('click', (ev) => {
      ev.preventDefault();
      ctx.clearRect(0, 0, cssWidth, cssHeight);
      hidden.value = '';
      dirty = false;
    });
  }

  window.SipitexBindSignaturePads = function () {
    document.querySelectorAll('[data-signature-pad]').forEach((wrap) => {
      bindPad(
        wrap.querySelector('canvas'),
        wrap.querySelector('input[type=hidden]'),
        wrap.querySelector('[data-signature-clear]')
      );
    });
  };

  document.addEventListener('DOMContentLoaded', () => window.SipitexBindSignaturePads());
})();
