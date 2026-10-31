// Sample game states (numbers as in the game screenshots; illustrative).
export const STATES = {
  v001: { map: { regions: { weu: 'front' }, sectors: { weu: ['ready', 'lock', 'lock'] }, frontPct: '%100', frontGap: 11, trait: { weu: 'corridor' }, sig: { weu: 'locker' }, tier: '1/4', team: [668, 380] } },
  v014: { map: { regions: { weu: 'free', eeu: 'front' }, freeShift: { weu: 'V.011' }, sectors: { eeu: ['done', 'ready', 'lock'] }, frontPct: '%66,7', trait: { eeu: 'corridor' }, sig: { eeu: 'locker' }, tier: '1/4' } },
  v015ok: { map: { regions: { weu: 'free', eeu: 'front' }, freeShift: { weu: 'V.011' }, sectors: { eeu: ['done', 'done', 'ready'] }, frontPct: '%33,3', frontGap: 20, trait: { eeu: 'corridor' }, sig: { eeu: 'locker' }, tier: '1/4' } },
  v015ca: { map: { regions: { weu: 'free', eeu: 'front' }, freeShift: { weu: 'V.011' }, sectors: { eeu: ['reocc', 'ready', 'lock'] }, frontPct: '%100', frontGap: 11, trait: { eeu: 'corridor' }, sig: { eeu: 'locker' }, tier: '1/4', reocc: 'eeu' } },
  v031: { map: { regions: { weu: 'free', eeu: 'free', me: 'free', af: 'free', ru: 'free', in: 'front' }, freeShift: { weu: 'V.011', eeu: 'V.019', me: 'V.024', af: 'V.027', ru: 'V.030' }, sectors: { in: ['done', 'ready', 'lock'] }, frontPct: '%66,7', trait: { in: 'sand' }, sig: { in: 'mortar' }, tier: '3/4' } },
  win: { map: { regions: { na: 'free', sa: 'free', weu: 'free', eeu: 'free', ru: 'free', me: 'free', af: 'free', in: 'free', cn: 'free', jk: 'free', sea: 'free', oc: 'free' }, freeShift: { weu: 'V.011', eeu: 'V.019', me: 'V.024', af: 'V.027', ru: 'V.030', in: 'V.034', cn: 'V.038', jk: 'V.040', sea: 'V.043', oc: 'V.045', na: 'V.046', sa: 'V.048' }, tier: '4/4' } },
  lose: { map: { regions: { weu: 'free', eeu: 'free', me: 'front' }, freeShift: { weu: 'V.011', eeu: 'V.019' }, sectors: { me: ['done', 'lock', 'lock'] }, frontPct: '%66,7', tier: '2/4' } },
};

