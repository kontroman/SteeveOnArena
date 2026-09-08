const fs=require('fs');
const read=p=>fs.readFileSync(p,'utf8').replace(/\r\n/g,'\n');
const write=(p,s)=>fs.writeFileSync(p,s);
const guid=p=>/guid: (\w+)/.exec(read(p+'.meta'))[1];
const ref=id=>'{fileID: 11400000, guid: '+guid('Assets/ScriptableObjects/Configs/Drops/'+id+'.asset')+', type: 2}';
const drops={WoodOak:['WoodOak',4,7],WoodBirch:['WoodBirch',4,7],WoodDark:['WoodDark',5,8],Wood:['WoodOak',4,7],Stone:['Stone',4,7],Sand:['Sand',4,7],CoalOreBlock:['CoalOre',3,5],IronOreBlock:['IronOre',3,5],GoldOreBlock:['GoldOre',2,4],DiamondOreBlock:['DiamondOre',1,3],NetheriteOre:['NetheriteOre',1,2]};
for(const [name,[item,min,max]] of Object.entries(drops)) {
 const p='Assets/Prefabs/Objects/Ores/'+name+'.prefab';
 let s=read(p);
 const entries=[[item,100,min,max]];
 const block='  _dropTable:\n'+entries.map(([id,chance,lo,hi])=>'  - Item: '+ref(id)+'\n    DropChance: '+chance+'\n    MinQuantity: '+lo+'\n    MaxQuantity: '+hi).join('\n')+'\n';
 s=s.replace(/  _dropTable:[\s\S]*?(?=  _isOneDrop:|---|$)/,block).replace(/  _isOneDrop: \d/,'  _isOneDrop: 0');
 write(p,s);
}
let db='Assets/ScriptableObjects/Configs/Game/BuildingsDatabase.asset',s=read(db);
for(const id of ['StorageBuilding','AlchemyBuilding']) {
 const g=guid('Assets/ScriptableObjects/Configs/Buildings/'+id+'.asset');
 if(!s.includes(g)) s+='  - {fileID: 11400000, guid: '+g+', type: 2}\n';
}
write(db,s);
for(const [name,loops] of [['WoodenPickaxe',4],['IronPickaxe',3],['DiamondPickaxe',2],['NetherithePickaxe',1]]) {
 const p='Assets/ScriptableObjects/Configs/Equipment/Pickaxe/'+name+'.asset';
 write(p,read(p).replace(/  _miningDuration: .*/,'  _miningDuration: 1').replace(/  _miningLoops: .*/,'  _miningLoops: '+loops));
}

for(const [name,item,min,max] of [['Spider','String',1,2],['Wolf','Leather',1,2],['Bear','Leather',2,3]]) {
 const p='Assets/Prefabs/Mobs/'+name+'.prefab';
 let s=read(p);
 s=s.replace(/  _dropTable:[\s\S]*?(?=  _isOneDrop:|---|$)/,'  _dropTable:\n  - Item: '+ref(item)+'\n    DropChance: 100\n    MinQuantity: '+min+'\n    MaxQuantity: '+max+'\n');
 s=s.replace(/  _isOneDrop: \d/,'  _isOneDrop: 0\n  _dropOnDeath: 1');
 s=s.replace(/(  _dropOnDeath: 1\n){2,}/g,'  _dropOnDeath: 1\n');
 write(p,s);
}
