from localize_catalog import *

values={}
for folder in ['Assets/DevotionSDK/Prefabs','Assets/Prefabs','Assets/Resources']:
    for p in (ROOT/folder).rglob('*.prefab'):
        for m in re.finditer(r'^  m_text: ("(?:\\.|[^"\\])*"|[^\n]*)',read(p),re.M):
            s=scalar(m.group(1))
            if isinstance(s,str) and re.search('[A-Za-z]',s) and not re.search('[А-Яа-я]',s): values.setdefault(s,[]).append(str(p.relative_to(ROOT)))
print(json.dumps(values,ensure_ascii=False,indent=2))