// desk / room states
const R = (name, line2, level, staff, cap, powered = true, stamp) => ({ name, line2, level, staff, cap, powered, stamp });
export const DESK = {
  v001: { counters: [{ d: '001' }, { d: '060', frac: '0' }, { d: '24', fixed: '/30' }, { d: '014' }, { d: '00', fixed: '/12' }, { d: '10', fixed: '/20' }], alarm: 0, freed: [], nextTarget: 'B. Avrupa?',
    telexHang: [['001/00', { t: 'KYGA-07 HAT AÇIK' }, 'Analog hat açık. Emirler kâğıtla.']],
    crt: [['DİNLEMEDE', 150, '#a6e8b8', true], ['ALFA · HAZIR', 110], ['BATI AVR. / 01', 104], ['YZ ALARMI 0/10', 64, '#5e9c73']],
    state: { idle: 4, reserve: 0, erzak: 24, malzeme: 14, plaka: 0, casting: 0, rooms: { yasam: R('YAŞAM ALANI', 'SV 1 · 2/2', 1, 2, 2), telsiz: R('TELSİZ ODASI', 'SV 1 · 0/2', 1, 0, 2), atolye: R('ATÖLYE', 'SV 0 · 0/0', 0, 0, 0, false, 'HASARLI'), revir: R('REVİR', 'SV 0 · 0/0', 0, 0, 0, false, 'HASARLI') } },
    shiftsLeft: '17 vardiya' },
  v014: { counters: [{ d: '014' }, { d: '062', frac: '0' }, { d: '22', fixed: '/30' }, { d: '019' }, { d: '01', fixed: '/12' }, { d: '11', fixed: '/20' }], alarm: 4, freed: [['weu', 'V.011']], nextTarget: 'D. Avrupa?',
    telexHang: [['014/08', { t: 'YAPILAMADI', red: true }, 'Revir onarımı: malzeme yetersiz (24 / 19)']],
    crt: [['DİNLEMEDE', 150, '#a6e8b8', true], ['ALFA · HAZIR', 110], ['DOĞU AVR. / 02', 104], ['YZ ALARMI 4/10', 64, '#5e9c73']],
    state: { idle: 4, reserve: 1, erzak: 22, malzeme: 19, plaka: 0, casting: 0, rooms: { yasam: R('YAŞAM ALANI', 'SV 1 · 2/2', 1, 2, 2), telsiz: R('TELSİZ ODASI', 'SV 1 · 0/2', 1, 0, 2), atolye: R('ATÖLYE', 'SV 3/3 · 2/6', 3, 2, 6), revir: R('REVİR', 'SV 0 · 0/0', 0, 0, 0, false, 'HASARLI') } },
    shiftsLeft: '15 vardiya' },
  v015: { counters: [{ d: '015' }, { d: '064', frac: '5' }, { d: '20', fixed: '/30' }, { d: '019' }, { d: '01', fixed: '/12' }, { d: '11', fixed: '/20' }], alarm: 5 },
  v015f: { counters: [{ d: '015' }, { d: '052', frac: '5' }, { d: '20', fixed: '/30' }, { d: '019' }, { d: '01', fixed: '/12' }, { d: '11', fixed: '/20' }], alarm: 6 },
  v015ok: { counters: [{ d: '015' }, { d: '062', frac: '5' }, { d: '26', fixed: '/30' }, { d: '026' }, { d: '01', fixed: '/12' }, { d: '11', fixed: '/20' }], alarm: 5 },
  win: { counters: [{ d: '048' }, { d: '140', frac: '0' }, { d: '30', fixed: '/30' }, { d: '064' }, { d: '12', fixed: '/12' }, { d: '16', fixed: '/20' }], alarm: 3 },
  lose: { counters: [{ d: '029' }, { d: '018', frac: '5' }, { d: '06', fixed: '/30' }, { d: '011' }, { d: '02', fixed: '/12' }, { d: '00', fixed: '/20' }], alarm: 9 },
  v031: { counters: [{ d: '031' }, { d: '088', frac: '5' }, { d: '26', fixed: '/30' }, { d: '042' }, { d: '05', fixed: '/12' }, { d: '14', fixed: '/20' }], alarm: 7,
    freed: [['weu', 'V.011'], ['eeu', 'V.019'], ['me', 'V.024'], ['af', 'V.027'], ['ru', 'V.030']], nextTarget: 'Hindistan?', tally: 31, doorWaiting: true,
    telexHang: [['031/02', { t: 'EMİR UYGULANDI' }, 'Cephe açıldı: Hindistan'], ['031/01', { t: 'ZİYARETÇİ' }, 'Kapıda biri var.']],
    crt: [['DİNLEMEDE', 150, '#a6e8b8', true], ['ALFA · HAZIR', 110], ['HİNDİSTAN / 02', 104], ['YZ ALARMI 7/10', 64, '#5e9c73']],
    state: { idle: 2, reserve: 1, erzak: 26, malzeme: 42, plaka: 2, casting: 0.4, patient: true, rooms: { yasam: R('YAŞAM ALANI', 'SV 2 · 4/4', 2, 4, 4), telsiz: R('TELSİZ ODASI', 'SV 2 · 2/2', 2, 2, 2), atolye: R('ATÖLYE', 'SV 3/3 · 4/6', 3, 4, 6), revir: R('REVİR', 'SV 1 · 1/2', 1, 1, 2) } },
    shiftsLeft: '19 vardiya' },
};
export const LOG = [['014/02', 'Alfa hazır · cephe D. Avrupa'], ['014/05', 'Kanal temiz, parazit yok'], ['014/07', 'Atölye: plaka kalıbı boş'], ['014/09', 'Dinlemede — emir bekleniyor']];
