#!/usr/bin/env bash
# ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-08.
#
# Generates a fresh broker account for exactly one run and exports it.
# Nothing is written to disk and nothing is echoed, so no credential can reach a log.
# Under GitHub Actions the values are registered as masked secrets and appended to the
# job environment; locally the script is sourced.
#
#   source ./new-run-credentials.sh
#
# The account is deliberately not 'guest' and not 'admin'. A non-guest RabbitMQ account
# carries no loopback restriction, so loopback_users never has to be relaxed.
#
# The account NAME is fixed because ActiveMQ authorises the web console by role and the role
# binding lives in a config file. A name grants nothing on its own; the SECRET is regenerated
# on every run and never written to disk or a log.
set -euo pipefail

# Sourcing detection differs per shell, so probe it without assuming bash-only variables.
_vicione_sourced=0
if [ -n "${BASH_VERSION:-}" ]; then
    [ "${BASH_SOURCE-$0}" != "${0}" ] && _vicione_sourced=1
elif [ -n "${ZSH_VERSION:-}" ]; then
    case "${ZSH_EVAL_CONTEXT:-}" in *:file:*) _vicione_sourced=1 ;; esac
fi

if [ "${_vicione_sourced}" -eq 0 ] && [ -z "${GITHUB_ENV:-}" ]; then
    echo "This script must be sourced: source ./new-run-credentials.sh" >&2
    return 1 2>/dev/null || exit 1
fi

# hexdump reads a fixed byte count, so no pipe is closed early. A 'tr | head' pipeline would
# raise SIGPIPE and abort the script under 'set -o pipefail'.
_vicione_secret() {
    # 32 hex characters, 16 bytes from the kernel CSPRNG.
    hexdump -n 16 -e '4/4 "%08x" 1 "\n"' /dev/urandom
}

VICIONE_SERVICEBUS_RMQ_USER="vicione_ci"
VICIONE_SERVICEBUS_RMQ_PASS="$(_vicione_secret)"
VICIONE_SERVICEBUS_AMQ_USER="vicione_ci"
VICIONE_SERVICEBUS_AMQ_PASS="$(_vicione_secret)"
VICIONE_SERVICEBUS_ARTEMIS_USER="vicione_ci"
VICIONE_SERVICEBUS_ARTEMIS_PASS="$(_vicione_secret)"
VICIONE_SERVICEBUS_PG_USER="vicione_ci"
VICIONE_SERVICEBUS_PG_PASS="$(_vicione_secret)"
# SQL Server enforces password complexity and the 'sa' account cannot be renamed, so the name is fixed
# and the suffix guarantees an upper case letter, a digit and a symbol on top of the random part.
VICIONE_SERVICEBUS_MSSQL_PASS="$(_vicione_secret)Aa1!"
export VICIONE_SERVICEBUS_RMQ_USER VICIONE_SERVICEBUS_RMQ_PASS
export VICIONE_SERVICEBUS_AMQ_USER VICIONE_SERVICEBUS_AMQ_PASS
export VICIONE_SERVICEBUS_ARTEMIS_USER VICIONE_SERVICEBUS_ARTEMIS_PASS
export VICIONE_SERVICEBUS_PG_USER VICIONE_SERVICEBUS_PG_PASS VICIONE_SERVICEBUS_MSSQL_PASS

if [ -n "${GITHUB_ENV:-}" ]; then
    # Mask before export so the values can never surface in the job log.
    echo "::add-mask::${VICIONE_SERVICEBUS_RMQ_PASS}"
    echo "::add-mask::${VICIONE_SERVICEBUS_AMQ_PASS}"
    echo "::add-mask::${VICIONE_SERVICEBUS_ARTEMIS_PASS}"
    echo "::add-mask::${VICIONE_SERVICEBUS_PG_PASS}"
    echo "::add-mask::${VICIONE_SERVICEBUS_MSSQL_PASS}"
    {
        echo "VICIONE_SERVICEBUS_RMQ_USER=${VICIONE_SERVICEBUS_RMQ_USER}"
        echo "VICIONE_SERVICEBUS_RMQ_PASS=${VICIONE_SERVICEBUS_RMQ_PASS}"
        echo "VICIONE_SERVICEBUS_AMQ_USER=${VICIONE_SERVICEBUS_AMQ_USER}"
        echo "VICIONE_SERVICEBUS_AMQ_PASS=${VICIONE_SERVICEBUS_AMQ_PASS}"
        echo "VICIONE_SERVICEBUS_ARTEMIS_USER=${VICIONE_SERVICEBUS_ARTEMIS_USER}"
        echo "VICIONE_SERVICEBUS_ARTEMIS_PASS=${VICIONE_SERVICEBUS_ARTEMIS_PASS}"
        echo "VICIONE_SERVICEBUS_PG_USER=${VICIONE_SERVICEBUS_PG_USER}"
        echo "VICIONE_SERVICEBUS_PG_PASS=${VICIONE_SERVICEBUS_PG_PASS}"
        echo "VICIONE_SERVICEBUS_MSSQL_PASS=${VICIONE_SERVICEBUS_MSSQL_PASS}"
    } >> "${GITHUB_ENV}"
fi

echo "Run-scoped broker credentials generated (values are not printed)."
