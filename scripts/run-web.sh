#!/usr/bin/env bash
# Arranca SIPITEX en segundo plano (Codespaces / local)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT/src/Sipitex.Web"

LOG=/tmp/sipitex-web.log
PID_FILE=/tmp/sipitex-web.pid

# Si ya hay una instancia, la detengo
if [[ -f "$PID_FILE" ]] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
  echo "Deteniendo instancia anterior (pid $(cat "$PID_FILE"))..."
  kill "$(cat "$PID_FILE")" 2>/dev/null || true
  sleep 1
fi
pkill -f 'Sipitex.Web.dll|dotnet.*Sipitex.Web' 2>/dev/null || true
sleep 1

export ASPNETCORE_URLS="http://0.0.0.0:5240"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

echo "==> Compilando y arrancando SIPITEX en segundo plano..."
nohup dotnet run --launch-profile http --urls "http://0.0.0.0:5240" >"$LOG" 2>&1 &
echo $! >"$PID_FILE"

echo -n "==> Esperando a que responda"
for i in $(seq 1 60); do
  if curl -fsS -o /dev/null http://127.0.0.1:5240/Account/Login 2>/dev/null \
     || curl -fsS -o /dev/null -w "%{http_code}" http://127.0.0.1:5240/Account/Login 2>/dev/null | grep -qE '200|302'; then
    echo
    echo
    echo "OK — SIPITEX está corriendo (pid $(cat "$PID_FILE"))"
    echo
    echo "Abre esta URL (puerto 5240 = Public en PORTS):"
    echo "  https://special-trout-jj9rp4vx6x643gx6-5240.app.github.dev/Account/Login"
    echo
    echo "O en PORTS haz clic en el globo de 5240."
    echo
    echo "Login: admin@sipitex.test"
    echo "Clave: Admin123!"
    echo
    echo "Probar local:  curl -I http://127.0.0.1:5240/Account/Login"
    echo "Ver log:       tail -f $LOG"
    echo "Detener:       kill \$(cat $PID_FILE)"
    exit 0
  fi
  echo -n "."
  sleep 1
done

echo
echo "ERROR: no respondió a tiempo. Últimas líneas del log:"
tail -40 "$LOG"
exit 1
