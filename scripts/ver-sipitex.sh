#!/usr/bin/env bash
# Arranque simple y robusto de SIPITEX (Codespaces)
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

LOG=/tmp/sipitex-web.log
PID_FILE=/tmp/sipitex-web.pid
PUBLISH_DIR=/tmp/sipitex-publish

echo "==> 1/4 Actualizando código..."
git fetch origin cursor/postgresql-database-ca9f || true
git checkout cursor/postgresql-database-ca9f || true
git pull --ff-only origin cursor/postgresql-database-ca9f || true

echo "==> 2/4 Limpiando proceso viejo..."
if [[ -f "$PID_FILE" ]]; then
  oldpid="$(cat "$PID_FILE" 2>/dev/null || true)"
  if [[ -n "${oldpid:-}" ]]; then
    kill "$oldpid" 2>/dev/null || true
    sleep 1
    kill -9 "$oldpid" 2>/dev/null || true
  fi
fi
# Liberar puerto 5240 si quedó ocupado
if command -v fuser >/dev/null 2>&1; then
  fuser -k 5240/tcp 2>/dev/null || true
elif command -v lsof >/dev/null 2>&1; then
  lsof -ti :5240 | xargs -r kill -9 2>/dev/null || true
fi
sleep 1
rm -f "$LOG" "$PID_FILE"

echo "==> 3/4 Publicando app (puede tardar 1-2 min)..."
rm -rf "$PUBLISH_DIR"
if ! dotnet publish "$ROOT/src/Sipitex.Web/Sipitex.Web.csproj" -c Release -o "$PUBLISH_DIR" --verbosity minimal; then
  echo
  echo "ERROR: falló la compilación."
  exit 1
fi

echo "==> 4/4 Arrancando servidor..."
export ASPNETCORE_URLS="http://0.0.0.0:5240"
export ASPNETCORE_ENVIRONMENT=Development
export Database__Provider=Sqlite
export ConnectionStrings__DefaultConnection="Data Source=/tmp/sipitex-codespace.db"

nohup dotnet "$PUBLISH_DIR/Sipitex.Web.dll" >"$LOG" 2>&1 &
echo $! >"$PID_FILE"
sleep 2

if ! kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
  echo "ERROR: el proceso murió al iniciar. Log:"
  cat "$LOG"
  exit 1
fi

echo -n "==> Esperando respuesta en :5240"
ok=0
for i in $(seq 1 90); do
  code="$(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5240/ping 2>/dev/null || echo 000)"
  code2="$(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5240/Account/Login 2>/dev/null || echo 000)"
  if [[ "$code" == "200" || "$code2" == "200" || "$code2" == "302" ]]; then
    ok=1
    break
  fi
  # Si el proceso murió, salir con log
  if ! kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
    echo
    echo "ERROR: el proceso se cayó. Log completo:"
    cat "$LOG"
    exit 1
  fi
  echo -n "."
  sleep 1
done
echo

if [[ "$ok" != "1" ]]; then
  echo "ERROR: timeout esperando HTTP. Log completo:"
  cat "$LOG"
  echo
  echo "PID: $(cat "$PID_FILE" 2>/dev/null || echo none)"
  ss -ltnp 2>/dev/null | grep 5240 || netstat -ltnp 2>/dev/null | grep 5240 || true
  exit 1
fi

echo
echo "============================================"
echo "  SIPITEX CORRIENDO"
echo "============================================"
echo "  PID: $(cat "$PID_FILE")"
echo
echo "  Abre la página ASÍ (no uses Chrome externo):"
echo "  1) Ctrl + Shift + P"
echo "  2) Simple Browser: Show"
echo "  3) http://127.0.0.1:5240/Account/Login"
echo
echo "  Usuario: admin@sipitex.test"
echo "  Clave:   Admin123!"
echo
echo "  Ver log:  tail -f $LOG"
echo "  Detener:  kill \$(cat $PID_FILE)"
echo "============================================"
