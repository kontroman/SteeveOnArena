"""Build the Russian source catalog from project UI, configs and runtime strings."""
from pathlib import Path
import re, json, yaml, uuid

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Localization'
def read(p):
    try: return p.read_text(encoding='utf-8-sig')
    except UnicodeDecodeError: return p.read_text(encoding='cp1251')
def write(p,s): p.write_text(s,encoding='utf-8')
def meta(p):
    if not Path(str(p)+'.meta').exists():
        write(Path(str(p)+'.meta'), 'fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')

def scalar(s):
    try: return yaml.safe_load(s)
    except Exception: return None

def main():
    catalog={x['key']:x['value'] for x in json.loads(read(OUT/'Russian.json'))['items']}
    sources={}
    def add(s,p):
        if not isinstance(s,str) or not re.search('[А-Яа-яЁё]',s): return
        catalog[s]=s
        sources.setdefault(s,[]).append(str(p.relative_to(ROOT)))
    for folder in ['Assets/Scripts','Assets/DevotionSDK/Scripts']:
        for p in (ROOT/folder).rglob('*.cs'):
            if 'Editor' in p.parts: continue
            text=read(p)
            for m in re.finditer(r'\$?"(?:\\.|[^"\\\n])*"',text):
                line=text[text.rfind('\n',0,m.start())+1:m.start()]
                if any(x in line for x in ['Debug.', 'Tooltip(', 'Header(', 'Exception(', '//']): continue
                raw=m.group()
                try: s=json.loads(raw.lstrip('$'))
                except Exception: continue
                if raw.startswith('$'):
                    i=[0]
                    def slot(m):
                        n=i[0]; i[0]+=1; return '{'+str(n)+'}'
                    s=re.sub(r'\{[^{}]+\}',slot,s)
                add(s,p)
    for p in (ROOT/'Assets').rglob('*'):
        if p.suffix not in ['.prefab','.asset','.unity'] or any(x in p.parts for x in ['Plugins','DevTex','TextMesh Pro']): continue
        if not p.read_bytes().startswith(b'%YAML'): continue
        text=read(p)
        for m in re.finditer(r'^\s*(?:m_text|m_Text|_displayName|displayName|_description|description|_title|title|_buildingName|_difficultyLabel|_format):\s*("(?:\\.|[^"\\])*"|[^\n]*)',text,re.M):
            add(scalar(m.group(1)),p)
    write(OUT/'Russian.json',json.dumps({'items':[{'key':k,'value':v} for k,v in catalog.items()]},ensure_ascii=False,indent=2)+'\n')
    write(ROOT/'Documentation/localization-sources.json',json.dumps(sources,ensure_ascii=False,indent=2)+'\n')
    print('Catalog:',len(catalog),'keys')

if __name__=='__main__': main()
