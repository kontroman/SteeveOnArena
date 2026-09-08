// Final resource-distribution pass, also run after the production asset generator.
const fs=require('fs'),path=require('path');
const read=p=>fs.readFileSync(p,'utf8').replace(/\r/g,'');
const guid=p=>read(p+'.meta').match(/^guid: (\w+)/m)[1];
const drop=id=>'Assets/ScriptableObjects/Configs/Drops/'+id+'.asset';
const ref=p=>`{fileID: 11400000, guid: ${guid(p)}, type: 2}`;
const ores='Assets/Prefabs/Objects/Ores/',harvest='Assets/Prefabs/Objects/Harvest/';
const prefabRef=p=>{const blocks=read(p).split(/^--- /m);const t=blocks.find(b=>/^!u!4 /.test(b)&&/m_Father: \{fileID: 0\}/.test(b));if(!t)throw Error('No root transform: '+p);return `{fileID: ${t.match(/m_GameObject: \{fileID: (\d+)\}/)[1]}, guid: ${guid(p)}, type: 3}`;};
const distributions=[
 [['WoodOak',.36],['Stone',.30],['CoalOreBlock',.16],['WoodBirch',.06],['WheatHarvest',.07],['WoolHarvest',.05]],
 [['Sand',.32],['IronOreBlock',.28],['CoalOreBlock',.16],['SugarCaneHarvest',.12],['GlassHarvest',.12]],
 [['CoalOreBlock',.20],['IronOreBlock',.34],['GoldOreBlock',.24],['DiamondOreBlock',.14],['NetherWartHarvest',.08]],
 [['WoodOak',.20],['WoodDark',.12],['Stone',.06],['CoalOreBlock',.08],['IronOreBlock',.08],['DiamondOreBlock',.10],['NetheriteOre',.14],['WheatHarvest',.07],['SugarCaneHarvest',.07],['NetherWartHarvest',.08]]
];
const rewards=[{WoodOak:13,Stone:10,CoalOre:5,String:3,Leather:2},{Sand:14,IronOre:12,CoalOre:8,String:3,Leather:2},{IronOre:18,GoldOre:12,CoalOre:12,DiamondOre:3},{WoodOak:24,String:5,Leather:6,DiamondOre:8,NetheriteOre:5,GhastTear:1}];
const data=JSON.parse(read('Documentation/balance-data.json'));
data.recipes.IronIngot={cost:{IronOre:1},output:1};
const allMetas=fs.readdirSync('Assets/ScriptableObjects/Configs/Drops').filter(p=>p.endsWith('.asset.meta'));
const byGuid=new Map(allMetas.map(p=>{const a='Assets/ScriptableObjects/Configs/Drops/'+p.slice(0,-5);return [guid(a),a];}));
for(let i=0;i<4;i++){
 data.encounters[i][3]=rewards[i];
 const p='Assets/ScriptableObjects/Levels/'+data.encounters[i][0]+'.asset';let s=read(p);
 if(Math.abs(distributions[i].reduce((a,[,w])=>a+w,0)-1)>1e-8)throw Error('Weights must sum to 1');
 const available=new Set(Object.keys(rewards[i]).map(drop));
 const spawn=distributions[i].map(([name,w])=>{const p=(name.endsWith('Harvest')?harvest:ores)+name+'.prefab';
  for(const m of read(p).matchAll(/- Item: .*guid: (\w+)/g)){const item=byGuid.get(m[1]);if(item)available.add(item);}
  return `  - resource: ${prefabRef(p)}\n    spawnChance: ${w}`;
 }).join('\n');
 s=s.replace(/  availableResources:[\s\S]*?(?=  resourceSpawnConfigs:)/,'  availableResources:\n'+[...available].map(p=>'  - '+ref(p)).join('\n')+'\n');
 s=s.replace(/  resourceSpawnConfigs:[\s\S]*?(?=  rewardResources:)/,'  resourceSpawnConfigs:\n'+spawn+'\n');
 s=s.replace(/  rewardResources:[\s\S]*?(?=  levelPrefab:)/,'  rewardResources:\n'+Object.entries(rewards[i]).map(([id,n])=>`  - Item: ${ref(drop(id))}\n    Amount: ${n}`).join('\n')+'\n');
 fs.writeFileSync(p,s);
}
let iron=read(drop('IronIngot'));
iron=iron.replace(/^  _craftCosts:[\s\S]*?(?=^  _craftAmount:)/m,'  _craftCosts:\n  - _resource: '+ref(drop('IronOre'))+'\n    _amount: 1\n');
iron=iron.replace(/  _description:[^\n]*(?:\n    [^\n]*)*/,'  _description: "'+ '\\u0031 '+ '\\u0440\\u0443\\u0434\\u0430 '+ '\\u2192 1 '+ '\\u0441\\u043B\\u0438\\u0442\\u043E\\u043A. '+ '\\u041E\\u0434\\u0438\\u043D '+ '\\u0442\\u0430\\u0439\\u043C\\u0435\\u0440: 7 '+ '\\u0441."');
fs.writeFileSync(drop('IronIngot'),iron);
fs.writeFileSync('Documentation/balance-data.json',JSON.stringify(data,null,2)+'\n');
console.log('Updated four level distributions, rewards, visible resources and iron 1:1 recipe.');
