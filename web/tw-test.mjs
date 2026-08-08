import postcss from 'postcss';
import tailwind from '@tailwindcss/postcss';
import fs from 'fs';

const css = fs.readFileSync('src/styles.css', 'utf8');
// simula o builder: from = caminho absoluto do css
const result = await postcss([tailwind]).process(css, { from: '/home/felipeb/openPc/web/src/styles.css' });
console.log('from abs:', ['bg-brand-600','max-w-6xl'].map(c => c + '=' + result.css.includes(c)).join(' '));
// simula from em outro lugar (ex: temp)
const result2 = await postcss([tailwind]).process(css, { from: '/tmp/xyz/styles.css' });
console.log('from tmp:', ['bg-brand-600','max-w-6xl'].map(c => c + '=' + result2.css.includes(c)).join(' '));
