# Unity CLI and MCP

This project uses Unity's official CLI and its built-in MCP server, connected
through `com.unity.pipeline` in `Packages/manifest.json`.

Configured on this Mac:

- Editor: `6000.5.9f1`
- Unity CLI: `1.0.0-beta.8`, installed at `/Users/jgo/.unity/bin/unity`
- Unity Pipeline: `0.7.0-exp.1`
- Codex server: `unity` in `~/.codex/config.toml`, pinned to this project's
  absolute path using `mcp --project-path`.

## Use

Open this project in Unity. From a terminal in the project directory:

```sh
unity --version
unity status --project-path . --format json
unity command --project-path . --limit 10 --format json
codex mcp get unity --json
```

Restart the Codex client after changing its MCP configuration. Keep Unity open
while using Editor tools. The MCP process is started automatically by Codex.

To configure another Mac, install the official Unity CLI, open this project,
and run this from the project directory:

```sh
unity mcp configure codex --project-path .
```

This writes the user's Codex MCP configuration and pins it to the current
project. It does not require an additional third-party Unity MCP package.

## Troubleshooting

If Unity is open but the CLI reports no Pipeline instance, choose
**Window → Pipeline → Stop Server**, then **Window → Pipeline → Start Server**.
If the server is already stopped, only **Start Server** is needed. This
recreates `Library/Pipeline/.unity-pipeline-port`. That generated file contains
the local authentication token; leave it under the ignored `Library` directory.

CLI probes from restricted sandboxes may need permission to discover local
processes and connect to the Editor on localhost.

## Official documentation

- [Unity CLI reference](https://docs.unity.com/en-us/unity-cli/unity-cli-reference)
- [Unity CLI replaces the in-Editor MCP server](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli)
- [Codex MCP configuration](https://developers.openai.com/codex/mcp/)
