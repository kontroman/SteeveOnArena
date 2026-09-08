const fs=require('fs'),crypto=require('crypto'),path=require('path');
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n');
const write=(p,s)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,s);};
function meta(p,type='asset') {
 if(fs.existsSync(p+'.meta'))return;
 const g=crypto.createHash('md5').update('MineArena-production-v1:'+p).digest('hex');
 let importer=type==='cs'?'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0':type==='shader'?'ShaderImporter:\n  externalObjects: {}':type==='prefab'?'PrefabImporter:\n  externalObjects: {}':'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000';
 write(p+'.meta',`fileFormatVersion: 2\nguid: ${g}\n${importer}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
}
const guid=p=>read(p+'.meta').match(/guid: (\w+)/)[1];
function field(s,k,v){const re=new RegExp('^  '+k+':[^\\n]*(?:\\n(?!  [A-Za-z_])[ \\t]+[^\\n]*)*','m');return re.test(s)?s.replace(re,'  '+k+': '+v):s+'  '+k+': '+v+'\n';}
const drops='Assets/ScriptableObjects/Configs/Drops/';
const ip=id=>drops+id+'.asset';
const ref=(p,f=11400000,t=2)=>`{fileID: ${f}, guid: ${guid(p)}, type: ${t}}`;
for(const p of ['Assets/Scripts/Managers/CraftProductionService.cs','Assets/Scripts/Item/Configs/PotionConfig.cs','Assets/Scripts/Player/PotionEffects.cs','Assets/Scripts/Item/HarvestPlantVisual.cs'])meta(p,'cs');
meta('Assets/Scripts/Item/PixelItem.shader','shader');
const data=JSON.parse(read('Documentation/balance-data.json'));
const extra={Wheat:'Пшеница',SugarCane:'Сахарный тростник',NetherWart:'Адский нарост',Sugar:'Сахар',GlassBottle:'Пузырёк',MelonSlice:'Ломтик арбуза',GlisteringMelon:'Сверкающий ломтик арбуза',GhastTear:'Слеза гаста',HealingPotion:'Зелье лечения',RegenerationPotion:'Зелье регенерации',SpeedPotion:'Зелье скорости'};
for(const [id,label] of Object.entries(extra)){
 let s=fs.existsSync(ip(id))?read(ip(id)):read(ip('String'));
 for(const [k,v] of Object.entries({m_Name:id,_name:id,_displayName:JSON.stringify(label),_prefab:'{fileID: 0}',_resourceCategory:id,_craftCosts:'[]',_craftAmount:1,_craftSeconds:7,_usable:0,_blockStyleIcon:0,_description:JSON.stringify(label)}))s=field(s,k,v);
 if(id.endsWith('Potion'))s=field(s,'m_Script',ref('Assets/Scripts/Item/Configs/PotionConfig.cs',11500000,3));
 write(ip(id),s);meta(ip(id));
}
function recipe(id,cost,out=1){data.recipes[id]={cost,output:out};let s=read(ip(id));s=field(s,'_craftCosts','\n'+Object.entries(cost).map(([r,n])=>'  - _resource: '+ref(ip(r))+'\n    _amount: '+n).join('\n'));s=field(s,'_craftAmount',out);write(ip(id),s);}
recipe('Sugar',{SugarCane:1});recipe('GlassBottle',{Glass:3},3);recipe('GlisteringMelon',{MelonSlice:1,GoldIngot:1});
recipe('HealingPotion',{GlassBottle:1,NetherWart:1,GlisteringMelon:1});
recipe('RegenerationPotion',{GlassBottle:1,NetherWart:1,GhastTear:1});
recipe('SpeedPotion',{GlassBottle:1,NetherWart:1,Sugar:2});
for(const [id,effect,strength,duration,description] of [
 ['HealingPotion',0,40,0,'Восстанавливает 40 здоровья. Выберите в быстром слоте и нажмите R, ЛКМ или «Выпить». При полном здоровье не расходуется.'],
 ['RegenerationPotion',1,3,10,'Восстанавливает 3 здоровья в секунду в течение 10 с. R / ЛКМ / «Выпить». Повторный приём обновляет длительность.'],
 ['SpeedPotion',2,0.3,20,'Скорость передвижения +30% на 20 с. R / ЛКМ / «Выпить». Эффекты скорости не складываются.']]){
 let s=read(ip(id));for(const [k,v] of Object.entries({_effect:effect,_strength:strength,_duration:duration,_description:JSON.stringify(description)}))s=field(s,k,v);write(ip(id),s);
}
const bp=id=>'Assets/ScriptableObjects/Configs/Buildings/'+id+'.asset';
const walk=p=>fs.readdirSync(p,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(p,e.name)):[path.join(p,e.name)]);
const itemPaths=Object.fromEntries(walk('Assets/ScriptableObjects').filter(p=>p.endsWith('.asset')).map(p=>[path.basename(p,'.asset'),p]));
itemPaths.DiamondChestplate=itemPaths.DiamondArmor;
const farmOutputs=[{Wheat:3,SugarCane:2,MelonSlice:1},{Wheat:4,SugarCane:3,MelonSlice:2,NetherWart:1},{Wheat:6,SugarCane:4,MelonSlice:3,NetherWart:2}];
data.production=farmOutputs;
data.buildings.FarmBuilding.forEach(l=>l[1]=[]);
data.buildings.FarmBuilding[1][0].Wheat=12;
data.buildings.FarmBuilding[2][0].Wheat=24;
data.buildings.LumberjackBuilding[1][1]=[...new Set([...data.buildings.LumberjackBuilding[1][1],'Wool','Sugar'])];
data.buildings.AlchemyBuilding[0][1]=['Glass','GlassBottle','GlisteringMelon','HealingPotion'];
data.buildings.AlchemyBuilding[1][1]=['SpeedPotion'];data.buildings.AlchemyBuilding[2][1]=['RegenerationPotion'];
for(const id of ['FarmBuilding','LumberjackBuilding','AlchemyBuilding']){
 let s=read(bp(id));let index=-1;
 s=s.replace(/  - _level: [\s\S]*?(?=  - _level: |  _currentLevel:)/g,block=>{
  index++;const unlocks=data.buildings[id][index][1];
  block=block.replace(/    _unlocks:[\s\S]*?(?=    _modelPrefab:)/,'    _unlocks:'+(unlocks.length?'\n'+unlocks.map(r=>'    - '+ref(itemPaths[r])).join('\n'):' []')+'\n');
  if(id==='FarmBuilding'){
   block=block.replace(/    _requiredResources:[\s\S]*?(?=    _unlocks:)/,'    _requiredResources:\n'+Object.entries(data.buildings[id][index][0]).map(([r,n])=>'    - _resource: '+ref(itemPaths[r])+'\n      _amount: '+n).join('\n')+'\n');
   block=block.replace(/    _production:[\s\S]*?(?=    _productionSeconds:|$)/,'').replace(/    _productionSeconds:.*\n/g,'');
   block+='    _production:\n'+Object.entries(farmOutputs[index]).map(([r,n])=>'    - Item: '+ref(ip(r))+'\n      Amount: '+n).join('\n')+'\n    _productionSeconds: '+[120,90,60][index]+'\n';
  }
  if(id==='FarmBuilding'||id==='AlchemyBuilding')block=block.replace(/    _craftOutputBonus: \d+/,'    _craftOutputBonus: 0');
  return block;
 });write(bp(id),s);
}
const db='Assets/ScriptableObjects/Configs/Game/ItemDatabase.asset';let database=read(db);
for(const id of Object.keys(extra))if(!database.includes(guid(ip(id))))database+='  - '+ref(ip(id))+'\n';write(db,database);
// A rare brewing ingredient is guaranteed by the late expedition.
const forest='Assets/ScriptableObjects/Levels/Level4_Forest.asset';let level=read(forest);
if(!level.match(new RegExp('Item: .*'+guid(ip('GhastTear')))))level=level.replace('  levelPrefab:','  - Item: '+ref(ip('GhastTear'))+'\n    Amount: 1\n  levelPrefab:');
write(forest,level);data.encounters.find(e=>e[0]==='Level4_Forest')[3].GhastTear=1;
write('Documentation/balance-data.json',JSON.stringify(data,null,2)+'\n');
console.log('Production and potion configs applied.');
