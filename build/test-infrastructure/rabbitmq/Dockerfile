# ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-08.
#
# RabbitMQ test fixture. Every input is immutably bound:
#   - the base image by digest, never by a moving tag
#   - the delayed message exchange plugin by SHA-256 over its bytes
#
# Credentials are never baked in. The run-scoped account is supplied at container start
# through RABBITMQ_DEFAULT_USER and RABBITMQ_DEFAULT_PASS, so no fixed product credential
# and no image layer ever carries a secret. 'guest' is not used and loopback_users stays
# at its secure default: a non-guest account has no loopback restriction to relax.
FROM rabbitmq:4.2-management@sha256:a2751b3b5eed89e47ebbd5e776de0d6d85d924002773933ab881da49c073b85d

ADD --chmod=644 \
    --checksum=sha256:f168b2c09810cde3726961d31f38e3408e6a7fbff3929908d6f962061d8e70a1 \
    https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v4.2.0/rabbitmq_delayed_message_exchange-4.2.0.ez \
    /opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-4.2.0.ez

COPY enabled_plugins /etc/rabbitmq/enabled_plugins
