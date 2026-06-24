#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

ENV_FILE="${MONSTERASP_ENV_FILE:-.env.deploy}"
if [[ ! -f "$ENV_FILE" ]]; then
  cat >&2 <<'EOF'
Missing .env.deploy.

Create /Users/judyahmed/TanzeemBackend/.env.deploy with:
MONSTERASP_FTP_HOST=site75553.siteasp.net
MONSTERASP_FTP_USER=your-ftp-login
MONSTERASP_FTP_PASS=your-ftp-password
MONSTERASP_FTP_REMOTE_DIR=wwwroot

Do not commit this file.
EOF
  exit 1
fi

set -a
source "$ENV_FILE"
set +a

: "${MONSTERASP_FTP_HOST:?Missing MONSTERASP_FTP_HOST}"
: "${MONSTERASP_FTP_USER:?Missing MONSTERASP_FTP_USER}"
: "${MONSTERASP_FTP_PASS:?Missing MONSTERASP_FTP_PASS}"

REMOTE_DIR="${MONSTERASP_FTP_REMOTE_DIR:-wwwroot}"
PUBLISH_DIR="${PUBLISH_DIR:-/Users/judyahmed/TanzeemBackend/publish}"

if [[ ! -d "$PUBLISH_DIR" ]]; then
  echo "Publish directory not found: $PUBLISH_DIR" >&2
  exit 1
fi

remote_url="ftp://${MONSTERASP_FTP_HOST}/${REMOTE_DIR}/"

find "$PUBLISH_DIR" -type f -print0 | while IFS= read -r -d '' file; do
  rel="${file#$PUBLISH_DIR/}"
  case "$rel" in
    appsettings.Development*.json)
      echo "Skipping ${rel}"
      continue
      ;;
  esac
  remote_path="${remote_url}${rel}"
  echo "Uploading ${rel}"
  curl --fail --silent --show-error --ftp-create-dirs \
    --retry 5 --retry-delay 2 --retry-all-errors \
    --user "${MONSTERASP_FTP_USER}:${MONSTERASP_FTP_PASS}" \
    --upload-file "$file" \
    "$remote_path" >/dev/null
done

echo "Backend upload completed."
