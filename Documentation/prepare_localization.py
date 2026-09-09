from localize_catalog import *

names={'Planks':'Доски','WoodDark':'Тёмный дуб','BowItem':'Лук','DiamondArmor':'Алмазный нагрудник'}
materials={'Diamond':'Алмазн','Golden':'Золот','Iron':'Железн','Netherite':'Незеритов','Netherithe':'Незеритов','Wooden':'Деревянн','Wood':'Деревянн','Stone':'Каменн'}
parts={'Boots':('ые','ботинки'),'Helmet':('ый','шлем'),'Leggings':('ые','поножи'),'Chestplate':('ой','нагрудник'),'Pickaxe':('ая','кирка'),'Sword':('ый','меч')}
for p in (ROOT/'Assets/ScriptableObjects/Configs').rglob('*.asset'):
    s=read(p)
    if not re.search(r'^  _name:',s,re.M): continue
    name=names.get(p.stem)
    for mat,prefix in materials.items():
        for part,(ending,noun) in parts.items():
            if p.stem in [mat+part,mat+part+'Item']:
                if mat=='Golden': ending={'ый':'ой','ая':'ая','ые':'ые','ой':'ой'}[ending]
                elif ending=='ой': ending='ый'
                name=prefix+ending+' '+noun
    if not name:
        m=re.search(r'^  _displayName: (.*)',s,re.M)
        if m: name=scalar(m.group(1))
    if not name: raise RuntimeError('Missing Russian name: '+str(p))
    line='  _displayName: '+json.dumps(name,ensure_ascii=False)
    if re.search(r'^  _displayName:',s,re.M): s=re.sub(r'^  _displayName:.*',lambda _:line,s,flags=re.M)
    else: s=re.sub(r'(^  _name:.*)',lambda m:m.group(1)+'\n'+line,s,flags=re.M)
    write(p,s)

p=ROOT/'Assets/Scripts/UI/FortuneWheel/WheelPointerGraphic.cs'
meta(p)
guid=re.search(r'guid: (\w+)',read(Path(str(p)+'.meta'))).group(1)
p=ROOT/'Assets/DevotionSDK/Prefabs/UI/FortuneWheelWindow.prefab'
s=read(p)
start=s.index('--- !u!114 &3129563312630811919\n')
end=s.find('\n--- !u!',start+1)
if end<0: end=len(s)
block=s[start:end]
block=re.sub(r'm_Script: .*','m_Script: {fileID: 11500000, guid: '+guid+', type: 3}',block)
block=block.split('  m_text:')[0]
block=block.replace('m_Color: {r: 1, g: 1, b: 1, a: 1}','m_Color: {r: 0.824, g: 0.596, b: 0.247, a: 1}')
s=s[:start]+block.rstrip()+s[end:]
write(p,s)
main()
