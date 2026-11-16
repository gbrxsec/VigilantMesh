# Security

Do not commit API keys, HMAC secrets, webhook URLs, VPN mappings, internal IP ranges, machine identifiers or personal data. Use environment variables or a secret manager in deployed environments.

The agent can collect hostname, username, local address, MAC address and VPN adapter information. This is operational metadata and may be personal data. Enable and retain only what your organization needs, obtain the required authorization, and protect the transport with valid TLS certificates.

If a secret is exposed, revoke and rotate it immediately. Do not rely on deleting a file from the latest branch; old clones, logs and caches may still contain it.
