#!/usr/bin/env node
/**
 * PreToolUse hook — protege archivos criticos del proyecto.
 *
 * Bloquea las escrituras sobre archivos que, segun CLAUDE.md, NO se modifican
 * sin aprobacion explicita de Sebastian:
 *   - 3 archivos de combate (arquitectura sensible)
 *   - 3 docs que solo Sebastian edita (GOLDEN_RULES, GDD, DESIGN_DECISIONS)
 *   - el propio hook (evita auto-desenganche silencioso; ver nota abajo)
 *
 * Cubre DOS superficies de escritura:
 *   1. Edit / Write / MultiEdit / NotebookEdit  -> via tool_input.file_path.
 *   2. Bash con comandos del plugin Unity-MCP (`script-update-or-create`,
 *      `script-delete`) que sobreescriben/borran .cs en disco SIN pasar por
 *      Edit/Write. Sin este matcher, `npx unity-mcp-cli run-tool
 *      script-update-or-create ... TurnManager.cs` editaba un protegido sin
 *      prompt ni hook (agujero T1 de la auditoria integral 2026-06).
 *
 * Mecanismo: lee el JSON del tool call por stdin; si el target apunta a un
 * archivo protegido, sale con codigo 2 (bloquea la herramienta y devuelve el
 * mensaje a Claude por stderr).
 *
 * Riesgo residual conocido: el agente podria editar .claude/settings.json para
 * desenganchar el hook y luego editar un protegido. Eso ya NO es silencioso —
 * es una accion deliberada, visible en el diff y revisable. settings.json NO se
 * protege a proposito (lo necesita el flujo update-config).
 *
 * Override puntual: si Sebastian autoriza editar uno de estos archivos, comenta
 * temporalmente el hook en .claude/settings.json o pidele que edite el a mano.
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
// Edit/Write. Si aparecen en un Bash junto a un archivo protegido, se bloquea.
const MCP_WRITE_TOOLS = ['script-update-or-create', 'script-delete'];

function norm(p) {
  return String(p || '').replace(/\\/g, '/').toLowerCase();
}

function basename(p) {
  const n = norm(p);
  const i = n.lastIndexOf('/');
  return i >= 0 ? n.slice(i + 1) : n;
}

function blocked(hit) {
  process.stderr.write(
    `BLOQUEADO por hook protect-files: "${hit}" es un archivo protegido ` +
      `(ver CLAUDE.md > Sobre archivos). NO lo edites sin aprobacion explicita ` +
      `de Sebastian. Si ya la tenes, pedile que confirme y desactive el hook ` +
      `temporalmente, o que aplique el cambio a mano.`
  );
  process.exit(2);
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
    process.exit(0); // si no se puede parsear, no bloquear
  }

  // Superficie 1: Edit/Write/MultiEdit/NotebookEdit -> path directo.
  const target = norm(ti.file_path || ti.notebook_path || ti.path || '');
  if (target) {
    const hit = PROTECTED.find((p) => target.endsWith(norm(p)));
    if (hit) blocked(hit);
  }

  // Superficie 2: Bash -> comandos MCP que escriben/borran .cs en disco.
  const command = norm(ti.command || '');
  if (command && MCP_WRITE_TOOLS.some((t) => command.includes(t))) {
    const hit = PROTECTED.find(
      (p) => command.includes(norm(p)) || command.includes(basename(p))
    );
    if (hit) blocked(hit);
  }

  process.exit(0);
});
