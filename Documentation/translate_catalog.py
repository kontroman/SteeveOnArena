"""Generate machine translations; runtime only reads checked-in JSON (no network).

Run explicitly after reviewing changes to the Russian catalog. Placeholder and
rich-text tag checks reject structurally invalid translations.
"""
from localize_catalog import *
from urllib.request import urlopen, Request
from urllib.parse import urlencode
from concurrent.futures import ThreadPoolExecutor, as_completed
import time

LANGUAGES={'English':'en','German':'de','Spanish':'es','Italian':'it','French':'fr','Portuguese':'pt','Turkish':'tr','Indonesian':'id'}
def structure(s): return sorted(re.findall(r'\{\d+(?::[^{}]+)?\}|</?[^>]+>',s))
def translate(s,lang):
    url='https://translate.googleapis.com/translate_a/single?'+urlencode({'client':'gtx','sl':'ru','tl':lang,'dt':'t','q':s})
    for attempt in range(4):
        try:
            with urlopen(Request(url,headers={'User-Agent':'Mozilla/5.0'}),timeout=40) as response:
                result=json.load(response)
            return ''.join(part[0] for part in result[0] if part[0])
        except Exception:
            if attempt==3: raise
            time.sleep(attempt+1)

def preserve_whitespace(source,result):
    result=re.sub(r'\{\s*(\d+)\s*\}',r'{\1}',result)
    return re.match(r'^\s*',source).group()+result.strip()+re.search(r'\s*$',source).group()

def language(name,code):
    source=json.loads(read(OUT/'Russian.json'))['items']
    path=OUT/(name+'.json')
    previous={i['key']:i['value'] for i in json.loads(read(path))['items']} if path.exists() else {}
    missing=[i for i in source if i['key'] not in previous]
    batches=[]; batch=[]; size=0
    for item in missing:
        if size+len(item['value'])>2300 or '\n' in item['value']:
            if batch: batches.append(batch)
            batch=[];size=0
        if '\n' in item['value']: batches.append([item]);continue
        batch.append(item);size+=len(item['value'])+1
    if batch:batches.append(batch)
    for batch in batches:
        combined=translate('\n'.join(i['value'] for i in batch),code)
        values=combined.split('\n') if len(batch)>1 else [combined]
        if len(values)!=len(batch): values=[translate(i['value'],code) for i in batch]
        for item,value in zip(batch,values):
            value=preserve_whitespace(item['value'],value)
            if structure(value)!=structure(item['value']):
                value=preserve_whitespace(item['value'],translate(item['value'],code))
            if structure(value)!=structure(item['value']): raise RuntimeError(name+': invalid placeholders: '+item['value']+' => '+value)
            previous[item['key']]=value
        write(path,json.dumps({'items':[{'key':i['key'],'value':previous[i['key']]} for i in source if i['key'] in previous]},ensure_ascii=False,indent=2)+'\n')
        meta(path)
        print(name,len(previous),'/',len(source),flush=True)
    return name

if __name__=='__main__':
    with ThreadPoolExecutor(max_workers=4) as pool:
        for result in as_completed([pool.submit(language,k,v) for k,v in LANGUAGES.items()]):
            print('DONE',result.result(),flush=True)
