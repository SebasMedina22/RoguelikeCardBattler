#!/usr/bin/env node
/**
 * PreToolUse hook — precaucion sobre archivos criticos del proyecto.
 *
 * Estos archivos, segun CLAUDE.md, no se tocan a la ligera:
 *   - 3 archivos de combate (arquitectura sensible)
 *   - 3 docs de autoridad (GOLDEN_RULES, GDD, DESIGN_DECISIONS)
 *   - el propio hook
 *
 * Modelo (cambiado 2026-06-16 a pedido de Sebastian): NO bloquea de forma dura.
 * Devuelve la decision de permiso "ask" -> Claude Code muestra un prompt de
 * confirmacion (allow/deny) en el momento. Si Sebastian aprueba, el agente edita;
 * si no, se cancela. La idea es que el agente edite por Sebastian con una
 * precaucion visible, no que Sebastian tenga que editar a mano. Antes el hook
 * hacia `exit 2` (bloqueo duro); eso obligaba al baile de desenganche o a edicion
 * manual, friccion que Sebastian decidio sacar.
 *
 * Cubre DOS superficies de escritura:
 *   1. Edit / Write / MultiEdit / NotebookEdit  -> via tool_input.file_path.
 *   2. Bash con comandos del plugin Unity-MCP (`script-update-or-create`,
 *      `script-delete`) que sobreescriben/borran .cs en disco SIN pasar por
 *      Edit/Write (vector T1 de la auditoria integral 2026-06). Sigue cubierto:
 *      tambien pide confirmacion.
 *
 * Mecanismo: lee el JSON del tool call por stdin; si el target apunta a un
 * archivo protegido, emite por stdout la decision "ask" y sale con codigo 0.
 * Si no toca un protegido, sale 0 sin opinar (flujo de permisos normal).
 */

const PROTECTED = [
  'Assets/Scripts/Gameplay/Combat/TurnManager.cs',
  'Assets/Scripts/Gameplay/Combat/ActionQueue.cs',
  'Assets/Scripts/Gameplay/Combat/PlayerCombatActor.cs',
  'Docs/dev/GOLDEN_RULES.md',
  'Docs/design/_gdd.md',
  'Docs/design/DESIGN_DECISIONS.md',
  '.claude/hooks/protect-files.js',
];

// Comandos del plugin Unity-MCP que escriben/borran .cs en disco SIN pasar por
// Edit/Write. Si aparecen en un Bash junto a un archivo protegido, se confirma.
const MCP_WRITE_TOOLS = ['script-update-or-create', 'script-delete'];

// Anclado a la forma REAL de invocacion del CLI (`run-tool <write-tool>`). Asi
// no se confirma por comandos que solo MENCIONAN el token en prosa (mensajes de
// commit, echo, armar payloads de test). El CLI siempre pasa por `run-tool <tool>`,
// asi que esto sigue siendo agnostico al prefijo (npx / npx --yes ...@latest / binario).
const MCP_WRITE_RE = new RegExp(`run-tool\\s+(${MCP_WRITE_TOOLS.join('|')})\\b`);

function norm(p) {
  return String(p || '').replace(/\\/g, '/').toLowerCase();
}

function basename(p) {
  const n = norm(p);
  const i = n.lastIndexOf('/');
  return i >= 0 ? n.slice(i + 1) : n;
}

function ask(hit) {
  // Decision de permiso "ask": Claude Code pide confirmacion a Sebastian antes
  // de ejecutar la herramienta. No bloquea de forma dura.
  const out = {
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: 'ask',
      permissionDecisionReason:
        `"${hit}" es un archivo protegido (ver CLAUDE.md > Sobre archivos). ` +
        `Confirma con Sebastian antes de editarlo.`,
    },
  };
  process.stdout.write(JSON.stringify(out));
  process.exit(0);
}

let raw = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (c) => (raw += c));
process.stdin.on('end', () => {
  let ti = {};
  try {
    const data = JSON.parse(raw || '{}');
    ti = data.tool_input || {};
  } catch (_) {
    process.exit(0); // si no se puede parsear, no opinar
  }

  // Superficie 1: Edit/Write/MultiEdit/NotebookEdit -> path directo.
  const target = norm(ti.file_path || ti.notebook_path || ti.path || '');
  if (target) {
    const hit = PROTECTED.find((p) => target.endsWith(norm(p)));
    if (hit) ask(hit);
  }

  // Superficie 2: Bash -> comandos del plugin Unity-MCP que escriben/borran .cs.
  const command = norm(ti.command || '');
  if (MCP_WRITE_RE.test(command)) {
    const hit = PROTECTED.find(
      (p) => command.includes(norm(p)) || command.includes(basename(p))
    );
    if (hit) ask(hit);
  }

  process.exit(0);
});
