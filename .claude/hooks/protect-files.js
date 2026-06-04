#!/usr/bin/env node
/**
 * PreToolUse hook — protege archivos criticos del proyecto.
 *
 * Bloquea Edit/Write/MultiEdit/NotebookEdit sobre archivos que, segun CLAUDE.md,
 * NO se modifican sin aprobacion explicita de Sebastian:
 *   - 3 archivos de combate (arquitectura sensible)
 *   - 3 docs que solo Sebastian edita (GOLDEN_RULES, GDD, DESIGN_DECISIONS)
 *
 * Mecanismo: lee el JSON del tool call por stdin; si el path apunta a un archivo
 * protegido, sale con codigo 2 (bloquea la herramienta y devuelve el mensaje a
 * Claude por stderr).
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
];

function norm(p) {
  return String(p || '').replace(/\\/g, '/').toLowerCase();
}

let raw = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (c) => (raw += c));
process.stdin.on('end', () => {
  let path = '';
  try {
    const data = JSON.parse(raw || '{}');
    const ti = data.tool_input || {};
    path = ti.file_path || ti.notebook_path || ti.path || '';
  } catch (_) {
    process.exit(0); // si no se puede parsear, no bloquear
  }

  const target = norm(path);
  if (!target) process.exit(0);

  const hit = PROTECTED.find((p) => target.endsWith(norm(p)));
  if (hit) {
    process.stderr.write(
      `BLOQUEADO por hook protect-files: "${hit}" es un archivo protegido ` +
        `(ver CLAUDE.md > Sobre archivos). NO lo edites sin aprobacion explicita ` +
        `de Sebastian. Si ya la tenes, pedile que confirme y desactive el hook ` +
        `temporalmente, o que aplique el cambio a mano.`
    );
    process.exit(2);
  }
  process.exit(0);
});
