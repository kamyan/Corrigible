import sys, asyncio, json
from playwright.async_api import async_playwright
# usage: measure_dump.py src.html out.tsv title
JS = r'''
() => {
  const root = document.querySelector('.c');
  const lines = [];
  const r1 = v => (Math.round(v * 2) / 2).toString();
  function hex(c) {
    const m = c.match(/rgba?\(([^)]+)\)/); if (!m) return c;
    const p = m[1].split(',').map(s => s.trim());
    const [r, g, b] = p.slice(0, 3).map(v => Math.round(parseFloat(v)));
    const a = p.length > 3 ? parseFloat(p[3]) : 1;
    const h = '#' + [r, g, b].map(v => v.toString(16).padStart(2, '0')).join('').toUpperCase();
    return a < 1 ? `${h}@${Math.round(a * 100) / 100}` : h;
  }
  function alpha(c) { const m = c.match(/rgba?\(([^)]+)\)/); if (!m) return 1; const p = m[1].split(','); return p.length > 3 ? parseFloat(p[3]) : 1; }
  function rot(cs) {
    if (cs.transform === 'none') return null;
    const m = new DOMMatrix(cs.transform);
    const a = Math.round(Math.atan2(m.b, m.a) * 1800 / Math.PI) / 10;
    const ty = Math.round(m.f * 10) / 10, tx = Math.round(m.e * 10) / 10;
    let s = '';
    if (a) s += `rot=${a}`;
    if (tx || ty) s += (s ? ' ' : '') + `tr=${tx},${ty}`;
    return s || null;
  }
  function border(cs) {
    const sides = ['top', 'right', 'bottom', 'left'].map(s => [parseFloat(cs[`border-${s}-width`]), cs[`border-${s}-style`], hex(cs[`border-${s}-color`])]);
    const on = sides.filter(s => s[0] > 0 && s[1] !== 'none');
    if (!on.length) return null;
    const same = sides.every(s => s[0] === sides[0][0] && s[1] === sides[0][1] && s[2] === sides[0][2]);
    if (same) return `bd=${sides[0][0]} ${sides[0][1]} ${sides[0][2]}`;
    return 'bd=' + sides.map((s, i) => 'TRBL'[i] + (s[0] > 0 && s[1] !== 'none' ? `${s[0]} ${s[1]} ${s[2]}` : '0')).join(' | ');
  }
  function radius(cs) {
    const v = ['top-left', 'top-right', 'bottom-right', 'bottom-left'].map(k => cs[`border-${k}-radius`]);
    if (v.every(x => x === '0px')) return null;
    if (v.every(x => x === v[0])) return `r=${v[0].replace('px', '')}`;
    return 'r=' + v.map(x => x.replace('px', '')).join('/');
  }
  function shadow(cs) {
    if (cs.boxShadow === 'none') return null;
    return 'sh=' + cs.boxShadow.split(/,(?![^(]*\))/).map(s => {
      const col = (s.match(/rgba?\([^)]*\)/) || [''])[0];
      const nums = s.replace(col, '').trim().split(/\s+/).filter(Boolean);
      const inset = nums.includes('inset') ? 'inset ' : '';
      const n = nums.filter(x => x !== 'inset').map(x => x.replace('px', ''));
      return `${inset}${n.join(',')} ${hex(col)}`;
    }).join(' + ');
  }
  function bgimg(cs) {
    if (cs.backgroundImage === 'none') return null;
    const b = cs.backgroundImage;
    if (b.includes('repeating-linear')) return 'bgimg=hatch/lines';
    if (b.includes('radial-gradient')) return 'bgimg=dots';
    if (b.includes('linear-gradient')) return 'bgimg=rule';
    if (b.includes('url(')) return 'bgimg=url';
    return 'bgimg=?';
  }
  function font(cs) {
    const fam = cs.fontFamily.split(',')[0].replace(/["']/g, '').trim();
    const short = {'Courier Prime': 'CP', 'Libre Franklin': 'LF', 'Kalam': 'KA'}[fam] || fam;
    const size = parseFloat(cs.fontSize);
    const ls = cs.letterSpacing === 'normal' ? 0 : parseFloat(cs.letterSpacing);
    const lh = cs.lineHeight === 'normal' ? 'n' : (Math.round(parseFloat(cs.lineHeight) * 10) / 10);
    let s = `${short} ${size}/${cs.fontWeight}`;
    if (ls) s += ` ls${Math.round(ls / size * 1000) / 1000}em`;
    s += ` lh${lh}`;
    s += ` ${hex(cs.color)}`;
    if (cs.textAlign !== 'start' && cs.textAlign !== 'left') s += ` al=${cs.textAlign}`;
    if (cs.textTransform !== 'none') s += ` tt=${cs.textTransform}`;
    if (cs.textDecorationLine !== 'none') s += ` deco=${cs.textDecorationLine}`;
    if (cs.fontStyle !== 'normal') s += ` ${cs.fontStyle}`;
    if (cs.textShadow !== 'none') s += ` tsh`;
    return s;
  }
  function ownText(el) {
    let t = '';
    for (const n of el.childNodes) if (n.nodeType === 3) t += n.textContent;
    return t.replace(/\s+/g, ' ').trim();
  }
  function label(el) {
    const tag = el.tagName.toLowerCase();
    const cls = (typeof el.className === 'string' ? el.className : (el.getAttribute('class') || '')).trim().split(/\s+/).filter(Boolean).slice(0, 3).join('.');
    return cls ? `${tag}.${cls}` : tag;
  }
  function walk(el, depth) {
    const cs = getComputedStyle(el);
    if (cs.display === 'none' || cs.visibility === 'hidden') return;
    const b = el.getBoundingClientRect();
    const tag = el.tagName.toLowerCase();
    if (b.width === 0 && b.height === 0 && tag !== 'svg') return;
    const parts = [label(el), `${r1(b.left)},${r1(b.top)} ${r1(b.width)}x${r1(b.height)}`];
    if (tag !== 'svg' && !(el instanceof SVGElement)) {
      const ow = el.offsetWidth, oh = el.offsetHeight;
      const rt = rot(cs);
      if (rt) { parts.push(rt); parts.push(`layout=${ow}x${oh}`); }
      if (alpha(cs.backgroundColor) > 0) parts.push(`bg=${hex(cs.backgroundColor)}`);
      const bi = bgimg(cs); if (bi) parts.push(bi);
      const bd = border(cs); if (bd) parts.push(bd);
      const rr = radius(cs); if (rr) parts.push(rr);
      const sh = shadow(cs); if (sh) parts.push(sh);
      if (parseFloat(cs.opacity) < 1) parts.push(`op=${cs.opacity}`);
      if (cs.clipPath && cs.clipPath !== 'none') parts.push('clip-path');
      if (cs.zIndex !== 'auto') parts.push(`z=${cs.zIndex}`);
      const pd = ['top', 'right', 'bottom', 'left'].map(s => parseFloat(cs[`padding-${s}`]));
      if (pd.some(v => v > 0) && (ownText(el) || el.children.length)) parts.push(`pad=${pd.join('/')}`);
      const t = ownText(el);
      if (t) {
        parts.push('| ' + font(cs));
        parts.push(`"${t}"`);
        if (el.children.length) {
          const full = el.textContent.replace(/\s+/g, ' ').trim();
          if (full !== t) parts.push(`full="${full}"`);
        }
      } else if (el.children.length === 0 && (tag === 'span' || tag === 'i' || tag === 'b')) {
        // boş kutu (çizgi, kutu vb.)
      }
      if (cs.display.includes('flex') || cs.display.includes('grid')) {
        const g = cs.gap && cs.gap !== 'normal' ? cs.gap.replace(/px/g, '') : '';
        parts.push(`[${cs.display}${cs.flexDirection === 'column' ? ' col' : ''}${g ? ' gap ' + g : ''}${cs.justifyContent !== 'normal' && cs.justifyContent !== 'flex-start' ? ' jc ' + cs.justifyContent : ''}${cs.alignItems !== 'normal' ? ' ai ' + cs.alignItems : ''}]`);
      }
      lines.push('  '.repeat(depth) + parts.join('  '));
      for (const c of el.children) walk(c, depth + 1);
    } else if (tag === 'svg') {
      const vb = el.getAttribute('viewBox'); if (vb) parts.push(`viewBox=${vb}`);
      const rt = rot(cs); if (rt) parts.push(rt);
      const texts = [...el.querySelectorAll('text')].map(t => `${t.textContent}@${t.getAttribute('x')},${t.getAttribute('y')}`);
      if (texts.length) parts.push('texts: ' + texts.slice(0, 12).join(' ; '));
      const kinds = {}; el.querySelectorAll('path,circle,rect,ellipse,polyline,line,use,g').forEach(n => kinds[n.tagName] = (kinds[n.tagName] || 0) + 1);
      parts.push('{' + Object.entries(kinds).map(([k, v]) => `${k}×${v}`).join(' ') + '}');
      lines.push('  '.repeat(depth) + parts.join('  '));
    }
  }
  for (const c of root.children) {
    const cmt = c.previousSibling;
    walk(c, 0);
  }
  return lines;
}
'''
async def main():
    src, out, title = sys.argv[1], sys.argv[2], sys.argv[3]
    async with async_playwright() as p:
        b = await p.chromium.launch(); pg = await b.new_page(viewport={'width': 1920, 'height': 1080})
        await pg.goto('file://' + src); await pg.evaluate('document.fonts.ready')
        await pg.add_style_tag(content='*{animation-play-state:paused !important; animation-delay:-0.01s !important}')
        await pg.wait_for_timeout(300)
        lines = await pg.evaluate(JS)
        head = [f'# {title}',
                '# Ölçü dökümü: tarayıcıda hesaplanmış son değerler (CSS px, 1920×1080, sol-üst köşe orijin, y aşağı).',
                '# Satır: girinti=DOM derinliği · etiket.sınıf · x,y GxY (dönüşümler dahil sınırlayıcı kutu) · alanlar:',
                '#   rot=derece (CSS saat yönü; Unity z = −rot) · tr=translate · layout=dönüşümsüz boyut · bg=zemin (#RRGGBB@alfa) · bgimg=doku türü',
                '#   bd=kenarlık (genişlik stil renk; T|R|B|L ayrı ise) · r=köşe yarıçapı (sol-üst/sağ-üst/sağ-alt/sol-alt) · sh=gölge (x,y,bulanıklık,yayılma renk)',
                '#   op=opaklık · pad=iç boşluk ü/s/a/s · [flex …] yerleşim · | font: CP=Courier Prime, LF=Libre Franklin, KA=Kalam  boyut/ağırlık  ls=harf aralığı(em)  lh=satır yüksekliği(px)  renk',
                '#   "metin"=öğenin kendi metni · full="…"=alt öğelerle birlikte tam metin · svg: viewBox, içindeki <text>ler, şekil sayıları',
                '']
        open(out, 'w', encoding='utf-8').write('\n'.join(head + lines) + '\n')
        print(out, len(lines))
        await b.close()
asyncio.run(main())
