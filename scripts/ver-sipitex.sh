#!/usr/bin/env bash
# Un solo comando para ver SIPITEX en Codespaces
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

LOG=/tmp/sipitex-web.log
PID_FILE=/tmp/sipitex-web.pid

echo "==> Actualizando rama..."
git fetch origin cursor/postgresql-database-ca9f 2>/dev/null || true
git checkout cursor/postgresql-database-ca9f 2>/dev/null || true
git pull --ff-only 2>/dev/null || true

echo "==> Deteniendo instancias anteriores..."
if [[ -f "$PID_FILE" ]] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
  kill "$(cat "$PID_FILE")" 2>/dev/null || true
fi
pkill -f 'Sipitex.Web|dotnet.*run.*Sipitex' 2>/dev/null || true
sleep 1

cd "$ROOT/src/Sipitex.Web"
export ASPNETCORE_URLS="http://0.0.0.0:5240"
export ASPNETCORE_ENVIRONMENT=Development
export Database__Provider=Sqlite
export ConnectionStrings__DefaultConnection="Data Source=/tmp/sipitex-codespace.db"

echo "==> Arrancando SIPITEX..."
nohup dotnet run --no-launch-profile --urls "http://0.0.0.0:5240" >"$LOG" 2>&1 &
echo $! >"$PID_FILE"

echo -n "==> Esperando"
ok=0
for i in $(seq 1 90); do
  code="$(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5240/Account/Login 2>/dev/null || true)"
  if [[ "$code" == "200" || "$code" == "302" ]]; then
    ok=1
    break
  fi
  echo -n "."
  sleep 1
done
echo

if [[ "$ok" != "1" ]]; then
  echo "ERROR: no arrancó. Log:"
  tail -50 "$LOG"
  exit 1
fi

# Detectar URL de Codespaces si existe
CS_URL=""
if [[ -n "${CODESPACE_NAME:-}" ]]; then
  CS_URL="https://${CODESPACE_NAME}-5240.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-app.github.dev}/Account/Login"
fi

echo
echo "============================================"
echo "  SIPITEX LISTO"
echo "============================================"
echo "  NO abras Chrome externo (suele dar 404)."
echo
echo "  Forma que SÍ funciona en Codespaces:"
echo "  1) Ctrl+Shift+P"
echo "  2) Escribe: Simple Browser: Show"
echo "  3) Pega: http://127.0.0.1:5240/Account/Login"
echo
echo "  O en PORTS → 5240 → clic en el icono del globo"
echo "  (Open in Browser), NO copies la URL a Chrome."
echo
echo "  Usuario: admin@sipitex.test"
echo "  Clave:   Admin123!"
echo
echo "  Prueba local: curl -I http://127.0.0.1:5240/ping"
echo "  Detener:     kill \$(cat $PID_FILE)"
echo "============================================"

# Intentar marcar el puerto público con gh si está disponible
if command -v gh >/dev/null 2>&1 && [[ -n "${CODESPACE_NAME:-}" ]]; then
  gh codespace ports visibility 5240:public -c "$CODESPACE_NAME" 2>/dev/null \
    && echo "Puerto 5240 marcado como Public." \
    || true
fi
