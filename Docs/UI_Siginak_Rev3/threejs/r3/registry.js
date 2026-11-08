// Model registry: name -> { build(): Model, family }. Each family file exports MODELS = { Name: { build, list?, note? } }.
import { MODELS as menu } from './models/menu.js';
import { MODELS as cabinet } from './models/cabinet.js';
import { MODELS as desk } from './models/desk.js';
import { MODELS as walls } from './models/walls.js';
import { MODELS as shelf } from './models/shelf.js';
import { MODELS as crt } from './models/crt.js';
import { MODELS as door } from './models/door.js';
import { MODELS as board } from './models/board.js';
import { MODELS as admission } from './models/admission.js';
import { MODELS as raidkit } from './models/raidkit.js';
import { MODELS as raidpieces } from './models/raidpieces.js';
import { MODELS as raidcivil } from './models/raidcivil.js';
import { MODELS as test } from './models/_test.js';

const FAM = { menu, cabinet, desk, walls, shelf, crt, door, board, admission, raidkit, raidcivil, raidpieces, test };
export const FAMILIES = Object.keys(FAM);
export const MODELS = {};
for (const [fam, list] of Object.entries(FAM)) for (const [name, def] of Object.entries(list)) {
  if (MODELS[name]) throw new Error('duplicate model name ' + name);
  const d = typeof def === 'function' ? { build: def } : def;
  MODELS[name] = { family: fam, ...d };
}
