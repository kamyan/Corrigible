// pipeline test models (not part of the library)
import { Model, gBox } from '../mlib.js';
export const MODELS = {
  _AxisTest: { build: () => {
    const m = new Model('_AxisTest', { family: 'test', budget: 'small', note: 'Unity içe aktarma denetimi: uzun kol +x, kısa kol +z (arkaya), küp +y; sarı taraf ön (−z).' });
    m.box(0.5, 0.05, 0.05, 0.005, 'steelBare', [0.25, 0.025, 0]);     // +x arm
    m.box(0.05, 0.05, 0.25, 0.005, 'steelBare', [0, 0.025, 0.125]);   // +z arm (back)
    m.box(0.08, 0.08, 0.08, 0.008, 'paintHazard', [0, 0.12, -0.04]);  // cube on top, toward the front
    const g = m.part('Flap', { pivot: [0.5, 0.05, 0], axis: 'z', range: [0, 90], note: '+x kolunun ucunda menteşe' });
    m.box(0.12, 0.01, 0.06, 0.002, 'brass', [0.56, 0.055, 0], [0, 0, 0], 'Flap');
    m.text('TXT_Test', { center: [0.25, 0.051, 0], size: [0.3, 0.04], normal: [0, 1, 0], up: [0, 0, 1], role: 'plateLabel', sample: 'TEST' });
    return m; } },
};
