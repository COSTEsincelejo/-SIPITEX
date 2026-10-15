// Token de antiforgery para fetch. La meta csrf-token la pone el layout autenticado.
(() => {
  function token() {
    return document.querySelector('meta[name="csrf-token"]')?.getAttribute('content') || '';
  }

  window.SipitexAntiforgery = {
    token,
    headers(extra) {
      const headers = {
        RequestVerificationToken: token(),
        Accept: 'application/json',
        'X-Requested-With': 'XMLHttpRequest'
      };
      if (extra) {
        Object.keys(extra).forEach((key) => {
          headers[key] = extra[key];
        });
      }
      return headers;
    }
  };
})();
