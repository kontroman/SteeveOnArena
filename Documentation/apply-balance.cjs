const fs = require('fs');
const crypto = require('crypto');
const read = p => fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n');
const write = (p,s) => fs.writeFileSync(p,s,'utf8');
const edit = (p,fn) => write(p,fn(read(p)));
function replace(s,a,b) { if(!s.includes(a)) throw Error('Missing text: '+a); return s.replace(a,b); }
function field(s,key,value) {
 const re = new RegExp('^  '+key+':[^\\n]*(?:\\n(?!  [A-Za-z_])[ \\t]+[^\\n]*)*','m');
 return re.test(s) ? s.replace(re,'  '+key+': '+value) : s+'  '+key+': '+value+'\n';
}
const walk = p => fs.readdirSync(p,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(p+'/'+e.name):[p+'/'+e.name]);
let assets = walk('Assets/ScriptableObjects').filter(p=>p.endsWith('.asset'));
let paths = Object.fromEntries(assets.map(p=>[p.split('/').pop().replace('.asset',''),p]));
paths.DiamondChestplate = paths.DiamondArmor;
const guid = p => /guid: (\w+)/.exec(read(p+'.meta'))[1];
const ref = id => `{fileID: 11400000, guid: ${guid(paths[id])}, type: 2}`;
const costs = (obj,indent='  ') => Object.entries(obj).map(([id,n])=>`${indent}- _resource: ${ref(id)}\n${indent}  _amount: ${n}`).join('\n');
const recipes = {};
function recipe(id,cost,output=1) {
 recipes[id]={cost,output};
 edit(paths[id],s=>field(field(s,'_craftCosts','\n'+costs(cost)),'_craftAmount',output));
}
function resource(id,label,source) {
 const p='Assets/ScriptableObjects/Configs/Drops/'+id+'.asset';
 if(!fs.existsSync(p)) {
  let s=read(paths[source]);
  for(const [key,value] of Object.entries({m_Name:id,_name:id,_displayName:JSON.stringify(label),_prefab:'{fileID: 0}',_resourceCategory:id,_description:JSON.stringify(label+' — материал для переработки и улучшений.'),_blockStyleIcon:0})) s=field(s,key,value);
  write(p,s); write(p+'.meta',`fileFormatVersion: 2\nguid: ${crypto.randomBytes(16).toString('hex')}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
 }
 paths[id]=p;
}
resource('IronIngot','Железный слиток','IronOre');
resource('GoldIngot','Золотой слиток','GoldOre');
resource('NetheriteIngot','Незеритовый слиток','NetheriteOre');
resource('Glass','Стекло','Sand');
resource('String','Нить','Planks');
resource('Leather','Кожа','Planks');
resource('Stick','Палка','Planks');
resource('NetheriteScrap','Незеритовый обломок','NetheriteOre');
resource('Wool','Шерсть','Planks');
for(const id of ['String','Leather']) edit(paths[id],s=>field(field(s,'_craftCosts','[]'),'_craftAmount',1));
recipe('Planks',{WoodOak:1},4);
recipe('Stick',{Planks:2},4);
recipe('Wool',{String:4});
recipe('NetheriteScrap',{NetheriteOre:1,CoalItem:1});
recipe('CoalItem',{CoalOre:2},3);
recipe('IronIngot',{IronOre:3,CoalItem:1});
recipe('GoldIngot',{GoldOre:3,CoalItem:2});
recipe('NetheriteIngot',{NetheriteScrap:4,GoldIngot:4});
recipe('Glass',{Sand:3,CoalItem:1},2);
edit(paths.WoodSwordItem,s=>field(s,'_craftCosts','[]'));
recipe('StoneSwordItem',{Stone:8,Stick:1});
recipe('IronSwordItem',{IronIngot:4,Stick:1});
recipe('GoldenSwordItem',{GoldIngot:4,IronIngot:2,Stick:1});
recipe('DiamondSwordItem',{DiamondOre:6,IronIngot:3,Planks:2});
recipe('NetheritheSwordItem',{NetheriteIngot:6,DiamondOre:4,Planks:3});
recipe('BowItem',{Stick:3,String:3});
recipe('WoodenPickaxe',{Planks:3,Stick:2});
recipe('IronPickaxe',{IronIngot:3,Planks:4});
recipe('DiamondPickaxe',{DiamondOre:5,IronIngot:2,Planks:2});
recipe('NetherithePickaxe',{NetheriteIngot:4,DiamondOre:3,Planks:3});
const armor=[];
for(const [tier,material,values] of [['Iron','IronIngot',[4,8,6,3]],['Golden','GoldIngot',[6,10,8,4]],['Diamond','DiamondOre',[8,14,10,6]],['Netherite','NetheriteIngot',[10,18,14,8]]]) {
 for(const [i,slot] of ['Helmet','Chestplate','Leggings','Boots'].entries()) {
  const id=tier+slot;
  recipe(id,{[material]:[3,6,5,2][i],Leather:[1,3,2,1][i],...(['Diamond','Netherite'].includes(tier)?{IronIngot:2}:{})});
  edit(paths[id],s=>field(s,'_resist',values[i])); armor.push(id);
 }
}
const tiers = t => armor.filter(a=>a.startsWith(t));
const buildings={
 LumberjackBuilding:[
  [{WoodOak:12,Stone:8},['Planks','Stick','WoodenPickaxe']],
  [{Planks:24,Stone:20,IronIngot:3},['BowItem','IronPickaxe']],
  [{Planks:36,IronIngot:6},['DiamondPickaxe','NetherithePickaxe']]],
 SmithBuilding:[
  [{WoodOak:20,Stone:24,CoalOre:6},['CoalItem','IronIngot','StoneSwordItem','IronSwordItem',...tiers('Iron')]],
  [{Planks:30,IronIngot:10,Stone:30},['GoldIngot','GoldenSwordItem',...tiers('Golden')]],
  [{Planks:16,IronIngot:12,DiamondOre:6},['DiamondSwordItem',...tiers('Diamond')]],
  [{Planks:32,IronIngot:24,DiamondOre:12,GoldIngot:8},['NetheriteScrap','NetheriteIngot','NetheritheSwordItem',...tiers('Netherite')]]],
 FarmBuilding:[[{WoodOak:16,Stone:10},['Wool']],[{Planks:24,IronIngot:4,Wool:4},[]],[{Planks:12,IronIngot:6,Wool:8},[]]],
 AlchemyBuilding:[[{Planks:20,Stone:20,IronIngot:4},['Glass']],[{Planks:24,GoldIngot:4,Glass:10},[]],[{Planks:12,IronIngot:6,Glass:20},[]]],
 StorageBuilding:[[{Planks:16,Stone:16},[]],[{Planks:32,IronIngot:6,Wool:4},[]],[{Planks:16,IronIngot:8,Wool:8},[]]]
};
for(const [id,levels] of Object.entries(buildings)) edit(paths[id],s=> {
 const models=[...s.matchAll(/^    _modelPrefab: (.+)$/gm)].map(m=>m[1]);
 const previews=[...s.matchAll(/^    _preview: (.+)$/gm)].map(m=>m[1]);
 const blocks=levels.map(([cost,unlocks],i)=>`  - _level: ${i+1}\n    _requiredResources:\n${costs(cost,'    ')}\n    _unlocks:${unlocks.length?'\n'+unlocks.map(u=>'    - '+ref(u)).join('\n'):' []'}\n    _modelPrefab: ${models[Math.min(i,models.length-1)]}\n    _preview: ${previews[Math.min(i,previews.length-1)]}\n    _craftOutputBonus: ${['FarmBuilding','AlchemyBuilding'].includes(id)?i:0}\n    _expeditionRewardBonusPercent: ${id==='StorageBuilding'?(i+1)*5:0}`);
 s=s.replace(/  _levels:[\s\S]*?(?=  _currentLevel:)/,'  _levels:\n'+blocks.join('\n')+'\n');
 if(id==='AlchemyBuilding') s=field(s,'_buildingName',JSON.stringify('Алхимическая лаборатория'));
 return s;
});
// All craftable outputs must resolve through the save/inventory database.
edit(paths.ItemDatabase,s=>{
 for(const id of [...Object.keys(recipes),...Object.values(recipes).flatMap(r=>Object.keys(r.cost)),'String','Leather']) if(!s.includes(guid(paths[id]))) s+='  - '+ref(id)+'\n';
 return s;
});
const encounters=[
 ['Level1_Village',[3,4,5],[[0],[0,0,7],[0,7]],{WoodOak:16,Stone:12,CoalOre:6,String:3,Leather:2},0.75],
 ['Level2_Sand',[4,5,7],[[0,7],[0,1],[0,1,8]],{Sand:18,IronOre:15,CoalOre:10,String:4,Leather:3},0.8],
 ['Level3_Mine',[5,7,8],[[0,1],[1,2,8],[2,2,9]],{IronOre:24,GoldOre:15,CoalOre:16,DiamondOre:4},0.85],
 ['Level4_Forest',[6,8,10],[[3,7],[3,4,9],[4,5,6]],{WoodOak:30,String:6,Leather:8,DiamondOre:10,NetheriteOre:6},0.9]
];
for(const [id,counts,types,rewards,threshold] of encounters) edit(paths[id],s=>{
 s=field(s,'rewardResources','\n'+Object.entries(rewards).map(([item,n])=>`  - Item: ${ref(item)}\n    Amount: ${n}`).join('\n'));
 s=field(s,'requiredKillPercentToOpenPortal',threshold);
 if(s.includes('  portalPrefab: {fileID: 0}')) s=s.replace('  portalPrefab: {fileID: 0}',read(paths.Level1_Village).match(/^  portalPrefab: .+$/m)[0]);
 if(['Level1_Village','Level4_Forest'].includes(id)) {
  const section=s.match(/  availableResources:[\s\S]*?(?=  resourceSpawnConfigs:)/)[0];
  let updated=section;
  for(const item of ['String','Leather']) if(!updated.includes(guid(paths[item]))) updated+='  - '+ref(item)+'\n';
  updated=[...new Set(updated.split('\n').filter(line=>!line.includes(guid(paths.CoalItem))))].join('\n');
  if(!updated.endsWith('\n')) updated+='\n';
  s=s.replace(section,updated);
 }
 s=field(s,'encounterWaves','\n'+counts.map((n,i)=>`  - MobCount: ${n}\n    DelayBetweenMobs: ${2-i*0.3}\n    MobTypes: `+types[i].map(t=>{const b=Buffer.alloc(4);b.writeInt32LE(t);return b.toString('hex');}).join('')).join('\n'));
 return s;
});
const mobs={Zombie:[70,8,2.2,2.6],Skeleton:[65,9,2.5,2.6],Spider:[50,6,1.5,3.5],Creeper:[60,28,3,2.8],Pillager:[110,12,2,2.8],ArcherPillager:[85,11,2.4,2.8],Wolf:[80,9,1.6,3.8],Bear:[210,20,2.8,2.5],Witch:[100,13,2.8,2.6],Enderman:[160,16,2,3.6]};
for(const [id,[hp,damage,delay,speed]] of Object.entries(mobs)) edit('Assets/Scripts/AI/MobPresets/'+id+'.asset',s=>{
 for(const [key,value] of Object.entries({MaxHealth:hp,Damage:damage,AttackDelay:delay,Speed:speed})) s=field(s,key,value);
 return s;
});
for(const [id,damage] of Object.entries({WoodSword:22,StoneSword:30,IronSword:42,GoldenSword:50,DiamondSword:65,NetheritheSword:82,BowAttack:32})) edit(paths[id],s=>field(s,'BaseDamage',damage));
write('Documentation/balance-data.json',JSON.stringify({recipes,buildings,encounters,mobs},null,2)+'\n');
require('./wire-harvesting.cjs');
console.log('Balanced '+Object.keys(recipes).length+' recipes, 5 buildings, 4 encounters and 10 enemies.');
