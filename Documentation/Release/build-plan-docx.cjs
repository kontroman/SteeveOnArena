// Regenerate the Word plan with Node.js; no external packages required.
const fs = require('fs');
const path = require('path');
const source = fs.readFileSync(path.join(__dirname, 'FirstLaunchPlan.md'), 'utf8');
const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const plain = s => s.replace(/\*\*/g, '').replace(/`/g, '').replace(/\[([^\]]+)\]\(([^)]+)\)/g, '$1 ($2)');
function paragraph(text, style = 'Normal') {
  return `<w:p><w:pPr><w:pStyle w:val="${style}"/></w:pPr><w:r><w:t xml:space="preserve">${esc(plain(text))}</w:t></w:r></w:p>`;
}
const lines = source.split(/\r?\n/);
const body = [];
for (let i = 0; i < lines.length; i++) {
  const line = lines[i].trim();
  if (!line) continue;
  if (line.startsWith('|')) {
    const rows = [];
    while (i < lines.length && lines[i].trim().startsWith('|')) {
      const cells = lines[i++].trim().slice(1, -1).split('|').map(s => s.trim());
      if (!cells.every(s => /^:?-+:?$/.test(s))) rows.push(cells);
    }
    i--;
    const width = Math.floor(9900 / rows[0].length);
    body.push(`<w:tbl><w:tblPr><w:tblW w:w="9900" w:type="dxa"/><w:tblBorders>${['top','left','bottom','right','insideH','insideV'].map(s => `<w:${s} w:val="single" w:sz="4" w:color="D7E0E5"/>`).join('')}</w:tblBorders><w:tblCellMar><w:top w:w="80" w:type="dxa"/><w:left w:w="90" w:type="dxa"/><w:bottom w:w="80" w:type="dxa"/><w:right w:w="90" w:type="dxa"/></w:tblCellMar></w:tblPr><w:tblGrid>${rows[0].map(() => `<w:gridCol w:w="${width}"/>`).join('')}</w:tblGrid>${rows.map((row, n) => `<w:tr>${n === 0 ? '<w:trPr><w:tblHeader/></w:trPr>' : ''}${row.map(cell => `<w:tc><w:tcPr><w:tcW w:w="${width}" w:type="dxa"/><w:shd w:fill="${n === 0 ? 'DDEEF1' : n % 2 ? 'FFFFFF' : 'F4F7F9'}"/></w:tcPr>${paragraph(cell, n === 0 ? 'TableHeader' : 'TableText')}</w:tc>`).join('')}</w:tr>`).join('')}</w:tbl>`);
    body.push(paragraph(''));
  } else {
    const heading = /^(#{1,4})\s+(.*)/.exec(line);
    body.push(heading ? paragraph(heading[2], heading[1].length === 1 ? 'Title' : `Heading${heading[1].length - 1}`) : paragraph(line.replace(/^- \[ \] /, '☐ ').replace(/^- /, '• ')));
  }
}
const ns = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main';
const xml = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>';
const style = (id, size, color, extra = '') => `<w:style w:type="paragraph" w:styleId="${id}"><w:name w:val="${id}"/><w:basedOn w:val="Normal"/><w:pPr><w:keepNext/><w:spacing w:before="220" w:after="100"/></w:pPr><w:rPr><w:b/><w:color w:val="${color}"/><w:sz w:val="${size}"/>${extra}</w:rPr></w:style>`;
const files = {
  '[Content_Types].xml': `${xml}<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/><Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/><Override PartName="/word/footer1.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml"/></Types>`,
  '_rels/.rels': `${xml}<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>`,
  'word/_rels/document.xml.rels': `${xml}<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer" Target="footer1.xml"/></Relationships>`,
  'word/document.xml': `${xml}<w:document xmlns:w="${ns}" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><w:body>${body.join('')}<w:sectPr><w:footerReference w:type="default" r:id="rId2"/><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1000" w:right="1000" w:bottom="1000" w:left="1000" w:header="400" w:footer="400"/></w:sectPr></w:body></w:document>`,
  'word/styles.xml': `${xml}<w:styles xmlns:w="${ns}"><w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri" w:cs="Calibri"/><w:sz w:val="22"/><w:lang w:val="ru-RU"/></w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:spacing w:after="110" w:line="264" w:lineRule="auto"/></w:pPr></w:pPrDefault></w:docDefaults><w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/></w:style>${style('Title', 44, '163D4A')}${style('Heading1', 30, '163D4A')}${style('Heading2', 25, '216878')}${style('Heading3', 23, '216878')}<w:style w:type="paragraph" w:styleId="TableText"><w:name w:val="Table Text"/><w:basedOn w:val="Normal"/><w:pPr><w:spacing w:after="40"/></w:pPr><w:rPr><w:sz w:val="19"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="TableHeader"><w:name w:val="Table Header"/><w:basedOn w:val="TableText"/><w:rPr><w:b/></w:rPr></w:style></w:styles>`,
  'word/footer1.xml': `${xml}<w:ftr xmlns:w="${ns}"><w:p><w:pPr><w:jc w:val="right"/></w:pPr><w:r><w:t>MineArena · </w:t></w:r><w:fldSimple w:instr="PAGE"/></w:p></w:ftr>`
};
// ZIP with stored entries, CRC32 and UTF-8 filenames.
function crc32(buffer) {
  let crc = -1;
  for (const byte of buffer) {
    crc ^= byte;
    for (let k = 0; k < 8; k++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0);
  }
  return (crc ^ -1) >>> 0;
}
const local = [], central = [];
let offset = 0;
for (const [name, content] of Object.entries(files)) {
  const filename = Buffer.from(name), data = Buffer.from(content);
  const header = Buffer.alloc(30);
  header.writeUInt32LE(0x04034b50); header.writeUInt16LE(20, 4); header.writeUInt16LE(0x800, 6);
  header.writeUInt16LE(0x5d28, 12); header.writeUInt32LE(crc32(data), 14);
  header.writeUInt32LE(data.length, 18); header.writeUInt32LE(data.length, 22); header.writeUInt16LE(filename.length, 26);
  const directory = Buffer.alloc(46);
  directory.writeUInt32LE(0x02014b50); directory.writeUInt16LE(20, 4);
  header.copy(directory, 6, 4, 30); directory.writeUInt32LE(offset, 42);
  local.push(header, filename, data); central.push(directory, filename);
  offset += header.length + filename.length + data.length;
}
const directory = Buffer.concat(central), end = Buffer.alloc(22);
end.writeUInt32LE(0x06054b50); end.writeUInt16LE(central.length / 2, 8); end.writeUInt16LE(central.length / 2, 10);
end.writeUInt32LE(directory.length, 12); end.writeUInt32LE(offset, 16);
const target = path.join(__dirname, 'MineArena-First-Launch-Plan.docx');
fs.writeFileSync(target, Buffer.concat([...local, directory, end]));
console.log(`Created ${target} (${Object.keys(files).length} XML parts)`);
